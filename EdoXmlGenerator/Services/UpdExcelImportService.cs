using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EdoXmlGenerator.Models;
using OfficeOpenXml;

namespace EdoXmlGenerator.Services;

public class UpdExcelImportService
{
    static UpdExcelImportService()
    {
        ExcelPackage.License.SetNonCommercialPersonal("EdoXmlGenerator");
    }

    public class UpdImportData
    {
        public string? DocNumber { get; set; }
        public DateTime? DocDate { get; set; }
        public string? SellerName { get; set; }
        public string? SellerInn { get; set; }
        public string? SellerKpp { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerInn { get; set; }
        public string? BuyerKpp { get; set; }
        public List<Upd53Doc.Upd53Product> Products { get; } = new();
    }

    public UpdImportData Import(string filePath)
    {
        var result = new UpdImportData();
        using var package = new ExcelPackage(new FileInfo(filePath));
        if (package.Workbook.Worksheets.Count == 0) return result;

        var sheet = package.Workbook.Worksheets[0];
        if (sheet?.Dimension == null) return result;

        // Попробуем извлечь шапку из бланка УПД
        ExtractHeader(sheet, result);

        // Импортируем товарные позиции
        result.Products.AddRange(ImportProductsFromSheet(sheet));
        return result;
    }

    public List<Upd53Doc.Upd53Product> ImportProducts(string filePath, int headerRow = 1, string? unitCode = null, string? unitName = null)
    {
        return Import(filePath).Products;
    }

    private static void ExtractHeader(ExcelWorksheet sheet, UpdImportData data)
    {
        var maxR = Math.Min(sheet.Dimension.End.Row, 30);
        var maxC = Math.Min(sheet.Dimension.End.Column, 120);

        for (int r = 1; r <= maxR; r++)
        {
            for (int c = 1; c <= maxC; c++)
            {
                var val = sheet.Cells[r, c].Text?.Trim() ?? string.Empty;

                // Номер и дата счета-фактуры
                if (val.Contains("СЧЕТ-ФАКТУРА") || val.Contains("Счет-фактура"))
                {
                    // Ищем номер в соседних ячейках
                    for (int off = 1; off <= 30; off++)
                    {
                        var cellVal = sheet.Cells[r, c + off].Text?.Trim();
                        if (!string.IsNullOrWhiteSpace(cellVal) && 
                            cellVal != "№" && 
                            !cellVal.StartsWith("от", StringComparison.OrdinalIgnoreCase) && 
                            cellVal != "\"" &&
                            !cellVal.StartsWith("("))
                        {
                            var cleanVal = cellVal.Replace("№", "").Trim();
                            if (!string.IsNullOrWhiteSpace(cleanVal))
                            {
                                if (string.IsNullOrEmpty(data.DocNumber))
                                    data.DocNumber = cleanVal;
                                break;
                            }
                        }
                    }
                }

                // Продавец (наименование)
                if (val.StartsWith("Продавец", StringComparison.OrdinalIgnoreCase) && !val.Contains("ИНН"))
                {
                    var name = FindNextNonEmpty(sheet, r, c);
                    if (!string.IsNullOrWhiteSpace(name) && !name.StartsWith("(") && !name.StartsWith("строка"))
                        data.SellerName = name;
                }
                // ИНН/КПП продавца
                else if (val.StartsWith("ИНН/КПП продавца", StringComparison.OrdinalIgnoreCase))
                {
                    var innKpp = FindNextNonEmpty(sheet, r, c);
                    ParseInnKpp(innKpp, out var inn, out var kpp);
                    data.SellerInn = inn;
                    data.SellerKpp = kpp;
                }
                // Покупатель (наименование)
                else if (val.StartsWith("Покупатель", StringComparison.OrdinalIgnoreCase) && !val.Contains("ИНН"))
                {
                    var name = FindNextNonEmpty(sheet, r, c);
                    if (!string.IsNullOrWhiteSpace(name) && !name.StartsWith("(") && !name.StartsWith("строка"))
                        data.BuyerName = name;
                }
                // ИНН/КПП покупателя
                else if (val.StartsWith("ИНН/КПП покупателя", StringComparison.OrdinalIgnoreCase))
                {
                    var innKpp = FindNextNonEmpty(sheet, r, c);
                    ParseInnKpp(innKpp, out var inn, out var kpp);
                    data.BuyerInn = inn;
                    data.BuyerKpp = kpp;
                }
            }
        }
    }

