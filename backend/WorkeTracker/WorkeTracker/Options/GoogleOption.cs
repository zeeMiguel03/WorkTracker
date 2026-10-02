using System.ComponentModel.DataAnnotations;

namespace API.Options;

public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    [Required]
    public string ClientId { get; init; } = string.Empty;
}