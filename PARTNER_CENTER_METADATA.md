# Partner Center — éléments à préparer

Ce fichier sert de checklist pour compléter rapidement votre fiche Microsoft Store.

## Identité du package (obligatoire)

Dans **Partner Center** :
- Réserver le nom de l’application
- Récupérer :
  - **Package/Identity Name**
  - **Publisher** (CN=...)

Puis mettre à jour :
- `EventViewer.Package/Package.appxmanifest`
  - `<Identity Name="..." Publisher="..." Version="..."/>`

## Listing Store

- Nom: **EventBeacon Tool**
- Description courte (1 ligne)
- Description longue (fonctionnalités principales)
- Catégorie: Utilitaires / Système
- Captures d’écran (au moins 1)
- Icônes/tiles (assets aux bonnes tailles)

## Confidentialité

- URL de politique de confidentialité (même si simple)
- Déclarer la collecte de données si applicable

## Support

- Email de support
- URL support (optionnel)

## Notes conformité

- Version Store : pas d’outils “admin” (SFC/CHKDSK/netsh) → masqués/désactivés
- Stockage: `%LocalAppData%` (snapshots) et `Documents` (exports)


