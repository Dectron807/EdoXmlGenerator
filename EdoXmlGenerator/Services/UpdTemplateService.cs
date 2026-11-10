using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace EdoXmlGenerator.Services;

/// <summary>
/// Сохранение и загрузка заполненной формы УПД в виде XML-шаблона (справочник).
/// </summary>
public class UpdTemplateService
{
    public string Save(Upd53Template template)
    {
        var serializer = new XmlSerializer(typeof(Upd53Template));
        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false)
        };
        using var ms = new MemoryStream();
        using (var writer = System.Xml.XmlWriter.Create(ms, settings))
        {
            serializer.Serialize(writer, template);
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public string SaveJson(Upd53Template template)
    {
        return JsonConvert.SerializeObject(template, Formatting.Indented);
    }

    public Upd53Template Load(string filePath)
    {
        var text = File.ReadAllText(filePath);
        var trimmed = text.TrimStart();
        if (trimmed.StartsWith("<", StringComparison.Ordinal))
            return LoadXml(text);
        var template = JsonConvert.DeserializeObject<Upd53Template>(text)
                       ?? throw new InvalidDataException("Файл пуст или имеет неверную структуру.");
        return template;
    }

    private static Upd53Template LoadXml(string xml)
    {
        var serializer = new XmlSerializer(typeof(Upd53Template));
        using var reader = new StringReader(xml);
        var template = serializer.Deserialize(reader) as Upd53Template
                       ?? throw new InvalidDataException("Файл не является XML-шаблоном УПД.");
        return template;
    }

    public void SaveToFile(string filePath, Upd53Template template)
    {
        File.WriteAllText(filePath, Save(template), new UTF8Encoding(false));
    }

    public static void ApplyParticipant(Upd53Template.ParticipantDto p,
        EdoXmlGenerator.Models.Upd53Doc.Upd53Participant target)
    {
        if (target == null) return;
        target.Name = p?.Name ?? string.Empty;
        target.Inn = p?.Inn ?? string.Empty;
        target.Kpp = p?.Kpp ?? string.Empty;
        target.Okpo = p?.Okpo ?? string.Empty;
        target.ShortName = p?.ShortName ?? string.Empty;
        target.OpfCode = p?.OpfCode ?? string.Empty;
        target.OpfFullName = p?.OpfFullName ?? string.Empty;
        target.InfoForParticipant = p?.InfoForParticipant ?? string.Empty;
        target.Address = p?.Address ?? string.Empty;
        target.UseStructuredAddress = p?.UseStructuredAddress ?? false;
        target.PostalIndex = p?.PostalIndex ?? string.Empty;
        target.RegionCode = p?.RegionCode ?? string.Empty;
        target.RegionName = p?.RegionName ?? string.Empty;
        target.District = p?.District ?? string.Empty;
        target.City = p?.City ?? string.Empty;
        target.Locality = p?.Locality ?? string.Empty;
        target.Street = p?.Street ?? string.Empty;
        target.House = p?.House ?? string.Empty;
        target.Building = p?.Building ?? string.Empty;
        target.Apartment = p?.Apartment ?? string.Empty;
        target.BankName = p?.BankName ?? string.Empty;
        target.Bik = p?.Bik ?? string.Empty;
        target.AccountNumber = p?.AccountNumber ?? string.Empty;
        target.CorrAccount = p?.CorrAccount ?? string.Empty;
        target.Phone = p?.Phone ?? string.Empty;
        target.Email = p?.Email ?? string.Empty;
    }

    public static Upd53Template.ParticipantDto FromParticipant(
        EdoXmlGenerator.Models.Upd53Doc.Upd53Participant src)
    {
        return new Upd53Template.ParticipantDto
        {
            Name = src?.Name ?? string.Empty,
            Inn = src?.Inn ?? string.Empty,
            Kpp = src?.Kpp ?? string.Empty,
            Okpo = src?.Okpo ?? string.Empty,
            ShortName = src?.ShortName ?? string.Empty,
            OpfCode = src?.OpfCode ?? string.Empty,
            OpfFullName = src?.OpfFullName ?? string.Empty,
            InfoForParticipant = src?.InfoForParticipant ?? string.Empty,
            Address = src?.Address ?? string.Empty,
            UseStructuredAddress = src?.UseStructuredAddress ?? false,
            PostalIndex = src?.PostalIndex ?? string.Empty,
            RegionCode = src?.RegionCode ?? string.Empty,
            RegionName = src?.RegionName ?? string.Empty,
            District = src?.District ?? string.Empty,
            City = src?.City ?? string.Empty,
            Locality = src?.Locality ?? string.Empty,
            Street = src?.Street ?? string.Empty,
            House = src?.House ?? string.Empty,
            Building = src?.Building ?? string.Empty,
            Apartment = src?.Apartment ?? string.Empty,
            BankName = src?.BankName ?? string.Empty,
            Bik = src?.Bik ?? string.Empty,
            AccountNumber = src?.AccountNumber ?? string.Empty,
            CorrAccount = src?.CorrAccount ?? string.Empty,
            Phone = src?.Phone ?? string.Empty,
            Email = src?.Email ?? string.Empty,
        };
    }
}

