using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EdoXmlGenerator.Models;

public partial class Upd53Doc
{
    public enum ParticipantKind { Organization, IndividualEntrepreneur }

    public class Upd53Participant
    {
        public bool IsOrganization { get; set; } = true;
        public bool IsEmpty { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public string Inn { get; set; } = string.Empty;
        public string Kpp { get; set; } = string.Empty;
        public string Okpo { get; set; } = string.Empty;

        // Доп. реквизиты (атрибуты УчастникТип)
        public string ShortName { get; set; } = string.Empty;     // СокрНаим
        public string OpfCode { get; set; } = string.Empty;       // КодОПФ
        public string OpfFullName { get; set; } = string.Empty;   // ПолнНаимОПФ
        public string InfoForParticipant { get; set; } = string.Empty; // ИнфДляУчаст

        public string Federation { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Вид адреса: false = АдрИнф (свободный текст), true = АдрРФ (структурный)
        public bool UseStructuredAddress { get; set; }
        public string PostalIndex { get; set; } = string.Empty;   // Индекс
        public string RegionCode { get; set; } = string.Empty;    // КодРегион
        public string RegionName { get; set; } = string.Empty;    // НаимРегион
        public string District { get; set; } = string.Empty;      // Район
        public string City { get; set; } = string.Empty;          // Город
        public string Locality { get; set; } = string.Empty;      // НаселПункт
        public string Street { get; set; } = string.Empty;        // Улица
        public string House { get; set; } = string.Empty;         // Дом
        public string Building { get; set; } = string.Empty;      // Корпус
        public string Apartment { get; set; } = string.Empty;     // Кварт

        public string BankName { get; set; } = string.Empty;
        public string Bik { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string CorrAccount { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public enum SignerPowerKind
    {
        Head = 1,
        ByProcuration = 2,
        ByElectronicProcuration = 3,
        ByLaw = 4,
        ByPaperProcuration = 5,
        Other = 6
    }

    public class Upd53Signer
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public SignerPowerKind PowerKind { get; set; } = SignerPowerKind.Head;
        public string Inn { get; set; } = string.Empty;
        public string ProcurationNumber { get; set; } = string.Empty;
        public string ProcurationDateText { get; set; } = string.Empty;

        // Тип подписи (ТипПодпис: 1/2/3), дата подписания
        public int SignType { get; set; } = 2;
        public string SignDateText { get; set; } = string.Empty;

        // Электронная доверенность (СвДоверЭл) — МЧД
        public string Urn { get; set; } = string.Empty;            // НомДовер (36)
        public string ElProcDateText { get; set; } = string.Empty; // ДатаВыдДовер
        public string ElProcInnerNo { get; set; } = string.Empty;  // ВнНомДовер
        public string StorageSystemId { get; set; } = string.Empty;// ИдСистХран
        public string StorageUrl { get; set; } = string.Empty;     // УРЛСист
    }

    public partial class Upd53Product : ObservableObject
    {
        [ObservableProperty]
        private int _lineNumber;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _code = string.Empty; // Графа А (код/артикул)

        // Графа 1б: Код вида товара (ТН ВЭД ЕАЭС)
        [ObservableProperty]
        private string _productTypeCode = string.Empty;

        [ObservableProperty]
        private string _unitCode = string.Empty;

        [ObservableProperty]
        private string _unitName = string.Empty;

        [ObservableProperty]
        private decimal _quantity;

        [ObservableProperty]
        private decimal _price;

        // Графа 6: Сумма акциза (0 — без акциза)
        [ObservableProperty]
        private decimal _exciseAmount;

        [ObservableProperty]
        private string _vatRateLabel = "22%";

        [ObservableProperty]
        private string _gtin = string.Empty;

        [ObservableProperty]
        private string _gtdNumber = string.Empty;

        [ObservableProperty]
        private string _countryCode = string.Empty;

        [ObservableProperty]
        private string _countryName = string.Empty;

        // Прослеживаемость (СведПрослеж)
        [ObservableProperty]
        private string _trackNo = string.Empty;

        [ObservableProperty]
        private string _trackUnitCode = string.Empty;

        [ObservableProperty]
        private string _trackUnitName = string.Empty;

        [ObservableProperty]
        private decimal _trackQty;

        // Графа 14: Стоимость товара, подлежащего прослеживаемости, без налога
        [ObservableProperty]
        private decimal _trackAmount;

        [ObservableProperty]
        private string _trackInfo = string.Empty;

        // Маркировка (НомСредИдентТов)
        [ObservableProperty]
        private string _kiz = string.Empty;

        [ObservableProperty]
        private string _packNo = string.Empty;

        [ObservableProperty]
        private decimal _markedQty;

        [ObservableProperty]
        private string _batchInfo = string.Empty;

        public List<Upd53InfoField> InfoFields { get; } = new();

        public event Action? TotalsChanged;

        public decimal SumWithoutVat => Math.Round(Quantity * Price, 2);

        partial void OnQuantityChanged(decimal value) => NotifyTotals();
        partial void OnPriceChanged(decimal value) => NotifyTotals();
        partial void OnVatRateLabelChanged(string value) => NotifyTotals();

        private void NotifyTotals()
        {
            OnPropertyChanged(nameof(SumWithoutVat));
            TotalsChanged?.Invoke();
        }
    }

    public ObservableCollection<Upd53Participant> Suppliers { get; } = new();
    public ObservableCollection<Upd53Participant> Shippers { get; } = new();
    public ObservableCollection<Upd53Participant> Buyers { get; } = new();
    public ObservableCollection<Upd53Participant> Consignees { get; } = new();
    public ObservableCollection<Upd53Product> Products { get; } = new();
    public ObservableCollection<Upd53Signer> Signers { get; } = new();

    public string FileId { get; set; } = Guid.NewGuid().ToString("D").ToUpperInvariant();
    public string DocNumber { get; set; } = string.Empty;
    public DateTime DocDate { get; set; } = DateTime.Today;

    // Статус УПД: 1 = СЧФДОП (счет-фактура и передаточный документ), 2 = ДОП (передаточный документ)
    public int Status { get; set; } = 1;
    public string Function { get; set; } = "СЧФДОП";

    // Строка 3: Грузоотправитель — признак "он же" (совпадает с продавцом)
    public bool ShipperIsSeller { get; set; }

    // Строка 4: Грузополучатель — совпадает с покупателем
    public bool ConsigneeIsBuyer { get; set; }

    // Строка 5: К платежно-расчетному документу (СвПРД)
    public string PaymentDocNumber { get; set; } = string.Empty;
    public string PaymentDocDateText { get; set; } = string.Empty;
    public string PaymentDocAmount { get; set; } = string.Empty;

    // Строка 5а: Документ об отгрузке (ДокПодтвОтгрНом)
    public string ShipmentDocName { get; set; } = string.Empty;
    public string ShipmentDocNumber { get; set; } = string.Empty;
    public string ShipmentDocDateText { get; set; } = string.Empty;

    // Валюта (строка 7)
    public string CurrencyCode { get; set; } = "643";
    public string CurrencyName { get; set; } = "Российский рубль";
    public string CurrencyRate { get; set; } = string.Empty;

    // Строка 8: Идентификатор госконтракта
    public string StateContractId { get; set; } = string.Empty;

    // Сведения об отгрузке и передаче (строки [10]–[16])
    public string OperationContent { get; set; } = "Товары переданы, результаты работ (услуг) приняты без претензий";
    public string SaleOrder { get; set; } = string.Empty;
    public string TransferDateText { get; set; } = string.Empty;

    // Строка [10]: Основание передачи (договор/соглашение/заказ)
    public string BaseDocName { get; set; } = string.Empty;
    public string BaseDocNumber { get; set; } = string.Empty;
    public string BaseDocDateText { get; set; } = string.Empty;

    public string CorrectionReasonCode { get; set; } = string.Empty;
    public string CorrectionNumber { get; set; } = string.Empty;
    public string CorrectionDateText { get; set; } = string.Empty;

    // Инфополя ИнфПолФХЖ1 / ИнфПолФХЖ3 (список "Идентиф=Значен")
    public List<Upd53InfoField> HeaderInfoFields { get; } = new();
    public List<Upd53InfoField> DocInfoFields { get; } = new();

    // Авансовые реквизиты ДопСвФХЖ1
    public string AdvanceStateContractId { get; set; } = string.Empty; // ИдГосКон
    public string AdvanceConditionCode { get; set; } = string.Empty;    // СпОбстФСЧФ (1-6)
    public List<Upd53Obligation> Obligations { get; } = new();          // ВидОбяз

    // Строка [11]: Транспорт и условия доставки (Тран)
    public string TransportInfo { get; set; } = string.Empty;   // СвТран
    public string Incoterms { get; set; } = string.Empty;       // Инкотермс (3)
    public string IncotermsVersion { get; set; } = string.Empty;// ВерИнкотермс (4)

    // Строка [13], [14]: СвПерВещи (передача вещи)
    public string GoodsTransferDateText { get; set; } = string.Empty; // ДатаПерВещ
    public string GoodsTransferInfo { get; set; } = string.Empty;     // СвПерВещ

    // Строка [12]: СвЛицПер (лицо, передавшее товар / сдавшее работы)
    public bool HasTransferPerson { get; set; }
    public string TransferPersonKind { get; set; } = "Раб"; // "Раб" | "ФЛ" | "ПредОрг"
    public string TransferPersonPosition { get; set; } = string.Empty;
    public string TransferPersonFio { get; set; } = string.Empty;

    // Строка [15]: Ответственный за правильность оформления факта хоз. жизни (со стороны продавца)
    public string ResponsibleTransferPosition { get; set; } = string.Empty;
    public string ResponsibleTransferFio { get; set; } = string.Empty;

    // Строка [16]: Наименование составителя документа (в т.ч. комиссионера/агента)
    public string EconomicSubjectTransfer { get; set; } = string.Empty;

    public string AdditionalInfo { get; set; } = string.Empty;
}

public class Upd53InfoField
{
    public string Ident { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class Upd53Obligation
{
    public string Code { get; set; } = string.Empty;   // КодВидОбяз
    public string Name { get; set; } = string.Empty;   // НаимВидОбяз
}
