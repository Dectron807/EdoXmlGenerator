namespace EdoXmlGenerator.Models;

public class DocumentTemplate
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DocumentType Type { get; set; }
}