public class Upd53Template
{
    public string? DocumentNumber { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    public string? Function { get; set; }
    public string? CorrectionNumber { get; set; }
    public DateTime? CorrectionDate { get; set; }

    public bool ShipperIsSeller { get; set; }
    public bool ConsigneeIsBuyer { get; set; }

    public string? PaymentDocNumber { get; set; }
    public DateTime? PaymentDocDate { get; set; }
    public string? ShipmentDocName { get; set; }
    public string? ShipmentDocNumber { get; set; }
    public DateTime? ShipmentDocDate { get; set; }

    public string? StateContractId { get; set; }

    public ParticipantDto? Supplier { get; set; }
    public ParticipantDto? Shipper { get; set; }
    public ParticipantDto? Buyer { get; set; }
    public ParticipantDto? Consignee { get; set; }

    public string? CurrencyCode { get; set; }
    public string? CurrencyName { get; set; }
    public string? CurrencyRate { get; set; }
    public string? OperationContent { get; set; }
    public string? SaleOrder { get; set; }
    public DateTime? TransferDate { get; set; }
    public string? BaseDocName { get; set; }
    public string? BaseDocNumber { get; set; }
    public DateTime? BaseDocDate { get; set; }

    // Авансовые реквизиты / инфополя / транспорт / передача
    public string? AdvanceStateContractId { get; set; }
    public string? AdvanceConditionCode { get; set; }
    public List<ObligationDto>? Obligations { get; set; }
    public List<InfoFieldDto>? HeaderInfoFields { get; set; }
    public List<InfoFieldDto>? DocInfoFields { get; set; }
    public string? TransportInfo { get; set; }
    public string? Incoterms { get; set; }
    public string? IncotermsVersion { get; set; }
    public DateTime? GoodsTransferDate { get; set; }
    public string? GoodsTransferInfo { get; set; }
    public bool HasTransferPerson { get; set; }
    public string? TransferPersonPosition { get; set; }
    public string? TransferPersonFio { get; set; }

    public string? ResponsibleTransferPosition { get; set; }
    public string? ResponsibleTransferFio { get; set; }
    public string? EconomicSubjectTransfer { get; set; }

    public string? SignerLastName { get; set; }
    public string? SignerFirstName { get; set; }
    public string? SignerMiddleName { get; set; }
    public string? SignerPosition { get; set; }
    public string? SignerInn { get; set; }
    public string? ProcurationNumber { get; set; }
    public DateTime? ProcurationDate { get; set; }
    public int SignerPowerKindIndex { get; set; }
    public int SignerSignTypeIndex { get; set; }
    public DateTime? SignerSignDate { get; set; }
    public string? SignerUrn { get; set; }
    public DateTime? SignerElProcDate { get; set; }
    public string? SignerElProcInnerNo { get; set; }
    public string? SignerStorageSystemId { get; set; }
    public string? SignerStorageUrl { get; set; }

    public List<ProductDto>? Products { get; set; }

    public class ParticipantDto
    {
        public string? Name { get; set; }
        public string? Inn { get; set; }
        public string? Kpp { get; set; }
        public string? Okpo { get; set; }
        public string? ShortName { get; set; }
        public string? OpfCode { get; set; }
        public string? OpfFullName { get; set; }
        public string? InfoForParticipant { get; set; }
        public string? Address { get; set; }
        public bool UseStructuredAddress { get; set; }
        public string? PostalIndex { get; set; }
        public string? RegionCode { get; set; }
        public string? RegionName { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Locality { get; set; }
        public string? Street { get; set; }
        public string? House { get; set; }
        public string? Building { get; set; }
        public string? Apartment { get; set; }
        public string? BankName { get; set; }
        public string? Bik { get; set; }
        public string? AccountNumber { get; set; }
        public string? CorrAccount { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class ObligationDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
    }

    public class InfoFieldDto
    {
        public string? Ident { get; set; }
        public string? Value { get; set; }
    }

    public class ProductDto
    {
        public int LineNumber { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? ProductTypeCode { get; set; }
        public string? UnitCode { get; set; }
        public string? UnitName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal ExciseAmount { get; set; }
        public string? VatRateLabel { get; set; }
        public string? Gtin { get; set; }
        public string? GtdNumber { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public string? TrackNo { get; set; }
        public string? TrackUnitCode { get; set; }
        public string? TrackUnitName { get; set; }
        public decimal TrackQty { get; set; }
        public decimal TrackAmount { get; set; }
        public string? TrackInfo { get; set; }
        public string? Kiz { get; set; }
        public string? PackNo { get; set; }
        public decimal MarkedQty { get; set; }
        public string? BatchInfo { get; set; }
        public List<InfoFieldDto>? InfoFields { get; set; }
    }
}
