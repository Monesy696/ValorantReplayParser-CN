using Replay.Encoding.Archives;
using Replay.Encoding.PayloadEncryption;

namespace Replay.Encoding.Tests.PayloadEncryption;

public class ValorantSeededTransformChina13_10Tests
{
    private const string Branch = "++Ares-Core+release-china-13.10";
    private const string PayloadHex = "BFDF6F9EA1F27BA00000C66EAFAF2E0000339C0DD34B0C45C48063038003562A43C0C949";

    // Synthetic regression vectors calculated from the recovered equations.
    // Inputs contain no private replay data. Native-client oracle vectors are unavailable.
    [TestCase(0, 2u, "")]
    [TestCase(1, 3u, "01")]
    [TestCase(7, 5u, "5D")]
    [TestCase(8, 10u, "F9")]
    [TestCase(9, 11u, "1A01")]
    [TestCase(31, 29u, "301F3417")]
    [TestCase(32, 34u, "6F3EF7BE")]
    [TestCase(33, 35u, "E3F653DB00")]
    [TestCase(63, 61u, "B37D0F6AC61A7C69")]
    [TestCase(64, 66u, "D75BFDC1F6BECE3C")]
    [TestCase(65, 67u, "746FF7A7DBBB11F301")]
    [TestCase(95, 93u, "4B30C0577F3FD0BD8C8C2731")]
    [TestCase(96, 98u, "E18FFECAF83742B6EE1F7E5F")]
    [TestCase(127, 125u, "9412425BBD5F73FEA9D66FB764C65023")]
    [TestCase(128, 130u, "EBA3BF02FE9D9C6DDC3381AFAADC8EA7")]
    [TestCase(135, 133u, "C07EEF0F9B5FB764A86AE785B4B44EA165")]
    [TestCase(287, 285u, "3330C0577F6FD0BD66AC7FFCF2DD07FFADA01C9EDA6E12CD0756FACCF23B3B127EB85A7D")]
    [TestCase(288, 290u, "C98FFECAF86742B62D25E925E1C3FD605383DE8DA1B4EA7A4AC41F96A80683F2B012B31E")]
    [TestCase(287, 0u, "6F05077CFBF7F3DCDCD9F715378ED0204CAA3CD806F987822E81B1BCCE238E3CE53D6A41")]
    [TestCase(287, 2147483648u, "3F2A2BA4DFFBE9E6DCD9F715378ED020287AFE5978AE580BE34EB209F70C227BA8661434")]
    [TestCase(287, 4294967295u, "FF28AFD07E1F649B276DA279921CB2D03535A350479E7466BFFF3CE02E629B1025F4B003")]
    [TestCase(287, 305419896u, "50E5A8DD89D8BF6846EF014D82EB5013885885955DB725A78910B26BF7576E26F0C8BE78")]
    public void Apply_MatchesSyntheticRegressionVector(int bits, uint seed, string expected)
    {
        var transform = PayloadTransformRegistry.CreateDefault().GetRequired(Branch);
        var size = transform.GetOutputByteCount(bits);
        using var explicitInput = new BitArchiveReader(Convert.FromHexString(PayloadHex), bits);
        using var remainingInput = new BitArchiveReader(Convert.FromHexString(PayloadHex), bits);
        var explicitOutput = Enumerable.Repeat((byte)0xa5, size + 3).ToArray();
        var remainingOutput = Enumerable.Repeat((byte)0xa5, size + 3).ToArray();

        transform.Apply(explicitInput, bits, seed, explicitOutput);
        transform.Apply(remainingInput, seed, remainingOutput);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(explicitOutput.AsSpan(0, size)), Is.EqualTo(expected));
            Assert.That(Convert.ToHexString(remainingOutput.AsSpan(0, size)), Is.EqualTo(expected));
            Assert.That(explicitOutput.AsSpan(size).ToArray(), Is.All.EqualTo(0xa5));
            Assert.That(remainingOutput.AsSpan(size).ToArray(), Is.All.EqualTo(0xa5));
            Assert.That(explicitInput.AtEnd, Is.True);
            Assert.That(remainingInput.AtEnd, Is.True);
        });
    }

    [TestCase(-1, 1, ArchiveErrorCode.InvalidBitCount)]
    [TestCase(65, 8, ArchiveErrorCode.BufferTooSmall)]
    [TestCase(65, 9, ArchiveErrorCode.EndOfArchive)]
    public void Apply_InvalidInput_PreservesInputAndOutput(int bits, int size, ArchiveErrorCode code)
    {
        var transform = PayloadTransformRegistry.CreateDefault().GetRequired(Branch);
        using var input = new BitArchiveReader(new byte[8], 64);
        var output = Enumerable.Repeat((byte)0xa5, size).ToArray();
        var error = Assert.Throws<ArchiveReadException>(() => transform.Apply(input, bits, 0u, output));
        Assert.Multiple(() =>
        {
            Assert.That(error!.ErrorCode, Is.EqualTo(code));
            Assert.That(input.BitPosition, Is.Zero);
            Assert.That(output, Is.All.EqualTo(0xa5));
        });
    }

    [TestCase("++Ares-Core+release-13.10")]
    [TestCase("++Ares-Core+release-china-13.11")]
    [TestCase("++ares-core+release-china-13.10")]
    public void Registry_RejectsUnknownBranch(string branch)
    {
        Assert.Throws<UnsupportedPayloadTransformVersionException>(() =>
            PayloadTransformRegistry.CreateDefault().GetRequired(branch));
    }
}
