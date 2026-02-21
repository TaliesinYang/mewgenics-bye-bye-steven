using System.Windows;
using Microsoft.Win32;
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
            Title = "Select Mewgenics Save File",
            Filter = "Save files (*.sav)|*.sav|All files (*.*)|*.*",
            InitialDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Glaiel Games", "Mewgenics"),
        };

        if (dialog.ShowDialog() == true && DataContext is MainViewModel vm)
        {
            vm.SaveFilePath = dialog.FileName;
        }
    }
}
