using System.Windows;
using Microsoft.Win32;
using MewgenicsSaveGuardian.Localization;
using MewgenicsSaveGuardian.ViewModels;

namespace MewgenicsSaveGuardian;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Instance["DialogTitle"],
            Filter = Loc.Instance["DialogFilter"],
            InitialDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Glaiel Games", "Mewgenics"),
        };

        if (dialog.ShowDialog() == true && DataContext is MainViewModel vm)
        {
            vm.SaveFilePath = dialog.FileName;
        }
    }

    private void OnBrowseExeClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Instance["DialogExeTitle"],
            Filter = Loc.Instance["DialogExeFilter"],
        };

        if (dialog.ShowDialog() == true && DataContext is MainViewModel vm)
        {
            vm.GameExePath = dialog.FileName;
        }
    }
}
