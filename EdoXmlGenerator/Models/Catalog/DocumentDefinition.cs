using System.Collections.ObjectModel;

namespace EdoXmlGenerator.Models.Catalog;

/// <summary>Метаданные типа документа: описание схемы формы и генерации.</summary>
public class DocumentDefinition
{
    public DocumentDefinition(string id, string title, string category, string description)
    {
        Id = id;
        Title = title;
        Category = category;
        Description = description;
    }

    /// <summary>Уникальный идентификатор типа документа.</summary>
    public string Id { get; set; }

    /// <summary>Название типа документа (в каталоге и шапке формы).</summary>
    public string Title { get; set; }

    /// <summary>Категория (группа в сайдбаре): Продажи, Акты, Договоры ...</summary>
    public string Category { get; set; }

    /// <summary>Краткое описание (подпись под заголовком формы).</summary>
    public string Description { get; set; }

    /// <summary>Имя корневого элемента XML для этого типа.</summary>
    public string RootElement { get; set; } = "Документ";

    /// <summary>Секции формы в порядке отображения.</summary>
    public ObservableCollection<DocSection> Sections { get; } = new();

    /// <summary>Все поля всех секций (удобно для генерации).</summary>
    public IEnumerable<FieldDef> AllFields
    {
        get { foreach (var s in Sections) foreach (var f in s.Fields) yield return f; }
    }
}
