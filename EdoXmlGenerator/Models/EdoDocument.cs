using System.Collections.ObjectModel;
using System.Globalization;

namespace EdoXmlGenerator.Models;

public class EdoDocument
{
    public DocumentType Type { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Today;

    public Participant Supplier { get; set; } = new();

    public Participant Buyer { get; set; } = new();

    public ObservableCollection<Product> Products { get; set; } = new();

    public string DateText => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public decimal TotalWithoutVat
    {
        get
        {
            var sum = 0m;
            foreach (var p in Products)
            {
                sum += p.VatRate <= 0 ? p.Sum : Math.Round(p.Sum / (1 + (decimal)p.VatRate / 100), 2);
            }
            return Math.Round(sum, 2);
        }
    }

    public decimal TotalVat
    {
        get
        {
            var sum = 0m;
            foreach (var p in Products)
            {
                if (p.VatRate <= 0)
                    continue;

                var withoutVat = Math.Round(p.Sum / (1 + (decimal)p.VatRate / 100), 2);
                sum += Math.Round(p.Sum - withoutVat, 2);
            }
            return Math.Round(sum, 2);
        }
    }

    public decimal Total
    {
        get => Math.Round(TotalWithoutVat + TotalVat, 2);
    }
}
