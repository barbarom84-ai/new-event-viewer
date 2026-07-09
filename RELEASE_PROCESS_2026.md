# Release Process 2026

Cycle de release mensuel (stable + preview) pour **EventViewer.WinUI**.

## Cadence

- Semaine 1: gel fonctionnel + triage crashs
- Semaine 2: release preview
- Semaine 3: correction regressions + validation support
- Semaine 4: release stable + `RELEASE_NOTES.md`

## Checklist de non-régression

1. `dotnet build EventViewer.Core/EventViewer.Core.csproj -c Release`
2. `dotnet build EventViewer.WinUI/EventViewer.WinUI.csproj -c Release -p:Platform=x64`
3. `dotnet build EventViewer.WinUI/EventViewer.WinUI.csproj -c Release -p:Platform=x64 -p:StoreBuild=true`
4. `dotnet test EventViewer.Tests/EventViewer.Tests.csproj -c Release`
5. Publish smoke :
   - `build_winui_usb.bat`
   - `build_winui_store.bat`
6. Signature USB (release publique) :
   - `.\build_winui_usb.ps1 -Sign` avec PFX / thumbprint de production
   - vérifier `Get-AuthenticodeSignature` → `Valid`
   - suivre `STORE_SECURITY_CHECKLIST.md`
7. Tests manuels :
   - chargement Système / Applications / Sécurité
   - export CSV + JSON
   - snapshot + nouveautés
   - timeline + bandeau santé
   - feedback Utile / Pas utile
   - télémétrie ON/OFF
   - IA locale ; cloud uniquement si clé de test + consentement + HTTPS
   - maintenance visible en USB (avec confirmations), masquée en Store
   - auto-fix DCOM ≠ correction DNS
8. WPF legacy (optionnel) : build `EventViewer.csproj` tant qu’il reste dans la solution
9. Avant Store : Identity Partner Center, captures (`STORE_SCREENSHOTS.md`), URL privacy, WACK, checklist sécurité

## Notes de version

Mettre à jour `RELEASE_NOTES.md` (Added / Changed / Fixed / Known Issues).

## KPI (revue mensuelle)

- Crash / session
- Temps de chargement moyen
- Usage export JSON
- Usage insights / timeline / snapshot
- Ratio feedback utile / non utile
- Ratio résumés cloud vs local (si IA activée)
