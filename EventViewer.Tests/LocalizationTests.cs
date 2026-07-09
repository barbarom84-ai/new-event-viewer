using EventViewer.Core;

namespace EventViewer.Tests;

public class LocalizationTests
{
    public LocalizationTests()
    {
        Loc.Initialize(AppLanguage.French);
    }

    [Fact]
    public void Loc_French_ReturnsKnownUiString()
    {
        Loc.Apply(AppLanguage.French);
        Assert.Equal("Actualiser", Loc.T("Action.Refresh"));
        Assert.Equal("Permissions DCOM insuffisantes", Loc.T("Event.10016.Title"));
    }

    [Fact]
    public void Loc_English_ReturnsTranslatedUiString()
    {
        Loc.Apply(AppLanguage.English);
        Assert.Equal("Refresh", Loc.T("Action.Refresh"));
        Assert.Equal("Insufficient DCOM permissions", Loc.T("Event.10016.Title"));
    }

    [Fact]
    public void Loc_Italian_ReturnsTranslatedUiString()
    {
        Loc.Apply(AppLanguage.Italian);
        Assert.Equal("Aggiorna", Loc.T("Action.Refresh"));
        Assert.Equal("Autorizzazioni DCOM insufficienti", Loc.T("Event.10016.Title"));
    }

    [Fact]
    public void AppLanguage_ResolveEffective_FallsBackToFrench()
    {
        Assert.Equal(AppLanguage.French, AppLanguage.ResolveEffective("zz-ZZ"));
        Assert.Equal(AppLanguage.English, AppLanguage.ResolveEffective("en-GB"));
        Assert.Equal(AppLanguage.Italian, AppLanguage.ResolveEffective("it-IT"));
    }
}
