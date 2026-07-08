# Observateur d'événements — lecture simple

Alternative claire à l'Observateur d'événements Windows. **Application WinUI 3** pensée pour les utilisateurs novices : langage français, actions visibles, détails techniques repliés.

> La version WPF historique reste dans le dépôt en **legacy gelé**. Le produit actif est `EventViewer.WinUI`.

## Fonctionnalités (v1 + phase 2 WinUI)

- État de santé en tête (« Tout va bien » / « Attention » / « Action recommandée »)
- Liste d'incidents en langage clair (pas de jargon au premier plan)
- Panneau **Comprendre** toujours visible : Que faire → En bref → Ce que ça signifie
- Timeline compacte des 24 dernières heures
- Recherche simple
- Journaux : Système / Applications / Sécurité
- Export CSV et rapport JSON
- **Snapshots** : enregistrer un point, voir les nouveautés
- **Maintenance** (hors build Store) : Vider DNS, SFC, Reset réseau
- **Télémétrie locale opt-in**, feedback utile / pas utile, et **IA bêta**
- **IA cloud optionnelle** (endpoint OpenAI-compatible + `EVENTVIEWER_AI_API_KEY`, fallback local)
- Exécution administrateur pour lire les journaux

## Prérequis

- Windows 10/11
- .NET 8 SDK
- Windows App SDK (restauré via NuGet)

## Lancer en développement

```powershell
dotnet build EventViewer.WinUI\EventViewer.WinUI.csproj -c Release -p:Platform=x64
dotnet run --project EventViewer.WinUI\EventViewer.WinUI.csproj -c Release -p:Platform=x64 --no-build
```

Publier USB ou Store-flagged :

```batch
build_winui_usb.bat
build_winui_store.bat
```

- USB : `bin\WinUI_USB\EventViewer.WinUI.exe` (admin recommandé)
- Store-flagged : `bin\WinUI_Store\EventViewer.WinUI.exe` (maintenance masquée)

Packaging MSIX : voir `STORE_PUBLISHING.md`.

Soumission Store : `PARTNER_CENTER_METADATA.md`, `STORE_SCREENSHOTS.md`, `PRIVACY_POLICY_TEMPLATE.md`, `RELEASE_NOTES.md`.

## Structure

```
EventViewer.Core/     # Logique métier (ErrorAnalyzer, EventLog, insights, export)
EventViewer.WinUI/    # App WinUI 3 (MVVM, Fluent dark)
EventViewer.Tests/    # Tests unitaires Core
EventViewer.csproj    # WPF legacy (gelé)
```

## Tests & CI

```powershell
dotnet test EventViewer.Tests\EventViewer.Tests.csproj -c Release
```

CI GitHub Actions : Core + WPF legacy + WinUI (+ flag Store) + tests.

## Phase suivante

Soumission Partner Center réelle (Identity, URL privacy hébergée, captures, MSIX signé).

## License

Projet libre d'utilisation et de modification.
