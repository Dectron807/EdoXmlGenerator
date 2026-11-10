using CommunityToolkit.Mvvm.ComponentModel;

namespace EdoXmlGenerator.Models;

public partial class Product : ObservableObject
{
    [ObservableProperty]
    private int _lineNumber;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _unit = string.Empty;

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private int _vatRate;

    public event Action? TotalsChanged;

    public decimal Sum
    {
        get => Math.Round(Quantity * Price, 2);
    }

    partial void OnQuantityChanged(decimal value) => NotifyTotals();

    partial void OnPriceChanged(decimal value) => NotifyTotals();

    partial void OnVatRateChanged(int value) => NotifyTotals();

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(Sum));
        TotalsChanged?.Invoke();
    }
}
