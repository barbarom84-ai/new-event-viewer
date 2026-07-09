using EventViewer.Core;

namespace EventViewer.Tests;

public class RecommendationFeedbackStoreTests
{
    [Fact]
    public void Add_WritesJsonlLine()
    {
        var path = Path.Combine(AppPaths.StateDirectory, "recommendation-feedback.jsonl");
        var before = File.Exists(path) ? File.ReadAllLines(path).Length : 0;

        var item = new EventItem
        {
            TimeCreated = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            TimeCreatedAt = DateTime.Now,
            Level = "Erreur",
            Message = "disk error",
            FullMessage = "disk error",
            Source = "Disk",
            EventId = 7
        };

        RecommendationFeedbackStore.Add(item, useful: true);

        Assert.True(File.Exists(path));
        var after = File.ReadAllLines(path).Length;
        Assert.True(after >= before + 1);
    }
}
