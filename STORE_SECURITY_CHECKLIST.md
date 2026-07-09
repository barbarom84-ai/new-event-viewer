# Checklist sécurité — signature USB + Microsoft Store

Produit actif : **EventViewer.WinUI**.  
Objectif : livrer un binaire **signé** (USB) et un package Store **conforme**, sans promettre une app « uncrackable ».

## 1. Prérequis

| Élément | USB public | Store |
|---------|------------|-------|
| Certificat Authenticode OV/EV (ou organisation) | Obligatoire | Géré par Partner Center à la soumission MSIX* |
| Windows SDK / `signtool` | Recommandé | Visual Studio Packaging |
| Identity / Publisher Partner Center | — | Obligatoire dans `Package.appxmanifest` |
| URL privacy publique | Recommandé | Obligatoire |
| Captures + WACK | — | Obligatoire |

\*Le Store re-signe souvent le package ; le Publisher `CN=...` du manifeste doit **correspondre** à l’identité Partner Center.

## 2. Signature USB (Authenticode)

### Certificat de production

1. Obtenir un certificat **Code Signing** (OV/EV) auprès d’une CA (DigiCert, Sectigo, GlobalSign…).
2. Exporter / stocker le PFX hors dépôt Git (coffre, CI secrets).
3. Définir les variables (session ou CI) :

```powershell
$env:EVENTVIEWER_SIGN_PFX = "C:\secure\WinBeacon-codesign.pfx"
$env:EVENTVIEWER_SIGN_PFX_PASSWORD = "<secret>"
# ou :
$env:EVENTVIEWER_SIGN_THUMBPRINT = "ABC123..."
```

4. Publier + signer :

```powershell
.\build_winui_usb.ps1 -Sign
# ou
.\build_winui_usb.ps1 -Sign -SignPfxPath C:\secure\codesign.pfx -SignPfxPassword "<secret>"
```

5. Vérifier :

```powershell
Get-AuthenticodeSignature bin\WinUI_USB\EventViewer.WinUI.exe | Format-List *
```

Attendu : `Status = Valid` avec un éditeur de confiance (pas « Unknown publisher »).

### Certificat de développement (test local uniquement)

```powershell
.\tools\New-DevCodeSigningCert.ps1
$env:EVENTVIEWER_SIGN_THUMBPRINT = "<thumbprint affiché>"
.\build_winui_usb.ps1 -Sign -AllowSelfSigned
```

Ne **jamais** distribuer un build signé avec ce certificat de test.

## 3. Packaging Store (MSIX)

1. Remplacer dans `EventViewer.WinUI/Package.appxmanifest` :
   - `Identity Name`
   - `Publisher` (`CN=...` exact Partner Center)
   - `Version`
2. Visual Studio → **EventViewer.WinUI** → Package and Publish → Create App Packages → **Microsoft Store**.
3. Upload Partner Center + soumission.
4. Lancer **Windows App Certification Kit (WACK)** sur le package local avant / après upload.

Build de smoke non packagé (comportements Store, sans MSIX) :

```powershell
.\build_winui_store.ps1
```

## 4. Checklist sécurité produit (avant release)

### Secrets & réseau
- [ ] Aucune clé API en clair dans le dépôt / artefacts
- [ ] Cloud IA : HTTPS public uniquement + consentement utilisateur
- [ ] Préférer `EVENTVIEWER_AI_API_KEY` à un stockage local

### Surfaces admin
- [ ] Version Store : panneau Maintenance **absent**
- [ ] USB : DNS / SFC / Reset réseau → **dialogue de confirmation**
- [ ] Auto-fix → confirmation ; DCOM 10016 → pas de fix réseau

### Données locales
- [ ] État sous `%LocalAppData%\WinBeacon\State`
- [ ] Exports sous Documents (ou fallback AppData)
- [ ] Télémétrie **opt-in**, off par défaut

### Build
- [ ] `dotnet test EventViewer.Tests` vert
- [ ] USB build signé (prod) ou clairement marqué « unsigned / test »
- [ ] Manifeste Store Identity non-placeholder
- [ ] `artifacts\*.pfx` / secrets absents du commit

### Store listing
- [ ] Privacy URL hébergée (`PRIVACY_POLICY_TEMPLATE.md` publié)
- [ ] Captures (`STORE_SCREENSHOTS.md`)
- [ ] Métadonnées (`PARTNER_CENTER_METADATA.md`)
- [ ] Capability `runFullTrust` assumée / justifiée
- [ ] Notes de version à jour (`RELEASE_NOTES.md`)

## 5. Ce que la signature ne fait pas

- Elle n’empêche pas le reverse-engineering de l’exe.
- Elle n’empêche pas un admin local de modifier son PC.
- Elle **réduit** SmartScreen / « éditeur inconnu » et prouve l’intégrité du fichier distribué.

## Scripts

| Script | Rôle |
|--------|------|
| `tools/Sign-Authenticode.ps1` | Signer exe/dll/msix |
| `tools/New-DevCodeSigningCert.ps1` | Cert self-signed local |
| `build_winui_usb.ps1 -Sign` | Publish USB + signer |
| `build_winui_store.ps1` | Publish smoke Store-flagged |
