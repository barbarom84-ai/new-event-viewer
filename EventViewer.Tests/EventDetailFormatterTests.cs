using EventViewer.Core;

namespace EventViewer.Tests;

public class EventDetailFormatterTests
{
    private const string SampleDcomMessage =
        "The description for Event ID '10016' in Source 'DCOM' cannot be found.  " +
        "The local computer may not have the necessary registry information or message DLL files to display the message, " +
        "or you may not have permission to access them.  The following information is part of the event:" +
        "'propres à l’application', 'Local', 'Activation', " +
        "'{2593F8B9-4EAF-457C-B68A-50F6B8EA6B54}', '{15C20B67-12E7-4BB6-92BB-7AFF07997402}', " +
        "'Z790', 'marco', 'S-1-5-21-152142461-1528529460-2509690188-1001', " +
        "'LocalHost (avec LRPC)', 'Non disponible', 'Non disponible'";

    [Fact]
    public void FormatTechnicalDetail_Dcom10016_ReplacesMissingDllText()
    {
        var detail = EventDetailFormatter.FormatTechnicalDetail(10016, "DCOM", SampleDcomMessage);

        Assert.DoesNotContain("cannot be found", detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Détail technique (DCOM 10016)", detail);
        Assert.Contains("PerAppRuntimeBroker", detail);
        Assert.Contains("{2593F8B9-4EAF-457C-B68A-50F6B8EA6B54}", detail);
        Assert.Contains("{15C20B67-12E7-4BB6-92BB-7AFF07997402}", detail);
        Assert.Contains("marco", detail);
        Assert.Contains("Activation", detail);
    }

    [Fact]
    public void ExtractInsertions_ParsesQuotedPayload()
    {
        var insertions = EventDetailFormatter.ExtractInsertions(SampleDcomMessage);
        Assert.True(insertions.Count >= 8);
        Assert.Contains(insertions, s => s.Contains("2593F8B9", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(insertions, s => s.Equals("marco", StringComparison.OrdinalIgnoreCase));
    }
}
