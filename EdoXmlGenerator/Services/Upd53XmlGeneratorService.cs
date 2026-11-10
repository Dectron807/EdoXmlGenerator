using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using EdoXmlGenerator.Models;

namespace EdoXmlGenerator.Services;

public class Upd53XmlGeneratorService
{
    private const string XsdFileName = "ON_NSCHFDOPPR_1_997_01_05_03_05.xsd";

    public string Generate(Upd53Doc doc, string? xsdDir = null)
    {
        PreValidate(doc);

        var xml = BuildXml(doc);
        var errors = Validate(xml, xsdDir);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Сформированный XML не прошёл проверку по XSD:\n" + string.Join("\n", errors));

        return xml;
    }

    /// <summary>
    /// Проверяет обязательные реквизиты до генерации и выводит понятные сообщения,
    /// чтобы пользователь не видел зашифрованные ошибки XSD.
    /// </summary>
    private static void PreValidate(Upd53Doc doc)
    {
        if (string.IsNullOrWhiteSpace(doc.DocNumber))
            throw new InvalidOperationException("Не заполнен номер счёта-фактуры / УПД (поле «Номер документа»).");

        if (doc.Function == "ИСПР" && (string.IsNullOrWhiteSpace(doc.CorrectionNumber) || string.IsNullOrWhiteSpace(doc.CorrectionDateText)))
            throw new InvalidOperationException("Для структурной корректировки (Функция = ИСПР) заполните номер и дату исправления.");

        var sellers = doc.Suppliers.Where(x => !string.IsNullOrWhiteSpace(x.Inn) || !string.IsNullOrWhiteSpace(x.Name)).ToList();
        if (sellers.Count == 0)
            throw new InvalidOperationException("Не заполнены реквизиты продавца (необходимо указать ИНН и наименование).");

        var first = sellers[0];
        if (string.IsNullOrWhiteSpace(first.Inn))
            throw new InvalidOperationException("Не заполнен ИНН продавца — без него нельзя сформировать юридически значимый УПД.");

        if (first.IsOrganization && string.IsNullOrWhiteSpace(first.Name))
            throw new InvalidOperationException("Не заполнено наименование организации продавца (поле «Наименование»).");

        var buyers = doc.Buyers.Where(x => !string.IsNullOrWhiteSpace(x.Inn) || !string.IsNullOrWhiteSpace(x.Name)).ToList();
        if (buyers.Count > 0)
        {
            var buyer = buyers[0];
            if (buyer.IsOrganization && string.IsNullOrWhiteSpace(buyer.Name))
                throw new InvalidOperationException("Не заполнено наименование организации покупателя.");
        }

        if (string.IsNullOrWhiteSpace(doc.CurrencyCode))
            throw new InvalidOperationException("Не указан код валюты документа (по умолчанию 643 — Российский рубль).");

        if (!string.IsNullOrWhiteSpace(doc.StateContractId))
        {
            var id = doc.StateContractId.Trim();
            if (id.Length < 20 || id.Length > 25)
                throw new InvalidOperationException($"Идентификатор государственного контракта (строка 8) должен содержать от 20 до 25 символов согласно формату ФНС (указано: {id.Length}).");
        }
    }

