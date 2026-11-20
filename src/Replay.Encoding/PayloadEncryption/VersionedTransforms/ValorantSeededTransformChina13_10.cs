// Format recovered from the three selected China 13.10 Swiftplay files.
// Do not use those version constants for another branch.
using System.Buffers.Binary;
using System.Numerics;
using Replay.Encoding.Archives;
using static Replay.Encoding.PayloadEncryption.ValorantSeededTransformHelpers;

namespace Replay.Encoding.PayloadEncryption.VersionedTransforms;

internal sealed class ValorantSeededTransformChina13_10 : IPayloadTransform
{
    private const uint SeedAddend = 0x6ed99667;
    private const uint SeedOffset = 103;
    private const byte TailXor = 103;
    private const ulong Multiplier = 0x2545f4914f6cdd1d;
    public IReadOnlyCollection<string> SupportedReplayVersions { get; } = ["++Ares-Core+release-china-13.10"];
    public int GetOutputByteCount(int bits) => ValorantSeededTransformHelpers.GetOutputByteCount(bits);

    public void Apply(FBitArchive input, uint seed, Span<byte> output) =>
        ApplyRemaining(input, seed, output);

    public void Apply(FBitArchive input, int bitCount, uint seed, Span<byte> output)
    {
        CopyInputToOutput(input, bitCount, output);
        Decode(output[..GetOutputByteCount(bitCount)], bitCount, seed);
    }

    private void ApplyRemaining(FBitArchive input, uint seed, Span<byte> output)
    {
        var bitCount = CopyInputToOutput(input, output);
        Decode(output[..GetOutputByteCount(bitCount)], bitCount, seed);
    }

    private static void Decode(Span<byte> output, int bitCount, uint seed)
    {
        var a = Mix(seed + SeedAddend, seed + SeedOffset);
        var b = Mix(seed, seed);
        var state = seed;
        while (bitCount >= 64)
        {
            var word = Decode64(BinaryPrimitives.ReadUInt64LittleEndian(output), state);
            BinaryPrimitives.WriteUInt64LittleEndian(output, word);
            state = Advance(ref a, ref b);
            output = output[8..];
            bitCount -= 64;
        }
        if (bitCount >= 32)
        {
            var word = Decode32(BinaryPrimitives.ReadUInt32LittleEndian(output), state);
            BinaryPrimitives.WriteUInt32LittleEndian(output, word);
            state = Advance(ref a, ref b);
            output = output[4..];
            bitCount -= 32;
        }
        while (bitCount >= 8)
        {
            output[0] = Decode8(output[0], state);
            state = Advance(ref a, ref b);
            output = output[1..];
            bitCount -= 8;
        }
        if (bitCount != 0)
            output[0] ^= (byte)(((1 << bitCount) - 1) & (state ^ TailXor));
    }

    internal static ulong Mix(uint seed, uint offsetSeed) =>
        ((((seed >> 15) ^ seed) >> 12) ^ (offsetSeed << 25) ^ seed) * Multiplier;

    internal static uint Advance(ref ulong a, ref ulong b)
    {
        var state = (uint)((a + b) >> 32);
        b ^= a;
        a = BitOperations.RotateRight(a, 9) ^ (b << 14) ^ b;
        b = BitOperations.RotateLeft(b, 36);
        return state;
    }

    private static ulong Decode64(ulong word, uint state)
    {
        word = BitOperations.RotateRight(word, (int)(BitOperations.RotateRight(state, 7) % 63) + 1);
        word = Swap(word, 0x5555_5555_5555_5555) - BitOperations.RotateRight(state, 5);
        word = Swap(word, 0x5555_5555_5555_5555);
        word = SwapWide(word, 0x3333_3333_3333_3333, 2);
        word = SwapWide(word, 0x0f0f_0f0f_0f0f_0f0f, 4);
        word = SwapWide(word, 0x00ff_00ff_00ff_00ff, 8);
        return BitOperations.RotateLeft(word, 32) - BitOperations.RotateRight(state, 3);
    }

    private static uint Decode32(uint word, uint state)
    {
        word = BitOperations.RotateRight(word, (int)(BitOperations.RotateLeft(state, 7) % 31) + 1);
        word = (uint)Swap(word, 0x5555_5555) - BitOperations.RotateLeft(state, 5);
        word = (uint)Swap(word, 0x5555_5555);
        word = (uint)SwapWide(word, 0x3333_3333, 2);
        word = (uint)SwapWide(word, 0x0f0f_0f0f, 4);
        word = (uint)SwapWide(word, 0x00ff_00ff, 8);
        return BitOperations.RotateLeft(word, 16) - BitOperations.RotateLeft(state, 3);
    }

    internal static byte Decode8(byte word, uint state)
    {
        var shift = (int)(state * 19_487_171u % 7) + 1;
        word = (byte)((word >> shift) | (word << (8 - shift)));
        word = (byte)(Swap(word, 0x55) - state * 161_051u);
        word = (byte)Swap(word, 0x55);
        word = (byte)SwapWide(word, 0x33, 2);
        word = (byte)SwapWide(word, 0x0f, 4);
        return (byte)(word - state * 1331u);
    }

    private static ulong Swap(ulong value, ulong mask) => SwapWide(value, mask, 1);
    private static ulong SwapWide(ulong value, ulong mask, int width) =>
        ((value & mask) << width) | ((value >> width) & mask);
}
