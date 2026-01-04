using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace EventViewer
{
    /// <summary>
    /// Détecte automatiquement les tags pour les événements système.
    /// </summary>
    public sealed class TagDetector
    {
        // Dictionnaire des catégories avec leurs mots-clés
        private static readonly Dictionary<string, TagInfo> TagCategories = new()
        {
            ["MATÉRIEL"] = new TagInfo
            {
                Name = "MATÉRIEL",
                Keywords = new[] { "disk", "bad block", "ntfs", "hardware", "drive", "ssd", "hdd", "sata", "storage", "volume", "partition" },
                Color = Color.FromRgb(244, 67, 54), // Rouge
                Advice = "⚠️ Problème matériel détecté !\n\n" +
                        "🔧 Actions recommandées :\n" +
                        "1. Vérifiez l'état de santé du disque avec CrystalDiskInfo\n" +
                        "2. Sauvegardez vos données importantes immédiatement\n" +
                        "3. Exécutez CHKDSK /F pour réparer les secteurs défectueux\n" +
                        "4. Si le problème persiste, envisagez un remplacement du disque\n\n" +
                        "💡 Astuce : Un disque défaillant peut causer des pertes de données graves."
            },
            ["RÉSEAU"] = new TagInfo
            {
                Name = "RÉSEAU",
                Keywords = new[] { "network", "dns", "tcp", "ip", "ethernet", "wifi", "connection", "internet", "lan", "wan", "dhcp", "router" },
                Color = Color.FromRgb(33, 150, 243), // Bleu
                Advice = "🌐 Problème réseau détecté !\n\n" +
                        "🔧 Actions recommandées :\n" +
                        "1. Vérifiez que le câble réseau est bien branché\n" +
                        "2. Redémarrez votre routeur/box Internet (30 secondes)\n" +
                        "3. Essayez de changer les serveurs DNS (8.8.8.8 / 1.1.1.1)\n" +
                        "4. Désactivez/réactivez votre carte réseau\n" +
                        "5. Vérifiez les paramètres du pare-feu\n\n" +
                        "💡 Astuce : Si le problème persiste, contactez votre FAI."
            },
            ["MÉMOIRE"] = new TagInfo
            {
                Name = "MÉMOIRE",
                Keywords = new[] { "memory", "ram", "page file", "paging", "swap", "virtual memory", "out of memory", "oom" },
                Color = Color.FromRgb(156, 39, 176), // Violet
                Advice = "🧠 Problème de mémoire détecté !\n\n" +
                        "🔧 Actions recommandées :\n" +
                        "1. Fermez les applications inutilisées\n" +
                        "2. Redémarrez l'ordinateur pour libérer la RAM\n" +
                        "3. Augmentez la taille du fichier d'échange (pagefile)\n" +
                        "4. Vérifiez les processus gourmands dans le Gestionnaire des tâches\n" +
                        "5. Envisagez d'ajouter plus de RAM\n\n" +
                        "💡 Astuce : 8 Go minimum recommandé pour Windows 10/11."
            },
            ["SERVICE"] = new TagInfo
            {
                Name = "SERVICE",
                Keywords = new[] { "service", "svchost", "daemon", "timeout", "failed to start", "stopped unexpectedly" },
                Color = Color.FromRgb(255, 152, 0), // Orange
                Advice = "⚙️ Problème de service Windows détecté !\n\n" +
                        "🔧 Actions recommandées :\n" +
                        "1. Ouvrez services.msc et trouvez le service problématique\n" +
                        "2. Vérifiez les dépendances du service\n" +
                        "3. Essayez de redémarrer le service manuellement\n" +
                        "4. Configurez les options de récupération du service\n" +
                        "5. Vérifiez les permissions du compte de service\n\n" +
                        "💡 Astuce : Certains services non-critiques peuvent être désactivés."
            },
            ["SÉCURITÉ"] = new TagInfo
            {
                Name = "SÉCURITÉ",
                Keywords = new[] { "security", "virus", "malware", "threat", "firewall", "authentication", "login", "credential", "unauthorized" },
                Color = Color.FromRgb(255, 193, 7), // Jaune
                Advice = "🔒 Alerte de sécurité détectée !\n\n" +
                        "🔧 Actions recommandées :\n" +
                        "1. Lancez une analyse complète avec Windows Defender\n" +
                        "2. Vérifiez les tentatives de connexion suspectes\n" +
                        "3. Changez vos mots de passe si nécessaire\n" +
                        "4. Activez l'authentification à deux facteurs\n" +
                        "5. Vérifiez les règles du pare-feu\n\n" +
                        "💡 Astuce : Ne jamais ignorer les alertes de sécurité."
            }
        };

        /// <summary>
        /// Détecte automatiquement le tag approprié pour un événement.
        /// </summary>
        public TagInfo? DetectTag(string message, string source)
        {
            var textToAnalyze = $"{message} {source}".ToLowerInvariant();

            foreach (var category in TagCategories)
            {
                if (category.Value.Keywords.Any(keyword => textToAnalyze.Contains(keyword)))
                {
                    return category.Value;
                }
            }

            return null; // Aucun tag détecté
        }

        /// <summary>
        /// Obtient tous les tags disponibles.
        /// </summary>
        public IEnumerable<TagInfo> GetAllTags() => TagCategories.Values;
    }

    /// <summary>
    /// Représente les informations d'un tag.
    /// </summary>
    public sealed class TagInfo
    {
        public required string Name { get; init; }
        public required string[] Keywords { get; init; }
        public required Color Color { get; init; }
        public required string Advice { get; init; }

        public SolidColorBrush Brush => new(Color);
    }
}