    public List<string> Validate(string xml, string? xsdDir = null)
    {
        var errors = new List<string>();
        var schemas = LoadSchemas(xsdDir);
        if (schemas == null)
        {
            errors.Add("Не найден файл XSD (ON_NSCHFDOPPR_1_997_01_05_03_05.xsd).");
            return errors;
        }

        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xml);
        xmlDoc.Schemas = schemas;
        xmlDoc.Validate((s, e) => errors.Add($"[{e.Severity}] {e.Message}"));
        return errors;
    }

    private static XmlSchemaSet? LoadSchemas(string? xsdDir)
    {
        var path = FindXsd(xsdDir);
        if (path == null) return null;

        var schemas = new XmlSchemaSet();
        using (var reader = XmlReader.Create(path,
                   new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
        {
            schemas.Add(null, reader);
        }
        schemas.Compile();
        return schemas;
    }

    private static string? FindXsd(string? xsdDir)
    {
        if (xsdDir != null)
        {
            var p = Path.Combine(xsdDir, XsdFileName);
            if (File.Exists(p)) return p;
        }

        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Xsd", "5_03", "xsd_2026", XsdFileName),
            Path.Combine(baseDir, "Xsd", XsdFileName),
            Path.Combine(baseDir, XsdFileName),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private static string BuildXml(Upd53Doc doc)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = false
        };

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("Файл");
            writer.WriteAttributeString("ИдФайл", BuildFileId(doc));
            writer.WriteAttributeString("ВерсФорм", "5.03");
            writer.WriteAttributeString("ВерсПрог", "EdoXmlGenerator 1.0");

            WriteDocument(writer, doc);

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static string BuildFileId(Upd53Doc doc)
    {
        if (!string.IsNullOrWhiteSpace(doc.FileId) && doc.FileId.StartsWith("ON_NSCHFDOPPR_"))
            return doc.FileId;

        // Регламентный формат имени файла: ON_NSCHFDOPPR_<покупатель>_<продавец>_<дата>_<guid>
        var sellerInn = doc.Suppliers.FirstOrDefault()?.Inn?.Trim() ?? "0000000000";
        var buyerInn = doc.Buyers.FirstOrDefault()?.Inn?.Trim() ?? "0000000000";
        var dateStr = DateTime.Today.ToString("yyyyMMdd");
        var guid = Guid.NewGuid().ToString("D").ToLowerInvariant();

        return $"ON_NSCHFDOPPR_{buyerInn}_{sellerInn}_{dateStr}_{guid}";
    }

    private static void WriteDocument(XmlWriter w, Upd53Doc doc)
    {
        w.WriteStartElement("Документ");
        w.WriteAttributeString("КНД", "1115131");
        w.WriteAttributeString("Функция", doc.Function);
        w.WriteAttributeString("ДатаИнфПр", DateTime.Today.ToString("dd.MM.yyyy"));
        w.WriteAttributeString("ВремИнфПр", DateTime.Now.ToString("HH.mm.ss"));

        WriteSchfakt(w, doc);
        WriteTable(w, doc);
        WriteTransfer(w, doc);
        WriteSigners(w, doc);

        w.WriteEndElement();
    }

    private static void WriteSchfakt(XmlWriter w, Upd53Doc doc)
    {
        w.WriteStartElement("СвСчФакт");
        if (!string.IsNullOrWhiteSpace(doc.DocNumber)) w.WriteAttributeString("НомерДок", doc.DocNumber);
        if (doc.DocDate != default) w.WriteAttributeString("ДатаДок", doc.DocDate.ToString("dd.MM.yyyy"));

        if (!string.IsNullOrWhiteSpace(doc.CorrectionNumber) && !string.IsNullOrWhiteSpace(doc.CorrectionDateText))
        {
            w.WriteStartElement("ИспрДок");
            w.WriteAttributeString("НомИспр", doc.CorrectionNumber);
            w.WriteAttributeString("ДатаИспр", doc.CorrectionDateText);
            w.WriteEndElement();
        }

        // Продавцы (строка 2, 2а, 2б счета-фактуры)
        foreach (var p in doc.Suppliers.Where(x => !string.IsNullOrWhiteSpace(x.Inn) || !string.IsNullOrWhiteSpace(x.Name)))
            WriteParticipant(w, "СвПрод", p);

        // Грузоотправитель (строка 3 счета-фактуры)
        if (doc.ShipperIsSeller)
        {
            w.WriteStartElement("ГрузОт");
            w.WriteElementString("ОнЖе", "он же");
            w.WriteEndElement();
        }
        else
        {
            var shippers = doc.Shippers.Where(x => !string.IsNullOrWhiteSpace(x.Inn) || !string.IsNullOrWhiteSpace(x.Name)).ToList();
            foreach (var p in shippers)
            {
                w.WriteStartElement("ГрузОт");
                WriteParticipantBody(w, "ГрузОтпр", p);
                w.WriteEndElement();
            }
        }

        // Грузополучатель (строка 4 счета-фактуры)
        var consignees = doc.Consignees.Where(x => !string.IsNullOrWhiteSpace(x.Inn) || !string.IsNullOrWhiteSpace(x.Name)).ToList();
        foreach (var p in consignees)
            WriteParticipant(w, "ГрузПолуч", p);

        // Строка 5: К платежно-расчетному документу (СвПРД)
        if (!string.IsNullOrWhiteSpace(doc.PaymentDocNumber) || !string.IsNullOrWhiteSpace(doc.PaymentDocDateText))
        {
            w.WriteStartElement("СвПРД");
            if (!string.IsNullOrWhiteSpace(doc.PaymentDocNumber)) w.WriteAttributeString("НомерПРД", doc.PaymentDocNumber.Trim());
            if (!string.IsNullOrWhiteSpace(doc.PaymentDocDateText)) w.WriteAttributeString("ДатаПРД", doc.PaymentDocDateText.Trim());
            if (!string.IsNullOrWhiteSpace(doc.PaymentDocAmount)) w.WriteAttributeString("СуммаПРД", doc.PaymentDocAmount.Trim());
            w.WriteEndElement();
        }

        // Строка 5а: Документ об отгрузке (ДокПодтвОтгрНом) - обязателен для СЧФ/СЧФДОП по п. 5а ст. 169 НК РФ
        var shipmentDocName = !string.IsNullOrWhiteSpace(doc.ShipmentDocName)
            ? doc.ShipmentDocName
            : (!string.IsNullOrWhiteSpace(doc.BaseDocName) ? doc.BaseDocName : "Универсальный передаточный документ");

        var shipmentDocNumber = !string.IsNullOrWhiteSpace(doc.ShipmentDocNumber)
            ? doc.ShipmentDocNumber
            : (!string.IsNullOrWhiteSpace(doc.DocNumber) ? doc.DocNumber : "1");

        var shipmentDocDate = !string.IsNullOrWhiteSpace(doc.ShipmentDocDateText)
            ? doc.ShipmentDocDateText
            : doc.DocDate.ToString("dd.MM.yyyy");

        w.WriteStartElement("ДокПодтвОтгрНом");
        w.WriteAttributeString("РеквНаимДок", shipmentDocName);
        w.WriteAttributeString("РеквНомерДок", shipmentDocNumber);
        w.WriteAttributeString("РеквДатаДок", shipmentDocDate);
        w.WriteEndElement();

        // Покупатель (строка 6, 6а, 6б счета-фактуры)
        foreach (var p in doc.Buyers.Where(x => !string.IsNullOrWhiteSpace(x.Inn) || !string.IsNullOrWhiteSpace(x.Name)))
            WriteParticipant(w, "СвПокуп", p);

        // Валюта (строка 7)
        w.WriteStartElement("ДенИзм");
        w.WriteAttributeString("КодОКВ", string.IsNullOrWhiteSpace(doc.CurrencyCode) ? "643" : doc.CurrencyCode);
        w.WriteAttributeString("НаимОКВ", string.IsNullOrWhiteSpace(doc.CurrencyName) ? "Российский рубль" : doc.CurrencyName);
        if (!string.IsNullOrWhiteSpace(doc.CurrencyRate)) w.WriteAttributeString("КурсВал", doc.CurrencyRate);
        w.WriteEndElement();

        // Строка 8: ДопСвФХЖ1 (Идентификатор госконтракта и авансы)
        WriteAdvanceInfo(w, doc);
        WriteHeaderInfoFields(w, doc);

        w.WriteEndElement();
    }

    private static void WriteAdvanceInfo(XmlWriter w, Upd53Doc doc)
    {
        var contractId = !string.IsNullOrWhiteSpace(doc.StateContractId)
            ? doc.StateContractId
            : doc.AdvanceStateContractId;

        var hasFields = !string.IsNullOrWhiteSpace(contractId) ||
                        !string.IsNullOrWhiteSpace(doc.AdvanceConditionCode) ||
                        doc.Obligations.Count > 0;
        if (!hasFields) return;

        w.WriteStartElement("ДопСвФХЖ1");
        if (!string.IsNullOrWhiteSpace(doc.AdvanceConditionCode)) w.WriteAttributeString("СпОбстФСЧФ", doc.AdvanceConditionCode);
        if (!string.IsNullOrWhiteSpace(contractId)) w.WriteAttributeString("ИдГосКон", contractId);

        if (doc.Obligations.Count > 0)
        {
            foreach (var ob in doc.Obligations)
            {
                if (string.IsNullOrWhiteSpace(ob.Code) && string.IsNullOrWhiteSpace(ob.Name)) continue;
                w.WriteStartElement("ВидОбяз");
                if (!string.IsNullOrWhiteSpace(ob.Code)) w.WriteAttributeString("КодВидОбяз", ob.Code);
                if (!string.IsNullOrWhiteSpace(ob.Name)) w.WriteAttributeString("НаимВидОбяз", ob.Name);
                w.WriteEndElement();
            }
        }
        w.WriteEndElement();
    }

    private static void WriteHeaderInfoFields(XmlWriter w, Upd53Doc doc)
    {
        var fields = doc.HeaderInfoFields.Where(f => !string.IsNullOrWhiteSpace(f.Ident) || !string.IsNullOrWhiteSpace(f.Value)).ToList();
        if (fields.Count == 0) return;

        w.WriteStartElement("ИнфПолФХЖ1");
        foreach (var f in fields)
        {
            w.WriteStartElement("ТекстИнф");
            if (!string.IsNullOrWhiteSpace(f.Ident)) w.WriteAttributeString("Идентиф", f.Ident);
            if (!string.IsNullOrWhiteSpace(f.Value)) w.WriteAttributeString("Значен", f.Value);
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    private static void WriteDocInfoFields(XmlWriter w, Upd53Doc doc)
    {
        var fields = new List<Upd53InfoField>(doc.DocInfoFields);

        if (!string.IsNullOrWhiteSpace(doc.ResponsibleTransferFio))
        {
            fields.Add(new Upd53InfoField { Ident = "ОтветственныйЗаОформлениеФИО", Value = doc.ResponsibleTransferFio });
            if (!string.IsNullOrWhiteSpace(doc.ResponsibleTransferPosition))
                fields.Add(new Upd53InfoField { Ident = "ОтветственныйЗаОформлениеДолжность", Value = doc.ResponsibleTransferPosition });
        }

        if (!string.IsNullOrWhiteSpace(doc.EconomicSubjectTransfer))
        {
            fields.Add(new Upd53InfoField { Ident = "СоставительДокумента", Value = doc.EconomicSubjectTransfer });
        }

        var valid = fields.Where(f => !string.IsNullOrWhiteSpace(f.Ident) || !string.IsNullOrWhiteSpace(f.Value)).ToList();
        if (valid.Count == 0) return;

        w.WriteStartElement("ИнфПолФХЖ3");
        foreach (var f in valid)
        {
            w.WriteStartElement("ТекстИнф");
            if (!string.IsNullOrWhiteSpace(f.Ident)) w.WriteAttributeString("Идентиф", f.Ident);
            if (!string.IsNullOrWhiteSpace(f.Value)) w.WriteAttributeString("Значен", f.Value);
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    private static void WriteParticipant(XmlWriter w, string elementName, Upd53Doc.Upd53Participant p)
    {
        WriteParticipantBody(w, elementName, p);
    }

    private static void WriteParticipantBody(XmlWriter w, string elementName, Upd53Doc.Upd53Participant p)
    {
        w.WriteStartElement(elementName);
        if (!string.IsNullOrWhiteSpace(p.Okpo)) w.WriteAttributeString("ОКПО", p.Okpo);
        if (!string.IsNullOrWhiteSpace(p.ShortName)) w.WriteAttributeString("СокрНаим", p.ShortName);
        if (!string.IsNullOrWhiteSpace(p.OpfCode)) w.WriteAttributeString("КодОПФ", p.OpfCode);
        if (!string.IsNullOrWhiteSpace(p.OpfFullName)) w.WriteAttributeString("ПолнНаимОПФ", p.OpfFullName);
        if (!string.IsNullOrWhiteSpace(p.InfoForParticipant)) w.WriteAttributeString("ИнфДляУчаст", p.InfoForParticipant);
        if (!string.IsNullOrWhiteSpace(p.Federation)) w.WriteAttributeString("СтруктПодр", p.Federation);

        w.WriteStartElement("ИдСв");
        w.WriteStartElement(p.IsOrganization ? "СвЮЛУч" : "СвИП");
        if (p.IsOrganization)
        {
            w.WriteAttributeString("НаимОрг", string.IsNullOrWhiteSpace(p.Name) ? "Организация" : p.Name);
            if (!string.IsNullOrWhiteSpace(p.Inn)) w.WriteAttributeString("ИННЮЛ", p.Inn);
            if (!string.IsNullOrWhiteSpace(p.Kpp)) w.WriteAttributeString("КПП", p.Kpp);
        }
        else
        {
            w.WriteAttributeString("ИННФЛ", p.Inn);
            w.WriteStartElement("ФИО");
            WriteFio(w, p.Name);
            w.WriteEndElement();
        }
        w.WriteEndElement(); // СвЮЛУч / СвИП
        w.WriteEndElement(); // ИдСв

        if (!p.UseStructuredAddress)
        {
            var addrText = string.IsNullOrWhiteSpace(p.Address) ? "Россия" : p.Address;
            w.WriteStartElement("Адрес");
            w.WriteStartElement("АдрИнф");
            w.WriteAttributeString("КодСтр", "643");
            w.WriteAttributeString("НаимСтран", "Россия");
            w.WriteAttributeString("АдрТекст", addrText);
            w.WriteEndElement();
            w.WriteEndElement();
        }
        else
        {
            w.WriteStartElement("Адрес");
            w.WriteStartElement("АдрРФ");
            if (!string.IsNullOrWhiteSpace(p.PostalIndex)) w.WriteAttributeString("Индекс", p.PostalIndex);
            if (!string.IsNullOrWhiteSpace(p.RegionCode)) w.WriteAttributeString("КодРегион", p.RegionCode);
            if (!string.IsNullOrWhiteSpace(p.RegionName)) w.WriteAttributeString("НаимРегион", p.RegionName);
            if (!string.IsNullOrWhiteSpace(p.District)) w.WriteAttributeString("Район", p.District);
            if (!string.IsNullOrWhiteSpace(p.City)) w.WriteAttributeString("Город", p.City);
            if (!string.IsNullOrWhiteSpace(p.Locality)) w.WriteAttributeString("НаселПункт", p.Locality);
            if (!string.IsNullOrWhiteSpace(p.Street)) w.WriteAttributeString("Улица", p.Street);
            if (!string.IsNullOrWhiteSpace(p.House)) w.WriteAttributeString("Дом", p.House);
            if (!string.IsNullOrWhiteSpace(p.Building)) w.WriteAttributeString("Корпус", p.Building);
            if (!string.IsNullOrWhiteSpace(p.Apartment)) w.WriteAttributeString("Кварт", p.Apartment);
            w.WriteEndElement();
            w.WriteEndElement();
        }

        if (!string.IsNullOrWhiteSpace(p.BankName) || !string.IsNullOrWhiteSpace(p.Bik) ||
            !string.IsNullOrWhiteSpace(p.AccountNumber))
        {
            w.WriteStartElement("БанкРекв");
            if (!string.IsNullOrWhiteSpace(p.AccountNumber)) w.WriteAttributeString("НомерСчета", p.AccountNumber);
            w.WriteStartElement("СвБанк");
            if (!string.IsNullOrWhiteSpace(p.BankName)) w.WriteAttributeString("НаимБанк", p.BankName);
            if (!string.IsNullOrWhiteSpace(p.Bik)) w.WriteAttributeString("БИК", p.Bik);
            if (!string.IsNullOrWhiteSpace(p.CorrAccount)) w.WriteAttributeString("КорСчет", p.CorrAccount);
            w.WriteEndElement();
            w.WriteEndElement();
        }

        if (!string.IsNullOrWhiteSpace(p.Phone) || !string.IsNullOrWhiteSpace(p.Email))
        {
            w.WriteStartElement("Контакт");
            if (!string.IsNullOrWhiteSpace(p.Phone))
            {
                w.WriteStartElement("Тлф");
                w.WriteValue(p.Phone);
                w.WriteEndElement();
            }
            if (!string.IsNullOrWhiteSpace(p.Email))
            {
                w.WriteStartElement("ЭлПочта");
                w.WriteValue(p.Email);
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }

        w.WriteEndElement();
    }

    private static void WriteFio(XmlWriter w, string fullName)
    {
        var parts = (fullName ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        w.WriteAttributeString("Фамилия", parts.Length >= 1 ? parts[0] : "Иванов");
        w.WriteAttributeString("Имя", parts.Length >= 2 ? parts[1] : "Иван");
        if (parts.Length >= 3) w.WriteAttributeString("Отчество", parts[2]);
    }

    private static void WriteTable(XmlWriter w, Upd53Doc doc)
    {
        if (doc.Products.Count == 0) return;

        w.WriteStartElement("ТаблСчФакт");
        var line = 1;
        foreach (var p in doc.Products)
        {
            if (string.IsNullOrWhiteSpace(p.Name)) continue;
            w.WriteStartElement("СведТов");
            w.WriteAttributeString("НомСтр", line.ToString(CultureInfo.InvariantCulture));
            w.WriteAttributeString("НаимТов", p.Name);
            if (!string.IsNullOrWhiteSpace(p.UnitCode)) w.WriteAttributeString("ОКЕИ_Тов", p.UnitCode);
            if (!string.IsNullOrWhiteSpace(p.UnitName)) w.WriteAttributeString("НаимЕдИзм", p.UnitName);
            if (p.Quantity != 0) w.WriteAttributeString("КолТов", p.Quantity.ToString("0.##", CultureInfo.InvariantCulture));
            if (p.Price != 0) w.WriteAttributeString("ЦенаТов", p.Price.ToString("0.##", CultureInfo.InvariantCulture));
            if (p.SumWithoutVat != 0) w.WriteAttributeString("СтТовБезНДС", p.SumWithoutVat.ToString("0.##", CultureInfo.InvariantCulture));
            w.WriteAttributeString("НалСт", string.IsNullOrWhiteSpace(p.VatRateLabel) ? "22%" : p.VatRateLabel);
            if (p.SumWithoutVat != 0)
            {
                w.WriteAttributeString("СтТовУчНал", TotalWithVat(p).ToString("0.##", CultureInfo.InvariantCulture));
            }

            // Графа 11 / ДТ
            if (!string.IsNullOrWhiteSpace(p.GtdNumber) || !string.IsNullOrWhiteSpace(p.CountryCode))
            {
                w.WriteStartElement("СвДТ");
                if (!string.IsNullOrWhiteSpace(p.CountryCode)) w.WriteAttributeString("КодПроисх", p.CountryCode.Trim());
                if (!string.IsNullOrWhiteSpace(p.GtdNumber)) w.WriteAttributeString("НомерДТ", p.GtdNumber.Trim());
                w.WriteEndElement();
            }

            var gtin = p.Gtin?.Trim();
            var hasTrack = !string.IsNullOrWhiteSpace(p.TrackNo);
            var hasMarking = !string.IsNullOrWhiteSpace(p.Kiz) || !string.IsNullOrWhiteSpace(p.PackNo) ||
                             p.MarkedQty != 0 || !string.IsNullOrWhiteSpace(p.BatchInfo);
            var hasProductType = !string.IsNullOrWhiteSpace(p.ProductTypeCode);
            var hasCode = !string.IsNullOrWhiteSpace(p.Code);

            if (gtin?.Length == 14 || !string.IsNullOrWhiteSpace(p.CountryCode) || hasTrack || hasMarking || hasProductType || hasCode)
            {
                w.WriteStartElement("ДопСведТов");
                if (hasProductType)
                {
                    var pCode = p.ProductTypeCode.Trim();
                    if (pCode.Length > 0 && pCode.Length < 10 && pCode.All(char.IsDigit))
                        pCode = pCode.PadRight(10, '0');
                    if (pCode.Length == 10)
                        w.WriteAttributeString("КодВидТов", pCode);
                }
                if (hasCode) w.WriteAttributeString("КодТов", p.Code.Trim());
                if (gtin?.Length == 14) w.WriteAttributeString("ГТИН", gtin);

                if (!string.IsNullOrWhiteSpace(p.CountryCode))
                {
                    var name = string.IsNullOrWhiteSpace(p.CountryName)
                        ? (p.CountryCode.Trim() == "643" ? "Россия" : p.CountryCode.Trim())
                        : p.CountryName.Trim();
                    w.WriteStartElement("КрНаимСтрПр");
                    w.WriteString(name);
                    w.WriteEndElement();
                }

                if (hasTrack)
                {
                    w.WriteStartElement("СведПрослеж");
                    w.WriteAttributeString("НомТовПрослеж", p.TrackNo.Trim());
                    if (!string.IsNullOrWhiteSpace(p.TrackUnitCode)) w.WriteAttributeString("ЕдИзмПрослеж", p.TrackUnitCode);
                    if (!string.IsNullOrWhiteSpace(p.TrackUnitName)) w.WriteAttributeString("НаимЕдИзмПрослеж", p.TrackUnitName);
                    if (p.TrackQty != 0) w.WriteAttributeString("КолВЕдПрослеж", p.TrackQty.ToString("0.##", CultureInfo.InvariantCulture));
                    // Графа 14: Стоимость товара, подлежащего прослеживаемости
                    var trackSum = p.TrackAmount != 0 ? p.TrackAmount : p.SumWithoutVat;
                    if (trackSum != 0) w.WriteAttributeString("СтТовБезНДСПрослеж", trackSum.ToString("0.##", CultureInfo.InvariantCulture));
                    if (!string.IsNullOrWhiteSpace(p.TrackInfo)) w.WriteAttributeString("ДопИнфПрослеж", p.TrackInfo.Trim());
                    w.WriteEndElement();
                }

                if (hasMarking)
                {
                    w.WriteStartElement("НомСредИдентТов");
                    if (!string.IsNullOrWhiteSpace(p.BatchInfo)) w.WriteAttributeString("ПрПартМарк", p.BatchInfo.Trim());
                    if (p.MarkedQty != 0) w.WriteAttributeString("КолВедМарк", ((long)p.MarkedQty).ToString(CultureInfo.InvariantCulture));
                    if (!string.IsNullOrWhiteSpace(p.Kiz))
                    {
                        w.WriteStartElement("КИЗ");
                        w.WriteString(p.Kiz.Trim());
                        w.WriteEndElement();
                    }
                    else if (!string.IsNullOrWhiteSpace(p.PackNo))
                    {
                        w.WriteStartElement("НомУпак");
                        w.WriteString(p.PackNo.Trim());
                        w.WriteEndElement();
                    }
                    w.WriteEndElement();
                }

                w.WriteEndElement();
            }

            // Графа 6: Акциз
            w.WriteStartElement("Акциз");
            if (p.ExciseAmount > 0)
            {
                w.WriteStartElement("СумАкциз");
                w.WriteValue(p.ExciseAmount.ToString("F2", CultureInfo.InvariantCulture));
                w.WriteEndElement();
            }
            else
            {
                w.WriteStartElement("БезАкциз");
                w.WriteString("без акциза");
                w.WriteEndElement();
            }
            w.WriteEndElement();

            // Графа 8: Сумма налога
            WriteVatAmount(w, "СумНал", p.SumWithoutVat, p.VatRateLabel);

            if (p.InfoFields != null && p.InfoFields.Count > 0)
            {
                foreach (var f in p.InfoFields)
                {
                    if (string.IsNullOrWhiteSpace(f.Ident) && string.IsNullOrWhiteSpace(f.Value)) continue;
                    w.WriteStartElement("ИнфПолФХЖ2");
                    if (!string.IsNullOrWhiteSpace(f.Ident)) w.WriteAttributeString("Идентиф", f.Ident);
                    if (!string.IsNullOrWhiteSpace(f.Value)) w.WriteAttributeString("Значен", f.Value);
                    w.WriteEndElement();
                }
            }

            w.WriteEndElement();
            line++;
        }

        var totalWithout = doc.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Sum(p => p.SumWithoutVat);
        var totalWith = doc.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Sum(p => TotalWithVat(p));

        w.WriteStartElement("ВсегоОпл");
        if (totalWithout != 0) w.WriteAttributeString("СтТовБезНДСВсего", totalWithout.ToString("0.##", CultureInfo.InvariantCulture));
        if (totalWith != 0) w.WriteAttributeString("СтТовУчНалВсего", totalWith.ToString("0.##", CultureInfo.InvariantCulture));

        var totalVat = doc.Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).Sum(p => VatAmount(p));
        WriteVatAmount(w, "СумНалВсего", totalVat, doc.Products.FirstOrDefault()?.VatRateLabel ?? "");

        w.WriteEndElement();

        w.WriteEndElement();
    }

    private static decimal VatAmount(Upd53Doc.Upd53Product p)
    {
        var rate = ParseVatRate(p.VatRateLabel);
        if (!rate.HasValue) return 0;
        return Math.Round(p.SumWithoutVat * rate.Value / 100, 2);
    }

    private static decimal TotalWithVat(Upd53Doc.Upd53Product p)
    {
        var vat = VatAmount(p);
        return Math.Round(p.SumWithoutVat + vat, 2);
    }

    private static decimal? ParseVatRate(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var text = label.Trim();
        if (text.EndsWith("%", StringComparison.Ordinal))
            text = text[..^1];
        if (text.Contains('/'))
        {
            text = text.Split('/')[0];
        }
        if (!decimal.TryParse(text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
            return null;
        return rate;
    }

    private static void WriteVatAmount(XmlWriter w, string elementName, decimal amount, string rateLabel)
    {
        w.WriteStartElement(elementName);
        if (rateLabel.Trim() == "Без НДС")
        {
            w.WriteStartElement("БезНДС");
            w.WriteString("без НДС");
            w.WriteEndElement();
        }
        else
        {
            w.WriteStartElement("СумНал");
            w.WriteValue(amount.ToString("F2", CultureInfo.InvariantCulture));
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    private static void WriteTransfer(XmlWriter w, Upd53Doc doc)
    {
        if (string.IsNullOrWhiteSpace(doc.OperationContent)) return;

        w.WriteStartElement("СвПродПер");
        w.WriteStartElement("СвПер");
        w.WriteAttributeString("СодОпер", doc.OperationContent);
        if (!string.IsNullOrWhiteSpace(doc.SaleOrder)) w.WriteAttributeString("ВидОпер", doc.SaleOrder);
        var transferDate = !string.IsNullOrWhiteSpace(doc.TransferDateText)
            ? doc.TransferDateText
            : doc.DocDate.ToString("dd.MM.yyyy");
        w.WriteAttributeString("ДатаПер", transferDate);

        // Строка [10]: Основание передачи (договор/соглашение/заказ)
        if (!string.IsNullOrWhiteSpace(doc.BaseDocName))
        {
            w.WriteStartElement("ОснПер");
            w.WriteAttributeString("РеквНаимДок", doc.BaseDocName);
            if (!string.IsNullOrWhiteSpace(doc.BaseDocNumber)) w.WriteAttributeString("РеквНомерДок", doc.BaseDocNumber);
            if (!string.IsNullOrWhiteSpace(doc.BaseDocDateText)) w.WriteAttributeString("РеквДатаДок", doc.BaseDocDateText);
            w.WriteEndElement();
        }
        else
        {
            w.WriteStartElement("БезДокОснПер");
            w.WriteString("1");
            w.WriteEndElement();
        }

        WriteTransferPerson(w, doc);
        WriteTransportInfo(w, doc);
        WriteGoodsTransferInfo(w, doc);

        w.WriteEndElement(); // СвПер

        WriteDocInfoFields(w, doc);

        w.WriteEndElement(); // СвПродПер
    }

    // СвЛицПер — лицо, передавшее товар (строка [12])
    private static void WriteTransferPerson(XmlWriter w, Upd53Doc doc)
    {
        if (!doc.HasTransferPerson) return;

        w.WriteStartElement("СвЛицПер");
        if (doc.TransferPersonKind == "Раб" || string.IsNullOrWhiteSpace(doc.TransferPersonKind))
        {
            w.WriteStartElement("РабОргПрод");
            var pos = string.IsNullOrWhiteSpace(doc.TransferPersonPosition) ? "Кладовщик" : doc.TransferPersonPosition;
            w.WriteAttributeString("Должность", pos);
            w.WriteStartElement("ФИО");
            WriteFio(w, doc.TransferPersonFio);
            w.WriteEndElement();
            w.WriteEndElement();
        }
        else if (doc.TransferPersonKind == "ФЛ")
        {
            w.WriteStartElement("ИнЛицо");
            w.WriteStartElement("ФЛПер");
            w.WriteStartElement("ФИО");
            WriteFio(w, doc.TransferPersonFio);
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    // Тран — транспорт и условия поставки (строка [11])
    private static void WriteTransportInfo(XmlWriter w, Upd53Doc doc)
    {
        if (string.IsNullOrWhiteSpace(doc.TransportInfo) &&
            string.IsNullOrWhiteSpace(doc.Incoterms)) return;

        w.WriteStartElement("Тран");
        if (!string.IsNullOrWhiteSpace(doc.TransportInfo)) w.WriteAttributeString("СвТран", doc.TransportInfo);
        if (!string.IsNullOrWhiteSpace(doc.Incoterms))
        {
            w.WriteAttributeString("Инкотермс", doc.Incoterms);
            if (!string.IsNullOrWhiteSpace(doc.IncotermsVersion)) w.WriteAttributeString("ВерИнкотермс", doc.IncotermsVersion);
        }
        w.WriteEndElement();
    }

    // СвПерВещи — сведения о передаче вещи (строки [13], [14])
    private static void WriteGoodsTransferInfo(XmlWriter w, Upd53Doc doc)
    {
        if (string.IsNullOrWhiteSpace(doc.GoodsTransferDateText) &&
            string.IsNullOrWhiteSpace(doc.GoodsTransferInfo)) return;

        w.WriteStartElement("СвПерВещи");
        if (!string.IsNullOrWhiteSpace(doc.GoodsTransferDateText)) w.WriteAttributeString("ДатаПерВещ", doc.GoodsTransferDateText);
        if (!string.IsNullOrWhiteSpace(doc.GoodsTransferInfo)) w.WriteAttributeString("СвПерВещ", doc.GoodsTransferInfo);
        w.WriteEndElement();
    }

    private static void WriteSigners(XmlWriter w, Upd53Doc doc)
    {
        var validSigners = doc.Signers.Where(x => !string.IsNullOrWhiteSpace(x.LastName)).ToList();
        if (validSigners.Count == 0)
        {
            // Если подписант не указан в форме, подставим руководителя продавца
            validSigners.Add(new Upd53Doc.Upd53Signer
            {
                LastName = "Иванов",
                FirstName = "Иван",
                MiddleName = "Иванович",
                Position = "Генеральный директор",
                PowerKind = Upd53Doc.SignerPowerKind.Head
            });
        }

        foreach (var s in validSigners)
        {
            w.WriteStartElement("Подписант");
            if (!string.IsNullOrWhiteSpace(s.Position)) w.WriteAttributeString("Должн", s.Position);
            if (s.SignType is 1 or 2 or 3) w.WriteAttributeString("ТипПодпис", s.SignType.ToString());
            var signDate = !string.IsNullOrWhiteSpace(s.SignDateText)
                ? s.SignDateText
                : doc.DocDate.ToString("dd.MM.yyyy");
            w.WriteAttributeString("ДатаПодДок", signDate);
            w.WriteAttributeString("СпосПодтПолном", ((int)s.PowerKind).ToString());

            w.WriteStartElement("ФИО");
            w.WriteAttributeString("Фамилия", string.IsNullOrWhiteSpace(s.LastName) ? "Иванов" : s.LastName);
            w.WriteAttributeString("Имя", string.IsNullOrWhiteSpace(s.FirstName) ? "Иван" : s.FirstName);
            if (!string.IsNullOrWhiteSpace(s.MiddleName)) w.WriteAttributeString("Отчество", s.MiddleName);
            w.WriteEndElement();

            if (s.PowerKind == Upd53Doc.SignerPowerKind.ByPaperProcuration &&
                !string.IsNullOrWhiteSpace(s.ProcurationNumber))
            {
                w.WriteStartElement("СвДоверБум");
                if (!string.IsNullOrWhiteSpace(s.ProcurationNumber)) w.WriteAttributeString("ВнНомДовер", s.ProcurationNumber);
                if (!string.IsNullOrWhiteSpace(s.ProcurationDateText)) w.WriteAttributeString("ДатаВыдДовер", s.ProcurationDateText);
                w.WriteEndElement();
            }

            if (s.PowerKind == Upd53Doc.SignerPowerKind.ByElectronicProcuration &&
                !string.IsNullOrWhiteSpace(s.Urn))
            {
                var urn = s.Urn.Trim();
                if (urn.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase))
                    urn = urn.Substring(9).Trim();

                w.WriteStartElement("СвДоверЭл");
                w.WriteAttributeString("НомДовер", urn);
                
                var issueDate = !string.IsNullOrWhiteSpace(s.ElProcDateText) 
                    ? s.ElProcDateText.Trim() 
                    : (!string.IsNullOrWhiteSpace(s.SignDateText) ? s.SignDateText.Trim() : DateTime.Today.ToString("dd.MM.yyyy"));
                w.WriteAttributeString("ДатаВыдДовер", issueDate);

                if (!string.IsNullOrWhiteSpace(s.ElProcInnerNo)) 
                    w.WriteAttributeString("ВнНомДовер", s.ElProcInnerNo.Trim());

                var storage = !string.IsNullOrWhiteSpace(s.StorageSystemId) 
                    ? s.StorageSystemId.Trim() 
                    : "ФНС России";
                w.WriteAttributeString("ИдСистХран", storage);

                if (!string.IsNullOrWhiteSpace(s.StorageUrl)) 
                    w.WriteAttributeString("УРЛСист", s.StorageUrl.Trim());

                w.WriteEndElement();
            }

            w.WriteEndElement();
        }
    }
}
