using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EdoXmlGenerator.Services;

namespace EdoXmlGenerator.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private string _xml;

    public ICommand CopyCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand BackCommand { get; }

    public PreviewViewModel(string xml, ICommand backCommand, IDialogService? dialogs = null)
    {
        _xml = xml;
        BackCommand = backCommand;
        _dialogs = dialogs ?? new DialogService();

        CopyCommand = new RelayCommand(Copy);
        SaveCommand = new RelayCommand(Save);
    }

    private void Copy()
    {
        try
        {
            System.Windows.Clipboard.SetText(Xml);
            _dialogs.ShowInfo("XML скопирован в буфер обмена.");
        }
        catch
        {
            _dialogs.ShowError("Не удалось скопировать текст.");
        }
    }

    private void Save()
    {
        var defaultFileName = GetSuggestedFileName(Xml);
        var file = _dialogs.SaveFileDialog("XML-файлы (*.xml)|*.xml|Все файлы (*.*)|*.*", "Сохранить XML-документ", defaultFileName);
        if (file == null) return;

        try
        {
            System.IO.File.WriteAllText(file, Xml, new System.Text.UTF8Encoding(false));
            _dialogs.ShowInfo($"Документ успешно сохранён:\n{file}");
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Не удалось сохранить файл: {ex.Message}");
        }
    }

    private static string GetSuggestedFileName(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return "документ.xml";

        // Для официальных документов ФНС извлекаем регламентный ИдФайл
        var match = System.Text.RegularExpressions.Regex.Match(xml, @"ИдФайл=""([^""]+)""");
        if (match.Success)
            return match.Groups[1].Value + ".xml";

        // Для каталожных документов формируем имя: <КорневойЭлемент>_<Номер>.xml
        var rootMatch = System.Text.RegularExpressions.Regex.Match(xml, @"<([A-Za-zА-Яа-я0-9_\-]+)");
        var numMatch = System.Text.RegularExpressions.Regex.Match(xml, @"Номер=""([^""]+)""|НомерДок=""([^""]+)""");
        if (rootMatch.Success && !rootMatch.Groups[1].Value.StartsWith("?"))
        {
            var root = rootMatch.Groups[1].Value;
            var num = numMatch.Success ? (numMatch.Groups[1].Success ? numMatch.Groups[1].Value : numMatch.Groups[2].Value) : "";
            if (!string.IsNullOrWhiteSpace(num))
            {
                var cleanNum = System.Text.RegularExpressions.Regex.Replace(num, @"[\\/:*?""<>|]", "_");
                return $"{root}_{cleanNum}.xml";
            }
            return $"{root}.xml";
        }

        return "документ.xml";
    }
}
