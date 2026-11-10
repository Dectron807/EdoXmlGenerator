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

public partial class UpdFormViewModel : ObservableObject
{
    private readonly Upd53XmlGeneratorService _generator = new();
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private string _documentNumber = "1";

    [ObservableProperty]
    private DateTime _documentDate = DateTime.Today;

    [ObservableProperty]
    private int _statusIndex = 0; // 0 = Статус 1 (СЧФДОП), 1 = Статус 2 (ДОП), 2 = СЧФ

    [ObservableProperty]
    private string _function = "СЧФДОП";

    partial void OnStatusIndexChanged(int value)
    {
        switch (value)
        {
            case 0:
                Function = "СЧФДОП";
                break;
            case 1:
                Function = "ДОП";
                break;
            case 2:
                Function = "СЧФ";
                break;
        }
    }

    partial void OnFunctionChanged(string value)
    {
        if (value == "СЧФДОП" && StatusIndex != 0) StatusIndex = 0;
        else if (value == "ДОП" && StatusIndex != 1) StatusIndex = 1;
        else if (value == "СЧФ" && StatusIndex != 2) StatusIndex = 2;
    }

    public Upd53Doc.Upd53Participant Supplier { get; } = new();
    public Upd53Doc.Upd53Participant Shipper { get; } = new();
    public Upd53Doc.Upd53Participant Buyer { get; } = new();
    public Upd53Doc.Upd53Participant Consignee { get; } = new();

    // Строка 3: грузоотправитель "он же"
    [ObservableProperty]
    private bool _shipperIsSeller = true;

    // Строка 4: грузополучатель совпадает с покупателем
    [ObservableProperty]
    private bool _consigneeIsBuyer = true;

    // Строка 5: К платежно-расчетному документу (СвПРД)
    [ObservableProperty]
    private string _paymentDocNumber = string.Empty;

    [ObservableProperty]
    private DateTime? _paymentDocDate;

    // Строка 5а: Документ об отгрузке (ДокПодтвОтгрНом)
    [ObservableProperty]
    private string _shipmentDocName = "Универсальный передаточный документ";

    [ObservableProperty]
    private string _shipmentDocNumber = string.Empty;

    [ObservableProperty]
    private DateTime? _shipmentDocDate;

    // Валюта (строка 7)
    [ObservableProperty]
    private string _currencyCode = "643";

    [ObservableProperty]
    private string _currencyName = "Российский рубль";

    [ObservableProperty]
    private string _currencyRate = string.Empty;

    // Строка 8: Идентификатор госконтракта
    [ObservableProperty]
    private string _stateContractId = string.Empty;

    // Сведения об операции и передаче
    [ObservableProperty]
    private string _operationContent = "Товары переданы, работы (услуги) выполнены в полном объеме";

    [ObservableProperty]
    private string _saleOrder = string.Empty;

    [ObservableProperty]
    private DateTime? _transferDate;

    // Строка [10]: Основание передачи (договор/соглашение/заказ)
    [ObservableProperty]
    private string _baseDocName = "Договор поставки";

    [ObservableProperty]
    private string _baseDocNumber = string.Empty;

    [ObservableProperty]
    private DateTime? _baseDocDate;

    [ObservableProperty]
    private string _correctionNumber = string.Empty;

    [ObservableProperty]
    private DateTime? _correctionDate;

    // Авансовые реквизиты (ДопСвФХЖ1)
    [ObservableProperty]
    private string _advanceStateContractId = string.Empty;

    [ObservableProperty]
    private string _advanceConditionCode = string.Empty;

    public ObservableCollection<Upd53Obligation> Obligations { get; } = new();

    // Инфополя
    public ObservableCollection<Upd53InfoField> HeaderInfoFields { get; } = new();
    public ObservableCollection<Upd53InfoField> DocInfoFields { get; } = new();

    // Транспорт (Тран) — строка [11]
    [ObservableProperty]
    private string _transportInfo = string.Empty;

    [ObservableProperty]
    private string _incoterms = string.Empty;

    [ObservableProperty]
    private string _incotermsVersion = string.Empty;

    // СвПерВещи — строки [13], [14]
    [ObservableProperty]
    private DateTime? _goodsTransferDate;

    [ObservableProperty]
    private string _goodsTransferInfo = string.Empty;

