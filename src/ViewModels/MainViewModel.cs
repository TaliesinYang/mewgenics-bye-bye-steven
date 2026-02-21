using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MewgenicsSaveGuardian.Localization;
using MewgenicsSaveGuardian.Models;
using MewgenicsSaveGuardian.Services;

namespace MewgenicsSaveGuardian.ViewModels;

public record LanguageOption(string Code, string Name);

public partial class MainViewModel : ObservableObject
{
    private readonly SaveFileService _saveFileService = new();
    private readonly BackupService _backupService = new();
    private readonly ProcessService _processService = new();
    private readonly SettingsService _settingsService = new();
    private readonly SpeedHackService _speedHackService = new();
    private readonly DispatcherTimer _pollTimer;

    [ObservableProperty] private string _saveFilePath = string.Empty;
    [ObservableProperty] private bool _isGameRunning;
    [ObservableProperty] private int _saveScumLocation;
    [ObservableProperty] private bool _stevenMet;
    [ObservableProperty] private int _stevenStrikes;
    [ObservableProperty] private int _currentDay;
    [ObservableProperty] private bool _onAdventure;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _autoRelaunchGame;
    [ObservableProperty] private string _gameExePath = string.Empty;
    [ObservableProperty] private int _maxBackups = 5;
    [ObservableProperty] private BackupEntry? _selectedBackup;
    [ObservableProperty] private LanguageOption _selectedLanguage;
    [ObservableProperty] private bool _isPathsExpanded = true;
    [ObservableProperty] private bool _speedEnabled;
    [ObservableProperty] private double _speedMultiplier = 1.0;

    public ObservableCollection<BackupEntry> Backups { get; } = [];

    public List<LanguageOption> AvailableLanguages { get; } =
        Loc.SupportedLanguages
            .Select(c => new LanguageOption(c, Loc.LanguageDisplayNames[c]))
            .ToList();

    public bool IsPenaltyActive => SaveScumLocation == 1;
    public bool HasSaveFile => !string.IsNullOrEmpty(SaveFilePath) && File.Exists(SaveFilePath);

    public MainViewModel()
    {
        var settings = _settingsService.Load();

        // Initialize language first (before other properties trigger SaveSettings)
        var langCode = settings.Language;
        if (!Loc.SupportedLanguages.Contains(langCode))
            langCode = "en";
        Loc.Instance.Language = langCode;
        _selectedLanguage = AvailableLanguages.First(l => l.Code == langCode);

        SaveFilePath = settings.SaveFilePath;
        MaxBackups = settings.MaxBackups;
        AutoRelaunchGame = settings.AutoRelaunchGame;
        GameExePath = settings.GameExePath;
        SpeedEnabled = settings.SpeedEnabled;
        SpeedMultiplier = settings.SpeedMultiplier > 0 ? settings.SpeedMultiplier : 1.0;

        StatusMessage = Loc.Instance["StatusReady"];

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += (_, _) => PollStatus();
        _pollTimer.Start();

        if (HasSaveFile)
        {
            RefreshStatus();
            RefreshBackups();
        }

        IsPathsExpanded = !HasSaveFile;
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        Loc.Instance.Language = value.Code;
        StatusMessage = Loc.Instance["StatusReady"];
        SaveSettings();
    }

