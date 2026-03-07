# Publication Microsoft Store (MSIX) — Checklist

Ce dépôt contient deux modes de build :

- **USB (portable)** : fonctionnalités complètes (maintenance/admin), écriture dans le dossier de l'exe.
- **Store (MSIX)** : maintenance/admin désactivée et stockage Store-friendly.

## 1) Prérequis (machine de build)

- Installer **Visual Studio 2022** avec :
  - workload **Universal Windows Platform development**
  - workload **.NET desktop development**
  - composants **Windows Application Packaging Project** / **MSIX tooling**
- Installer le **Windows SDK** (>= 10.0.19041).

Note : le projet de packaging (`.wapproj`) se build généralement avec le **MSBuild de Visual Studio** (pas `dotnet msbuild`).

## 2) Build “Store” (flag `STORE_BUILD`)

Le flag Store est injecté via la propriété MSBuild `StoreBuild=true` (voir `EventViewer.csproj`).

- Build app Store (sans packaging) :

```powershell
dotnet publish -c Release -r win-x64 -p:StoreBuild=true --self-contained true -p:PublishSingleFile=true
```

Comportements Store :
- Snapshots : `%LocalAppData%\\EventBeaconTool\\State\\.event_snapshot.json`
- Exports : `%UserProfile%\\Documents\\EventBeaconTool\\Exports`
- Menu maintenance / actions admin : masqués ou refusés

## 3) Packaging MSIX (WAP)

Le projet de packaging est : `EventViewer.Package/EventViewer.Package.wapproj`

### Étapes dans Visual Studio

1. Ouvrir la solution `EventViewer.sln` (ou ouvrir le dossier et charger le `.wapproj`).
2. Ouvrir `EventViewer.Package/Package.appxmanifest`.
3. Mettre à jour l’**Identity** (Name/Publisher) pour matcher votre Partner Center.
4. Build du packaging project (Release / x64) avec génération de package.

## 4) Validation (WACK)

Avant soumission, exécuter le **Windows App Certification Kit (WACK)** sur le MSIX généré.

Objectif :
- aucun accès interdit (écriture dans Program Files)
- aucun comportement non autorisé

## 5) Publication via Partner Center

1. Créer l’app dans le **Partner Center**.
2. Réserver le nom.
3. Associer l’identité package (Publisher/Identity).
4. Remplir la page store (description, captures, catégories, politique de confidentialité).
5. Uploader le package MSIX.
6. Soumettre et corriger selon les retours.

## Notes importantes

- `autorun.inf` est utile pour la **clé USB**, mais ne concerne pas la version Store.
- Les fonctions “Maintenance” nécessitant admin doivent rester hors Store (ou dans une variante USB/admin).