    // СвЛицПер — строка [12] (лицо, передавшее товар / сдавшее работы)
    [ObservableProperty]
    private bool _hasTransferPerson = true;

    [ObservableProperty]
    private string _transferPersonPosition = "Кладовщик";

    [ObservableProperty]
    private string _transferPersonFio = string.Empty;

    // Строка [15]: Ответственный за правильность оформления со стороны продавца
    [ObservableProperty]
    private string _responsibleTransferPosition = "Генеральный директор";

    [ObservableProperty]
    private string _responsibleTransferFio = string.Empty;

    // Строка [16]: Наименование составителя документа
    [ObservableProperty]
    private string _economicSubjectTransfer = string.Empty;

    // Подписант
    [ObservableProperty]
    private string _signerLastName = string.Empty;

    [ObservableProperty]
    private string _signerFirstName = string.Empty;

    [ObservableProperty]
    private string _signerMiddleName = string.Empty;

    [ObservableProperty]
    private string _signerPosition = "Генеральный директор";

    [ObservableProperty]
    private string _signerInn = string.Empty;

    [ObservableProperty]
    private string _procurationNumber = string.Empty;

    [ObservableProperty]
    private DateTime? _procurationDate;

    [ObservableProperty]
    private int _signerPowerKindIndex = 0;

    [ObservableProperty]
    private int _signerSignTypeIndex = 0;

    [ObservableProperty]
    private DateTime? _signerSignDate = DateTime.Today;

    [ObservableProperty]
    private string _signerUrn = string.Empty;

    [ObservableProperty]
    private DateTime? _signerElProcDate;

    [ObservableProperty]
    private string _signerElProcInnerNo = string.Empty;

    [ObservableProperty]
    private string _signerStorageSystemId = string.Empty;

    [ObservableProperty]
    private string _signerStorageUrl = string.Empty;

    [ObservableProperty]
    private int _selectedIndex;

    public ObservableCollection<Upd53Doc.Upd53Product> Products { get; } = new();

    public ObservableCollection<ReferenceService.Unit> Units { get; } = new();

    public IReadOnlyList<string> StatusOptions { get; } = new[]
    {
        "Статус 1 — СЧФДОП (счёт-фактура и передаточный документ)",
        "Статус 2 — ДОП (передаточный документ / акт без НДС / УСН)",
        "СЧФ — счёт-фактура"
    };

    public IReadOnlyList<string> FunctionOptions { get; } = new[] { "СЧФДОП", "ДОП", "СЧФ" };

    public IReadOnlyList<string> VatRateOptions { get; } = new[]
    {
        "Без НДС", "0%", "5%", "7%", "10%", "20%", "22%",
        "5/105", "7/107", "10/110", "20/120", "22/122"
    };

    public IReadOnlyList<string> PowerKindOptions { get; } = new[]
    {
        "1 — Руководитель (без доверенности)",
        "2 — Представитель по доверенности (бумажной)",
        "3 — Представитель по машиночитаемой доверенности (МЧД)",
        "4 — По закону / учредительному документу",
        "5 — Представитель по доверенности (иной)",
        "6 — Иное уполномоченное лицо"
    };

    public IReadOnlyList<string> SignTypeOptions { get; } = new[]
    {
        "1 — Руководитель организации / ИП",
        "2 — Уполномоченный представитель",
        "3 — Иное лицо"
    };

    public string TotalWithoutVat => Products.Sum(p => p.SumWithoutVat).ToString("F2");
    public string TotalVat => Products.Sum(p => VatAmount(p)).ToString("F2");
    public string Total => Products.Sum(p => p.SumWithoutVat + VatAmount(p)).ToString("F2");

    public ICommand AddProductCommand { get; }
    public ICommand RemoveProductCommand { get; }
    public ICommand GenerateXmlCommand { get; }
    public ICommand SaveTemplateCommand { get; }
    public ICommand LoadTemplateCommand { get; }
    public ICommand ImportExcelCommand { get; }
    public ICommand ImportXmlCommand { get; }

    public event Action<string>? XmlGenerated;

    public UpdFormViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
        Supplier.IsOrganization = true;
        Supplier.IsEmpty = false;
        Buyer.IsOrganization = true;
        Buyer.IsEmpty = false;

