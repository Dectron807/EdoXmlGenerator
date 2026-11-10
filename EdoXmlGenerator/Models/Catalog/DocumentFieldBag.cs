using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EdoXmlGenerator.Models.Catalog;

/// <summary>
/// Хранилище значений полей динамической формы документа.
/// Ключом является Key из FieldDef. Поддерживает текстовые, числовые,
/// денежные, даты и логические значения с уведомлением об изменении.
/// </summary>
public partial class DocumentFieldBag : ObservableObject
{
    private readonly Dictionary<string, string> _text = new();
    private readonly Dictionary<string, DateTime?> _dates = new();
    private readonly Dictionary<string, bool> _bools = new();

    public string this[string key]
    {
        get => _text.TryGetValue(key, out var v) ? v : string.Empty;
        set
        {
            if (!_text.TryGetValue(key, out var old) || old != (value ?? string.Empty))
            {
                _text[key] = value ?? string.Empty;
                OnPropertyChanged(key);
            }
        }
    }

    public DateTime? GetDate(string key) =>
        _dates.TryGetValue(key, out var v) ? v : null;

    public void SetDate(string key, DateTime? value)
    {
        if (!_dates.TryGetValue(key, out var old) || old != value)
        {
            _dates[key] = value;
            OnPropertyChanged(key);
        }
    }

    public bool GetBool(string key) =>
        _bools.TryGetValue(key, out var v) && v;

    public void SetBool(string key, bool value)
    {
        if (!_bools.TryGetValue(key, out var old) || old != value)
        {
            _bools[key] = value;
            OnPropertyChanged(key);
        }
    }

    /// <summary>Числовое значение поля (для Number/Money) или null.</summary>
    public decimal? GetDecimal(string key)
    {
        var raw = this[key];
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d
            : null;
    }

    public string Get(string key) => this[key];

    public void Set(string key, string value) => this[key] = value;
}
