namespace UplivaAI.Services;

public sealed class AzureStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "upliva-media";
    public int MaxImageSizeMb { get; set; } = 5;
}
