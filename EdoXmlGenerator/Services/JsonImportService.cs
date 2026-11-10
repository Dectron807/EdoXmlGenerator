using System.Collections.Generic;
using System.IO;
using EdoXmlGenerator.Models;
using Newtonsoft.Json;

namespace EdoXmlGenerator.Services;

public class JsonImportService
{
    public EdoDocument Import(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var dto = JsonConvert.DeserializeObject<DocumentDto>(json)
                  ?? throw new InvalidDataException("Файл пуст или имеет неверную структуру.");

        var doc = new EdoDocument
        {
            Type = ParseType(dto.Type),
            Number = dto.Number ?? string.Empty,
            Date = DateTime.TryParse(dto.Date, out var parsed) ? parsed : DateTime.Today,
            Supplier = MapParticipant(dto.Supplier),
            Buyer = MapParticipant(dto.Buyer)
        };

        doc.Products.Clear();
        var line = 1;
        if (dto.Products != null)
        {
            foreach (var g in dto.Products)
            {
                doc.Products.Add(new Product
                {
                    LineNumber = line++,
                    Code = g.Code ?? string.Empty,
                    Name = g.Name ?? string.Empty,
                    Unit = g.Unit ?? string.Empty,
                    Quantity = g.Quantity,
                    Price = g.Price,
                    VatRate = g.VatRate
                });
            }
        }

        return doc;
    }

    private static DocumentType ParseType(string? type)
    {
        if (string.Equals(type, "Акт", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type, "АКТ", System.StringComparison.OrdinalIgnoreCase))
            return DocumentType.Akt;

        return DocumentType.Nakladnaya;
    }

    private static Participant MapParticipant(ParticipantDto? p) => new()
    {
        Name = p?.Name ?? string.Empty,
        Inn = p?.Inn ?? string.Empty,
        Kpp = p?.Kpp ?? string.Empty,
        Okpo = p?.Okpo ?? string.Empty,
        Address = p?.Address ?? string.Empty
    };

    private class DocumentDto
    {
        public string? Type { get; set; }
        public string? Number { get; set; }
        public string? Date { get; set; }
        public ParticipantDto? Supplier { get; set; }
        public ParticipantDto? Buyer { get; set; }
        public List<ProductDto>? Products { get; set; }
    }

    private class ParticipantDto
    {
        public string? Name { get; set; }
        public string? Inn { get; set; }
        public string? Kpp { get; set; }
        public string? Okpo { get; set; }
        public string? Address { get; set; }
    }

    private class ProductDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public int VatRate { get; set; }
    }
}
