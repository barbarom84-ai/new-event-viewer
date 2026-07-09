using EventViewer.Core;

namespace EventViewer.Tests;

public class SnapshotManagerTests
{
    [Fact]
    public void CreateEventKey_UsesExpectedDelimiter()
    {
        var key = SnapshotManager.CreateEventKey(1000, "Disk", "bad block on volume");
        Assert.Equal("1000|Disk|bad block on volume", key);
    }

    [Fact]
    public void CreateEventKey_DifferentEvents_Differ()
    {
        var older = SnapshotManager.CreateEventKey(41, "Kernel-Power", "Unexpected shutdown");
        var newer = SnapshotManager.CreateEventKey(7000, "Service Control Manager", "service failed");
        Assert.NotEqual(older, newer);
        Assert.Contains("|", older);
    }
}
