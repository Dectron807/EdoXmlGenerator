using System.Windows;
using EdoXmlGenerator.Services;
using EdoXmlGenerator.ViewModels;

namespace EdoXmlGenerator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new DialogService());
    }
}
