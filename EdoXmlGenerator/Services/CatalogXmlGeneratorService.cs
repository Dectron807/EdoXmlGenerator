using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using EdoXmlGenerator.Models.Catalog;
using EdoXmlGenerator.ViewModels;

namespace EdoXmlGenerator.Services;

/// <summary>
/// Генерирует структурированный XML для динамических документов каталога.
/// Корень — RootElement из определения; внутри — секции с полями, при наличии
/// таблицы позиций — элемент Товары/Позиции с перечнем.
/// </summary>
public class CatalogXmlGeneratorService
{
    public string Generate(CatalogFormViewModel vm)
    {
        var d = vm.Definition;
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
            writer.WriteStartElement(d.RootElement);
            writer.WriteAttributeString("ТипДокумента", d.Title);
            writer.WriteAttributeString("Версия", "1.0");

            foreach (var section in d.Sections)
            {
                WriteSection(writer, vm, section, d);
            }

            if (vm.HasItems)
                WriteItems(writer, vm);

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static void WriteSection(XmlWriter w, CatalogFormViewModel vm, DocSection section, DocumentDefinition d)
    {
        w.WriteStartElement("Секция");
        w.WriteAttributeString("Наименование", section.Title);

        foreach (var f in section.Fields)
        {
            if (f.Key == "itemsTable") continue;

            var elementName = SafeName(f.Label);
            object? value = null;

            switch (f.Kind)
            {
                case FieldKind.Bool:
                    value = vm.Bag.GetBool(f.Key);
                    break;
                case FieldKind.Date:
                    value = vm.Bag.GetDate(f.Key)?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    break;
                case FieldKind.Number:
                case FieldKind.Money:
                    value = vm.Bag.GetDecimal(f.Key)?.ToString("F2", CultureInfo.InvariantCulture);
                    break;
                default:
                    value = vm.Bag[f.Key];
                    break;
            }

            if (value == null) continue;
            var s = value is bool b ? (b ? "Да" : "Нет") : value.ToString() ?? string.Empty;
            if (s.Length == 0) continue;

            w.WriteStartElement(elementName);
            w.WriteString(TrimText(s));
            w.WriteEndElement();
        }

        w.WriteEndElement(); // Секция
    }

    private static void WriteItems(XmlWriter w, CatalogFormViewModel vm)
    {
        w.WriteStartElement("Позиции");
        foreach (var p in vm.Products)
        {
            w.WriteStartElement("Позиция");
            w.WriteElementString("Наименование", p.Name ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(p.Code)) w.WriteElementString("Код", p.Code);
            w.WriteElementString("Единица", p.Unit ?? string.Empty);
            w.WriteElementString("Количество", F(p.Quantity));
            w.WriteElementString("Цена", F(p.Price));
            w.WriteElementString("СтавкаНДС", p.VatRate.ToString(CultureInfo.InvariantCulture));
            w.WriteElementString("Сумма", F(p.Sum));
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    private static string F(decimal v) => v.ToString("F2", CultureInfo.InvariantCulture);

    private static string TrimText(string s) => s?.Trim() ?? string.Empty;

    /// <summary>Преобразует русскую подпись в безопасное имя XML-элемента.</summary>
    private static string SafeName(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return "Поле";
        var chars = label.Trim().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray();
        var s = new string(chars);
        return s.Length == 0 ? "Поле" : s;
    }
}
