using System.Collections.ObjectModel;

namespace EdoXmlGenerator.Models.Catalog;

/// <summary>Секция формы — визуальный блок с полями (метка слева, поле справа).</summary>
public class DocSection
{
    public DocSection(string title)
    {
        Title = title;
    }

    /// <summary>Заголовок секции (синяя плашка над блоком).</summary>
    public string Title { get; set; }

    /// <summary>Список полей секции в порядке отображения.</summary>
    public ObservableCollection<FieldDef> Fields { get; } = new();
}
