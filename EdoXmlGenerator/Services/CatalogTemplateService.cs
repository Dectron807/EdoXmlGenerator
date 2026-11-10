using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using EdoXmlGenerator.Models;
using EdoXmlGenerator.Models.Catalog;
using EdoXmlGenerator.ViewModels;

namespace EdoXmlGenerator.Services;

/// <summary>
/// Сохранение и загрузка заполненной динамической формы (каталог документов)
/// в виде XML-шаблона данных: значения полей + таблица позиций.
/// </summary>
public class CatalogTemplateService
{
    private const string Root = "Шаблон";
    private const string FieldElement = "Поле";
    private const string ProductElement = "Позиция";

    public string Save(CatalogFormViewModel vm)
    {
        var fields = new List<XElement>();
        foreach (var f in vm.Definition.AllFields)
        {
            string? value = null;
            switch (f.Kind)
            {
                case FieldKind.Bool:
                    value = vm.Bag.GetBool(f.Key) ? "1" : "0";
                    break;
                case FieldKind.Date:
                    value = vm.Bag.GetDate(f.Key)?.ToString("yyyy-MM-dd");
                    break;
                default:
                    value = vm.Bag[f.Key];
                    break;
            }
            if (string.IsNullOrEmpty(value)) continue;
            fields.Add(new XElement(FieldElement,
                new XAttribute("key", f.Key),
                new XAttribute("type", f.Kind.ToString()),
                value));
        }

        var products = new List<XElement>();
        foreach (var p in vm.Products)
        {
            products.Add(new XElement(ProductElement,
                new XAttribute("name", p.Name),
                new XAttribute("code", p.Code),
                new XAttribute("unit", p.Unit),
                new XAttribute("quantity", p.Quantity.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("price", p.Price.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("vatRate", p.VatRate)));
        }

        var doc = new XDocument(new XElement(Root,
            new XAttribute("docId", vm.Definition.Id),
            new XAttribute("title", vm.Definition.Title),
            fields.Count > 0 ? new XElement("Значения", fields) : null,
            products.Count > 0 ? new XElement("Позиции", products) : null));

        return doc.Declaration == null
            ? "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + doc.ToString(SaveOptions.None)
            : doc.ToString(SaveOptions.None);
    }

    public void SaveToFile(string filePath, CatalogFormViewModel vm)
    {
        File.WriteAllText(filePath, Save(vm), new UTF8Encoding(false));
    }

    /// <summary>Загружает XML-шаблон и применяет значения в форму.</summary>
    /// <returns>Тип документа, на который рассчитан шаблон, или null, если применимо к текущему.</returns>
    public void Load(string filePath, CatalogFormViewModel vm)
    {
        if (!File.Exists(filePath))
            throw new InvalidDataException("Файл не найден.");

        XDocument doc;
        try
        {
            doc = XDocument.Load(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("Не удалось разобрать XML-шаблон: " + ex.Message, ex);
        }

        var root = doc.Root;
        if (root == null || root.Name.LocalName != Root)
            throw new InvalidDataException("Файл не является шаблоном документа редактора ЭДО.");

        var docId = (string?)root.Attribute("docId");
        if (!string.IsNullOrEmpty(docId) && !string.Equals(docId, vm.Definition.Id, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"Шаблон предназначен для документа «{root.Attribute("title")?.Value}» и не подходит к текущему.");

        var valuesElement = root.Element("Значения");
        if (valuesElement != null)
        {
            foreach (var node in valuesElement.Elements(FieldElement))
            {
                var key = (string?)node.Attribute("key");
                if (string.IsNullOrEmpty(key)) continue;

                var type = node.Attribute("type")?.Value;
                var value = node.Value;

                switch (type)
                {
                    case nameof(FieldKind.Bool):
                        vm.Bag.SetBool(key, value is "1" or "true" or "True");
                        break;
                    case nameof(FieldKind.Date):
                        vm.Bag.SetDate(key, DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var d) ? d : null);
                        break;
                    default:
                        vm.Bag[key] = value;
                        break;
                }
            }
        }

        vm.Products.Clear();
        var productsElement = root.Element("Позиции");
        if (productsElement != null)
        {
            var index = 1;
            foreach (var node in productsElement.Elements(ProductElement))
            {
                var p = new Product
                {
                    LineNumber = index++,
                    Name = node.Attribute("name")?.Value ?? string.Empty,
                    Code = node.Attribute("code")?.Value ?? string.Empty,
                    Unit = node.Attribute("unit")?.Value ?? string.Empty,
                    Quantity = ParseDecimal(node.Attribute("quantity")?.Value),
                    Price = ParseDecimal(node.Attribute("price")?.Value),
                    VatRate = int.TryParse(node.Attribute("vatRate")?.Value, out var v) ? v : 22,
                };
                p.TotalsChanged += vm.RecalcTotalsForImport;
                vm.Products.Add(p);
            }
        }

        vm.RecalcTotalsForImport();
    }

    private static decimal ParseDecimal(string? s) =>
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
}