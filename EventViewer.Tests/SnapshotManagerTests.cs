using EventViewer;

namespace EventViewer.Tests;

public class SnapshotManagerTests
{
    [Fact]
    public void CreateEventKey_UsesExpectedPrefixAndDelimiter()
    {
        var key = SnapshotManager.CreateEventKey(1001, "ApplicationError", new string('x', 120));

        Assert.StartsWith("1001|ApplicationError|", key);
        Assert.True(key.Length < 90);
    }
}
