using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using EdoXmlGenerator.Services;
using EdoXmlGenerator.ViewModels;

namespace EdoXmlGenerator.Views;

public partial class PreviewView : UserControl
{
    public PreviewView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is PreviewViewModel vm)
            RenderXml(vm.Xml);
    }

    private void RenderXml(string xml)
    {
        XmlView.Document.Blocks.Clear();
        var paragraph = new Paragraph { Margin = new Thickness(0) };

        foreach (var token in XmlTokenizer.Tokenize(xml))
        {
            paragraph.Inlines.Add(new Run(token.Text)
            {
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(token.Color)!
            });
        }

        XmlView.Document.Blocks.Add(paragraph);
    }
}
