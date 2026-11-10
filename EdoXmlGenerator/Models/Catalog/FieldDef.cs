namespace EdoXmlGenerator.Models.Catalog;

/// <summary>Описание одного поля внутри секции динамической формы документа.</summary>
public class FieldDef
{
    /// <summary>Уникальный ключ поля (ключ в FieldBag).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Подпись (метка слева в строке формы).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Тип поля (определяет контрол ввода).</summary>
    public FieldKind Kind { get; set; } = FieldKind.Text;

    /// <summary>Поле обязательно для генерации XML.</summary>
    public bool Required { get; set; }

    /// <summary>Многострочное поле (для Kind = TextArea).</summary>
    public int Rows { get; set; } = 3;

    /// <summary>Ограничение длины текста (0 — без ограничения).</summary>
    public int MaxLength { get; set; }

    /// <summary>Подсказка/пример ввода (placeholder).</summary>
    public string? Placeholder { get; set; }
}
