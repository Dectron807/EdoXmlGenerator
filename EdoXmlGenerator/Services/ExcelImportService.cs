using System;
using System.Collections.Generic;
using System.IO;
using EdoXmlGenerator.Models;
using OfficeOpenXml;

namespace EdoXmlGenerator.Services;

public class ExcelImportService
{
    static ExcelImportService()
    {
        ExcelPackage.License.SetNonCommercialPersonal("EdoXmlGenerator");
    }

    public List<Product> ImportProducts(string filePath, int headerRow = 1)
    {
        var products = new List<Product>();
        var line = 1;

        using var package = new ExcelPackage(new FileInfo(filePath));
        var sheet = package.Workbook.Worksheets[0];
        if (sheet == null || sheet.Dimension == null)
            return products;

        var startRow = sheet.Dimension.Start.Row;
        var endRow = sheet.Dimension.End.Row;

        if (headerRow > startRow)
            startRow = headerRow + 1;

        var codeIdx = FindColumn(sheet, headerRow, new[] { "Код", "CODE", "Артикул" });
        var nameIdx = FindColumn(sheet, headerRow, new[] { "Наименование", "Товар", "NAME" });
        var unitIdx = FindColumn(sheet, headerRow, new[] { "Единица", "Ед.изм", "Ед", "UNIT" });
        var qtyIdx = FindColumn(sheet, headerRow, new[] { "Количество", "Кол-во", "Кол", "QTY", "QUANTITY" });
        var priceIdx = FindColumn(sheet, headerRow, new[] { "Цена", "PRICE" });
        var vatIdx = FindColumn(sheet, headerRow, new[] { "СтавкаНДС", "НДС", "VAT" });

        for (int row = startRow; row <= endRow; row++)
        {
            if (nameIdx.HasValue && IsRowEmpty(sheet, row))
                break;

            var product = new Product { LineNumber = line };
            if (codeIdx.HasValue)
                product.Code = GetString(sheet, row, codeIdx.Value);
            if (nameIdx.HasValue)
                product.Name = GetString(sheet, row, nameIdx.Value);
            if (unitIdx.HasValue)
                product.Unit = GetString(sheet, row, unitIdx.Value);
            if (qtyIdx.HasValue)
                product.Quantity = GetDecimal(sheet, row, qtyIdx.Value);
            if (priceIdx.HasValue)
                product.Price = GetDecimal(sheet, row, priceIdx.Value);
            if (vatIdx.HasValue)
                product.VatRate = (int)GetDecimal(sheet, row, vatIdx.Value);

            if (!string.IsNullOrWhiteSpace(product.Name) || product.Quantity != 0 || product.Price != 0)
            {
                products.Add(product);
                line++;
            }
        }

        return products;
    }

    private int? FindColumn(ExcelWorksheet sheet, int headerRow, string[] candidates)
    {
        if (sheet.Dimension == null)
            return null;

        for (int col = sheet.Dimension.Start.Column; col <= sheet.Dimension.End.Column; col++)
        {
            var value = sheet.Cells[headerRow, col].Text?.Trim() ?? string.Empty;
            foreach (var candidate in candidates)
            {
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
                    return col;
            }
        }
        return null;
    }

    private static bool IsRowEmpty(ExcelWorksheet sheet, int row)
    {
        if (sheet.Dimension == null)
            return true;

        for (int col = sheet.Dimension.Start.Column; col <= sheet.Dimension.End.Column; col++)
        {
            if (!string.IsNullOrWhiteSpace(sheet.Cells[row, col].Text))
                return false;
        }
        return true;
    }

    private static string GetString(ExcelWorksheet sheet, int row, int col)
    {
        return sheet.Cells[row, col].Text?.Trim() ?? string.Empty;
    }

    private static decimal GetDecimal(ExcelWorksheet sheet, int row, int col)
    {
        var val = sheet.Cells[row, col].Value;
        if (val == null)
            return 0;

        if (val is decimal dec) return dec;
        if (val is double d) return (decimal)d;
        if (val is int i) return i;
        if (val is long l) return l;

        return decimal.TryParse(val.ToString(), out var result) ? result : 0;
    }
}
