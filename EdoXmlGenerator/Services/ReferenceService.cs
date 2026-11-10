using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace EdoXmlGenerator.Services;

public class ReferenceService
{
    public IReadOnlyList<Unit> Units { get; }
    public IReadOnlyList<int> VatRates { get; }

    public ReferenceService()
    {
        Units = LoadUnits();
        VatRates = LoadVatRates();
    }

    private IReadOnlyList<Unit> LoadUnits()
    {
        var path = GetDataPath("Units.json");
        if (!File.Exists(path))
            return new List<Unit>();

        try
        {
            var items = JsonConvert.DeserializeObject<List<UnitDto>>(File.ReadAllText(path));
            var list = new List<Unit>();
            if (items != null)
            {
            foreach (var i in items)
                list.Add(new Unit(i.Code ?? string.Empty, i.Name ?? string.Empty, i.Full ?? string.Empty));
            }
            return list;
        }
        catch
        {
            return new List<Unit>();
        }
    }

    private IReadOnlyList<int> LoadVatRates()
    {
        var path = GetDataPath("VatRates.json");
        if (!File.Exists(path))
            return new List<int> { 0, 5, 7, 10, 20 };

        try
        {
            var items = JsonConvert.DeserializeObject<List<VatRateDto>>(File.ReadAllText(path));
            var list = new List<int>();
            if (items != null)
            {
                foreach (var i in items)
                    list.Add(i.Rate);
            }
            return list;
        }
        catch
        {
            return new List<int> { 0, 5, 7, 10, 20 };
        }
    }

    private static string GetDataPath(string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDir, "Data", fileName);
        if (File.Exists(candidate))
            return candidate;

        candidate = Path.Combine(baseDir, fileName);
        return candidate;
    }

    public record Unit(string Code, string Name, string Full);

    private class UnitDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Full { get; set; }
    }

    private class VatRateDto
    {
        public int Rate { get; set; }
        public string? Label { get; set; }
    }
}
