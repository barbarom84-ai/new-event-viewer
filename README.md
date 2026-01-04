# ⚡ Observateur d'Événements Windows Moderne

Une alternative moderne, élégante et portable à l'Observateur d'événements Windows natif. Interface WPF avec thème sombre professionnel.

## 🎯 Fonctionnalités

- ✨ **Interface WPF moderne** avec thème sombre élégant
- 🔍 **Lecture des 50 derniers événements** (Erreurs et Avertissements)
- 🎨 **Indicateurs visuels par priorité**
  - 🔴 Rouge : Erreur
  - 🟡 Jaune : Avertissement
  - 🔵 Bleu : Information
- 💡 **Traduction intelligente des événements** - Plus besoin de Google !
  - Base de données de 40+ événements Windows courants
  - Explications en langage clair et français
  - Solutions recommandées pour chaque problème
  - Double-clic pour voir les détails complets
- 💾 **Application portable** (exécutable unique)
- ⚡ **Optimisé pour une taille minimale** (~15-25 MB)
- 📊 **Support des journaux** System, Application et Security
- 🚀 **Chargement asynchrone** pour une interface fluide
- 🎯 **Virtualisation UI** pour des performances optimales
- 🔮 **Prêt pour l'enrichissement IA** (fonctionnalité future)

## 🚀 Compilation

### Prérequis
- .NET 8.0 SDK ou supérieur
- Windows 10/11

### Méthode Simple

Exécutez simplement le script de compilation :

```batch
build.bat
```

### Méthode Manuelle

```batch
dotnet publish -c Release -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=true ^
    -p:TrimMode=link ^
    -p:EnableCompressionInSingleFile=true
```

L'exécutable sera disponible dans :  
`bin\Release\net8.0-windows\win-x64\publish\EventViewer.exe`

## 💡 Utilisation

1. **Exécuter en tant qu'administrateur** (requis pour accéder aux journaux Windows)
2. Sélectionner le journal à consulter (System, Application, Security)
3. Cliquer sur "🔄 Actualiser" pour charger les derniers événements
4. **Double-cliquer sur un événement** pour voir l'explication détaillée et les solutions recommandées

### 💡 Comprendre les Événements

Chaque événement affiche maintenant :
- **Message original** : Le message technique de Windows
- **💡 Explication claire** : Titre descriptif en langage courant
- **Description** : Ce que signifie réellement cet événement
- **Solution recommandée** : Comment résoudre le problème

Plus besoin de chercher sur Google ! L'application contient une base de connaissances de 40+ événements Windows courants.

## 📦 Mode Portable

L'application est conçue pour être complètement portable :

- ✅ **Aucune installation** nécessaire
- ✅ **Fichier unique** exécutable
- ✅ **Copie sur clé USB** possible
- ✅ **Exécution sur n'importe quel PC** Windows

## 🎨 Interface Utilisateur

- **Thème sombre** professionnel inspiré de Visual Studio Code
- **Icônes de priorité** avec effet de brillance pour identification rapide
- **Layout responsive** s'adaptant à la taille de la fenêtre
- **Messages tronqués** intelligemment pour une meilleure lisibilité
- **Ombres portées** et coins arrondis pour un design moderne
- **États visuels interactifs** (hover, selection)

## ⚙️ Optimisations Techniques

### Taille de l'Exécutable
- ✅ PublishTrimmed : Suppression du code inutilisé
- ✅ TrimMode=link : Trimming agressif
- ✅ EnableCompressionInSingleFile : Compression intégrée
- ✅ DebugType=none : Pas de symboles de debug
- ✅ PublishReadyToRun : Compilation AOT partielle

### Performance
- ✅ Chargement asynchrone des événements
- ✅ Cache des SolidColorBrush (frozen)
- ✅ Virtualisation UI pour la liste
- ✅ Mode de recyclage pour le ListView
- ✅ Allocations minimisées (Span, init properties)

## ⚠️ Notes Importantes

- **Privilèges administrateur** requis pour lire les journaux système Windows
- L'application demande automatiquement l'élévation via le manifest
- Les journaux Security peuvent nécessiter des permissions spéciales
- Compatible Windows 10 version 1809 et supérieur

## 🔧 Structure du Projet

```
new-event-viewer/
├── App.xaml              # Ressources et thème sombre
├── App.xaml.cs           # Point d'entrée de l'application
├── MainWindow.xaml       # Interface utilisateur
├── MainWindow.xaml.cs    # Logique métier
├── ErrorAnalyzer.cs      # Traduction des IDs d'événements en langage clair
├── EventViewer.csproj    # Configuration du projet
├── app.manifest          # Élévation administrateur
├── build.bat             # Script de compilation
└── README.md             # Documentation
```

## 🧠 Base de Connaissances des Événements

L'application intègre une base de données complète d'événements Windows :

### Catégories Couvertes
- 🖥️ **Événements système critiques** (Redémarrages, arrêts inattendus)
- 💿 **Événements disque** (Erreurs matérielles, espace faible)
- 🌐 **Événements réseau** (Connexions perdues, DNS)
- 🔐 **Sécurité et authentification** (Échecs de connexion, audits)
- 🔄 **Windows Update** (Échecs d'installation)
- 📱 **Applications** (Plantages, blocages)
- 🔌 **Pilotes et périphériques** (USB, erreurs de chargement)
- 💾 **Mémoire et performance** (BSOD, ressources)
- ⚙️ **Services Windows** (Échecs de démarrage, timeouts)
- 🛡️ **Windows Defender** (Menaces détectées)
- 🔒 **BitLocker** (Volumes verrouillés)
- 🖨️ **Impression** (Erreurs du spouleur)

### Enrichissement IA (Futur)

La classe `ErrorAnalyzer` est conçue pour être étendue avec une intégration IA :
- Méthode `EnrichWithAIAsync()` prête pour OpenAI, Azure OpenAI ou Claude
- Analyse contextuelle des messages complets
- Génération de solutions personnalisées
- Apprentissage continu des nouveaux événements

## 🎯 Cas d'Usage

- 🔍 **Diagnostic système** rapide avec explications intégrées
- 🛠️ **Support technique** sur site - plus besoin d'Internet pour comprendre les erreurs
- 📊 **Surveillance événements** critiques avec contexte immédiat
- 💼 **Administration système** portable avec base de connaissances
- 🎓 **Formation et démonstration** - apprenez à identifier les problèmes Windows
- 🚑 **Dépannage d'urgence** - solutions rapides sans recherche Google

## 📝 License

Projet libre d'utilisation et de modification.

---

**Développé avec ❤️ en C# et WPF**

