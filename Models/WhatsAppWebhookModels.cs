using System.Text.Json.Serialization;

namespace UplivaAI.Models;

public class WhatsAppWebhookPayload
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("entry")]
    public List<WhatsAppEntry>? Entry { get; set; }
}

public class WhatsAppEntry
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("changes")]
    public List<WhatsAppChange>? Changes { get; set; }
}

public class WhatsAppChange
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("value")]
    public WhatsAppValue? Value { get; set; }
}

public class WhatsAppValue
{
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; set; }

    [JsonPropertyName("metadata")]
    public WhatsAppMetadata? Metadata { get; set; }

    [JsonPropertyName("contacts")]
    public List<WhatsAppContact>? Contacts { get; set; }

    [JsonPropertyName("messages")]
    public List<WhatsAppMessage>? Messages { get; set; }
}

public class WhatsAppMetadata
{
    [JsonPropertyName("display_phone_number")]
    public string? DisplayPhoneNumber { get; set; }

    [JsonPropertyName("phone_number_id")]
    public string? PhoneNumberId { get; set; }
}

public class WhatsAppContact
{
    [JsonPropertyName("wa_id")]
    public string? WaId { get; set; }

    [JsonPropertyName("profile")]
    public WhatsAppProfile? Profile { get; set; }
}

public class WhatsAppProfile
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class WhatsAppMessage
{
    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public WhatsAppText? Text { get; set; }

    [JsonPropertyName("interactive")]
    public WhatsAppInteractive? Interactive { get; set; }

    [JsonPropertyName("image")]
    public WhatsAppImage? Image { get; set; }
}

public class WhatsAppText
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

public class WhatsAppInteractive
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("button_reply")]
    public WhatsAppButtonReply? ButtonReply { get; set; }

    [JsonPropertyName("list_reply")]
    public WhatsAppListReply? ListReply { get; set; }
}

public class WhatsAppButtonReply
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class WhatsAppListReply
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class WhatsAppImage
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
