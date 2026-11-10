using CommunityToolkit.Mvvm.ComponentModel;

namespace EdoXmlGenerator.Models;

public partial class Participant : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _inn = string.Empty;

    [ObservableProperty]
    private string _kpp = string.Empty;

    [ObservableProperty]
    private string _okpo = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;
}
