using System;
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

public partial class CatalogFormViewModel : ObservableObject
{
    private readonly IDialogService _dialogs;
    private readonly CatalogXmlGeneratorService _generator = new();
    private readonly CatalogTemplateService _templateService = new();

    public DocumentDefinition Definition { get; }

    public DocumentFieldBag Bag { get; } = new();

    public ObservableCollection<SectionVm> Sections { get; } = new();

    public bool HasItems { get; }

    public ObservableCollection<Product> Products { get; } = new();

    public ICommand AddProductCommand { get; }
    public ICommand RemoveProductCommand { get; }
    public ICommand SaveTemplateCommand { get; }
    public ICommand LoadTemplateCommand { get; }
    public ICommand GenerateCommand { get; }

    public event Action<string>? XmlGenerated;

    public CatalogFormViewModel(DocumentDefinition definition, IDialogService dialogs)
    {
        Definition = definition;
        _dialogs = dialogs;

        foreach (var s in definition.Sections)
            Sections.Add(new SectionVm(s, Bag));

        HasItems = definition.AllFields.Any(f => f.Key == "itemsTable");

        AddProductCommand = new RelayCommand(AddProduct);
        RemoveProductCommand = new RelayCommand<Product>(RemoveProduct);
        SaveTemplateCommand = new RelayCommand(SaveTemplate);
        LoadTemplateCommand = new RelayCommand(LoadTemplate);
        GenerateCommand = new RelayCommand(Generate);
    }

    public string Title => Definition.Title;
    public string Description => Definition.Description;

    private void AddProduct()
    {
        var p = new Product
        {
            LineNumber = Products.Count + 1,
            Unit = "шт",
            VatRate = 22,
        };
        p.TotalsChanged += RecalcTotals;
        Products.Add(p);
        RecalcTotals();
    }

    private void RemoveProduct(Product? product)
    {
        if (product == null) return;
        Products.Remove(product);
        RecalcTotals();
    }

    public string TotalWithoutVat => TotalCalc(false).ToString("F2");
    public string TotalVat => (TotalCalc(true) - TotalCalc(false)).ToString("F2");
    public string Total => TotalCalc(true).ToString("F2");

    private decimal TotalCalc(bool withVat)
    {
        if (Products.Count == 0) return 0m;
        if (!withVat) return Products.Sum(p => p.VatRate <= 0 ? p.Sum : Math.Round(p.Sum / (1 + (decimal)p.VatRate / 100), 2));
        return Products.Sum(p => p.Sum);
    }

    private void RecalcTotals()
    {
        OnPropertyChanged(nameof(TotalWithoutVat));
        OnPropertyChanged(nameof(TotalVat));
        OnPropertyChanged(nameof(Total));
    }

    /// <summary>Публичная точка пересчёта итогов (вызывается из шаблона при импорте позиций).</summary>
    public void RecalcTotalsForImport() => RecalcTotals();

    private void SaveTemplate()
    {
        var path = _dialogs.SaveFileDialog("XML-шаблон (*.xml)|*.xml", "Сохранить XML-шаблон данных",
            Definition.Id + "-шаблон.xml");
        if (path == null) return;
        try
        {
            _templateService.SaveToFile(path, this);
            _dialogs.ShowInfo("XML-шаблон данных сохранён.");
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Не удалось сохранить шаблон: {ex.Message}");
        }
    }

    private void LoadTemplate()
    {
        var path = _dialogs.OpenFileDialog("XML-шаблон (*.xml)|*.xml", "Загрузить XML-шаблон данных");
        if (path == null) return;
        try
        {
            _templateService.Load(path, this);
            _dialogs.ShowInfo("XML-шаблон данных применён.");
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Не удалось загрузить шаблон: {ex.Message}");
        }
    }

    private void Generate()
    {
        var errors = new List<string>();
        foreach (var f in Definition.AllFields.Where(f => f.Required))
        {
            switch (f.Kind)
            {
                case FieldKind.Bool:
                    break;
                case FieldKind.Date:
                    if (Bag.GetDate(f.Key) == null) errors.Add($"Не заполнено поле: {f.Label}.");
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(Bag[f.Key])) errors.Add($"Не заполнено поле: {f.Label}.");
                    break;
            }
        }

        if (errors.Count > 0)
        {
            _dialogs.ShowError("Заполните обязательные поля:\n\n" + string.Join("\n", errors));
            return;
        }

        var xml = _generator.Generate(this);
        XmlGenerated?.Invoke(xml);
    }
}
