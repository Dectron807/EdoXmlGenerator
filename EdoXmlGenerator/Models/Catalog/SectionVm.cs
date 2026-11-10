using System.Collections.ObjectModel;
using System.Linq;

namespace EdoXmlGenerator.Models.Catalog;

/// <summary>Секция динамической формы в представлении (заголовок + обёртки полей).</summary>
public class SectionVm
{
    private readonly DocumentFieldBag _bag;

    public SectionVm(DocSection section, DocumentFieldBag bag)
    {
        _bag = bag;
        Title = section.Title;
        foreach (var f in section.Fields)
            Fields.Add(new FieldVm(f, bag));
    }

    public string Title { get; }

    public ObservableCollection<FieldVm> Fields { get; } = new();

    public bool HasItemsPlaceholder => Fields.Any(f => f.IsItemsPlaceholder);
}
