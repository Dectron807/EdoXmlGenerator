using System.Collections.Generic;
using System.Text.RegularExpressions;
using EdoXmlGenerator.Models;

namespace EdoXmlGenerator.Services;

public class ValidationService
{
    public List<string> Validate(EdoDocument doc)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(doc.Number))
            errors.Add("Не указан номер документа.");

        if (string.IsNullOrWhiteSpace(doc.Supplier.Name))
            errors.Add("Не указано наименование поставщика.");

        if (string.IsNullOrWhiteSpace(doc.Buyer.Name))
            errors.Add("Не указано наименование покупателя.");

        ValidateInn(doc.Supplier.Inn, "поставщика", errors);
        ValidateInn(doc.Buyer.Inn, "покупателя", errors);
        ValidateKpp(doc.Supplier.Kpp, "поставщика", errors);
        ValidateKpp(doc.Buyer.Kpp, "покупателя", errors);

        if (doc.Products.Count == 0)
            errors.Add("Не добавлено ни одной позиции товаров.");

        var index = 0;
        foreach (var p in doc.Products)
        {
            index++;
            var prefix = $"Позиция {index}";

            if (string.IsNullOrWhiteSpace(p.Name))
                errors.Add($"{prefix}: не указано наименование товара.");

            if (p.Quantity <= 0)
                errors.Add($"{prefix}: количество должно быть больше нуля.");

            if (p.Price < 0)
                errors.Add($"{prefix}: цена не может быть отрицательной.");

            if (p.VatRate is not (0 or 5 or 7 or 10 or 22))
                errors.Add($"{prefix}: некорректная ставка НДС.");
        }

        return errors;
    }

    private static void ValidateInn(string inn, string who, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(inn))
        {
            errors.Add($"Не указан ИНН {who}.");
            return;
        }

        if (!Regex.IsMatch(inn.Trim(), @"^\d{10}$|^\d{12}$"))
            errors.Add($"ИНН {who} должен содержать 10 или 12 цифр.");
    }

    private static void ValidateKpp(string kpp, string who, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(kpp))
            return;

        if (!Regex.IsMatch(kpp.Trim(), @"^\d{9}$"))
            errors.Add($"КПП {who} должен содержать ровно 9 цифр.");
    }
}
