using AlgorithmsLab4.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AlgorithmsLab4.App.Views;

/// <summary>
/// Задание 3: обработчик выбора текстового файла. Остальная логика находится
/// во ViewModel, чтобы её можно было тестировать отдельно от интерфейса.
/// </summary>
public partial class WordLabView : UserControl
{
    public WordLabView()
    {
        InitializeComponent();
    }

    private async void OpenTextFile_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WordLabViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть текстовый файл",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Текстовые файлы")
                {
                    Patterns = ["*.txt", "*.md", "*.csv", "*.log"],
                    MimeTypes = ["text/plain"],
                },
                FilePickerFileTypes.All,
            ],
        });

        if (files.Count == 0)
        {
            return;
        }

        var path = files[0].Path.LocalPath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            await viewModel.LoadTextAsync(path);
        }
    }
}
