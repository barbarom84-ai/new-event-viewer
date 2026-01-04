using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace EventViewer
{
    /// <summary>
    /// Gère les opérations de maintenance système et la vérification des privilèges.
    /// </summary>
    public sealed class SystemMaintenanceHelper
    {
        /// <summary>
        /// Vérifie si l'application s'exécute avec des privilèges administrateur.
        /// </summary>
        public static bool IsRunningAsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Exécute une commande CMD en arrière-plan et récupère le résultat.
        /// </summary>
        public static async Task<CommandResult> ExecuteCommandAsync(string command, string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                var output = new StringBuilder();
                var error = new StringBuilder();

                using var process = new Process { StartInfo = processInfo };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        error.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                return new CommandResult
                {
                    Success = process.ExitCode == 0,
                    ExitCode = process.ExitCode,
                    Output = output.ToString(),
                    Error = error.ToString()
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    ExitCode = -1,
                    Output = string.Empty,
                    Error = $"Exception lors de l'exécution: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Lance le System File Checker pour réparer les fichiers système corrompus.
        /// </summary>
        public static async Task<CommandResult> RunSystemFileCheckerAsync()
        {
            return await ExecuteCommandAsync("cmd.exe", "/c sfc /scannow");
        }

        /// <summary>
        /// Vide le cache DNS pour résoudre les problèmes de résolution de noms.
        /// </summary>
        public static async Task<CommandResult> FlushDnsAsync()
        {
            return await ExecuteCommandAsync("ipconfig", "/flushdns");
        }

        /// <summary>
        /// Nettoie le cache DNS (alias pour FlushDnsAsync).
        /// </summary>
        public static async Task<CommandResult> ClearDnsCacheAsync()
        {
            return await FlushDnsAsync();
        }

        /// <summary>
        /// Redémarre le service DNS Client.
        /// </summary>
        public static async Task<CommandResult> RestartDnsClientAsync()
        {
            var stopResult = await ExecuteCommandAsync("net", "stop Dnscache");
            if (!stopResult.Success)
                return stopResult;

            await Task.Delay(1000); // Attendre 1 seconde

            return await ExecuteCommandAsync("net", "start Dnscache");
        }

        /// <summary>
        /// Exécute CHKDSK sur un volume spécifique (nécessite un redémarrage).
        /// </summary>
        public static async Task<CommandResult> ScheduleChkdskAsync(string drive = "C:")
        {
            return await ExecuteCommandAsync("cmd.exe", $"/c echo Y | chkdsk {drive} /f /r");
        }

        /// <summary>
        /// Nettoie les fichiers temporaires Windows.
        /// </summary>
        public static async Task<CommandResult> CleanTempFilesAsync()
        {
            return await ExecuteCommandAsync("cmd.exe", "/c del /q /f /s %TEMP%\\* 2>nul");
        }

        /// <summary>
        /// Réinitialise le catalogue Winsock pour les problèmes réseau.
        /// </summary>
        public static async Task<CommandResult> ResetWinsockAsync()
        {
            return await ExecuteCommandAsync("netsh", "winsock reset");
        }

        /// <summary>
        /// Réinitialise la pile TCP/IP.
        /// </summary>
        public static async Task<CommandResult> ResetTcpIpAsync()
        {
            return await ExecuteCommandAsync("netsh", "int ip reset");
        }
    }

    /// <summary>
    /// Représente le résultat d'une commande système.
    /// </summary>
    public sealed class CommandResult
    {
        public bool Success { get; init; }
        public int ExitCode { get; init; }
        public string Output { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;

        public string GetFullOutput()
        {
            var result = new StringBuilder();
            
            if (!string.IsNullOrWhiteSpace(Output))
            {
                result.AppendLine("=== SORTIE ===");
                result.AppendLine(Output);
            }

            if (!string.IsNullOrWhiteSpace(Error))
            {
                result.AppendLine();
                result.AppendLine("=== ERREURS ===");
                result.AppendLine(Error);
            }

            return result.ToString();
        }
    }
}

