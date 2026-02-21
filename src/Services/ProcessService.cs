using System.Diagnostics;

namespace MewgenicsSaveGuardian.Services;

public class ProcessService
{
    private const string GameProcessName = "Mewgenics";
    private const string SteamGameUrl = "steam://rungameid/686060";
    private const int GracefulCloseTimeoutMs = 10_000;

    public bool IsGameRunning()
    {
        return GetGameProcess() is not null;
    }

    public Process? GetGameProcess()
    {
        return Process.GetProcessesByName(GameProcessName).FirstOrDefault();
    }

    public bool CloseGame()
    {
        var process = GetGameProcess();
        if (process is null)
            return true;

        try
        {
            process.CloseMainWindow();
            if (process.WaitForExit(GracefulCloseTimeoutMs))
                return true;

            process.Kill();
            return process.WaitForExit(5_000);
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    /// <summary>
    /// Launch game via exe path if available, otherwise via Steam.
    /// </summary>
    public void LaunchGame(string? exePath = null)
    {
        if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(exePath) ?? "",
            });
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = SteamGameUrl,
                UseShellExecute = true,
            });
        }
    }

    /// <summary>
    /// Try to detect game exe path from the running process.
    /// </summary>
    public string? DetectGamePath()
    {
        var process = GetGameProcess();
        if (process is null)
            return null;

        try
        {
            var path = process.MainModule?.FileName;
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                return path;
        }
        catch
        {
            // Access denied or process exited
        }

        return null;
    }
}
