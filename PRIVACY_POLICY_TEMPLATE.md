# Politique de Confidentialité — Observateur d'événements

Dernière mise à jour : 2026-07-08

> Publiez ce texte sur une page web publique, puis collez l’URL dans Partner Center.

## Qui nous sommes

Éditeur : EventBeacon  
Application : Observateur d'événements (Windows)  
Contact : `À RENSEIGNER` (email support)

## Données utilisées pour le fonctionnement

Pour afficher les incidents, l'application lit les **journaux d'événements Windows** sur votre appareil (Système, Applications, Sécurité). Ces données restent sur votre PC et ne sont pas envoyées à nos serveurs par défaut.

## Stockage local

Selon le mode d'installation :

- État (snapshots, réglages, feedback, télémétrie locale) :  
  `%LocalAppData%\EventBeaconTool\State`
- Exports demandés par vous (CSV / JSON) :  
  `%UserProfile%\Documents\EventBeaconTool\Exports`

## Télémétrie (opt-in)

Désactivée par défaut. Si vous l'activez dans Options, l'application peut enregistrer **localement** des événements techniques, par exemple :

- démarrage / chargement des journaux
- exports
- succès ou échec d'actions
- exceptions applicatives

Format : fichier local `telemetry.log.jsonl`.  
Aucun envoi automatique vers un serveur EventBeacon n'est effectué par cette télémétrie locale.

Vous pouvez désactiver la télémétrie à tout moment dans Options.

## Feedback « Utile / Pas utile »

Si vous donnez un avis sur une recommandation, il est stocké **localement** (fichier JSONL) pour améliorer le produit. Il contient typiquement l'ID d'événement, la source et un booléen utile/non utile.

## Fonction IA optionnelle

- **Mode local** : explications issues du catalogue intégré ; aucune donnée n'est envoyée sur Internet.
- **Mode IA bêta + cloud** : uniquement si vous activez l'IA bêta **et** configurez un endpoint compatible ainsi qu'une clé API (`EVENTVIEWER_AI_API_KEY` ou réglage). Dans ce cas, un résumé d'incident (ID, source, niveau, extrait de message) peut être envoyé au fournisseur d'API **que vous avez choisi**.

Nous ne contrôlons pas la politique du fournisseur d'API tiers. Sans configuration cloud, rien n'est envoyé.

## Données que nous ne collectons pas (par défaut)

- Compte utilisateur obligatoire
- Publicité ciblée
- Vente de données personnelles
- Accès à vos documents hors exports que vous déclenchez

## Version Microsoft Store

Les outils de maintenance système avancés (ex. SFC, reset réseau) sont masqués. L'application reste limitée à la lecture des journaux, aux explications et aux exports.

## Vos choix

- Activer / désactiver la télémétrie
- Activer / désactiver l'IA bêta
- Ne pas configurer de clé cloud
- Supprimer les fichiers locaux du dossier State si vous souhaitez effacer snapshots / feedback / logs locaux

## Contact

Pour toute question confidentialité ou support : `À RENSEIGNER`
