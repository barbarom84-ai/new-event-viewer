using EventViewer.Core;

namespace EventViewer.Tests;

public class ComComponentResolverTests
{
    [Fact]
    public void ExtractGuids_FindsClsidAndAppId()
    {
        var text =
            "CLSID {00000000-0000-0000-0000-000000000000} and APPID " +
            "{2ED83BAA-B2FD-43B1-99BF-E6149C622692} to user NETWORK SERVICE";

        var guids = ComComponentResolver.ExtractGuids(text);
        Assert.Contains("{2ED83BAA-B2FD-43B1-99BF-E6149C622692}", guids);
        Assert.Contains("{00000000-0000-0000-0000-000000000000}", guids);
    }

    [Fact]
    public void ResolveFromMessage_SkipsNullGuid_AndResolvesWaaSMedic()
    {
        var text =
            "The application-specific permission settings do not grant Local Activation permission " +
            "for the COM Server application with CLSID " +
            "{00000000-0000-0000-0000-000000000000} " +
            "and APPID {2ED83BAA-B2FD-43B1-99BF-E6149C622692} " +
            "to the user NT AUTHORITY\\NETWORK SERVICE";

        var info = ComComponentResolver.ResolveFromMessage(text, "DCOM");
        Assert.NotNull(info);
        Assert.Equal("{2ED83BAA-B2FD-43B1-99BF-E6149C622692}", info!.Guid);
        Assert.Equal("WaaSMedicSvc", info.RegistryName);
        Assert.Equal("Windows Update Medic", info.FriendlyName);
        Assert.Contains("Windows Update Medic", info.DisplayBoth);
        Assert.Contains("{2ED83BAA-B2FD-43B1-99BF-E6149C622692}", info.DisplayBoth);
    }

    [Fact]
    public void DisplayBoth_ShowsNameAndGuid()
    {
        var info = new ComComponentInfo
        {
            Guid = "{2ED83BAA-B2FD-43B1-99BF-E6149C622692}",
            RegistryName = "WaaSMedicSvc",
            FriendlyName = "Windows Update Medic"
        };

        Assert.Equal(
            "Windows Update Medic (WaaSMedicSvc) · {2ED83BAA-B2FD-43B1-99BF-E6149C622692}",
            info.DisplayBoth);
    }
}
