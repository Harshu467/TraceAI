using System.ComponentModel.DataAnnotations;

namespace TraceAI.Api.Models;

public class ContactRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string Message { get; set; } = string.Empty;
}
