using System;
using System.Threading.Tasks;

namespace EventViewer
{
    /// <summary>
    /// Service IA beta avec fallback local deterministic.
    /// </summary>
    public sealed class AiAssistantService
    {
        private readonly ErrorAnalyzer _errorAnalyzer;

        public AiAssistantService(ErrorAnalyzer errorAnalyzer)
        {
            _errorAnalyzer = errorAnalyzer;
        }

        public Task<string> BuildIncidentSummaryAsync(EventLogItem evt, bool aiBetaEnabled)
        {
            if (evt == null)
            {
                return Task.FromResult("Aucun evenement selectionne.");
            }

            // Placeholder: meme en beta on reste en fallback local tant qu'aucune API externe n'est configuree.
            var explanation = _errorAnalyzer.GetExplanation(evt.EventId, evt.Source);
            var prefix = aiBetaEnabled ? "[BETA IA - fallback local]" : "[Mode local]";
            var summary = $"{prefix}\n\n" +
                          $"Incident {evt.EventId} ({evt.Level}) - {evt.Source}\n" +
                          $"Titre: {explanation.Title}\n" +
                          $"Description: {explanation.Description}\n" +
                          $"Action recommandee: {explanation.Solution}";

            return Task.FromResult(summary);
        }
    }
}
