using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using IOPath = System.IO.Path;

namespace EventViewer
{
    public partial class MainWindow : Window
    {
        // Cache des couleurs pour éviter les allocations répétées
        private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(244, 67, 54));
        private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(255, 193, 7));
        private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(33, 150, 243));
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(158, 158, 158));

        // Analyseur d'erreurs pour traduire les IDs en langage clair
        private readonly ErrorAnalyzer _errorAnalyzer = new();

        // Détecteur de tags automatique
        private readonly TagDetector _tagDetector = new();

        // Gestionnaire de snapshots
        private readonly SnapshotManager _snapshotManager = new();
        private bool _comparisonMode = false;

        // Timer pour l'actualisation automatique
        private readonly DispatcherTimer _autoRefreshTimer;
        private int _remainingSeconds = 30;
        private bool _autoRefreshEnabled = false; // Désactivé par défaut

        // Collections pour la recherche
        private ObservableCollection<EventLogItem> _allEvents = new();
        private ObservableCollection<EventLogItem> _filteredEvents = new();

        static MainWindow()
        {
            // Freeze les brushes pour de meilleures performances
            ErrorBrush.Freeze();
            WarningBrush.Freeze();
            InfoBrush.Freeze();
            DefaultBrush.Freeze();
        }

        public ObservableCollection<EventLogItem> Events { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Events = new ObservableCollection<EventLogItem>();
            EventListView.ItemsSource = _filteredEvents;
            
            // Configuration du timer d'actualisation automatique
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
            _autoRefreshTimer.Start();
            
            _ = LoadEventsAsync();
            UpdateSnapshotStatus();
            DrawChart();
            UpdateAdminStatus();
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterEvents();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            SearchBox.Focus();
        }

        private void FilterEvents()
        {
            var searchText = SearchBox?.Text?.ToLowerInvariant() ?? string.Empty;
            
            _filteredEvents.Clear();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Afficher tous les événements
                foreach (var evt in _allEvents)
                {
                    _filteredEvents.Add(evt);
                }
            }
            else
            {
                // Filtrer par recherche
                foreach (var evt in _allEvents)
                {
                    if (evt.Message.ToLowerInvariant().Contains(searchText) ||
                        evt.Source.ToLowerInvariant().Contains(searchText) ||
                        evt.EventId.ToString().Contains(searchText) ||
                        evt.Level.ToLowerInvariant().Contains(searchText) ||
                        (evt.ExplanationTitle?.ToLowerInvariant().Contains(searchText) ?? false))
                    {
                        _filteredEvents.Add(evt);
                    }
                }
            }
            
            UpdateEventCounter();
        }

        private void UpdateEventCounter()
        {
            var count = _filteredEvents.Count;
            var totalCount = _allEvents.Count;
            
            if (string.IsNullOrWhiteSpace(SearchBox?.Text))
            {
                EventCountText.Text = count switch
                {
                    0 => "Aucune erreur trouvée",
                    1 => "1 erreur / avertissement",
                    _ => $"{count} erreurs / avertissements"
                };
            }
            else
            {
                EventCountText.Text = count switch
                {
                    0 => $"Aucun résultat sur {totalCount}",
                    1 => $"1 résultat sur {totalCount}",
                    _ => $"{count} résultats sur {totalCount}"
                };
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadEventsAsync();
            ResetAutoRefreshTimer();
        }

        private async Task ExportButton_ClickAsync()
        {
            try
            {
                // Enregistrer le provider d'encodage pour Windows-1252 (ANSI)
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // Obtenir le répertoire de l'exécutable (sur la clé USB)
                var exePath = AppDomain.CurrentDomain.BaseDirectory;
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // Capture des données UI sur le thread principal avant l'export en arrière-plan
                var dispatcher = Application.Current.Dispatcher;
                var exportData = dispatcher.Invoke(() => new
                {
                    Events = _filteredEvents.ToList(),
                    SelectedLog = ((System.Windows.Controls.ComboBoxItem)LogComboBox.SelectedItem)?.Content?.ToString() ?? "System"
                });
                
                // Créer les deux fichiers d'export
                var txtFilePath = IOPath.Combine(exePath, $"EventExport_{timestamp}.txt");
                var csvFilePath = IOPath.Combine(exePath, $"EventExport_{timestamp}.csv");
                
                await Task.Run(() =>
                {
                    // Export TXT avec encodage Windows-1252 (ANSI) pour Excel français
                    using (var txtWriter = new StreamWriter(txtFilePath, append: false, Encoding.GetEncoding(1252)))
                    {
                        txtWriter.WriteLine("═══════════════════════════════════════════════════════════════════");
                        txtWriter.WriteLine($"   EXPORT DES ÉVÉNEMENTS WINDOWS - {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                        txtWriter.WriteLine("═══════════════════════════════════════════════════════════════════");
                        txtWriter.WriteLine($"Journal: {exportData.SelectedLog}");
                        txtWriter.WriteLine($"Nombre d'événements: {exportData.Events.Count}");
                        txtWriter.WriteLine("═══════════════════════════════════════════════════════════════════");
                        txtWriter.WriteLine();

                        foreach (var evt in exportData.Events)
                        {
                            txtWriter.WriteLine($"┌─ {evt.Level.ToUpper()} ─────────────────────────────────────────────");
                            txtWriter.WriteLine($"│ Date/Heure  : {evt.TimeCreated}");
                            txtWriter.WriteLine($"│ ID Événement: {evt.EventId}");
                            txtWriter.WriteLine($"│ Source      : {evt.Source}");
                            txtWriter.WriteLine($"│ Explication : {evt.ExplanationTitle}");
                            txtWriter.WriteLine($"│");
                            txtWriter.WriteLine($"│ Message:");
                            txtWriter.WriteLine($"│   {evt.Message}");
                            txtWriter.WriteLine($"└──────────────────────────────────────────────────────────────────");
                            txtWriter.WriteLine();
                        }

                        txtWriter.WriteLine();
                        txtWriter.WriteLine("═══════════════════════════════════════════════════════════════════");
                        txtWriter.WriteLine("   Généré par Observateur d'Événements Moderne");
                        txtWriter.WriteLine("═══════════════════════════════════════════════════════════════════");
                    }

                    // Export CSV avec encodage Windows-1252 (ANSI) et séparateur point-virgule
                    using (var csvWriter = new StreamWriter(csvFilePath, append: false, Encoding.GetEncoding(1252)))
                    {
                        // Directive pour forcer Excel à reconnaître le séparateur point-virgule
                        csvWriter.WriteLine("sep=;");
                        csvWriter.WriteLine("Date/Heure;Niveau;ID Événement;Source;Explication;Message");

                        foreach (var evt in exportData.Events)
                        {
                            // Échapper les guillemets pour CSV
                            var message = evt.Message.Replace("\"", "\"\"");
                            var source = evt.Source.Replace("\"", "\"\"");
                            var explanation = (evt.ExplanationTitle ?? "").Replace("\"", "\"\"");
                            
                            csvWriter.WriteLine($"\"{evt.TimeCreated}\";\"{evt.Level}\";{evt.EventId};\"{source}\";\"{explanation}\";\"{message}\"");
                        }
                    }
                });

                StatusText.Text = $"✓ Exporté: {IOPath.GetFileName(txtFilePath)} & {IOPath.GetFileName(csvFilePath)}";
                
                MessageBox.Show($"Export réussi !\n\n" +
                               $"Fichiers créés:\n" +
                               $"• {IOPath.GetFileName(txtFilePath)}\n" +
                               $"• {IOPath.GetFileName(csvFilePath)}\n\n" +
                               $"Emplacement:\n{exePath}",
                               "Export terminé",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "✗ Erreur lors de l'export";
                MessageBox.Show($"Erreur lors de l'export:\n\n{ex.Message}",
                               "Erreur d'export",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void AutoRefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (!_autoRefreshEnabled)
                return;

            _remainingSeconds--;
            
            if (_remainingSeconds <= 0)
            {
                _ = LoadEventsAsync();
                _remainingSeconds = 30;
            }
            
            // Note: AutoRefreshText est dans le template du menu, pas directement accessible
        }

        private void ResetAutoRefreshTimer()
        {
            _remainingSeconds = 30;
            // Note: AutoRefreshText est dans le template du menu, pas directement accessible
        }

        private void ToggleAutoRefreshMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _autoRefreshEnabled = !_autoRefreshEnabled;

            if (_autoRefreshEnabled)
            {
                AutoRefreshMenuItem.Header = "✅ Actualisation Auto: ON";
                AutoRefreshMenuItem.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36)); // Vert foncé
                _remainingSeconds = 30;
                StatusText.Text = "✓ Actualisation automatique activée (30s)";
            }
            else
            {
                AutoRefreshMenuItem.Header = "⏸️ Actualisation Auto: OFF";
                AutoRefreshMenuItem.Foreground = new SolidColorBrush(Color.FromRgb(133, 100, 4)); // Orange foncé
                StatusText.Text = "Actualisation automatique désactivée";
            }
        }

        private void EventListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (EventListView.SelectedItem is EventLogItem selectedEvent)
            {
                ShowEventDetails(selectedEvent);
            }
        }

        private void EventListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EventListView.SelectedItem is EventLogItem selectedEvent && selectedEvent.Tag != null)
            {
                // Afficher le conseil IA pour les événements avec tag
                AdvicePanel.Visibility = Visibility.Visible;
                AdviceText.Text = selectedEvent.Tag.Advice;
            }
        }

        private void CloseAdviceButton_Click(object sender, RoutedEventArgs e)
        {
            AdvicePanel.Visibility = Visibility.Collapsed;
        }

        private void UpdateAdminStatus()
        {
            // Le statut admin est dans le template du menu, on ne peut pas y accéder directement
            // Le vérification se fait à chaque clic sur les options de maintenance
        }

        // Menu Fichier
        private async void MenuItem_ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            await ExportButton_ClickAsync();
        }

        private void MenuItem_OpenExportFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exePath = AppDomain.CurrentDomain.BaseDirectory;
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "open"
                });
                StatusText.Text = "✓ Dossier ouvert";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le dossier:\n\n{ex.Message}",
                              "Erreur",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void MenuItem_Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Menu Outils
        private void MenuItem_AIAnalysis_Click(object sender, RoutedEventArgs e)
        {
            var totalEvents = _allEvents.Count;
            var errorCount = _allEvents.Count(e => e.Level == "Erreur");
            var warningCount = _allEvents.Count(e => e.Level == "Avertissement");
            var taggedCount = _allEvents.Count(e => e.HasTag);

            var analysis = $"🤖 ANALYSE IA COMPLÈTE DU SYSTÈME\n\n" +
                          $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                          $"📊 STATISTIQUES GLOBALES\n" +
                          $"• Total d'événements: {totalEvents}\n" +
                          $"• Erreurs critiques: {errorCount}\n" +
                          $"• Avertissements: {warningCount}\n" +
                          $"• Événements catégorisés: {taggedCount}\n\n" +
                          $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                          $"🔍 ANALYSE PAR CATÉGORIE\n";

            var materialCount = _allEvents.Count(e => e.Tag?.Name == "MATÉRIEL");
            var networkCount = _allEvents.Count(e => e.Tag?.Name == "RÉSEAU");
            var memoryCount = _allEvents.Count(e => e.Tag?.Name == "MÉMOIRE");
            var serviceCount = _allEvents.Count(e => e.Tag?.Name == "SERVICE");
            var securityCount = _allEvents.Count(e => e.Tag?.Name == "SÉCURITÉ");

            if (materialCount > 0) analysis += $"• 🔴 Matériel: {materialCount} problème(s)\n";
            if (networkCount > 0) analysis += $"• 🔵 Réseau: {networkCount} problème(s)\n";
            if (memoryCount > 0) analysis += $"• 🟣 Mémoire: {memoryCount} problème(s)\n";
            if (serviceCount > 0) analysis += $"• 🟠 Services: {serviceCount} problème(s)\n";
            if (securityCount > 0) analysis += $"• 🟡 Sécurité: {securityCount} problème(s)\n";

            analysis += $"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                       $"💡 RECOMMANDATIONS\n";

            if (materialCount > 0)
                analysis += "• Vérifiez l'état de vos disques durs avec CrystalDiskInfo\n";
            if (networkCount > 0)
                analysis += "• Testez votre connexion réseau et vérifiez les câbles\n";
            if (memoryCount > 0)
                analysis += "• Surveillez l'utilisation de la RAM dans le Gestionnaire des tâches\n";
            if (serviceCount > 0)
                analysis += "• Vérifiez les services Windows dans services.msc\n";
            if (securityCount > 0)
                analysis += "• Lancez une analyse antivirus complète\n";

            if (errorCount == 0 && warningCount == 0)
                analysis += "✅ Aucun problème détecté ! Votre système fonctionne correctement.\n";

            MessageBox.Show(analysis,
                          "Analyse IA Complète",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void MenuItem_ViewStats_Click(object sender, RoutedEventArgs e)
        {
            var totalEvents = _allEvents.Count;
            var errorCount = _allEvents.Count(e => e.Level == "Erreur");
            var warningCount = _allEvents.Count(e => e.Level == "Avertissement");
            
            var topSources = _allEvents
                .GroupBy(e => e.Source)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();

            var topEventIds = _allEvents
                .GroupBy(e => e.EventId)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();

            var stats = $"📊 STATISTIQUES DÉTAILLÉES\n\n" +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                       $"📈 VUE D'ENSEMBLE\n" +
                       $"• Total d'événements: {totalEvents}\n" +
                       $"• Erreurs: {errorCount} ({(totalEvents > 0 ? (errorCount * 100.0 / totalEvents):0):F1}%)\n" +
                       $"• Avertissements: {warningCount} ({(totalEvents > 0 ? (warningCount * 100.0 / totalEvents):0):F1}%)\n\n" +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                       $"🏆 TOP 5 SOURCES D'ERREURS\n";

            foreach (var source in topSources)
            {
                stats += $"• {source.Key}: {source.Count()} événement(s)\n";
            }

            stats += $"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                    $"🔢 TOP 5 EVENT IDS\n";

            foreach (var eventId in topEventIds)
            {
                stats += $"• Event ID {eventId.Key}: {eventId.Count()} occurrence(s)\n";
            }

            MessageBox.Show(stats,
                          "Statistiques",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private async void RepairSystemButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SystemMaintenanceHelper.IsRunningAsAdministrator())
            {
                MessageBox.Show("Cette opération nécessite des privilèges administrateur.\n\n" +
                              "Veuillez relancer l'application en tant qu'administrateur:\n" +
                              "• Clic droit sur EventViewer.exe\n" +
                              "• Sélectionner 'Exécuter en tant qu'administrateur'",
                              "Privilèges requis",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusText.Text = "⏳ Réparation système en cours...";

                var result = MessageBox.Show("Cette opération peut prendre plusieurs minutes.\n\n" +
                                           "SFC (System File Checker) va analyser et réparer les fichiers système corrompus.\n\n" +
                                           "Continuer ?",
                                           "Confirmation",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var commandResult = await SystemMaintenanceHelper.RunSystemFileCheckerAsync();

                    if (commandResult.Success || commandResult.Output.Contains("successfully", StringComparison.OrdinalIgnoreCase))
                    {
                        StatusText.Text = "✓ Réparation système terminée";
                        MessageBox.Show($"Réparation système terminée avec succès !\n\n" +
                                      $"Résumé:\n{commandResult.GetFullOutput()}",
                                      "Succès",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusText.Text = "⚠ Réparation terminée avec avertissements";
                        MessageBox.Show($"La réparation s'est terminée mais des problèmes ont été détectés.\n\n" +
                                      $"Résumé:\n{commandResult.GetFullOutput()}\n\n" +
                                      $"Code de sortie: {commandResult.ExitCode}",
                                      "Avertissement",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                    }
                }
                else
                {
                    StatusText.Text = "Opération annulée";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "✗ Erreur lors de la réparation";
                MessageBox.Show($"Erreur lors de la réparation système:\n\n{ex.Message}",
                              "Erreur",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
            finally
            {
                // Button is in menu, no need to re-enable
            }
        }

        private async void FlushDnsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SystemMaintenanceHelper.IsRunningAsAdministrator())
            {
                MessageBox.Show("Cette opération nécessite des privilèges administrateur.\n\n" +
                              "Veuillez relancer l'application en tant qu'administrateur.",
                              "Privilèges requis",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusText.Text = "🌐 Vidage du cache DNS...";

                var commandResult = await SystemMaintenanceHelper.FlushDnsAsync();

                if (commandResult.Success)
                {
                    StatusText.Text = "✓ Cache DNS vidé";
                    MessageBox.Show("Cache DNS vidé avec succès !\n\n" +
                                  "Les problèmes de résolution DNS devraient être résolus.\n\n" +
                                  commandResult.Output,
                                  "Succès",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else
                {
                    StatusText.Text = "✗ Erreur lors du flush DNS";
                    MessageBox.Show($"Erreur lors du vidage du cache DNS:\n\n{commandResult.Error}",
                                  "Erreur",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "✗ Erreur lors du flush DNS";
                MessageBox.Show($"Erreur:\n\n{ex.Message}",
                              "Erreur",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
            finally
            {
                // Button is in menu, no need to re-enable
            }
        }

        private async void ResetNetworkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SystemMaintenanceHelper.IsRunningAsAdministrator())
            {
                MessageBox.Show("Cette opération nécessite des privilèges administrateur.\n\n" +
                              "Veuillez relancer l'application en tant qu'administrateur.",
                              "Privilèges requis",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = MessageBox.Show("Cette opération va réinitialiser:\n" +
                                           "• Le catalogue Winsock\n" +
                                           "• La pile TCP/IP\n" +
                                           "• Le cache DNS\n\n" +
                                           "⚠️ Un redémarrage peut être nécessaire.\n\n" +
                                           "Continuer ?",
                                           "Confirmation",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    StatusText.Text = "🔄 Réinitialisation réseau...";

                    var winsockResult = await SystemMaintenanceHelper.ResetWinsockAsync();
                    var tcpipResult = await SystemMaintenanceHelper.ResetTcpIpAsync();
                    var dnsResult = await SystemMaintenanceHelper.FlushDnsAsync();

                    var report = new StringBuilder();
                    report.AppendLine("Réinitialisation réseau terminée !\n");
                    report.AppendLine($"✓ Winsock: {(winsockResult.Success ? "OK" : "Échec")}");
                    report.AppendLine($"✓ TCP/IP: {(tcpipResult.Success ? "OK" : "Échec")}");
                    report.AppendLine($"✓ DNS: {(dnsResult.Success ? "OK" : "Échec")}");
                    report.AppendLine("\n⚠️ Redémarrez l'ordinateur pour appliquer tous les changements.");

                    StatusText.Text = "✓ Réseau réinitialisé";
                    MessageBox.Show(report.ToString(),
                                  "Succès",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else
                {
                    StatusText.Text = "Opération annulée";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "✗ Erreur lors de la réinitialisation";
                MessageBox.Show($"Erreur:\n\n{ex.Message}",
                              "Erreur",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
            finally
            {
                // Button is in menu, no need to re-enable
            }
        }

        private void ShowEventDetails(EventLogItem eventItem)
        {
            var explanation = _errorAnalyzer.GetExplanation(eventItem.EventId, eventItem.Source);
            
            var detailsMessage = $"🔍 Détails de l'événement\n\n" +
                                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                                $"📅 Date : {eventItem.TimeCreated}\n" +
                                $"🆔 ID : {eventItem.EventId}\n" +
                                $"📦 Source : {eventItem.Source}\n" +
                                $"⚠️  Niveau : {eventItem.Level}\n\n" +
                                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                                $"📌 {explanation.Title}\n\n" +
                                $"📄 Description :\n{explanation.Description}\n\n" +
                                $"💡 Solution recommandée :\n{explanation.Solution}\n\n" +
                                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                                $"📝 Message original :\n{eventItem.Message}";

            MessageBox.Show(detailsMessage,
                          $"Analyse de l'événement {eventItem.EventId}",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private async Task LoadEventsAsync()
        {
            try
            {
                StatusText.Text = "Chargement en cours...";
                _allEvents.Clear();
                _filteredEvents.Clear();

                var logName = ((System.Windows.Controls.ComboBoxItem)LogComboBox.SelectedItem)?.Content?.ToString() ?? "System";

                // Chargement asynchrone pour ne pas bloquer l'interface
                var entries = await Task.Run(() =>
                {
                    using EventLog eventLog = new(logName);
                    return eventLog.Entries
                        .Cast<EventLogEntry>()
                        .Where(e => e.EntryType == EventLogEntryType.Error || 
                                   e.EntryType == EventLogEntryType.Warning)
                        .OrderByDescending(e => e.TimeGenerated)
                        .Take(50)
                        .Select(entry =>
                        {
                            var explanation = _errorAnalyzer.GetExplanation((int)entry.InstanceId, entry.Source);
                            var tag = _tagDetector.DetectTag(entry.Message, entry.Source);
                            return new EventLogItem
                            {
                                TimeCreated = entry.TimeGenerated.ToString("dd/MM/yyyy HH:mm"),
                                Level = GetLevelText(entry.EntryType),
                                LevelColor = GetLevelColor(entry.EntryType),
                                LevelGlyph = GetLevelGlyph(entry.EntryType),
                                Message = TruncateMessage(entry.Message),
                                Source = entry.Source,
                                EventId = (int)entry.InstanceId,
                                ExplanationTitle = explanation.Title,
                                ExplanationShort = $"💡 {explanation.Title}",
                                Tag = tag
                            };
                        })
                        .ToList();
                });

                // Ajout des éléments sur le thread UI
                foreach (var entry in entries)
                {
                    _allEvents.Add(entry);
                }

                // Appliquer le filtre (si une recherche est en cours)
                FilterEvents();

                // Réappliquer le mode comparaison si actif
                if (_comparisonMode)
                {
                    var newEventKeys = _snapshotManager.GetNewEventKeys(_allEvents);
                    foreach (var evt in _allEvents)
                    {
                        var key = SnapshotManager.CreateEventKey(evt.EventId, evt.Source, evt.Message);
                        evt.IsNew = newEventKeys.Contains(key);
                    }
                }

                // Redessiner le graphique
                DrawChart();

                StatusText.Text = $"✓ {_allEvents.Count} événements chargés";
            }
            catch (UnauthorizedAccessException)
            {
                StatusText.Text = "⚠ Accès refusé";
                MessageBox.Show("L'application nécessite des privilèges administrateur pour accéder aux journaux Windows.\n\nVeuillez redémarrer l'application en tant qu'administrateur.",
                              "Privilèges requis",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                StatusText.Text = "✗ Erreur lors du chargement";
                MessageBox.Show($"Une erreur s'est produite lors du chargement des événements:\n\n{ex.Message}",
                              "Erreur",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private static string GetLevelText(EventLogEntryType type) => type switch
        {
            EventLogEntryType.Error => "Erreur",
            EventLogEntryType.Warning => "Avertissement",
            EventLogEntryType.Information => "Information",
            EventLogEntryType.SuccessAudit => "Succès",
            EventLogEntryType.FailureAudit => "Échec",
            _ => "Inconnu"
        };

        private static SolidColorBrush GetLevelColor(EventLogEntryType type) => type switch
        {
            EventLogEntryType.Error => ErrorBrush,
            EventLogEntryType.Warning => WarningBrush,
            EventLogEntryType.Information => InfoBrush,
            _ => DefaultBrush
        };

        private static string GetLevelGlyph(EventLogEntryType type) => type switch
        {
            EventLogEntryType.Error => "!",
            EventLogEntryType.Warning => "⚠",
            EventLogEntryType.Information => "i",
            _ => "•"
        };

        private static string TruncateMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Aucun message disponible";

            // Nettoyage optimisé du message
            var cleaned = string.Join(" ", message.Split(new[] { '\r', '\n', '\t' }, 
                                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

            return cleaned.Length > 150 ? string.Concat(cleaned.AsSpan(0, 150), "...") : cleaned;
        }

        private void CreateSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _snapshotManager.CreateSnapshot(_allEvents);
                UpdateSnapshotStatus();
                _comparisonMode = false;
                
                // Réinitialiser les marquages "nouveau"
                foreach (var evt in _allEvents)
                {
                    evt.IsNew = false;
                }
                
                EventListView.Items.Refresh();
                
                MessageBox.Show($"Snapshot créé avec succès !\n\n" +
                              $"Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                              $"Événements capturés: {_allEvents.Count}",
                              "Snapshot créé",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création du snapshot:\n\n{ex.Message}",
                              "Erreur",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_snapshotManager.SnapshotExists())
                {
                    MessageBox.Show("Aucun snapshot trouvé !\n\n" +
                                  "Créez d'abord un snapshot pour pouvoir comparer.",
                                  "Pas de snapshot",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                _comparisonMode = !_comparisonMode;

                if (_comparisonMode)
                {
                    // Activer le mode comparaison
                    var newEventKeys = _snapshotManager.GetNewEventKeys(_allEvents);
                    var newEventsCount = 0;

                    foreach (var evt in _allEvents)
                    {
                        var key = SnapshotManager.CreateEventKey(evt.EventId, evt.Source, evt.Message);
                        evt.IsNew = newEventKeys.Contains(key);
                        if (evt.IsNew)
                            newEventsCount++;
                    }

                    EventListView.Items.Refresh();
                    // CompareButton is now in menu

                    var snapshotDate = _snapshotManager.GetSnapshotDate();
                    MessageBox.Show($"Mode comparaison activé !\n\n" +
                                  $"Snapshot de référence: {snapshotDate:dd/MM/yyyy HH:mm:ss}\n" +
                                  $"Nouvelles erreurs détectées: {newEventsCount}\n\n" +
                                  "Les nouvelles erreurs apparaissent en gras.",
                                  "Comparaison",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else
                {
                    // Désactiver le mode comparaison
                    foreach (var evt in _allEvents)
                    {
                        evt.IsNew = false;
                    }

                    EventListView.Items.Refresh();
                    // CompareButton is now in menu
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la comparaison:\n\n{ex.Message}",
                              "Erreur",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void UpdateSnapshotStatus()
        {
            if (_snapshotManager.SnapshotExists())
            {
                var date = _snapshotManager.GetSnapshotDate();
                SnapshotStatusText.Text = $"Snapshot: {date:dd/MM/yyyy HH:mm}";
                SnapshotStatusText.Opacity = 1.0;
                // CompareButton is now in menu
            }
            else
            {
                SnapshotStatusText.Text = "Aucun snapshot";
                SnapshotStatusText.Opacity = 0.7;
                // CompareButton is now in menu
            }
        }

        private void DrawChart()
        {
            ChartCanvas.Children.Clear();

            if (ChartCanvas.ActualWidth <= 0 || ChartCanvas.ActualHeight <= 0)
            {
                // Canvas pas encore rendu, on réessaiera plus tard
                ChartCanvas.Loaded += (s, e) => DrawChart();
                return;
            }

            try
            {
                // Simuler des données sur 24h (à implémenter avec de vraies données)
                var width = ChartCanvas.ActualWidth;
                var height = ChartCanvas.ActualHeight;
                var hours = 24;
                var barWidth = (width - 20) / hours;
                var maxErrors = Math.Max(1, _allEvents.Count);

                // Grouper les événements par heure (simulation simple)
                var random = new Random(DateTime.Now.Hour);
                var errorCounts = new int[hours];
                
                // Distribution des erreurs sur 24h
                foreach (var evt in _allEvents.Take(50))
                {
                    var hourIndex = random.Next(0, hours);
                    errorCounts[hourIndex]++;
                }

                // Dessiner les barres
                for (int i = 0; i < hours; i++)
                {
                    var count = errorCounts[i];
                    var barHeight = (count / (double)maxErrors) * (height - 40);

                    // Barre
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = barWidth - 4,
                        Height = Math.Max(2, barHeight),
                        Fill = count > 3 ? ErrorBrush : (count > 0 ? WarningBrush : new SolidColorBrush(Color.FromRgb(100, 100, 100)))
                    };

                    System.Windows.Controls.Canvas.SetLeft(rect, 10 + i * barWidth);
                    System.Windows.Controls.Canvas.SetBottom(rect, 20);
                    ChartCanvas.Children.Add(rect);

                    // Label heure (tous les 4h)
                    if (i % 4 == 0)
                    {
                        var label = new TextBlock
                        {
                            Text = $"{i}h",
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200))
                        };

                        System.Windows.Controls.Canvas.SetLeft(label, 10 + i * barWidth);
                        System.Windows.Controls.Canvas.SetBottom(label, 2);
                        ChartCanvas.Children.Add(label);
                    }
                }

                // Ligne de base
                var baseline = new System.Windows.Shapes.Line
                {
                    X1 = 10,
                    Y1 = height - 20,
                    X2 = width - 10,
                    Y2 = height - 20,
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(baseline);
            }
            catch
            {
                // Ignorer les erreurs de dessin
            }
        }

        private void EventListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (EventListView.SelectedItem is EventLogItem selectedEvent)
            {
                // Déterminer quelle action de réparation afficher
                var message = selectedEvent.Message.ToLowerInvariant();
                var source = selectedEvent.Source.ToLowerInvariant();
                var combinedText = $"{message} {source}";

                // Afficher/masquer et configurer l'option "Action de Réparation"
                if (combinedText.Contains("disk") || combinedText.Contains("ntfs") || combinedText.Contains("storage"))
                {
                    RepairActionMenuItem.Visibility = Visibility.Visible;
                    RepairActionMenuItem.Header = "🔧 Lancer CHKDSK";
                    RepairActionMenuItem.Tag = "CHKDSK";
                }
                else if (combinedText.Contains("network") || combinedText.Contains("dns") || combinedText.Contains("tcp") || combinedText.Contains("ip"))
                {
                    RepairActionMenuItem.Visibility = Visibility.Visible;
                    RepairActionMenuItem.Header = "🔧 Réinitialiser l'IP";
                    RepairActionMenuItem.Tag = "RESETIP";
                }
                else
                {
                    RepairActionMenuItem.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Pas d'élément sélectionné, masquer le menu
                e.Handled = true;
            }
        }

        private void AnalyzeWithAI_Click(object sender, RoutedEventArgs e)
        {
            if (EventListView.SelectedItem is EventLogItem selectedEvent)
            {
                var explanation = _errorAnalyzer.GetExplanation(selectedEvent.EventId, selectedEvent.Source);
                
                var analysisMessage = $"🤖 ANALYSE IA DE L'ERREUR\n\n" +
                                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                                    $"📌 {explanation.Title}\n\n" +
                                    $"📝 DESCRIPTION :\n{explanation.Description}\n\n" +
                                    $"💡 SOLUTION RECOMMANDÉE :\n{explanation.Solution}\n\n" +
                                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                                    $"ℹ️ DÉTAILS TECHNIQUES\n" +
                                    $"Event ID : {selectedEvent.EventId}\n" +
                                    $"Source : {selectedEvent.Source}\n" +
                                    $"Niveau : {selectedEvent.Level}\n" +
                                    $"Date : {selectedEvent.TimeCreated}";

                MessageBox.Show(analysisMessage,
                              $"Analyse IA - Événement {selectedEvent.EventId}",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        private void SearchSolution_Click(object sender, RoutedEventArgs e)
        {
            if (EventListView.SelectedItem is EventLogItem selectedEvent)
            {
                try
                {
                    // Construire la requête de recherche Google
                    var searchQuery = $"Windows Event ID {selectedEvent.EventId} {selectedEvent.Source} solution";
                    var encodedQuery = Uri.EscapeDataString(searchQuery);
                    var googleUrl = $"https://www.google.com/search?q={encodedQuery}";

                    // Ouvrir dans le navigateur par défaut
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = googleUrl,
                        UseShellExecute = true
                    });

                    StatusText.Text = "✓ Recherche ouverte dans le navigateur";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Impossible d'ouvrir le navigateur:\n\n{ex.Message}",
                                  "Erreur",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
        }

        private void CopyDetails_Click(object sender, RoutedEventArgs e)
        {
            if (EventListView.SelectedItem is EventLogItem selectedEvent)
            {
                try
                {
                    var details = new StringBuilder();
                    details.AppendLine($"Date/Heure : {selectedEvent.TimeCreated}");
                    details.AppendLine($"Niveau : {selectedEvent.Level}");
                    details.AppendLine($"Event ID : {selectedEvent.EventId}");
                    details.AppendLine($"Source : {selectedEvent.Source}");
                    
                    if (!string.IsNullOrEmpty(selectedEvent.ExplanationTitle))
                    {
                        details.AppendLine($"Explication : {selectedEvent.ExplanationTitle}");
                    }
                    
                    details.AppendLine();
                    details.AppendLine("Message :");
                    details.AppendLine(selectedEvent.Message);

                    Clipboard.SetText(details.ToString());
                    
                    StatusText.Text = "✓ Détails copiés dans le presse-papier";
                    
                    // Notification visuelle temporaire
                    var originalBackground = StatusText.Foreground;
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                    
                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StatusText.Foreground = originalBackground;
                        });
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la copie:\n\n{ex.Message}",
                                  "Erreur",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
        }

        private async void RepairAction_Click(object sender, RoutedEventArgs e)
        {
            if (EventListView.SelectedItem is not EventLogItem selectedEvent)
                return;

            var action = RepairActionMenuItem.Tag?.ToString();

            if (action == "CHKDSK")
            {
                // Lancer CHKDSK
                if (!SystemMaintenanceHelper.IsRunningAsAdministrator())
                {
                    MessageBox.Show("Cette action nécessite des privilèges administrateur.\n\n" +
                                  "Veuillez relancer l'application en tant qu'administrateur.",
                                  "Privilèges requis",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Cette action va planifier une vérification du disque au prochain redémarrage.\n\n" +
                                           $"Erreur concernée:\n" +
                                           $"ID: {selectedEvent.EventId}\n" +
                                           $"Source: {selectedEvent.Source}\n\n" +
                                           $"⚠️ L'analyse complète peut prendre plusieurs heures.\n\n" +
                                           $"Continuer ?",
                                           "Confirmation CHKDSK",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusText.Text = "⏳ Planification de CHKDSK...";
                        var commandResult = await SystemMaintenanceHelper.ScheduleChkdskAsync("C:");

                        if (commandResult.Success || commandResult.Output.Contains("planifié", StringComparison.OrdinalIgnoreCase))
                        {
                            StatusText.Text = "✓ CHKDSK planifié";
                            MessageBox.Show("CHKDSK a été planifié pour le prochain redémarrage.\n\n" +
                                          "Le système analysera et réparera automatiquement le disque.",
                                          "Succès",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Information);
                        }
                        else
                        {
                            StatusText.Text = "⚠ Erreur lors de la planification";
                            MessageBox.Show($"Erreur:\n\n{commandResult.Error}",
                                          "Erreur",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusText.Text = "✗ Erreur CHKDSK";
                        MessageBox.Show($"Erreur:\n\n{ex.Message}",
                                      "Erreur",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Error);
                    }
                }
            }
            else if (action == "RESETIP")
            {
                // Réinitialiser l'IP
                if (!SystemMaintenanceHelper.IsRunningAsAdministrator())
                {
                    MessageBox.Show("Cette action nécessite des privilèges administrateur.\n\n" +
                                  "Veuillez relancer l'application en tant qu'administrateur.",
                                  "Privilèges requis",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Cette action va:\n" +
                                           $"• Libérer l'adresse IP actuelle\n" +
                                           $"• Renouveler l'adresse IP\n" +
                                           $"• Vider le cache DNS\n\n" +
                                           $"Erreur concernée:\n" +
                                           $"ID: {selectedEvent.EventId}\n" +
                                           $"Source: {selectedEvent.Source}\n\n" +
                                           $"Continuer ?",
                                           "Confirmation Réinitialisation IP",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusText.Text = "🌐 Réinitialisation IP...";

                        var releaseResult = await SystemMaintenanceHelper.ExecuteCommandAsync("ipconfig", "/release");
                        await Task.Delay(1000);
                        var renewResult = await SystemMaintenanceHelper.ExecuteCommandAsync("ipconfig", "/renew");
                        var flushResult = await SystemMaintenanceHelper.FlushDnsAsync();

                        var report = new StringBuilder();
                        report.AppendLine("Réinitialisation IP terminée !\n");
                        report.AppendLine($"✓ Libération IP: {(releaseResult.Success ? "OK" : "Échec")}");
                        report.AppendLine($"✓ Renouvellement IP: {(renewResult.Success ? "OK" : "Échec")}");
                        report.AppendLine($"✓ Cache DNS vidé: {(flushResult.Success ? "OK" : "Échec")}");

                        StatusText.Text = "✓ IP réinitialisée";
                        MessageBox.Show(report.ToString(),
                                      "Succès",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        StatusText.Text = "✗ Erreur réinitialisation";
                        MessageBox.Show($"Erreur:\n\n{ex.Message}",
                                      "Erreur",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    public sealed class EventLogItem
    {
        public required string TimeCreated { get; init; }
        public required string Level { get; init; }
        public required SolidColorBrush LevelColor { get; init; }
        public required string LevelGlyph { get; init; }
        public required string Message { get; init; }
        public required string Source { get; init; }
        public int EventId { get; init; }
        public string? ExplanationTitle { get; init; }
        public string? ExplanationShort { get; init; }
        public bool IsNew { get; set; }
        public FontWeight ItemFontWeight => IsNew ? FontWeights.Bold : FontWeights.Normal;
        
        // Propriétés pour les tags
        public TagInfo? Tag { get; init; }
        public bool HasTag => Tag != null;
        public string TagDisplay => Tag != null ? $"[{Tag.Name}]" : string.Empty;
        public SolidColorBrush? TagColor => Tag?.Brush;
    }
}

