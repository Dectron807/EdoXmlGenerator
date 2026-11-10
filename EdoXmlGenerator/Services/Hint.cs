using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EdoXmlGenerator.Services;

/// <summary>
/// Присоединённые подсказки и inline-валидация полей ввода (УПД и др.).
/// Подсказка-пример показывается поверх пустого поля; при вводе данных
/// формат проверяется по маске (ValidateAs), и под полем появляется красная
/// пометка об ошибке — как в каталоге документов.
/// </summary>
public static class Hint
{
    public static readonly DependencyProperty AttachedHintProperty = DependencyProperty.RegisterAttached(
        "AttachedHint", typeof(string), typeof(Hint),
        new FrameworkPropertyMetadata(string.Empty));

    public static string GetAttachedHint(DependencyObject obj) =>
        (string)obj.GetValue(AttachedHintProperty);

    public static void SetAttachedHint(DependencyObject obj, string value) =>
        obj.SetValue(AttachedHintProperty, value);

    /// <summary>Маска проверки формата: inn, kpp, okpo, bik, account, corr, bnof, index, phone, email, ...</summary>
    public static readonly DependencyProperty ValidateAsProperty = DependencyProperty.RegisterAttached(
        "ValidateAs", typeof(string), typeof(Hint),
        new FrameworkPropertyMetadata(string.Empty, OnValidateAsChanged));

    public static string GetValidateAs(DependencyObject obj) =>
        (string)obj.GetValue(ValidateAsProperty);

    public static void SetValidateAs(DependencyObject obj, string value) =>
        obj.SetValue(ValidateAsProperty, value);

    /// <summary>Текст ошибки формата (задаёт поведение; пусто — корректно).</summary>
    public static readonly DependencyProperty ErrorProperty = DependencyProperty.RegisterAttached(
        "Error", typeof(string), typeof(Hint),
        new FrameworkPropertyMetadata(string.Empty));

    public static string GetError(DependencyObject obj) =>
        (string)obj.GetValue(ErrorProperty);

    public static void SetError(DependencyObject obj, string value) =>
        obj.SetValue(ErrorProperty, value);

    private static void OnValidateAsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox box)
        {
            box.TextChanged -= OnTextChanged;
            box.TextChanged += OnTextChanged;
            box.PreviewTextInput -= OnPreviewTextInput;
            box.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(box, OnPasting);
            UpdateError(box);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox box) return;
        var mask = (GetValidateAs(box) ?? string.Empty).ToLowerInvariant();
        var allowed = GetAllowedCharacterClass(mask);
        if (!string.IsNullOrEmpty(allowed) && Regex.IsMatch(e.Text, "[^" + allowed + "]"))
            e.Handled = true;
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.SourceDataObject.GetDataPresent(DataFormats.Text)) return;
        var text = e.SourceDataObject.GetData(DataFormats.Text) as string;
        if (text is null || sender is not TextBox box) return;

        var mask = (GetValidateAs(box) ?? string.Empty).ToLowerInvariant();
        var allowed = GetAllowedCharacterClass(mask);
        if (string.IsNullOrEmpty(allowed)) return;

        // Оставляем в буфере только допустимые символы, остальное вырезаем.
        var sanitized = Regex.Replace(text, "[^" + allowed + "]", string.Empty);
        e.DataObject = new DataObject(DataFormats.Text, sanitized);
    }

    /// <summary>
    /// Возвращает класс допустимых символов (без квадратных скобок) для маски.
    /// Для масок, состоящих только из цифр, разрешены лишь цифры; для "letters" — только буквы.
    /// Пустая строка — фильтр не применяется.
    /// </summary>
    private static string? GetAllowedCharacterClass(string mask)
    {
        switch (mask)
        {
            case "inn":
            case "kpp":
            case "okpo":
            case "bik":
            case "account":
            case "corr":
            case "bnof":
            case "index":
                return @"0-9";
            case "number":
            case "decimal":
                return @"0-9.,\-";
            case "letters":
                return @"a-zA-Zа-яА-ЯёЁ\s\-";
            default:
                return null;
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox box) UpdateError(box);
    }

    private static void UpdateError(TextBox box)
    {
        var mask = (GetValidateAs(box) ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrEmpty(mask))
        {
            SetError(box, string.Empty);
            return;
        }

        var value = box.Text;
        if (string.IsNullOrWhiteSpace(value))
        {
            SetError(box, string.Empty);
            return;
        }

        SetError(box, Validate(mask, value.Trim()) ?? string.Empty);
    }

    /// <summary>Валидация значения по маске; возвращает текст ошибки или null.</summary>
    public static string? Validate(string mask, string value)
    {
        switch (mask)
        {
            case "inn":
                return Regex.IsMatch(value, @"^\d{10}$") || Regex.IsMatch(value, @"^\d{12}$")
                    ? null : "ИНН: 10 или 12 цифр";
            case "kpp":
                return Regex.IsMatch(value, @"^\d{9}$") ? null : "КПП: 9 цифр";
            case "okpo":
                return Regex.IsMatch(value, @"^\d{8}(\d{2})?$") ? null : "ОКПО: 8 или 10 цифр";
            case "bik":
                return Regex.IsMatch(value, @"^\d{9}$") ? null : "БИК: 9 цифр";
            case "account":
            case "corr":
            case "bnof":
                return Regex.IsMatch(value, @"^\d{20}$") ? null : "Счёт: 20 цифр";
            case "index":
                return Regex.IsMatch(value, @"^\d{6}$") ? null : "Индекс: 6 цифр";
            case "phone":
                return Regex.IsMatch(value, @"^[+\d (\-)]{10,18}$") ? null : "Некорректный телефон";
            case "email":
                return Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") ? null : "Некорректный e-mail";
            case "letters":
                return Regex.IsMatch(value, @"^[A-Za-zА-Яа-яЁё\s\-]+$") ? null : "Только буквы";
            case "number":
            case "decimal":
                return decimal.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                    ? null : "Введите число";
            default:
                return null;
        }
    }
}