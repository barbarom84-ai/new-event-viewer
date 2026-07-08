using System.Diagnostics;
using System.Text;

namespace EventViewer.Core;

/// <summary>
/// System maintenance commands (USB / elevated builds only).
/// </summary>
public sealed class SystemMaintenanceHelper
{
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

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(e.Data);
                }
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

    public static Task<CommandResult> RunSystemFileCheckerAsync()
        => ExecuteCommandAsync("cmd.exe", "/c sfc /scannow");

    public static Task<CommandResult> FlushDnsAsync()
        => ExecuteCommandAsync("ipconfig", "/flushdns");

    public static Task<CommandResult> OpenDiskCleanupAsync()
        => ExecuteCommandAsync("cleanmgr.exe", string.Empty);

    public static Task<CommandResult> RunDefenderQuickScanAsync()
    {
        var defender = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Windows Defender",
            "MpCmdRun.exe");

        if (File.Exists(defender))
        {
            return ExecuteCommandAsync(defender, "-Scan -ScanType 1");
        }

        return OpenUriAsync("windowsdefender://threat");
    }

    public static Task<CommandResult> OpenUriAsync(string uri)
    {
        try
        {
            var started = Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });

            return Task.FromResult(new CommandResult
            {
                Success = started != null,
                ExitCode = started != null ? 0 : -1,
                Output = started != null ? $"Ouverture : {uri}" : string.Empty,
                Error = started == null ? $"Impossible d'ouvrir {uri}" : string.Empty
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CommandResult
            {
                Success = false,
                ExitCode = -1,
                Error = ex.Message
            });
        }
    }

    public static async Task<CommandResult> ResetNetworkAsync()
    {
        var winsock = await ExecuteCommandAsync("netsh", "winsock reset");
        if (!winsock.Success)
        {
            return winsock;
        }

        var tcp = await ExecuteCommandAsync("netsh", "int ip reset");
        return new CommandResult
        {
            Success = tcp.Success,
            ExitCode = tcp.ExitCode,
            Output = winsock.Output + Environment.NewLine + tcp.Output,
            Error = string.Join(Environment.NewLine, new[] { winsock.Error, tcp.Error }.Where(s => !string.IsNullOrWhiteSpace(s)))
        };
    }
}

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

    public string GetUserSummary(string actionLabel)
    {
        if (Success)
        {
            return $"{actionLabel} terminé avec succès.";
        }

        var detail = string.IsNullOrWhiteSpace(Error) ? Output : Error;
        detail = detail.Trim();
        if (detail.Length > 200)
        {
            detail = detail[..200] + "…";
        }

        return string.IsNullOrWhiteSpace(detail)
            ? $"{actionLabel} a échoué (code {ExitCode})."
            : $"{actionLabel} a échoué : {detail}";
    }
}
