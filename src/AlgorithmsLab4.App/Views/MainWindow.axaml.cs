using AlgorithmsLab4.App.ViewModels;
using Avalonia.Controls;

namespace AlgorithmsLab4.App.Views;

/// <summary>Главное окно: три вкладки заданий и переключение темы.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => (DataContext as MainWindowViewModel)?.StopAll();
    }
}
