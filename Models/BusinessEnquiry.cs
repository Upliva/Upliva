using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessEnquiry
{
    public int Id { get; set; }
    public int BusinessId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Status { get; set; } = "New";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
