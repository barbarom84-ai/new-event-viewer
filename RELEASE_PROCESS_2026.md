# Release Process 2026

Ce document formalise le cycle de release mensuel (stable + preview) et la revue KPI.

## Cadence

- Semaine 1: gel fonctionnel + triage crashs.
- Semaine 2: release preview (workflow `Preview Build`).
- Semaine 3: correction regressions + validation support.
- Semaine 4: release stable + notes de version.

## Checklist de non-regression

1. Build USB (`dotnet build EventViewer.sln -c Release`).
2. Build Store flag (`dotnet build EventViewer.csproj -c Release -p:StoreBuild=true`).
3. Tests unitaires (`dotnet test EventViewer.Tests/EventViewer.Tests.csproj -c Release`).
4. Test manuel:
   - chargement `System/Application/Security`
   - export CSV + JSON
   - snapshot + comparaison
   - timeline incidents + insights severite
   - menu maintenance masque en Store
5. Vérifier télémétrie opt-in (activée/désactivée) et feedback recommandations.

## Notes de version

- Utiliser le format:
  - Added
  - Changed
  - Fixed
  - Known Issues

## KPI (revue mensuelle)

- Crash/session
- Temps chargement moyen des événements
- Utilisation export JSON
- Taux d'usage insights/timeline
- Feedback utile/non-utile des recommandations