    private static string FindNextNonEmpty(ExcelWorksheet sheet, int r, int startC)
    {
        for (int c = startC + 1; c <= Math.Min(sheet.Dimension.End.Column, startC + 40); c++)
        {
            var txt = sheet.Cells[r, c].Text?.Trim();
            if (!string.IsNullOrWhiteSpace(txt) && !txt.StartsWith("(") && txt != ":")
                return txt;
        }
        return string.Empty;
    }

    private static void ParseInnKpp(string text, out string? inn, out string? kpp)
    {
        inn = null;
        kpp = null;
        if (string.IsNullOrWhiteSpace(text)) return;
        var parts = text.Split(new[] { '/', '\\', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1) inn = Regex.Match(parts[0], @"\d{10,12}").Value;
        if (parts.Length >= 2) kpp = Regex.Match(parts[1], @"\d{9}").Value;
    }

    private static List<Upd53Doc.Upd53Product> ImportProductsFromSheet(ExcelWorksheet sheet)
    {
        var products = new List<Upd53Doc.Upd53Product>();
        if (sheet.Dimension == null) return products;

        int? nameCol = null;
        int? codeCol = null;
        int? typeCodeCol = null;
        int? unitCodeCol = null;
        int? unitNameCol = null;
        int? qtyCol = null;
        int? priceCol = null;
        int? exciseCol = null;
        int? vatCol = null;
        int? countryCodeCol = null;
        int? countryNameCol = null;
        int? gtdCol = null;
        int? trackAmountCol = null;
        int startRow = -1;

        // 1. Ищем строку с номерами граф УПД (А, 1, 1а, 1б, 2, 2а, 3, 4, 5, 7, 8, 9, 10, 11, 14)
        for (int r = sheet.Dimension.Start.Row; r <= Math.Min(sheet.Dimension.End.Row, 45); r++)
        {
            for (int c = sheet.Dimension.Start.Column; c <= sheet.Dimension.End.Column; c++)
            {
                var txt = sheet.Cells[r, c].Text?.Trim() ?? string.Empty;
                if (txt == "1а" && nameCol == null) nameCol = c;
                else if (txt == "А" && codeCol == null) codeCol = c;
                else if (txt == "1б" && typeCodeCol == null) typeCodeCol = c;
                else if (txt == "2" && unitCodeCol == null) unitCodeCol = c;
                else if (txt == "2а" && unitNameCol == null) unitNameCol = c;
                else if (txt == "3" && qtyCol == null) qtyCol = c;
                else if (txt == "4" && priceCol == null) priceCol = c;
                else if (txt == "6" && exciseCol == null) exciseCol = c;
                else if (txt == "7" && vatCol == null) vatCol = c;
                else if (txt == "10" && countryCodeCol == null) countryCodeCol = c;
                else if (txt == "10а" && countryNameCol == null) countryNameCol = c;
                else if (txt == "11" && gtdCol == null) gtdCol = c;
                else if (txt == "14" && trackAmountCol == null) trackAmountCol = c;
            }

            if (nameCol.HasValue && qtyCol.HasValue)
            {
                startRow = r + 1;
                break;
            }
        }

        // 2. Если строка граф не найдена, ищем по текстовым заголовкам
        if (startRow == -1)
        {
            for (int r = sheet.Dimension.Start.Row; r <= Math.Min(sheet.Dimension.End.Row, 30); r++)
            {
                for (int c = sheet.Dimension.Start.Column; c <= sheet.Dimension.End.Column; c++)
                {
                    var txt = (sheet.Cells[r, c].Text?.Trim() ?? string.Empty).ToLowerInvariant();
                    if ((txt.Contains("наименование") || txt == "товар" || txt == "name") && nameCol == null)
                        nameCol = c;
                    else if ((txt.Contains("артикул") || txt == "код" || txt == "code") && codeCol == null)
                        codeCol = c;
                    else if ((txt.Contains("ед") || txt.Contains("единиц") || txt == "unit") && unitNameCol == null)
                        unitNameCol = c;
                    else if ((txt.Contains("кол-во") || txt.Contains("количеств") || txt == "qty") && qtyCol == null)
                        qtyCol = c;
                    else if ((txt.Contains("цена") || txt == "price") && priceCol == null)
                        priceCol = c;
                    else if ((txt.Contains("акциз") || txt == "excise") && exciseCol == null)
                        exciseCol = c;
                    else if ((txt.Contains("ндс") || txt.Contains("ставка") || txt == "vat") && vatCol == null)
                        vatCol = c;
                    else if ((txt.Contains("гтд") || txt.Contains("декларац") || txt.Contains("рнпт")) && gtdCol == null)
                        gtdCol = c;
                    else if ((txt.Contains("страна") || txt.Contains("оксм")) && countryNameCol == null)
                        countryNameCol = c;
                }

                if (nameCol.HasValue && (qtyCol.HasValue || priceCol.HasValue))
                {
                    startRow = r + 1;
                    break;
                }
            }
        }

        if (startRow == -1 || !nameCol.HasValue)
            return products;

        var line = 1;
        for (int row = startRow; row <= sheet.Dimension.End.Row; row++)
        {
            var nameVal = GetStr(sheet, row, nameCol.Value);

            // Остановка при достижении итогов
            if (nameVal.Contains("Всего к оплате") || nameVal.Contains("Итого") || nameVal.StartsWith("Руководитель"))
                break;

            // Проверяем всю строку на признак итогов
            var rowText = sheet.Cells[row, 1, row, Math.Min(sheet.Dimension.End.Column, 50)].Text;
            if (rowText.Contains("Всего к оплате") || rowText.Contains("Руководитель организации"))
                break;

            var qty = qtyCol.HasValue ? GetDec(sheet, row, qtyCol.Value) : 0;
            var price = priceCol.HasValue ? GetDec(sheet, row, priceCol.Value) : 0;

            if (string.IsNullOrWhiteSpace(nameVal) && qty == 0 && price == 0)
                continue;

            var product = new Upd53Doc.Upd53Product
            {
                LineNumber = line,
                Name = nameVal,
                Quantity = qty,
                Price = price
            };

            if (codeCol.HasValue) product.Code = GetStr(sheet, row, codeCol.Value);
            if (exciseCol.HasValue) product.ExciseAmount = GetDec(sheet, row, exciseCol.Value);
            if (typeCodeCol.HasValue) product.ProductTypeCode = GetStr(sheet, row, typeCodeCol.Value);
            if (unitCodeCol.HasValue) product.UnitCode = GetStr(sheet, row, unitCodeCol.Value);
            if (unitNameCol.HasValue) product.UnitName = GetStr(sheet, row, unitNameCol.Value);
            if (vatCol.HasValue) product.VatRateLabel = ParseVat(GetStr(sheet, row, vatCol.Value));
            if (countryCodeCol.HasValue) product.CountryCode = GetStr(sheet, row, countryCodeCol.Value);
            if (countryNameCol.HasValue) product.CountryName = GetStr(sheet, row, countryNameCol.Value);
            if (gtdCol.HasValue)
            {
                var gtdVal = GetStr(sheet, row, gtdCol.Value);
                if (gtdVal.Length >= 25 && gtdVal.Contains('/'))
                    product.TrackNo = gtdVal;
                else
                    product.GtdNumber = gtdVal;
            }
            if (trackAmountCol.HasValue) product.TrackAmount = GetDec(sheet, row, trackAmountCol.Value);

            // По умолчанию единица измерения "шт", если не указана
            if (string.IsNullOrWhiteSpace(product.UnitName) && string.IsNullOrWhiteSpace(product.UnitCode))
            {
                product.UnitCode = "796";
                product.UnitName = "шт";
            }

            products.Add(product);
            line++;
        }

        return products;
    }

    private static string ParseVat(string raw)
    {
        var t = (raw ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(t)) return "22%";
        if (t.Equals("Без НДС", StringComparison.OrdinalIgnoreCase) || t == "0" || t == "0%") return "Без НДС";
        if (t.Contains('/')) return t;
        if (decimal.TryParse(t.Replace('%', ' ').Trim().Replace(',', '.'), NumberStyles.Any,
            CultureInfo.InvariantCulture, out var rate) && rate != 0)
            return $"{rate}%";
        return t.EndsWith("%") ? t : t + "%";
    }

    private static string GetStr(ExcelWorksheet s, int r, int c) => s.Cells[r, c].Text?.Trim() ?? string.Empty;

    private static decimal GetDec(ExcelWorksheet s, int r, int c)
    {
        var v = s.Cells[r, c].Value;
        if (v == null) return 0;
        if (v is decimal dec) return dec;
        if (v is double d) return (decimal)d;
        if (v is int i) return i;
        if (v is long l) return l;
        var txt = v.ToString()?.Trim().Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out var res) ? res : 0;
    }
}
