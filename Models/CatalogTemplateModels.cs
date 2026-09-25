namespace UplivaAI.Models;

/// <summary>
/// Defines the fields that should be rendered for a business type's catalog.
/// Templates are stored as editable JSON files under CatalogTemplates so new
/// business types/fields can be added without changing the database schema.
/// </summary>
public class CatalogTemplateDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ItemLabel { get; set; } = "Catalog item";
    public string AddButtonText { get; set; } = "Add catalog item";
    public List<CatalogFieldDefinition> Fields { get; set; } = [];
}

public class CatalogFieldDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public string Placeholder { get; set; } = string.Empty;
    public string HelpText { get; set; } = string.Empty;
    public bool Required { get; set; }
    public bool ShowOnWhatsApp { get; set; } = true;
    public int SortOrder { get; set; }
    public List<string> Options { get; set; } = [];
}
