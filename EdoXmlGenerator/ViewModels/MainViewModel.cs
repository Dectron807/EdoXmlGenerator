using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EdoXmlGenerator.Models;
using EdoXmlGenerator.Models.Catalog;
using EdoXmlGenerator.Services;

namespace EdoXmlGenerator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private bool _isPreviewVisible;

    [ObservableProperty]
    private string _searchText = string.Empty;

    private readonly List<DocumentDefinition> _allDocs = new();
    private HashSet<string> _expandedCategories = new();
    private string? _lastDocId;
    private string _selectedDocId = "upd";

    public ObservableCollection<CategoryVm> Categories { get; } = new();

    public ICommand BackToFormCommand { get; }

    public MainViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
        BackToFormCommand = new RelayCommand(BackToForm);
        foreach (var d in DocumentCatalog.GetDocuments())
            _allDocs.Add(d);
        foreach (var cat in _allDocs.Select(d => d.Category).Distinct())
            _expandedCategories.Add(cat);
        RebuildSidebar();
        SelectDocument("upd");
    }

    partial void OnSearchTextChanged(string value)
    {
        RebuildSidebar();
    }

    private void RebuildSidebar()
    {
        Categories.Clear();
        var query = (SearchText ?? string.Empty).ToLowerInvariant().Replace('ё', 'е').Trim();
        var filtered = _allDocs.Where(d => query.Length == 0 ||
            d.Title.ToLowerInvariant().Replace('ё', 'е').Contains(query));
        foreach (var catName in filtered.Select(d => d.Category).Distinct())
        {
            var items = filtered.Where(d => d.Category == catName).ToList();
            if (items.Count == 0) continue;
            var cat = new CategoryVm(catName, _expandedCategories.Contains(catName), () => ToggleCategory(catName));
            foreach (var def in items)
            {
                var navItem = new DocNavItem(def, SelectDocument)
                {
                    IsSelected = (def.Id == _selectedDocId)
                };
                cat.Items.Add(navItem);
            }
            Categories.Add(cat);
        }
    }

    private void ToggleCategory(string name)
    {
        if (_expandedCategories.Contains(name))
            _expandedCategories.Remove(name);
        else
            _expandedCategories.Add(name);
        RebuildSidebar();
    }

    private void SelectDocument(string id)
    {
        _selectedDocId = id;
        _lastDocId = id;
        IsPreviewVisible = false;

        // Раскрываем категорию выбранного документа, если она была свернута
        var targetDoc = _allDocs.FirstOrDefault(d => d.Id == id);
        if (targetDoc != null && !_expandedCategories.Contains(targetDoc.Category))
        {
            _expandedCategories.Add(targetDoc.Category);
            RebuildSidebar();
        }
        else
        {
            // Обновляем подсветку выбора во всех элементах
            foreach (var cat in Categories)
            {
                foreach (var item in cat.Items)
                {
                    item.IsSelected = (item.Definition.Id == id);
                }
            }
        }

        CurrentViewModel = CreateViewModel(id);
    }

    private ObservableObject CreateViewModel(string id)
    {
        if (id == "upd")
        {
            var vm = new UpdFormViewModel(_dialogs);
            vm.XmlGenerated += OnXmlGenerated;
            return vm;
        }

        var def = DocumentCatalog.Find(id);
        if (def == null) return CreateViewModel("upd");

        var catVm = new CatalogFormViewModel(def, _dialogs);
        catVm.XmlGenerated += OnXmlGenerated;
        return catVm;
    }

    private void OnXmlGenerated(string xml)
    {
        IsPreviewVisible = true;
        CurrentViewModel = new PreviewViewModel(xml, BackToFormCommand);
    }

    private void BackToForm()
    {
        IsPreviewVisible = false;
        CurrentViewModel = CreateViewModel(_lastDocId ?? "upd");
    }
}

/// <summary>Категория в сайдбаре (группа документов, сворачиваемая).</summary>
public class CategoryVm
{
    private readonly System.Action _toggle;

    public CategoryVm(string name, bool isExpanded, System.Action toggle)
    {
        Name = name;
        IsExpanded = isExpanded;
        _toggle = toggle;
        ToggleCommand = new RelayCommand(_toggle);
    }

    public string Name { get; }

    public bool IsExpanded { get; }

    public ObservableCollection<DocNavItem> Items { get; } = new();

    public ICommand ToggleCommand { get; }
}

/// <summary>Пункт навигации в сайдбаре — конкретный тип документа.</summary>
public partial class DocNavItem : ObservableObject
{
    private readonly System.Action<string> _select;

    [ObservableProperty]
    private bool _isSelected;

    public DocNavItem(DocumentDefinition def, System.Action<string> select)
    {
        Definition = def;
        _select = select;
    }

    public DocumentDefinition Definition { get; }

    public string Title => Definition.Title;

    public ICommand SelectCommand => new RelayCommand(() => _select(Definition.Id));
}
