using System;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EdoXmlGenerator.Services;

namespace EdoXmlGenerator.Models.Catalog;

/// <summary>
/// Обёртка поля для двусторонней привязки в динамической форме.
/// Наблюдает за изменениями DocumentFieldBag и переизлучает события для
/// визуального контрола, соответствующего типу поля.
/// </summary>
public partial class FieldVm : ObservableObject
{
    public FieldDef Def { get; }
    public DocumentFieldBag Bag { get; }

    public FieldVm(FieldDef def, DocumentFieldBag bag)
    {
        Def = def;
        Bag = bag;
        Bag.PropertyChanged += OnBagPropertyChanged;
    }

    public string Label => Def.Label;
    public bool IsRequired => Def.Required;
    public int Rows => Def.Rows;
    public int MaxLength => Def.MaxLength;

    /// <summary>Подсказка/пример ввода.</summary>
    public string Placeholder => FieldHintsService.GetPlaceholder(Def) ?? string.Empty;

    /// <summary>Текст ошибки формата (пусто — корректно/не проверено).</summary>
    public string ErrorText => HasError ? FieldHintsService.Validate(Def, Text) ?? string.Empty : string.Empty;

    /// <summary>Поле заполнено, но формат некорректен.</summary>
    public bool HasError => !string.IsNullOrWhiteSpace(Text) &&
                            FieldHintsService.Validate(Def, Text) != null;

    /// <summary>Поле содержит какой-либо текст (для отображения подсказки).</summary>
    public bool HasText => !string.IsNullOrEmpty(Text);

    /// <summary>Показывать подсказку-пример (поле ввода типа текста, ещё не заполнено).</summary>
    public bool ShowPlaceholder => (IsText || IsTextArea || IsNumber) && !HasText;

    public bool IsText => Def.Kind == FieldKind.Text;
    public bool IsTextArea => Def.Kind == FieldKind.TextArea;
    public bool IsNumber => Def.Kind is FieldKind.Number or FieldKind.Money;
    public bool IsDate => Def.Kind == FieldKind.Date;
    public bool IsBool => Def.Kind == FieldKind.Bool;
    public bool IsItemsPlaceholder => Def.Key == "itemsTable";

    public string Text
    {
        get => Bag[Def.Key];
        set => Bag[Def.Key] = value;
    }

    public DateTime? Date
    {
        get => Bag.GetDate(Def.Key);
        set => Bag.SetDate(Def.Key, value);
    }

    public string Number
    {
        get => Bag[Def.Key];
        set => Bag[Def.Key] = value;
    }

    public bool Checked
    {
        get => Bag.GetBool(Def.Key);
        set => Bag.SetBool(Def.Key, value);
    }

    private void OnBagPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == Def.Key)
        {
            OnPropertyChanged(nameof(Text));
            OnPropertyChanged(nameof(Date));
            OnPropertyChanged(nameof(Number));
            OnPropertyChanged(nameof(Checked));
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(ErrorText));
            OnPropertyChanged(nameof(HasText));
            OnPropertyChanged(nameof(ShowPlaceholder));
        }
    }
}
