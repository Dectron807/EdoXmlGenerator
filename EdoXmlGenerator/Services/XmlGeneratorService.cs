using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using EdoXmlGenerator.Models;

namespace EdoXmlGenerator.Services;

public class XmlGeneratorService
{
    public string Generate(EdoDocument doc)
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
            writer.WriteStartElement("Документ");

            writer.WriteElementString("Тип", GetTypeName(doc.Type));
            writer.WriteElementString("Номер", doc.Number);
            writer.WriteElementString("Дата", doc.DateText);

            WriteParticipant(writer, "Поставщик", doc.Supplier);
            WriteParticipant(writer, "Покупатель", doc.Buyer);

            writer.WriteStartElement("Товары");
            foreach (var product in doc.Products)
            {
                WriteProduct(writer, product);
            }
            writer.WriteEndElement();

            writer.WriteStartElement("Итого");
            writer.WriteElementString("СуммаБезНДС", ToText(doc.TotalWithoutVat));
            writer.WriteElementString("СуммаНДС", ToText(doc.TotalVat));
            writer.WriteElementString("СуммаВсего", ToText(doc.Total));
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private void WriteParticipant(XmlWriter writer, string name, Participant p)
    {
        writer.WriteStartElement(name);
        writer.WriteElementString("Наименование", p.Name);
        writer.WriteElementString("ИНН", p.Inn);
        if (!string.IsNullOrWhiteSpace(p.Kpp))
            writer.WriteElementString("КПП", p.Kpp);
        if (!string.IsNullOrWhiteSpace(p.Okpo))
            writer.WriteElementString("ОКПО", p.Okpo);
        if (!string.IsNullOrWhiteSpace(p.Address))
            writer.WriteElementString("Адрес", p.Address);
        writer.WriteEndElement();
    }

    private void WriteProduct(XmlWriter writer, Product p)
    {
        writer.WriteStartElement("Товар");
        writer.WriteElementString("Код", p.Code);
        writer.WriteElementString("Наименование", p.Name);
        writer.WriteElementString("Единица", p.Unit);
        writer.WriteElementString("Количество", ToText(p.Quantity));
        writer.WriteElementString("Цена", ToText(p.Price));
        writer.WriteElementString("СтавкаНДС", p.VatRate.ToString(CultureInfo.InvariantCulture));
        writer.WriteElementString("Сумма", ToText(p.Sum));
        writer.WriteEndElement();
    }

    private static string GetTypeName(DocumentType type) => type switch
    {
        DocumentType.Nakladnaya => "Накладная",
        DocumentType.Akt => "Акт",
        _ => "Документ"
    };

    private static string ToText(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
}
