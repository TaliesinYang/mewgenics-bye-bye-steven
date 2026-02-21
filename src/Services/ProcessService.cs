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
            // Process already exited
            return true;
        }
    }

    public void LaunchGame()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = SteamGameUrl,
            UseShellExecute = true,
        });
    }
}
