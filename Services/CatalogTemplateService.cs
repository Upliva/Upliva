using System.Text.Json;
using UplivaAI.Models;

namespace UplivaAI.Services;

public sealed class CatalogTemplateService(IWebHostEnvironment environment) : ICatalogTemplateService
{
    private readonly IWebHostEnvironment _environment = environment;

    public CatalogTemplateDefinition GetTemplate(string? templateOrBusinessType)
    {
        var key = ResolveKey(templateOrBusinessType);
        var path = Path.Combine(_environment.ContentRootPath, "CatalogTemplates", $"{key}.json");
        if (!File.Exists(path)) return CreateGenericTemplate();

        try
        {
            var template = JsonSerializer.Deserialize<CatalogTemplateDefinition>(File.ReadAllText(path), JsonOptions);
            if (template is not null && template.Fields.Count > 0) return template;
        }
        catch { }

        return CreateGenericTemplate();
    }

    public IReadOnlyList<CatalogDisplayFieldViewModel> GetDisplayFields(string? templateOrBusinessType, string? attributesJson, bool forWhatsApp = true)
    {
        Dictionary<string, string> attributes;
        try
        {
            attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(attributesJson ?? "{}", JsonOptions)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var template = GetTemplate(templateOrBusinessType);
        return template.Fields
            .Where(x => x.ShowOnWhatsApp && attributes.TryGetValue(x.Key, out var value) && !string.IsNullOrWhiteSpace(value))
            .OrderBy(x => x.SortOrder)
            .Select(x => new CatalogDisplayFieldViewModel
            {
                Key = x.Key,
                Label = x.Label,
                Value = attributes[x.Key]
            })
            .ToList();
    }

    public IReadOnlyList<CatalogTemplateDefinition> GetTemplates()
    {
        var directory = Path.Combine(_environment.ContentRootPath, "CatalogTemplates");
        if (!Directory.Exists(directory)) return [CreateGenericTemplate()];

        var result = new List<CatalogTemplateDefinition>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(x => x))
        {
            try
            {
                var template = JsonSerializer.Deserialize<CatalogTemplateDefinition>(File.ReadAllText(file), JsonOptions);
                if (template is not null && template.Fields.Count > 0) result.Add(template);
            }
            catch { }
        }
        return result.Count > 0 ? result : [CreateGenericTemplate()];
    }

    public static string ResolveKey(string? businessType)
    {
        var normalized = Normalize(businessType);
        return normalized switch
        {
            "realestate" or "property" => "real-estate",
            "furniture" => "furniture",
            "restaurant" or "cafe" or "food" => "restaurant",
            "salonbeauty" or "salon" or "beauty" => "salon-beauty",
            "grocery" or "supermarket" => "grocery",
            "mobileshop" or "mobile" => "mobile-shop",
            "electronicsstore" or "electronics" => "electronics",
            "hardware" => "hardware",
            "pathologydiagnostic" or "pathology" or "diagnostic" => "pathology",
            "schooleducation" or "school" or "education" => "education",
            "transportation" or "transport" => "transportation",
            "banquethotelresort" or "hotel" or "resort" or "banquet" => "hospitality",
            "rentalproperty" or "rental" => "rental-property",
            "retail" => "retail",
            _ => "generic"
        };
    }

    private static string Normalize(string? value) =>
        new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static CatalogTemplateDefinition CreateGenericTemplate() => new()
    {
        Key = "generic",
        DisplayName = "Generic business",
        ItemLabel = "Product / service",
        AddButtonText = "Add catalog item",
        Fields =
        [
            new() { Key = "features", Label = "Key features", Type = "textarea", Placeholder = "Main features, specifications or service details", ShowOnWhatsApp = true, SortOrder = 10 },
            new() { Key = "availability", Label = "Availability", Type = "text", Placeholder = "Available / On request", ShowOnWhatsApp = true, SortOrder = 20 }
        ]
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
}
