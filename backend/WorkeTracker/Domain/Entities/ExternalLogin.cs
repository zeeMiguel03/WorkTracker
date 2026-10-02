using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("external_logins")]
public sealed class ExternalLogin
{
    [Key]
    [Column("id")]
    public int Id { get; private set; }

    [Required]
    [Column("provider")]
    [MaxLength(30)]
    public string Provider { get; private set; } = string.Empty;

    [Required]
    [Column("provider_subject")]
    [MaxLength(255)]
    public string ProviderSubject { get; private set; } = string.Empty;

    [Required]
    [Column("user_id")]
    public int UserId { get; private set; }

    public User User { get; private set; } = null!;

    private ExternalLogin() { }

    public static ExternalLogin Google(User user, string subject)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Google subject is required.", nameof(subject));
        }
            
        return new ExternalLogin
        {
            Provider = "Google",
            ProviderSubject = subject,
            User = user
        };
    }
}