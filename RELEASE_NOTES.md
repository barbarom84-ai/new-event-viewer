# Notes de version

## 1.1.0 — 2026-07-08

### Added
- Correcteur automatique (« Correction en 1 clic ») selon le type d'incident, avec confirmation
- Feedback « Utile / Pas utile » sur les recommandations
- Options IA cloud (endpoint OpenAI-compatible + clé via `EVENTVIEWER_AI_API_KEY`) avec fallback local
- Scripts publish Store-flagged WinUI (`build_winui_store`)
- Métadonnées Partner Center, checklist captures, politique de confidentialité à jour

### Changed
- Produit actif : WinUI 3 (WPF en legacy)
- Chemins d'écriture : AppData / Documents (évite les crashs d'écriture dans le dossier exe)
- Documentation USB / Store recentrée sur WinUI

### Fixed
- Crash au bascule Télémétrie / IA bêta (écriture settings)
- Export CSV / JSON vers un dossier utilisateur accessible
- Labels honnêtes local vs cloud pour l'assistant d'explication

### Known Issues
- L'élévation administrateur reste requise pour une lecture complète et certaines corrections
- Le packaging MSIX signé Partner Center se fait via Visual Studio (« Package and Publish »)
- SFC peut prendre plusieurs minutes ; prévoir un feedback utilisateur patient
- Toutes les erreurs ne sont pas auto-corrigibles (ex. mémoire : conseil manuel)
