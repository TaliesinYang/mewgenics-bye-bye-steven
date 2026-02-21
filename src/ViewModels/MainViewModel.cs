using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MewgenicsSaveGuardian.Models;
using MewgenicsSaveGuardian.Services;

namespace MewgenicsSaveGuardian.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SaveFileService _saveFileService = new();
    private readonly BackupService _backupService = new();
    private readonly ProcessService _processService = new();
    private readonly SettingsService _settingsService = new();
    private readonly DispatcherTimer _pollTimer;

    [ObservableProperty] private string _saveFilePath = string.Empty;
    [ObservableProperty] private bool _isGameRunning;
    [ObservableProperty] private int _saveScumLocation;
    [ObservableProperty] private bool _stevenMet;
    [ObservableProperty] private int _stevenStrikes;
    [ObservableProperty] private int _currentDay;
    [ObservableProperty] private bool _onAdventure;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _clearStevenHistory;
    [ObservableProperty] private bool _autoRelaunchGame;
    [ObservableProperty] private int _maxBackups = 5;
    [ObservableProperty] private BackupEntry? _selectedBackup;

    public ObservableCollection<BackupEntry> Backups { get; } = [];

    public bool IsPenaltyActive => SaveScumLocation == 1;
    public bool HasSaveFile => !string.IsNullOrEmpty(SaveFilePath) && File.Exists(SaveFilePath);

    public MainViewModel()
    {
        var settings = _settingsService.Load();
        SaveFilePath = settings.SaveFilePath;
        MaxBackups = settings.MaxBackups;
        ClearStevenHistory = settings.ClearStevenHistory;
        AutoRelaunchGame = settings.AutoRelaunchGame;

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += (_, _) => PollStatus();
        _pollTimer.Start();

        if (HasSaveFile)
        {
            RefreshStatus();
            RefreshBackups();
        }
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
    }

    partial void OnMaxBackupsChanged(int value) => SaveSettings();
    partial void OnClearStevenHistoryChanged(bool value) => SaveSettings();
    partial void OnAutoRelaunchGameChanged(bool value) => SaveSettings();

    [RelayCommand]
    private void AutoFindPath()
    {
        var paths = SettingsService.FindAllSavePaths();
        if (paths.Count == 1)
        {
            SaveFilePath = paths[0];
            StatusMessage = "Save file found automatically.";
        }
        else if (paths.Count > 1)
        {
            SaveFilePath = paths[0];
            StatusMessage = $"Found {paths.Count} save files. Using first one.";
        }
        else
        {
            StatusMessage = "No save files found. Please browse manually.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySave))]
    private async Task RemovePenaltyAsync()
    {
        if (IsGameRunning)
        {
            var result = MessageBox.Show(
                "Mewgenics is running. Close it first?",
                "Game Running",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsBusy = true;
                StatusMessage = "Closing game...";
                var closed = await Task.Run(() => _processService.CloseGame());
                if (!closed)
                {
                    StatusMessage = "Failed to close game. Please close it manually.";
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
        StatusMessage = "Creating backup...";

        try
        {
            await Task.Run(() =>
            {
                _backupService.CreateBackup(SaveFilePath);
                _backupService.RotateBackups(SaveFilePath, MaxBackups);
            });

            StatusMessage = "Removing penalty...";
            await Task.Run(() =>
            {
                _saveFileService.ResetPenalty(SaveFilePath, ClearStevenHistory);

                if (!_saveFileService.VerifyIntegrity(SaveFilePath))
                    throw new InvalidOperationException("Database integrity check failed after modification.");
            });

            RefreshStatus();
            RefreshBackups();
            StatusMessage = "Penalty removed successfully!";

            if (AutoRelaunchGame)
            {
                _processService.LaunchGame();
                StatusMessage = "Penalty removed. Game relaunching...";
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

    [RelayCommand]
    private void LaunchGame()
    {
        _processService.LaunchGame();
        StatusMessage = "Launching game via Steam...";
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!HasSaveFile) return;

        IsBusy = true;
        StatusMessage = "Creating backup...";
        try
        {
            await Task.Run(() =>
            {
                _backupService.CreateBackup(SaveFilePath);
                _backupService.RotateBackups(SaveFilePath, MaxBackups);
            });
            RefreshBackups();
            StatusMessage = "Backup created.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup error: {ex.Message}";
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
            MessageBox.Show("Please close Mewgenics before restoring.", "Game Running",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Restore backup from {SelectedBackup.Timestamp:yyyy-MM-dd HH:mm:ss}?\nA safety backup will be created first.",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        StatusMessage = "Restoring backup...";
        try
        {
            await Task.Run(() => _backupService.RestoreBackup(SaveFilePath, SelectedBackup));
            RefreshStatus();
            RefreshBackups();
            StatusMessage = "Backup restored successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanModifySave() => HasSaveFile && !IsBusy;

    private void PollStatus()
    {
        var wasRunning = IsGameRunning;
        IsGameRunning = _processService.IsGameRunning();

        if (wasRunning && !IsGameRunning && HasSaveFile)
        {
            RefreshStatus();
        }

        RemovePenaltyCommand.NotifyCanExecuteChanged();
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
            StatusMessage = $"Read error: {ex.Message}";
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
            ClearStevenHistory = ClearStevenHistory,
            AutoRelaunchGame = AutoRelaunchGame,
        });
    }
}