        var refService = new ReferenceService();
        foreach (var u in refService.Units)
            Units.Add(u);
        if (Units.Count > 0)
            Products.Add(new Upd53Doc.Upd53Product
            {
                LineNumber = 1,
                VatRateLabel = "20%",
                UnitCode = "796",
                UnitName = "шт",
            });

        foreach (var p in Products)
            p.TotalsChanged += RecalcTotals;

        AddProductCommand = new RelayCommand(AddProduct);
        RemoveProductCommand = new RelayCommand<Upd53Doc.Upd53Product>(RemoveProduct);
        GenerateXmlCommand = new RelayCommand(GenerateXml);
        SaveTemplateCommand = new RelayCommand(SaveTemplate);
        LoadTemplateCommand = new RelayCommand(LoadTemplate);
        ImportExcelCommand = new RelayCommand(ImportExcel);
        ImportXmlCommand = new RelayCommand(ImportXml);
    }

    private void AddProduct()
    {
        var product = new Upd53Doc.Upd53Product
        {
            LineNumber = Products.Count + 1,
            VatRateLabel = "20%",
            UnitCode = "796",
            UnitName = "шт"
        };
        product.TotalsChanged += RecalcTotals;
        Products.Add(product);
        RecalcTotals();
    }

    private void RemoveProduct(Upd53Doc.Upd53Product? product)
    {
        if (product == null) return;
        product.TotalsChanged -= RecalcTotals;
        Products.Remove(product);
        RecalcTotals();
    }

    private void GenerateXml()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DocumentNumber))
            errors.Add("Не указан номер документа.");
        if (string.IsNullOrWhiteSpace(Supplier.Inn))
            errors.Add("Не указан ИНН продавца.");
        if (string.IsNullOrWhiteSpace(Buyer.Inn))
            errors.Add("Не указан ИНН покупателя.");
        if (Products.All(p => string.IsNullOrWhiteSpace(p.Name)))
            errors.Add("Не добавлено ни одной позиции товаров.");
        if (string.IsNullOrWhiteSpace(SignerLastName) || string.IsNullOrWhiteSpace(SignerFirstName))
            errors.Add("Укажите фамилию и имя подписанта.");
        if (string.IsNullOrWhiteSpace(CorrectionNumber) != (CorrectionDate == null))
            errors.Add("Для исправления укажите и номер, и дату исправления.");

        if (errors.Count > 0)
        {
            _dialogs.ShowError("Заполните обязательные поля:\n\n" + string.Join("\n", errors));
            return;
        }

        var doc = new Upd53Doc
        {
            DocNumber = DocumentNumber.Trim(),
            DocDate = DocumentDate,
            Function = Function,
            CurrencyCode = string.IsNullOrWhiteSpace(CurrencyCode) ? "643" : CurrencyCode.Trim(),
            CurrencyName = string.IsNullOrWhiteSpace(CurrencyName) ? "Российский рубль" : CurrencyName.Trim(),
            CurrencyRate = CurrencyRate.Trim(),
            OperationContent = OperationContent.Trim(),
            SaleOrder = SaleOrder.Trim(),
            TransferDateText = (TransferDate ?? DocumentDate).ToString("dd.MM.yyyy"),
            BaseDocName = BaseDocName.Trim(),
            BaseDocNumber = BaseDocNumber.Trim(),
            BaseDocDateText = BaseDocDate?.ToString("dd.MM.yyyy") ?? string.Empty,
            CorrectionNumber = CorrectionNumber.Trim(),
            CorrectionDateText = CorrectionDate?.ToString("dd.MM.yyyy") ?? string.Empty,
            ShipperIsSeller = ShipperIsSeller,
            ConsigneeIsBuyer = ConsigneeIsBuyer,
            PaymentDocNumber = PaymentDocNumber.Trim(),
            PaymentDocDateText = PaymentDocDate?.ToString("dd.MM.yyyy") ?? string.Empty,
            ShipmentDocName = ShipmentDocName.Trim(),
            ShipmentDocNumber = string.IsNullOrWhiteSpace(ShipmentDocNumber) ? DocumentNumber.Trim() : ShipmentDocNumber.Trim(),
            ShipmentDocDateText = (ShipmentDocDate ?? DocumentDate).ToString("dd.MM.yyyy"),
            StateContractId = StateContractId.Trim(),
            AdvanceStateContractId = AdvanceStateContractId.Trim(),
            AdvanceConditionCode = AdvanceConditionCode.Trim(),
            TransportInfo = TransportInfo.Trim(),
            Incoterms = Incoterms.Trim(),
            IncotermsVersion = IncotermsVersion.Trim(),
            GoodsTransferDateText = (GoodsTransferDate ?? TransferDate ?? DocumentDate).ToString("dd.MM.yyyy"),
            GoodsTransferInfo = GoodsTransferInfo.Trim(),
            HasTransferPerson = HasTransferPerson,
            TransferPersonKind = "Раб",
            TransferPersonPosition = TransferPersonPosition.Trim(),
            TransferPersonFio = TransferPersonFio.Trim(),
            ResponsibleTransferPosition = ResponsibleTransferPosition.Trim(),
            ResponsibleTransferFio = ResponsibleTransferFio.Trim(),
            EconomicSubjectTransfer = EconomicSubjectTransfer.Trim(),
        };

        foreach (var ob in Obligations)
            doc.Obligations.Add(new Upd53Obligation { Code = ob.Code.Trim(), Name = ob.Name.Trim() });
        foreach (var f in HeaderInfoFields)
            doc.HeaderInfoFields.Add(new Upd53InfoField { Ident = f.Ident.Trim(), Value = f.Value.Trim() });
        foreach (var f in DocInfoFields)
            doc.DocInfoFields.Add(new Upd53InfoField { Ident = f.Ident.Trim(), Value = f.Value.Trim() });

        doc.Suppliers.Add(CloneParticipant(Supplier));
        doc.Buyers.Add(CloneParticipant(Buyer));

        if (!ShipperIsSeller && !string.IsNullOrWhiteSpace(Shipper.Inn))
            doc.Shippers.Add(CloneParticipant(Shipper));
        else if (ShipperIsSeller)
            doc.ShipperIsSeller = true;

        if (ConsigneeIsBuyer)
        {
            var c = CloneParticipant(Buyer);
            doc.Consignees.Add(c);
        }
        else if (!string.IsNullOrWhiteSpace(Consignee.Inn))
        {
            doc.Consignees.Add(CloneParticipant(Consignee));
        }

        foreach (var p in Products.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            doc.Products.Add(new Upd53Doc.Upd53Product
            {
                LineNumber = p.LineNumber,
                Name = p.Name,
                Code = p.Code,
                ProductTypeCode = p.ProductTypeCode,
                UnitCode = string.IsNullOrWhiteSpace(p.UnitCode) ? "796" : p.UnitCode,
                UnitName = string.IsNullOrWhiteSpace(p.UnitName) ? "шт" : p.UnitName,
                Quantity = p.Quantity,
                Price = p.Price,
                ExciseAmount = p.ExciseAmount,
                VatRateLabel = string.IsNullOrWhiteSpace(p.VatRateLabel) ? "20%" : p.VatRateLabel,
                Gtin = p.Gtin,
                GtdNumber = p.GtdNumber,
                CountryCode = p.CountryCode,
                CountryName = p.CountryName,
                TrackNo = p.TrackNo,
                TrackUnitCode = p.TrackUnitCode,
                TrackUnitName = p.TrackUnitName,
                TrackQty = p.TrackQty,
                TrackAmount = p.TrackAmount,
                TrackInfo = p.TrackInfo,
                Kiz = p.Kiz,
                PackNo = p.PackNo,
                MarkedQty = p.MarkedQty,
                BatchInfo = p.BatchInfo,
            });
            foreach (var f in p.InfoFields)
                doc.Products[^1].InfoFields.Add(new Upd53InfoField { Ident = f.Ident, Value = f.Value });
        }

        doc.Signers.Add(new Upd53Doc.Upd53Signer
        {
            LastName = SignerLastName.Trim(),
            FirstName = SignerFirstName.Trim(),
            MiddleName = SignerMiddleName.Trim(),
            Position = SignerPosition.Trim(),
            PowerKind = (Upd53Doc.SignerPowerKind)(SignerPowerKindIndex + 1),
            Inn = SignerInn.Trim(),
            ProcurationNumber = ProcurationNumber.Trim(),
            ProcurationDateText = ProcurationDate?.ToString("dd.MM.yyyy") ?? string.Empty,
            SignType = SignerSignTypeIndex + 1,
            SignDateText = (SignerSignDate ?? DocumentDate).ToString("dd.MM.yyyy"),
            Urn = SignerUrn.Trim(),
            ElProcDateText = SignerElProcDate?.ToString("dd.MM.yyyy") ?? string.Empty,
            ElProcInnerNo = SignerElProcInnerNo.Trim(),
            StorageSystemId = SignerStorageSystemId.Trim(),
            StorageUrl = SignerStorageUrl.Trim(),
        });

        try
        {
            var xml = _generator.Generate(doc);
            XmlGenerated?.Invoke(xml);
        }
        catch (InvalidOperationException ex)
        {
            _dialogs.ShowError(ex.Message);
        }
    }

    private static Upd53Doc.Upd53Participant CloneParticipant(Upd53Doc.Upd53Participant src) => new()
    {
        IsOrganization = src.IsOrganization,
        IsEmpty = false,
        Name = src.Name.Trim(),
        Inn = src.Inn.Trim(),
        Kpp = src.Kpp.Trim(),
        Okpo = src.Okpo.Trim(),
        ShortName = src.ShortName.Trim(),
        OpfCode = src.OpfCode.Trim(),
        OpfFullName = src.OpfFullName.Trim(),
        InfoForParticipant = src.InfoForParticipant.Trim(),
        Address = src.Address.Trim(),
        UseStructuredAddress = src.UseStructuredAddress,
        PostalIndex = src.PostalIndex.Trim(),
        RegionCode = src.RegionCode.Trim(),
        RegionName = src.RegionName.Trim(),
        District = src.District.Trim(),
        City = src.City.Trim(),
        Locality = src.Locality.Trim(),
        Street = src.Street.Trim(),
        House = src.House.Trim(),
        Building = src.Building.Trim(),
        Apartment = src.Apartment.Trim(),
        BankName = src.BankName.Trim(),
        Bik = src.Bik.Trim(),
        AccountNumber = src.AccountNumber.Trim(),
        CorrAccount = src.CorrAccount.Trim(),
        Phone = src.Phone.Trim(),
        Email = src.Email.Trim(),
    };

    private static decimal VatAmount(Upd53Doc.Upd53Product p)
    {
        var label = string.IsNullOrWhiteSpace(p.VatRateLabel) ? "" : p.VatRateLabel.Trim();
        if (label == "Без НДС") return 0;
        var parts = label.Split('/');
        if (!decimal.TryParse(parts[0].TrimEnd('%'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rate))
            return 0;
        return Math.Round(p.SumWithoutVat * rate / 100, 2);
    }

    private void RecalcTotals()
    {
        OnPropertyChanged(nameof(TotalWithoutVat));
        OnPropertyChanged(nameof(TotalVat));
        OnPropertyChanged(nameof(Total));
    }

    private void SaveTemplate()
    {
        var template = BuildTemplate();
        var path = _dialogs.SaveFileDialog(
            "XML-шаблон УПД (*.xml)|*.xml", "Сохранить шаблон УПД", "Шаблон_УПД.xml");
        if (path == null) return;

        try
        {
            new UpdTemplateService().SaveToFile(path, template);
            _dialogs.ShowInfo("Шаблон сохранён.");
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Ошибка сохранения шаблона: {ex.Message}");
        }
    }

    private void LoadTemplate()
    {
        var path = _dialogs.OpenFileDialog(
            "XML/JSON шаблоны УПД (*.xml;*.upd.json)|*.xml;*.upd.json", "Загрузить шаблон УПД");
        if (path == null) return;

        try
        {
            var template = new UpdTemplateService().Load(path);
            ApplyTemplate(template);
            _dialogs.ShowInfo("Шаблон загружен.");
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Ошибка загрузки шаблона: {ex.Message}");
        }
    }

    private void ImportExcel()
    {
        var path = _dialogs.OpenFileDialog(
            "Excel-файлы (*.xlsx;*.xls)|*.xlsx;*.xls", "Импорт товаров и данных из Excel в УПД");
        if (path == null) return;

        try
        {
            var importData = new UpdExcelImportService().Import(path);

            if (!string.IsNullOrWhiteSpace(importData.DocNumber))
                DocumentNumber = importData.DocNumber;
            if (importData.DocDate.HasValue)
                DocumentDate = importData.DocDate.Value;
            if (!string.IsNullOrWhiteSpace(importData.SellerInn))
                Supplier.Inn = importData.SellerInn;
            if (!string.IsNullOrWhiteSpace(importData.SellerKpp))
                Supplier.Kpp = importData.SellerKpp;
            if (!string.IsNullOrWhiteSpace(importData.BuyerInn))
                Buyer.Inn = importData.BuyerInn;
            if (!string.IsNullOrWhiteSpace(importData.BuyerKpp))
                Buyer.Kpp = importData.BuyerKpp;

            if (importData.Products.Count > 0)
            {
                Products.Clear();
                foreach (var p in importData.Products)
                {
                    p.TotalsChanged += RecalcTotals;
                    Products.Add(p);
                }
                _dialogs.ShowInfo($"Импортировано позиций: {importData.Products.Count}");
            }
            else
            {
                _dialogs.ShowInfo("Файл загружен. Товарные строки не найдены или пусты.");
            }
            RecalcTotals();
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Ошибка импорта Excel: {ex.Message}");
        }
    }

    private void ImportXml()
    {
        var path = _dialogs.OpenFileDialog(
            "XML-файлы УПД (*.xml)|*.xml|JSON-файлы (*.json)|*.json", "Импорт документа УПД из XML");
        if (path == null) return;

        try
        {
            var template = new UpdTemplateService().Load(path);
            ApplyTemplate(template);
            _dialogs.ShowInfo("Документ импортирован из XML.");
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Ошибка импорта: {ex.Message}");
        }
    }

    private Upd53Template BuildTemplate()
    {
        return new Upd53Template
        {
            DocumentNumber = DocumentNumber,
            DocumentDate = DocumentDate,
            Function = Function,
            CorrectionNumber = CorrectionNumber,
            CorrectionDate = CorrectionDate,
            ShipperIsSeller = ShipperIsSeller,
            ConsigneeIsBuyer = ConsigneeIsBuyer,
            PaymentDocNumber = PaymentDocNumber,
            PaymentDocDate = PaymentDocDate,
            ShipmentDocName = ShipmentDocName,
            ShipmentDocNumber = ShipmentDocNumber,
            ShipmentDocDate = ShipmentDocDate,
            StateContractId = StateContractId,
            Supplier = UpdTemplateService.FromParticipant(Supplier),
            Shipper  = UpdTemplateService.FromParticipant(Shipper),
            Buyer    = UpdTemplateService.FromParticipant(Buyer),
            Consignee = UpdTemplateService.FromParticipant(Consignee),
            CurrencyCode = CurrencyCode,
            CurrencyName = CurrencyName,
            CurrencyRate = CurrencyRate,
            OperationContent = OperationContent,
            SaleOrder = SaleOrder,
            TransferDate = TransferDate,
            BaseDocName = BaseDocName,
            BaseDocNumber = BaseDocNumber,
            BaseDocDate = BaseDocDate,
            AdvanceStateContractId = AdvanceStateContractId,
            AdvanceConditionCode = AdvanceConditionCode,
            Obligations = Obligations.Select(o => new Upd53Template.ObligationDto { Code = o.Code, Name = o.Name }).ToList(),
            HeaderInfoFields = HeaderInfoFields.Select(f => new Upd53Template.InfoFieldDto { Ident = f.Ident, Value = f.Value }).ToList(),
            DocInfoFields = DocInfoFields.Select(f => new Upd53Template.InfoFieldDto { Ident = f.Ident, Value = f.Value }).ToList(),
            TransportInfo = TransportInfo,
            Incoterms = Incoterms,
            IncotermsVersion = IncotermsVersion,
            GoodsTransferDate = GoodsTransferDate,
            GoodsTransferInfo = GoodsTransferInfo,
            HasTransferPerson = HasTransferPerson,
            TransferPersonPosition = TransferPersonPosition,
            TransferPersonFio = TransferPersonFio,
            ResponsibleTransferPosition = ResponsibleTransferPosition,
            ResponsibleTransferFio = ResponsibleTransferFio,
            EconomicSubjectTransfer = EconomicSubjectTransfer,
            SignerLastName = SignerLastName,
            SignerFirstName = SignerFirstName,
            SignerMiddleName = SignerMiddleName,
            SignerPosition = SignerPosition,
            SignerInn = SignerInn,
            ProcurationNumber = ProcurationNumber,
            ProcurationDate = ProcurationDate,
            SignerPowerKindIndex = SignerPowerKindIndex,
            SignerSignTypeIndex = SignerSignTypeIndex,
            SignerSignDate = SignerSignDate,
            SignerUrn = SignerUrn,
            SignerElProcDate = SignerElProcDate,
            SignerElProcInnerNo = SignerElProcInnerNo,
            SignerStorageSystemId = SignerStorageSystemId,
            SignerStorageUrl = SignerStorageUrl,
            Products = Products.Select(p => new Upd53Template.ProductDto
            {
                LineNumber = p.LineNumber,
                Name = p.Name,
                Code = p.Code,
                ProductTypeCode = p.ProductTypeCode,
                UnitCode = p.UnitCode,
                UnitName = p.UnitName,
                Quantity = p.Quantity,
                Price = p.Price,
                ExciseAmount = p.ExciseAmount,
                VatRateLabel = p.VatRateLabel,
                Gtin = p.Gtin,
                GtdNumber = p.GtdNumber,
                CountryCode = p.CountryCode,
                CountryName = p.CountryName,
                TrackNo = p.TrackNo,
                TrackUnitCode = p.TrackUnitCode,
                TrackUnitName = p.TrackUnitName,
                TrackQty = p.TrackQty,
                TrackAmount = p.TrackAmount,
                TrackInfo = p.TrackInfo,
                Kiz = p.Kiz,
                PackNo = p.PackNo,
                MarkedQty = p.MarkedQty,
                BatchInfo = p.BatchInfo,
                InfoFields = p.InfoFields.Select(f => new Upd53Template.InfoFieldDto { Ident = f.Ident, Value = f.Value }).ToList(),
            }).ToList(),
        };
    }

    private void ApplyTemplate(Upd53Template t)
    {
        DocumentNumber = t.DocumentNumber ?? string.Empty;
        DocumentDate   = t.DocumentDate != default ? t.DocumentDate : DateTime.Today;
        Function       = t.Function ?? "СЧФДОП";
        CorrectionNumber = t.CorrectionNumber ?? string.Empty;
        CorrectionDate   = t.CorrectionDate;

        ShipperIsSeller = t.ShipperIsSeller;
        ConsigneeIsBuyer = t.ConsigneeIsBuyer;
        PaymentDocNumber = t.PaymentDocNumber ?? string.Empty;
        PaymentDocDate = t.PaymentDocDate;
        ShipmentDocName = t.ShipmentDocName ?? "Универсальный передаточный документ";
        ShipmentDocNumber = t.ShipmentDocNumber ?? string.Empty;
        ShipmentDocDate = t.ShipmentDocDate;
        StateContractId = t.StateContractId ?? string.Empty;

        if (t.Supplier != null)  UpdTemplateService.ApplyParticipant(t.Supplier, Supplier);
        if (t.Shipper  != null)  UpdTemplateService.ApplyParticipant(t.Shipper, Shipper);
        if (t.Buyer    != null)  UpdTemplateService.ApplyParticipant(t.Buyer, Buyer);
        if (t.Consignee != null) UpdTemplateService.ApplyParticipant(t.Consignee, Consignee);

        CurrencyCode      = t.CurrencyCode ?? "643";
        CurrencyName      = t.CurrencyName ?? "Российский рубль";
        CurrencyRate      = t.CurrencyRate ?? string.Empty;
        OperationContent  = t.OperationContent ?? string.Empty;
        SaleOrder         = t.SaleOrder ?? string.Empty;
        TransferDate      = t.TransferDate;
        BaseDocName       = t.BaseDocName ?? string.Empty;
        BaseDocNumber     = t.BaseDocNumber ?? string.Empty;
        BaseDocDate       = t.BaseDocDate;
        AdvanceStateContractId = t.AdvanceStateContractId ?? string.Empty;
        AdvanceConditionCode   = t.AdvanceConditionCode ?? string.Empty;
        TransportInfo      = t.TransportInfo ?? string.Empty;
        Incoterms          = t.Incoterms ?? string.Empty;
        IncotermsVersion   = t.IncotermsVersion ?? string.Empty;
        GoodsTransferDate  = t.GoodsTransferDate;
        GoodsTransferInfo  = t.GoodsTransferInfo ?? string.Empty;
        HasTransferPerson  = t.HasTransferPerson;
        TransferPersonPosition = t.TransferPersonPosition ?? string.Empty;
        TransferPersonFio  = t.TransferPersonFio ?? string.Empty;
        ResponsibleTransferPosition = t.ResponsibleTransferPosition ?? "Генеральный директор";
        ResponsibleTransferFio = t.ResponsibleTransferFio ?? string.Empty;
        EconomicSubjectTransfer = t.EconomicSubjectTransfer ?? string.Empty;

        Obligations.Clear();
        if (t.Obligations != null)
            foreach (var o in t.Obligations)
                Obligations.Add(new Upd53Obligation { Code = o.Code ?? string.Empty, Name = o.Name ?? string.Empty });
        HeaderInfoFields.Clear();
        if (t.HeaderInfoFields != null)
            foreach (var f in t.HeaderInfoFields)
                HeaderInfoFields.Add(new Upd53InfoField { Ident = f.Ident ?? string.Empty, Value = f.Value ?? string.Empty });
        DocInfoFields.Clear();
        if (t.DocInfoFields != null)
            foreach (var f in t.DocInfoFields)
                DocInfoFields.Add(new Upd53InfoField { Ident = f.Ident ?? string.Empty, Value = f.Value ?? string.Empty });

        SignerLastName   = t.SignerLastName ?? string.Empty;
        SignerFirstName  = t.SignerFirstName ?? string.Empty;
        SignerMiddleName = t.SignerMiddleName ?? string.Empty;
        SignerPosition   = t.SignerPosition ?? string.Empty;
        SignerInn        = t.SignerInn ?? string.Empty;
        ProcurationNumber = t.ProcurationNumber ?? string.Empty;
        ProcurationDate  = t.ProcurationDate;
        SignerPowerKindIndex = t.SignerPowerKindIndex;
        SignerSignTypeIndex = t.SignerSignTypeIndex;
        SignerSignDate  = t.SignerSignDate;
        SignerUrn       = t.SignerUrn ?? string.Empty;
        SignerElProcDate = t.SignerElProcDate;
        SignerElProcInnerNo = t.SignerElProcInnerNo ?? string.Empty;
        SignerStorageSystemId = t.SignerStorageSystemId ?? string.Empty;
        SignerStorageUrl = t.SignerStorageUrl ?? string.Empty;

        Products.Clear();
        if (t.Products != null)
        {
            var line = 1;
            foreach (var p in t.Products)
            {
                var product = new Upd53Doc.Upd53Product
                {
                    LineNumber    = line++,
                    Name          = p.Name ?? string.Empty,
                    Code          = p.Code ?? string.Empty,
                    ProductTypeCode = p.ProductTypeCode ?? string.Empty,
                    UnitCode      = p.UnitCode ?? string.Empty,
                    UnitName      = p.UnitName ?? string.Empty,
                    Quantity      = p.Quantity,
                    Price         = p.Price,
                    ExciseAmount  = p.ExciseAmount,
                    VatRateLabel  = p.VatRateLabel ?? "20%",
                    Gtin          = p.Gtin ?? string.Empty,
                    GtdNumber     = p.GtdNumber ?? string.Empty,
                    CountryCode   = p.CountryCode ?? string.Empty,
                    CountryName   = p.CountryName ?? string.Empty,
                    TrackNo       = p.TrackNo ?? string.Empty,
                    TrackUnitCode = p.TrackUnitCode ?? string.Empty,
                    TrackUnitName = p.TrackUnitName ?? string.Empty,
                    TrackQty      = p.TrackQty,
                    TrackAmount   = p.TrackAmount,
                    TrackInfo     = p.TrackInfo ?? string.Empty,
                    Kiz           = p.Kiz ?? string.Empty,
                    PackNo        = p.PackNo ?? string.Empty,
                    MarkedQty     = p.MarkedQty,
                    BatchInfo     = p.BatchInfo ?? string.Empty,
                };
                if (p.InfoFields != null)
                    foreach (var f in p.InfoFields)
                        product.InfoFields.Add(new Upd53InfoField { Ident = f.Ident ?? string.Empty, Value = f.Value ?? string.Empty });
                product.TotalsChanged += RecalcTotals;
                Products.Add(product);
            }
        }
        RecalcTotals();
    }
}
