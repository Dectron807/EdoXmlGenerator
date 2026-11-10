using Microsoft.Win32;

namespace EdoXmlGenerator.Services;

public interface IDialogService
{
    string? OpenFileDialog(string filter, string title);
    string? SaveFileDialog(string filter, string title, string defaultFileName);
    void ShowInfo(string message, string title = "Информация");
    void ShowError(string message, string title = "Ошибка");
    bool Confirm(string message, string title = "Подтверждение");
}

public class DialogService : IDialogService
{
    public string? OpenFileDialog(string filter, string title)
    {
        var dlg = new OpenFileDialog { Filter = filter, Title = title };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? SaveFileDialog(string filter, string title, string defaultFileName)
    {
        var dlg = new SaveFileDialog { Filter = filter, Title = title, FileName = defaultFileName };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public void ShowInfo(string message, string title = "Информация")
        => System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

    public void ShowError(string message, string title = "Ошибка")
        => System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

    public bool Confirm(string message, string title = "Подтверждение")
        => System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
}
