# Publication Microsoft Store & distribution USB

Produit actif : **EventViewer.WinUI**.  
Checklist signature / sécurité détaillée : **`STORE_SECURITY_CHECKLIST.md`**.

## Modes de distribution

| Mode | Script | Sortie | Notes |
|------|--------|--------|-------|
| USB portable | `build_winui_usb.bat` / `.ps1` | `bin\WinUI_USB\` | Maintenance dispo si admin ; **signer** avec Authenticode |
| Smoke Store-flagged | `build_winui_store.bat` / `.ps1` | `bin\WinUI_Store\` | `STORE_BUILD` : pas de maintenance ; pas un MSIX |
| MSIX Store | Visual Studio Package and Publish | App Packages | Identity Partner Center + signature Store |

## Signature USB

```powershell
# Production (PFX réel)
$env:EVENTVIEWER_SIGN_PFX = "C:\secure\codesign.pfx"
$env:EVENTVIEWER_SIGN_PFX_PASSWORD = "<secret>"
.\build_winui_usb.ps1 -Sign

# Dev local uniquement
.\tools\New-DevCodeSigningCert.ps1
.\build_winui_usb.ps1 -Sign -AllowSelfSigned -SignThumbprint <thumbprint>
```

Sans `-Sign` / variables d’environnement → build **non signé** (OK pour debug, pas pour distribution).

## Package MSIX (recommandé)

1. Ouvrir `EventViewer.sln`.
2. Sélectionner `EventViewer.WinUI`.
3. **Package and Publish** → Create App Packages → **Microsoft Store**.
4. Mettre à jour Identity / Publisher dans `EventViewer.WinUI/Package.appxmanifest` (voir `PARTNER_CENTER_METADATA.md`).
5. Soumettre via Partner Center.
6. Valider avec le **Windows App Certification Kit (WACK)**.

## Legacy WPF packaging

Le projet `EventViewer.Package` (`.wapproj`) reste disponible pour l’ancienne app WPF :

```powershell
.\build_store_msix.ps1
```

## Comportements Store (`StoreBuild=true`)

- Snapshots / settings / feedback / télémétrie : `%LocalAppData%\WinBeacon\State\`
- Exports : `%UserProfile%\Documents\WinBeacon\Exports`
- Menu / actions maintenance : masqués
- Auto-fix agressif (DNS/netsh/SFC) : fallback réglages Windows

## IA cloud (optionnel)

1. Activer **IA bêta** dans Options.
2. Accepter le **consentement d’envoi cloud**.
3. Endpoint **HTTPS** public uniquement.
4. Clé via `EVENTVIEWER_AI_API_KEY` (recommandé) ou blob DPAPI.
5. Sinon → fallback local automatique.

## Partner Center

1. Réserver le nom.
2. Associer Identity / Publisher.
3. Remplir métadonnées (`PARTNER_CENTER_METADATA.md`) + privacy URL hébergée.
4. Captures (`STORE_SCREENSHOTS.md`).
5. Uploader le MSIX, WACK, soumettre.
6. Suivre `STORE_SECURITY_CHECKLIST.md` avant chaque release.
