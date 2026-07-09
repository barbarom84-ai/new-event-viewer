using System.Diagnostics;
using System.Text;

namespace EventViewer.Core;

/// <summary>
/// System maintenance commands (USB / elevated builds only).
/// All process starts go through a fixed allowlist — no user-controlled command lines.
/// </summary>
public sealed class SystemMaintenanceHelper
{
    private static readonly HashSet<string> AllowedSettingsUris = new(StringComparer.OrdinalIgnoreCase)
    {
        "ms-settings:network",
        "ms-settings:network-status",
        "ms-settings:network-wifi",
        "ms-settings:storage",
        "ms-settings:storagesense",
        "ms-settings:windowsupdate",
        "windowsdefender://threat"
    };

    private static Task<CommandResult> ExecuteAllowlistedAsync(string fileName, string arguments)
    {
        // Only known Windows binaries with fixed arguments — never interpolate event text here.
        return ExecuteCommandAsync(fileName, arguments);
    }

    private static async Task<CommandResult> ExecuteCommandAsync(string command, string arguments, bool showConsole = false)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = !showConsole
            };

            if (!showConsole)
            {
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
                processInfo.StandardOutputEncoding = Encoding.UTF8;
                processInfo.StandardErrorEncoding = Encoding.UTF8;
            }

            var output = new StringBuilder();
            var error = new StringBuilder();

            using var process = new Process { StartInfo = processInfo };

            if (!showConsole)
            {
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
            }

            process.Start();
            if (!showConsole)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

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
                Error = Loc.T("Command.Exception", ex.Message)
            };
        }
    }

    /// <summary>
    /// Runs SFC in a branded visible console so the user can follow progress (10–30 min).
    /// </summary>
    public static async Task<CommandResult> RunSystemFileCheckerAsync()
    {
        try
        {
            var appTitle = SanitizeConsoleTitle(Loc.T("App.Title"));
            var windowTitle = $"{appTitle} — SFC /scannow";
            // color 0A = black background + light green (matches dark/forest accent).
            var args =
                "/c color 0A & " +
                $"title {windowTitle} & " +
                "cls & " +
                $"echo  {appTitle} & " +
                "echo  -------------------------------- & " +
                "echo  SFC /scannow & " +
                "echo. & " +
                "sfc /scannow";

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            });

            if (process == null)
            {
                return new CommandResult
                {
                    Success = false,
                    ExitCode = -1,
                    Error = Loc.T("Command.OpenFailed", "sfc /scannow")
                };
            }

            await process.WaitForExitAsync();
            return new CommandResult
            {
                Success = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                Output = string.Empty,
                Error = process.ExitCode == 0
                    ? string.Empty
                    : Loc.T("Command.FailedWithCode", "SFC", process.ExitCode)
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                ExitCode = -1,
                Error = Loc.T("Command.Exception", ex.Message)
            };
        }
    }

    private static string SanitizeConsoleTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "WinBeacon";
        }

        // Strip characters that break cmd.exe "title" / echo parsing.
        var cleaned = value
            .Replace("&", " ")
            .Replace("|", " ")
            .Replace(">", " ")
            .Replace("<", " ")
            .Replace("^", " ")
            .Replace("\"", "'")
            .Trim();

        return string.IsNullOrWhiteSpace(cleaned) ? "WinBeacon" : cleaned;
    }

    public static Task<CommandResult> FlushDnsAsync()
        => ExecuteAllowlistedAsync("ipconfig", "/flushdns");

    public static Task<CommandResult> OpenDiskCleanupAsync()
        => OpenGuiToolAsync("cleanmgr.exe", string.Empty);

    public static Task<CommandResult> OpenReliabilityHistoryAsync()
        => OpenGuiToolAsync("perfmon.exe", "/rel");

    public static Task<CommandResult> OpenDeviceManagerAsync()
        => OpenGuiToolAsync("mmc.exe", "devmgmt.msc");

    private static Task<CommandResult> OpenGuiToolAsync(string fileName, string arguments)
    {
        try
        {
            var started = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true
            });

            return Task.FromResult(new CommandResult
            {
                Success = started != null,
                ExitCode = started != null ? 0 : -1,
                Output = started != null ? Loc.T("Command.Opening", fileName) : string.Empty,
                Error = started == null ? Loc.T("Command.OpenFailed", fileName) : string.Empty
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CommandResult
            {
                Success = false,
                ExitCode = -1,
                Error = Loc.T("Command.Exception", ex.Message)
            });
        }
    }

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
        if (string.IsNullOrWhiteSpace(uri) || !AllowedSettingsUris.Contains(uri.Trim()))
        {
            return Task.FromResult(new CommandResult
            {
                Success = false,
                ExitCode = -1,
                Error = Loc.T("Command.UriNotAllowed")
            });
        }

        try
        {
            var started = Process.Start(new ProcessStartInfo
            {
                FileName = uri.Trim(),
                UseShellExecute = true
            });

            return Task.FromResult(new CommandResult
            {
                Success = started != null,
                ExitCode = started != null ? 0 : -1,
                Output = started != null ? Loc.T("Command.Opening", uri) : string.Empty,
                Error = started == null ? Loc.T("Command.OpenFailed", uri) : string.Empty
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
        var winsock = await ExecuteAllowlistedAsync("netsh", "winsock reset");
        if (!winsock.Success)
        {
            return winsock;
        }

        var tcp = await ExecuteAllowlistedAsync("netsh", "int ip reset");
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
            result.AppendLine(Loc.T("Command.OutputHeader"));
            result.AppendLine(Output);
        }

        if (!string.IsNullOrWhiteSpace(Error))
        {
            result.AppendLine();
            result.AppendLine(Loc.T("Command.ErrorHeader"));
            result.AppendLine(Error);
        }

        return result.ToString();
    }

    public string GetUserSummary(string actionLabel)
    {
        if (Success)
        {
            return Loc.T("Command.Success", actionLabel);
        }

        var detail = string.IsNullOrWhiteSpace(Error) ? Output : Error;
        detail = detail.Trim();
        if (detail.Length > 200)
        {
            detail = detail[..200] + "…";
        }

        return string.IsNullOrWhiteSpace(detail)
            ? Loc.T("Command.FailedWithCode", actionLabel, ExitCode)
            : Loc.T("Action.Failed", actionLabel, detail);
    }
}
