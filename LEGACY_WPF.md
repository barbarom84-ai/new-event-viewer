# Legacy WPF

The root `EventViewer.csproj` + `MainWindow.*` + root `*.cs` services are the **frozen WPF** app.

- Active product: `EventViewer.WinUI` + `EventViewer.Core`
- Do not add features to WPF
- Root service files (`ErrorAnalyzer.cs`, `AppPaths.cs`, …) intentionally duplicate older Core behavior for WPF-only compilation
- CI still builds WPF as a compile gate until Store/USB ship fully on WinUI

When retiring WPF: remove the project from `EventViewer.sln`, delete root WPF sources, drop WPF steps from `.github/workflows/*`, and retarget `EventViewer.Package` or delete it.
