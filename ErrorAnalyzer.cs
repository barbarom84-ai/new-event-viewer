using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventViewer
{
    /// <summary>
    /// Classe responsable de l'analyse et de la traduction des événements Windows en langage clair.
    /// </summary>
    public sealed class ErrorAnalyzer
    {
        // Dictionnaire des événements Windows les plus courants avec leurs explications
        private static readonly Dictionary<int, EventExplanation> KnownEvents = new()
        {
            // Événements système critiques
            [41] = new EventExplanation
            {
                Title = "Redémarrage inattendu du système",
                Description = "Le système a redémarré sans s'arrêter proprement. Cela peut être causé par une coupure de courant, un plantage système ou une mise à jour forcée.",
                Severity = "Critique",
                Solution = "Vérifiez l'alimentation électrique, les pilotes matériels et les mises à jour Windows récentes."
            },
            [1074] = new EventExplanation
            {
                Title = "Arrêt ou redémarrage système planifié",
                Description = "Le système s'est arrêté ou a redémarré de manière planifiée (par un utilisateur, une application ou Windows Update).",
                Severity = "Information",
                Solution = "Aucune action requise si cela était intentionnel."
            },
            [6008] = new EventExplanation
            {
                Title = "Arrêt inattendu du système",
                Description = "Le système s'est arrêté de manière imprévue lors du dernier démarrage.",
                Severity = "Critique",
                Solution = "Vérifiez les journaux pour identifier la cause (problème matériel, surchauffe, alimentation)."
            },
            [10016] = new EventExplanation
            {
                Title = "Permissions DCOM insuffisantes",
                Description = "Une application tente d'accéder à un composant DCOM sans les permissions nécessaires.",
                Severity = "Avertissement",
                Solution = "Généralement bénin. Si problématique, ajustez les permissions DCOM via dcomcnfg.exe."
            },

            // Événements disque
            [7] = new EventExplanation
            {
                Title = "Erreur de périphérique",
                Description = "Un périphérique de stockage a signalé une erreur matérielle. Cela peut indiquer un disque dur défaillant.",
                Severity = "Critique",
                Solution = "Sauvegardez vos données immédiatement et vérifiez l'état du disque avec CrystalDiskInfo ou chkdsk."
            },
            [11] = new EventExplanation
            {
                Title = "Erreur de contrôleur de disque",
                Description = "Le contrôleur a détecté une erreur sur le disque dur.",
                Severity = "Critique",
                Solution = "Testez le disque avec les outils du fabricant. Remplacez-le si nécessaire."
            },
            [51] = new EventExplanation
            {
                Title = "Erreur de pagination",
                Description = "Une erreur s'est produite lors de l'écriture de la page mémoire sur le disque.",
                Severity = "Erreur",
                Solution = "Vérifiez l'espace disque disponible et l'intégrité du fichier de pagination."
            },
            [153] = new EventExplanation
            {
                Title = "Espace disque faible",
                Description = "Un volume de disque a peu d'espace disponible.",
                Severity = "Avertissement",
                Solution = "Libérez de l'espace en supprimant des fichiers inutiles ou en utilisant l'outil de nettoyage de disque."
            },

            // Événements réseau
            [4201] = new EventExplanation
            {
                Title = "Connexion réseau perdue",
                Description = "La connexion réseau a été interrompue de manière inattendue.",
                Severity = "Avertissement",
                Solution = "Vérifiez les câbles réseau, le routeur et les paramètres de la carte réseau."
            },
            [8003] = new EventExplanation
            {
                Title = "Problème de résolution DNS",
                Description = "Le résolveur DNS ne peut pas contacter les serveurs DNS.",
                Severity = "Erreur",
                Solution = "Vérifiez la configuration DNS (8.8.8.8 pour Google DNS) et la connectivité Internet."
            },

            // Événements de sécurité et authentification
            [4625] = new EventExplanation
            {
                Title = "Échec d'ouverture de session",
                Description = "Une tentative de connexion a échoué (mauvais mot de passe ou compte verrouillé).",
                Severity = "Avertissement",
                Solution = "Vérifiez les identifiants. Si répété, cela peut indiquer une tentative d'intrusion."
            },
            [4624] = new EventExplanation
            {
                Title = "Ouverture de session réussie",
                Description = "Un utilisateur s'est connecté avec succès au système.",
                Severity = "Information",
                Solution = "Aucune action requise."
            },
            [4648] = new EventExplanation
            {
                Title = "Tentative de connexion avec identifiants explicites",
                Description = "Un utilisateur a tenté de se connecter en utilisant des identifiants différents.",
                Severity = "Information",
                Solution = "Normal si vous utilisez 'Exécuter en tant qu'administrateur' ou des connexions réseau."
            },

            // Événements Windows Update
            [19] = new EventExplanation
            {
                Title = "Échec d'installation de mise à jour",
                Description = "Windows Update n'a pas pu installer une mise à jour.",
                Severity = "Erreur",
                Solution = "Exécutez l'utilitaire de résolution des problèmes Windows Update ou réessayez plus tard."
            },
            [20] = new EventExplanation
            {
                Title = "Échec de téléchargement de mise à jour",
                Description = "Windows Update n'a pas pu télécharger les mises à jour.",
                Severity = "Erreur",
                Solution = "Vérifiez votre connexion Internet et l'espace disque disponible."
            },

            // Événements applicatifs
            [1000] = new EventExplanation
            {
                Title = "Plantage d'application",
                Description = "Une application s'est arrêtée de manière inattendue (crash).",
                Severity = "Erreur",
                Solution = "Réinstallez l'application, mettez à jour les pilotes ou vérifiez les fichiers corrompus."
            },
            [1001] = new EventExplanation
            {
                Title = "Rapport d'erreur Windows (WER)",
                Description = "Un rapport d'erreur a été généré suite à un plantage.",
                Severity = "Information",
                Solution = "Consultez les détails du rapport pour identifier l'application problématique."
            },
            [1002] = new EventExplanation
            {
                Title = "Blocage d'application",
                Description = "Une application ne répond plus.",
                Severity = "Avertissement",
                Solution = "Vérifiez si l'application est à jour et compatible avec votre version de Windows."
            },

            // Événements pilotes
            [219] = new EventExplanation
            {
                Title = "Échec de démarrage de pilote",
                Description = "Un pilote de périphérique n'a pas pu se charger au démarrage.",
                Severity = "Erreur",
                Solution = "Mettez à jour ou réinstallez le pilote via le Gestionnaire de périphériques."
            },

            // Événements mémoire
            [1001] = new EventExplanation
            {
                Title = "Erreur de vérification de bugcheck",
                Description = "Le système a détecté une erreur critique (écran bleu/BSOD).",
                Severity = "Critique",
                Solution = "Analysez le fichier dump mémoire avec BlueScreenView pour identifier le pilote fautif."
            },
            [2004] = new EventExplanation
            {
                Title = "Problème de ressources système",
                Description = "Le système manque de ressources (mémoire ou handles).",
                Severity = "Avertissement",
                Solution = "Fermez les applications inutilisées ou ajoutez de la RAM."
            },

            // Événements de service
            [7000] = new EventExplanation
            {
                Title = "Échec de démarrage de service",
                Description = "Un service Windows n'a pas pu démarrer.",
                Severity = "Erreur",
                Solution = "Vérifiez les dépendances du service et ses permissions dans services.msc."
            },
            [7001] = new EventExplanation
            {
                Title = "Service dépendant non démarré",
                Description = "Un service ne peut pas démarrer car un service dont il dépend n'est pas actif.",
                Severity = "Erreur",
                Solution = "Identifiez et démarrez le service dépendant en premier."
            },
            [7009] = new EventExplanation
            {
                Title = "Délai d'attente de service dépassé",
                Description = "Un service a mis trop de temps à répondre à une demande de démarrage ou de contrôle.",
                Severity = "Erreur",
                Solution = "Le service peut être bloqué. Redémarrez le système ou désactivez le service problématique."
            },
            [7011] = new EventExplanation
            {
                Title = "Délai d'attente transactionnel de service",
                Description = "Un service n'a pas répondu dans le délai imparti lors d'une transaction.",
                Severity = "Avertissement",
                Solution = "Vérifiez les performances système et les journaux du service spécifique."
            },
            [7031] = new EventExplanation
            {
                Title = "Service terminé de manière inattendue",
                Description = "Un service s'est arrêté de façon imprévue.",
                Severity = "Erreur",
                Solution = "Configurez les options de récupération du service dans services.msc."
            },
            [7034] = new EventExplanation
            {
                Title = "Service terminé de manière inattendue (répété)",
                Description = "Un service s'est arrêté sans raison apparente.",
                Severity = "Erreur",
                Solution = "Vérifiez les journaux d'application pour plus de détails sur la cause."
            },

            // Événements de performance
            [2019] = new EventExplanation
            {
                Title = "Espace insuffisant pour le fichier d'échange",
                Description = "Le système manque d'espace pour agrandir le fichier d'échange.",
                Severity = "Avertissement",
                Solution = "Libérez de l'espace disque ou augmentez la taille du fichier d'échange."
            },

            // Événements USB/Périphériques
            [10110] = new EventExplanation
            {
                Title = "Problème d'alimentation USB",
                Description = "Un périphérique USB demande plus de puissance que le port peut fournir.",
                Severity = "Avertissement",
                Solution = "Utilisez un hub USB alimenté ou connectez le périphérique directement au PC."
            },

            // Événements Kernel-Power
            [41] = new EventExplanation
            {
                Title = "Kernel-Power : Arrêt critique",
                Description = "Le système a redémarré sans s'arrêter proprement (coupure, crash matériel).",
                Severity = "Critique",
                Solution = "Vérifiez l'alimentation, la température du CPU/GPU et la stabilité du matériel."
            },

            // Événements de certificats
            [10] = new EventExplanation
            {
                Title = "Certificat SSL/TLS expiré ou invalide",
                Description = "Un certificat de sécurité utilisé pour une connexion HTTPS est invalide.",
                Severity = "Avertissement",
                Solution = "Mettez à jour les certificats racine ou contactez l'administrateur du service."
            },

            // Événements temps système
            [1] = new EventExplanation
            {
                Title = "Modification de l'heure système",
                Description = "L'heure du système a été modifiée.",
                Severity = "Information",
                Solution = "Vérifiez que la synchronisation NTP fonctionne correctement."
            },

            // Événements Windows Defender
            [1116] = new EventExplanation
            {
                Title = "Menace détectée par Windows Defender",
                Description = "Windows Defender a détecté un logiciel malveillant ou potentiellement indésirable.",
                Severity = "Avertissement",
                Solution = "Suivez les recommandations de Windows Defender pour supprimer ou mettre en quarantaine la menace."
            },
            [1117] = new EventExplanation
            {
                Title = "Action effectuée sur une menace",
                Description = "Windows Defender a pris des mesures contre une menace détectée.",
                Severity = "Information",
                Solution = "Vérifiez que la menace a été correctement traitée dans l'historique de protection."
            },

            // Événements de Bitlocker
            [24] = new EventExplanation
            {
                Title = "Volume BitLocker verrouillé",
                Description = "Un volume chiffré BitLocker est verrouillé et nécessite une clé de récupération.",
                Severity = "Erreur",
                Solution = "Utilisez votre clé de récupération BitLocker pour déverrouiller le volume."
            },

            // Événements d'impression
            [372] = new EventExplanation
            {
                Title = "Erreur du spooler d'impression",
                Description = "Le service de spouleur d'impression a rencontré un problème.",
                Severity = "Erreur",
                Solution = "Redémarrez le service 'Spouleur d'impression' ou réinstallez les pilotes d'imprimante."
            }
        };

        /// <summary>
        /// Obtient une explication claire pour un ID d'événement Windows donné.
        /// </summary>
        /// <param name="eventId">L'ID de l'événement Windows</param>
        /// <param name="eventSource">La source de l'événement (optionnel, pour un contexte supplémentaire)</param>
        /// <returns>Une explication détaillée de l'événement</returns>
        public EventExplanation GetExplanation(int eventId, string? eventSource = null)
        {
            // Vérifie si l'événement est dans le dictionnaire
            if (KnownEvents.TryGetValue(eventId, out var explanation))
            {
                return explanation;
            }

            // Si non trouvé, retourne une explication générique
            return new EventExplanation
            {
                Title = $"Événement Windows {eventId}",
                Description = $"Événement système non répertorié dans la base de connaissances. Source : {eventSource ?? "Inconnue"}",
                Severity = "Inconnu",
                Solution = "Recherchez cet ID d'événement dans la documentation Microsoft ou utilisez l'enrichissement IA pour plus de détails."
            };
        }

        /// <summary>
        /// Enrichit une explication existante avec des informations supplémentaires via IA.
        /// Cette méthode est un placeholder pour une future intégration d'IA (GPT, Claude, etc.)
        /// </summary>
        /// <param name="eventId">L'ID de l'événement</param>
        /// <param name="eventMessage">Le message complet de l'événement</param>
        /// <param name="eventSource">La source de l'événement</param>
        /// <returns>Une explication enrichie par l'IA</returns>
        public async Task<EventExplanation> EnrichWithAIAsync(int eventId, string eventMessage, string eventSource)
        {
            // TODO: Intégration future avec une API d'IA (OpenAI, Azure OpenAI, Claude, etc.)
            // 
            // Exemple d'implémentation future :
            // 1. Récupérer l'explication de base
            // 2. Construire un prompt avec le contexte complet
            // 3. Envoyer à l'API d'IA
            // 4. Parser et retourner la réponse enrichie
            //
            // var prompt = $"Explique en français et de manière simple cet événement Windows:\n" +
            //              $"ID: {eventId}\n" +
            //              $"Source: {eventSource}\n" +
            //              $"Message: {eventMessage}\n" +
            //              $"Fournis: titre, description, sévérité et solution.";
            //
            // var aiResponse = await aiClient.GetCompletionAsync(prompt);
            // return ParseAIResponse(aiResponse);

            await Task.Delay(100); // Simulation d'un appel asynchrone

            var baseExplanation = GetExplanation(eventId, eventSource);
            baseExplanation.Description += "\n\n[Enrichissement IA non encore implémenté - Cette fonctionnalité sera disponible prochainement]";
            
            return baseExplanation;
        }

        /// <summary>
        /// Obtient une explication courte (1 ligne) pour affichage dans une interface compacte.
        /// </summary>
        /// <param name="eventId">L'ID de l'événement</param>
        /// <returns>Un résumé court de l'événement</returns>
        public string GetShortExplanation(int eventId)
        {
            if (KnownEvents.TryGetValue(eventId, out var explanation))
            {
                return $"[ID {eventId}] {explanation.Title}";
            }

            return $"[ID {eventId}] Événement système non documenté";
        }

        /// <summary>
        /// Vérifie si un événement est connu dans la base de données.
        /// </summary>
        /// <param name="eventId">L'ID de l'événement à vérifier</param>
        /// <returns>True si l'événement est connu, False sinon</returns>
        public bool IsKnownEvent(int eventId) => KnownEvents.ContainsKey(eventId);

        /// <summary>
        /// Obtient le nombre total d'événements dans la base de connaissances.
        /// </summary>
        /// <returns>Le nombre d'événements connus</returns>
        public int GetKnownEventsCount() => KnownEvents.Count;
    }

    /// <summary>
    /// Représente une explication détaillée d'un événement Windows.
    /// </summary>
    public sealed class EventExplanation
    {
        /// <summary>
        /// Titre court et descriptif de l'événement
        /// </summary>
        public required string Title { get; init; }

        /// <summary>
        /// Description détaillée de ce que signifie l'événement
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Niveau de gravité : Critique, Erreur, Avertissement, Information, Inconnu
        /// </summary>
        public required string Severity { get; init; }

        /// <summary>
        /// Solution suggérée ou actions à entreprendre
        /// </summary>
        public required string Solution { get; init; }

        /// <summary>
        /// Retourne une représentation textuelle formatée de l'explication
        /// </summary>
        public override string ToString()
        {
            return $"[{Severity}] {Title}\n\n{Description}\n\n💡 Solution: {Solution}";
        }
    }
}

