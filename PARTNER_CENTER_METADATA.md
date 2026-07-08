# Partner Center — métadonnées prêtes à coller

Produit actif : **EventViewer.WinUI** (Observateur d'événements).  
Remplacez les champs `À RENSEIGNER` avant soumission.

## Identité du package (obligatoire)

Dans Partner Center → Product identity, récupérer puis coller dans  
`EventViewer.WinUI/Package.appxmanifest` :

| Champ | Valeur actuelle (placeholder) | À remplacer par |
|-------|-------------------------------|-----------------|
| Identity Name | `4d80fb5e-815a-440b-a75e-827b09f91c93` | Package/Identity Name Partner Center |
| Publisher | `CN=User Name` | Publisher Partner Center (`CN=...`) |
| Version | `1.1.0.0` | Incrémenter à chaque soumission |
| Publisher display name | `EventBeacon` | Nom éditeur public |

Legacy WPF : `EventViewer.Package/Package.appxmanifest` (même Identity si même listing).

## Listing Store (FR)

### Nom
**Observateur d'événements**

### Description courte (≤ 300 car.)
Comprenez les erreurs Windows en français clair. Santé du PC, explications et actions recommandées, sans jargon.

### Description longue
Observateur d'événements transforme les journaux Windows (Système, Applications, Sécurité) en un diagnostic simple.

En quelques secondes, voyez :
• si votre PC a besoin d'attention
• ce que signifie chaque incident, en langage courant
• quoi faire concrètement

Fonctionnalités
• Bandeau santé (« Tout va bien », « Attention », « Action recommandée »)
• Liste d'incidents lisible, sans jargon au premier plan
• Panneau « Comprendre » : Que faire → En bref → Ce que ça signifie
• Timeline des 24 dernières heures
• Recherche et export CSV / JSON
• Points de comparaison pour voir les nouveautés
• Explications locales ; IA optionnelle si vous configurez une clé (sinon tout reste hors-ligne)

Version Microsoft Store
• Les outils de maintenance système avancés sont masqués (conformité Store)
• Les données d'état restent dans votre profil Windows

Idéal pour le dépannage rapide, le support de proximité et les utilisateurs non techniques.

### Catégorie
Utilitaires → Outils système

### Mots-clés
événements windows, observateur, erreurs, diagnostic, pc, support, event log, maintenance

### Âge / public
Tous publics

## Captures d'écran (checklist)

Voir `STORE_SCREENSHOTS.md`. Minimum Store : **1** ; recommandé : **4**.

## Support & URLs

| Élément | Valeur |
|---------|--------|
| Email support | `À RENSEIGNER` |
| Site / support URL | `À RENSEIGNER` |
| Politique de confidentialité (URL publique) | Publier `PRIVACY_POLICY_TEMPLATE.md` puis coller l’URL ici |
| Notes de version | Voir `RELEASE_NOTES.md` |

## Confidentialité (déclarations Partner Center)

- Télémétrie : **opt-in**, locale, désactivée par défaut
- Feedback recommandations : local
- IA cloud : **optionnelle**, uniquement si l’utilisateur configure endpoint + clé ; sinon catalogue local
- Pas de compte obligatoire
- Version Store : pas d’actions admin (SFC / netsh)

## Notes conformité Store

- Flag `StoreBuild=true` : maintenance masquée
- Chemins : `%LocalAppData%\EventBeaconTool\State` et `Documents\EventBeaconTool\Exports`
- Capability : `runFullTrust` (app WinUI desktop)
- Tester WACK avant soumission
