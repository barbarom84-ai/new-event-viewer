# Publication Microsoft Store — Checklist (WPF legacy + WinUI)

Ce dépôt contient deux apps et deux modes de distribution :

- **USB (portable)** : fonctionnalités complètes (maintenance/admin).
- **Store** : maintenance masquée + chemins AppData/Documents (`STORE_BUILD`).

## Produit actif : WinUI 3

- Projet : `EventViewer.WinUI`
- Build USB : `build_winui_usb.bat` → `bin\WinUI_USB\`
- Build Store-flagged : `build_winui_store.bat` → `bin\WinUI_Store\`

### Package MSIX (recommandé via Visual Studio)

1. Ouvrir `EventViewer.sln`.
2. Sélectionner `EventViewer.WinUI`.
3. **Package and Publish** → Create App Packages.
4. Mettre à jour Identity / Publisher dans `EventViewer.WinUI/Package.appxmanifest`.
5. Signer et soumettre via Partner Center.
6. Valider avec le **Windows App Certification Kit (WACK)**.

## Legacy WPF packaging

Le projet `EventViewer.Package` (`.wapproj`) reste disponible pour l’ancienne app WPF :

```powershell
.\build_store_msix.ps1
```

## Comportements Store (`StoreBuild=true`)

- Snapshots / settings / feedback / télémétrie : `%LocalAppData%\EventBeaconTool\State\`
- Exports : `%UserProfile%\Documents\EventBeaconTool\Exports`
- Menu / actions maintenance : masqués

## IA cloud (optionnel)

1. Activer **IA bêta** dans Options.
2. Renseigner l’endpoint OpenAI-compatible.
3. Définir la clé via `EVENTVIEWER_AI_API_KEY` (recommandé) ou `AiApiKey` dans les settings.
4. Sans clé/endpoint valides → fallback local automatique.

## Partner Center

1. Réserver le nom.
2. Associer Identity / Publisher.
3. Remplir métadonnées (voir `PARTNER_CENTER_METADATA.md` et politique de confidentialité).
4. Uploader le MSIX, soumettre, corriger selon les retours.
