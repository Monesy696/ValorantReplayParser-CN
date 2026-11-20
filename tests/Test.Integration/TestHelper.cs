namespace Test.Integration;

public static class TestHelper
{
    public static byte[] ReadReplayBytes(string replayFileName)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Replays", replayFileName);
        if (!File.Exists(path))
        {
            Assert.Ignore("Replay fixture is not included. Supply an authorized local fixture in the test Replays directory.");
        }
        return File.ReadAllBytes(path);
    }
}
