using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EdoXmlGenerator.Models;
using EdoXmlGenerator.Services;

namespace EdoXmlGenerator.ViewModels;

public partial class DocumentFormViewModel : ObservableObject
{
    private readonly XmlGeneratorService _xmlGenerator = new();
    private readonly ValidationService _validator = new();
    private readonly ExcelImportService _excelImport = new();
    private readonly JsonImportService _jsonImport = new();
    private readonly IDialogService _dialogs;

    private EdoDocument _document = new();

    [ObservableProperty]
    private string _supplierName = string.Empty;

    [ObservableProperty]
    private string _supplierInn = string.Empty;

    [ObservableProperty]
    private string _supplierKpp = string.Empty;

    [ObservableProperty]
    private string _supplierOkpo = string.Empty;

    [ObservableProperty]
    private string _supplierAddress = string.Empty;

    [ObservableProperty]
    private string _buyerName = string.Empty;

    [ObservableProperty]
    private string _buyerInn = string.Empty;

    [ObservableProperty]
    private string _buyerKpp = string.Empty;

    [ObservableProperty]
    private string _buyerOkpo = string.Empty;

    [ObservableProperty]
    private string _buyerAddress = string.Empty;

    [ObservableProperty]
    private string _documentNumber = string.Empty;

    [ObservableProperty]
    private DateTime _documentDate = DateTime.Today;

    [ObservableProperty]
    private int _selectedIndex;

    public ObservableCollection<Product> Products { get; } = new();

    public ObservableCollection<UnitOption> Units { get; }
    public IReadOnlyList<int> VatRates { get; }

    public string TotalWithoutVat => _document.TotalWithoutVat.ToString("F2");
    public string TotalVat => _document.TotalVat.ToString("F2");
    public string Total => _document.Total.ToString("F2");

    public DocumentType DocumentType { get; private set; }

    public string FormTitle => DocumentType switch
    {
        DocumentType.Nakladnaya => "Создание товарной накладной",
        DocumentType.Akt => "Создание акта выполненных работ",
        _ => "Создание документа"
    };

    public string SupplierHeader => DocumentType == DocumentType.Akt ? "Исполнитель" : "Поставщик";
    public string BuyerHeader => DocumentType == DocumentType.Akt ? "Заказчик" : "Покупатель";
    public string ProductsHeader => DocumentType == DocumentType.Akt ? "Работы (услуги)" : "Товары";
    public string AddProductButtonText => DocumentType == DocumentType.Akt ? "Добавить работу/услугу" : "Добавить позицию";

    public ICommand AddProductCommand { get; }
    public ICommand RemoveProductCommand { get; }
    public ICommand ImportExcelCommand { get; }
    public ICommand ImportJsonCommand { get; }
    public ICommand GenerateXmlCommand { get; }

    public event Action<string>? XmlGenerated;

    public DocumentFormViewModel(DocumentType type, IDialogService dialogs)
    {
        DocumentType = type;
        _dialogs = dialogs;

        var refService = new ReferenceService();
        Units = new ObservableCollection<UnitOption>(
            refService.Units.Select(u => new UnitOption(u.Name, $"{u.Name} — {u.Full}")));
        VatRates = refService.VatRates.ToList();

        _document.Type = type;
        _document.Products = Products;

        foreach (var p in Products)
            p.TotalsChanged += RecalcTotals;

        AddProductCommand = new RelayCommand(AddProduct);
        RemoveProductCommand = new RelayCommand<Product>(RemoveProduct);
        ImportExcelCommand = new RelayCommand(ImportExcel);
        ImportJsonCommand = new RelayCommand(ImportJson);
        GenerateXmlCommand = new RelayCommand(GenerateXml);
    }

    private void AddProduct()
    {
        var product = new Product
        {
            LineNumber = Products.Count + 1,
            VatRate = 22
        };
        product.TotalsChanged += RecalcTotals;
        Products.Add(product);
        RecalcTotals();
    }

    private void RemoveProduct(Product? product)
    {
        if (product == null) return;
        Products.Remove(product);
        RecalcTotals();
    }

    private void ImportExcel()
    {
        var file = _dialogs.OpenFileDialog("Excel-файлы (*.xlsx;*.xls)|*.xlsx;*.xls", "Импорт товаров из Excel");
        if (file == null) return;

        try
        {
            var imported = _excelImport.ImportProducts(file);
            foreach (var p in imported)
            {
                p.TotalsChanged += RecalcTotals;
                Products.Add(p);
            }
            _dialogs.ShowInfo($"Импортировано позиций: {imported.Count}");
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Не удалось импортировать данные: {ex.Message}");
        }
        RecalcTotals();
    }

    private void ImportJson()
    {
        var file = _dialogs.OpenFileDialog("JSON-файлы (*.json)|*.json", "Импорт документа из JSON");
        if (file == null) return;

        try
        {
            var imported = _jsonImport.Import(file);
            ApplyDocument(imported);
            _dialogs.ShowInfo("Документ импортирован из JSON.");
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Не удалось импортировать документ: {ex.Message}");
        }
    }

    private void GenerateXml()
    {
        SyncToDocument();
        var errors = _validator.Validate(_document);

        if (errors.Count > 0)
        {
            _dialogs.ShowError("Документ содержит ошибки:\n\n" + string.Join("\n", errors));
            return;
        }

        var xml = _xmlGenerator.Generate(_document);
        XmlGenerated?.Invoke(xml);
    }

    private void ApplyDocument(EdoDocument src)
    {
        DocumentNumber = src.Number;
        DocumentDate = src.Date;

        SupplierName = src.Supplier.Name;
        SupplierInn = src.Supplier.Inn;
        SupplierKpp = src.Supplier.Kpp;
        SupplierOkpo = src.Supplier.Okpo;
        SupplierAddress = src.Supplier.Address;

        BuyerName = src.Buyer.Name;
        BuyerInn = src.Buyer.Inn;
        BuyerKpp = src.Buyer.Kpp;
        BuyerOkpo = src.Buyer.Okpo;
        BuyerAddress = src.Buyer.Address;

        Products.Clear();
        foreach (var p in src.Products)
        {
            p.TotalsChanged += RecalcTotals;
            Products.Add(p);
        }
        RecalcTotals();
    }

    private void SyncToDocument()
    {
        _document.Number = DocumentNumber;
        _document.Date = DocumentDate;

        _document.Supplier.Name = SupplierName;
        _document.Supplier.Inn = SupplierInn;
        _document.Supplier.Kpp = SupplierKpp;
        _document.Supplier.Okpo = SupplierOkpo;
        _document.Supplier.Address = SupplierAddress;

        _document.Buyer.Name = BuyerName;
        _document.Buyer.Inn = BuyerInn;
        _document.Buyer.Kpp = BuyerKpp;
        _document.Buyer.Okpo = BuyerOkpo;
        _document.Buyer.Address = BuyerAddress;
    }

    private void RecalcTotals()
    {
        OnPropertyChanged(nameof(TotalWithoutVat));
        OnPropertyChanged(nameof(TotalVat));
        OnPropertyChanged(nameof(Total));
    }

    public class UnitOption
    {
        public UnitOption(string name, string display) { Name = name; Display = display; }
        public string Name { get; }
        public string Display { get; }
        public override string ToString() => Display;
    }
}
