using System.Text;
using System.Text.Json;
using UplivaAI.Models;

namespace UplivaAI.Services;

public sealed class CatalogImportService : ICatalogImportService
{
    private static readonly string[] NameAliases = ["name", "productname", "product name", "item", "itemname", "title"];

    public async Task<(List<CatalogImportRecord> Records, List<string> Errors, string Format)> ParseAsync(
        IFormFile file, CatalogTemplateDefinition template, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".csv" and not ".json")
            return ([], ["Only CSV and JSON files are supported. Download the template and try again."], extension);

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
            return ([], ["The uploaded file is empty."], extension);

        return extension == ".json"
            ? ParseJson(text, template)
            : ParseCsv(text, template);
    }

    private static (List<CatalogImportRecord>, List<string>, string) ParseJson(string text, CatalogTemplateDefinition template)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                return ([], ["JSON must contain an array of catalog objects."], ".json");

            var records = new List<CatalogImportRecord>();
            var errors = new List<string>();
            var row = 1;
            foreach (var element in root.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"JSON row {row}: expected an object.");
                    row++;
                    continue;
                }
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in element.EnumerateObject())
                    values[property.Name.Trim()] = property.Value.ValueKind == JsonValueKind.Null ? string.Empty : property.Value.ToString().Trim();
                records.Add(new CatalogImportRecord { RowNumber = row++, Values = NormalizeKeys(values, template) });
            }
            return (records, errors, ".json");
        }
        catch (JsonException ex)
        {
            return ([], [$"Invalid JSON: {ex.Message}"], ".json");
        }
    }

    private static (List<CatalogImportRecord>, List<string>, string) ParseCsv(string text, CatalogTemplateDefinition template)
    {
        var lines = ParseCsvLines(text);
        if (lines.Count == 0) return ([], ["The CSV file does not contain any rows."], ".csv");

        var headers = lines[0].Select(x => x.Trim()).ToList();
        if (headers.Count == 0) return ([], ["The CSV header row is empty."], ".csv");

        var records = new List<CatalogImportRecord>();
        var errors = new List<string>();
        for (var i = 1; i < lines.Count; i++)
        {
            var cells = lines[i];
            if (cells.All(string.IsNullOrWhiteSpace)) continue;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
                values[headers[c]] = c < cells.Count ? cells[c].Trim() : string.Empty;
            records.Add(new CatalogImportRecord { RowNumber = i + 1, Values = NormalizeKeys(values, template) });
        }
        return (records, errors, ".csv");
    }

    private static Dictionary<string, string> NormalizeKeys(Dictionary<string, string> source, CatalogTemplateDefinition template)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            var key = ResolveKey(pair.Key, template);
            if (!string.IsNullOrWhiteSpace(key)) result[key] = pair.Value;
        }
        return result;
    }

    private static string ResolveKey(string header, CatalogTemplateDefinition template)
    {
        var normalized = Normalize(header);
        if (NameAliases.Contains(normalized, StringComparer.OrdinalIgnoreCase)) return "Name";

        var common = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["brand"]="Brand", ["model"]="Model", ["sku"]="SKU", ["category"]="Category",
            ["price"]="PriceText", ["pricetext"]="PriceText", ["sellingprice"]="PriceText",
            ["originalprice"]="OriginalPriceText", ["originalpricetext"]="OriginalPriceText", ["mrp"]="OriginalPriceText",
            ["discount"]="DiscountText", ["discounttext"]="DiscountText", ["image"]="ImageUrl", ["imageurl"]="ImageUrl",
            ["shortdescription"]="ShortDescription", ["description"]="Description", ["stockstatus"]="StockStatus",
            ["stock"]="StockStatus", ["rating"]="Rating", ["reviewcount"]="ReviewCount", ["reviews"]="ReviewCount",
            ["sortorder"]="SortOrder", ["displayorder"]="SortOrder"
        };
        if (common.TryGetValue(normalized, out var commonKey)) return commonKey;

        foreach (var field in template.Fields)
        {
            if (Normalize(field.Key) == normalized || Normalize(field.Label) == normalized) return field.Key;
        }
        return string.Empty;
    }

    private static string Normalize(string value) => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static List<List<string>> ParseCsvLines(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (ch == ',' && !quoted) { row.Add(cell.ToString()); cell.Clear(); }
            else if ((ch == '\n' || ch == '\r') && !quoted)
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString()); cell.Clear(); rows.Add(row); row = new List<string>();
            }
            else cell.Append(ch);
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
        return rows;
    }

    public static string CreateCsvTemplate(CatalogTemplateDefinition template)
    {
        var headers = new List<string> { "Name", "Brand", "Model", "SKU", "Category", "PriceText", "OriginalPriceText", "DiscountText", "ImageUrl", "ShortDescription", "Description", "StockStatus", "Rating", "ReviewCount", "SortOrder" };
        headers.AddRange(template.Fields.OrderBy(x => x.SortOrder).Select(x => x.Key));
        return string.Join(",", headers.Select(Csv)) + "\r\n";
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}

public interface ICatalogImportService
{
    Task<(List<CatalogImportRecord> Records, List<string> Errors, string Format)> ParseAsync(IFormFile file, CatalogTemplateDefinition template, CancellationToken cancellationToken = default);
}
