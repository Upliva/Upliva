using UplivaAI.Models;

namespace UplivaAI.Services;

public interface ICatalogTemplateService
{
    CatalogTemplateDefinition GetTemplate(string? businessType);
    IReadOnlyList<CatalogTemplateDefinition> GetTemplates();
    IReadOnlyList<CatalogDisplayFieldViewModel> GetDisplayFields(string? businessType, string? attributesJson, bool forWhatsApp);
}