    partial void OnSaveFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(HasSaveFile));
        SaveSettings();
        if (HasSaveFile)
        {
            RefreshStatus();
            RefreshBackups();
        }
    }

    partial void OnSaveScumLocationChanged(int value)
    {
        OnPropertyChanged(nameof(IsPenaltyActive));
        RemovePenaltyCommand.NotifyCanExecuteChanged();
        RestartAndClearCommand.NotifyCanExecuteChanged();
    }

    partial void OnMaxBackupsChanged(int value) => SaveSettings();
    partial void OnAutoRelaunchGameChanged(bool value) => SaveSettings();
    partial void OnGameExePathChanged(string value) => SaveSettings();

    partial void OnSpeedEnabledChanged(bool value)
    {
        SaveSettings();
        if (value)
            TryInjectSpeedHack();
        else
        {
            _speedHackService.SetSpeed(1.0);
            StatusMessage = Loc.Instance["StatusSpeedDisabled"];
        }
    }

    partial void OnSpeedMultiplierChanged(double value)
    {
        SaveSettings();
        if (SpeedEnabled)
            _speedHackService.SetSpeed(value);
    }

    [RelayCommand]
    private void TogglePathsExpanded() => IsPathsExpanded = !IsPathsExpanded;

    [RelayCommand]
    private void ApplySpeed()
    {
        if (SpeedEnabled)
            TryInjectSpeedHack();
    }

    [RelayCommand]
    private void AutoFindPath()
    {
        var paths = SettingsService.FindAllSavePaths();
        if (paths.Count == 1)
        {
            SaveFilePath = paths[0];
            StatusMessage = Loc.Instance["StatusFoundAuto"];
        }
        else if (paths.Count > 1)
        {
            SaveFilePath = paths[0];
            StatusMessage = Loc.Instance["StatusFoundMultiple"];
        }
        else
        {
            StatusMessage = Loc.Instance["StatusNotFound"];
        }
    }

    [RelayCommand]
    private void AutoDetectGameExe()
    {
        var path = _processService.DetectGamePath();
        if (path != null)
        {
            GameExePath = path;
            StatusMessage = Loc.Instance["StatusExeDetected"];
        }
        else
        {
            StatusMessage = Loc.Instance["StatusExeNotDetected"];
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemovePenalty))]
    private async Task RemovePenaltyAsync()
    {
        if (IsGameRunning)
        {
            var result = MessageBox.Show(
                Loc.Instance["MsgGameRunningClose"],
                Loc.Instance["MsgGameRunningTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsBusy = true;
                StatusMessage = Loc.Instance["StatusClosingGame"];
                var closed = await Task.Run(() => _processService.CloseGame());
                if (!closed)
                {
                    StatusMessage = Loc.Instance["StatusCloseGameFailed"];
                    IsBusy = false;
                    return;
                }
                await Task.Delay(1000);
            }
            else
            {
                return;
            }
        }

        IsBusy = true;
        StatusMessage = Loc.Instance["StatusCreatingBackup"];

        try
        {
            await Task.Run(() =>
            {
                _backupService.CreateBackup(SaveFilePath);
                _backupService.RotateBackups(SaveFilePath, MaxBackups);
            });

            StatusMessage = Loc.Instance["StatusRemovingPenalty"];
            await Task.Run(() =>
            {
                _saveFileService.ResetPenalty(SaveFilePath, false);

                if (!_saveFileService.VerifyIntegrity(SaveFilePath))
                    throw new InvalidOperationException("Database integrity check failed after modification.");
            });

            RefreshStatus();
            RefreshBackups();
            StatusMessage = Loc.Instance["StatusPenaltyRemoved"];

            if (AutoRelaunchGame)
            {
                _processService.LaunchGame(GameExePath);
                StatusMessage = Loc.Instance["StatusPenaltyRemovedRelaunch"];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySave))]
    private async Task RestartAndClearAsync()
    {
        IsBusy = true;

        try
        {
            if (IsGameRunning)
            {
                StatusMessage = Loc.Instance["StatusClosingGame"];
                var closed = await Task.Run(() => _processService.CloseGame());
                if (!closed)
                {
                    StatusMessage = Loc.Instance["StatusCloseGameFailed"];
                    return;
                }
                await Task.Delay(1000);
            }

            StatusMessage = Loc.Instance["StatusCreatingBackup"];
            await Task.Run(() =>
            {
                _backupService.CreateBackup(SaveFilePath);
                _backupService.RotateBackups(SaveFilePath, MaxBackups);
            });

            StatusMessage = Loc.Instance["StatusRemovingPenalty"];
            await Task.Run(() =>
            {
                _saveFileService.ResetPenalty(SaveFilePath, false);

                if (!_saveFileService.VerifyIntegrity(SaveFilePath))
                    throw new InvalidOperationException("Database integrity check failed after modification.");
            });

            RefreshStatus();
            RefreshBackups();

            _processService.LaunchGame();
            StatusMessage = Loc.Instance["StatusPenaltyRemovedRelaunch"];
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void LaunchGame()
    {
        _processService.LaunchGame(GameExePath);
        StatusMessage = string.IsNullOrEmpty(GameExePath)
            ? Loc.Instance["StatusLaunchingGame"]
            : Loc.Instance["StatusLaunchingDirect"];
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!HasSaveFile) return;

        IsBusy = true;
        StatusMessage = Loc.Instance["StatusCreatingBackup"];
        try
        {
            await Task.Run(() =>
            {
                _backupService.CreateBackup(SaveFilePath);
                _backupService.RotateBackups(SaveFilePath, MaxBackups);
            });
            RefreshBackups();
            StatusMessage = Loc.Instance["StatusBackupCreated"];
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (SelectedBackup is null) return;

        if (IsGameRunning)
        {
            MessageBox.Show(
                Loc.Instance["MsgCloseBeforeRestore"],
                Loc.Instance["MsgGameRunningTitle"],
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            string.Format(Loc.Instance["MsgConfirmRestore"],
                SelectedBackup.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
            Loc.Instance["MsgConfirmRestoreTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        StatusMessage = Loc.Instance["StatusRestoringBackup"];
        try
        {
            await Task.Run(() => _backupService.RestoreBackup(SaveFilePath, SelectedBackup));
            RefreshStatus();
            RefreshBackups();
            StatusMessage = Loc.Instance["StatusBackupRestored"];
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRemovePenalty() => HasSaveFile && !IsBusy && IsPenaltyActive;
    private bool CanModifySave() => HasSaveFile && !IsBusy;

    private void TryInjectSpeedHack()
    {
        var dllPath = _speedHackService.GetDllPath();
        if (string.IsNullOrEmpty(dllPath))
        {
            StatusMessage = Loc.Instance["StatusSpeedDllNotFound"];
            return;
        }

        var process = _processService.GetGameProcess();
        if (process == null)
        {
            _speedHackService.EnsureSharedMemory(SpeedMultiplier);
            return;
        }

        if (!_speedHackService.IsInjected(process.Id))
        {
            _speedHackService.EnsureSharedMemory(SpeedMultiplier);
            if (!_speedHackService.InjectDll(process.Id, dllPath))
            {
                StatusMessage = Loc.Instance["StatusSpeedInjectionFailed"];
                return;
            }
        }

        _speedHackService.SetSpeed(SpeedMultiplier);
        StatusMessage = string.Format(Loc.Instance["StatusSpeedApplied"], SpeedMultiplier);
    }

    private void PollStatus()
    {
        var wasRunning = IsGameRunning;
        IsGameRunning = _processService.IsGameRunning();

        if (wasRunning && !IsGameRunning && HasSaveFile)
        {
            RefreshStatus();
        }

        if (!wasRunning && IsGameRunning && SpeedEnabled)
        {
            TryInjectSpeedHack();
        }

        RemovePenaltyCommand.NotifyCanExecuteChanged();
        RestartAndClearCommand.NotifyCanExecuteChanged();
    }

    private void RefreshStatus()
    {
        if (!HasSaveFile) return;

        try
        {
            var info = _saveFileService.ReadStatus(SaveFilePath);
            SaveScumLocation = info.SaveScumLocation;
            StevenMet = info.StevenMet;
            StevenStrikes = info.StevenStrikes;
            CurrentDay = info.CurrentDay;
            OnAdventure = info.OnAdventure;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void RefreshBackups()
    {
        if (!HasSaveFile) return;

        Backups.Clear();
        foreach (var b in _backupService.GetBackups(SaveFilePath))
            Backups.Add(b);
    }

    private void SaveSettings()
    {
        _settingsService.Save(new AppSettings
        {
            SaveFilePath = SaveFilePath,
            MaxBackups = MaxBackups,
            AutoRelaunchGame = AutoRelaunchGame,
            GameExePath = GameExePath,
            Language = SelectedLanguage.Code,
            SpeedEnabled = SpeedEnabled,
            SpeedMultiplier = SpeedMultiplier,
        });
    }
}
