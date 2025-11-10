using System.ComponentModel.DataAnnotations;

namespace BettsTax.Web.Options;

public class SamlOptions
{
    public const string SectionName = "Saml";

    [Required]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    public string MetadataUrl { get; set; } = string.Empty;

    [Required]
    public string SignOnUrl { get; set; } = string.Empty;

    [Required]
    public string LogoutUrl { get; set; } = string.Empty;

    [Required]
    public string CertificatePath { get; set; } = string.Empty;

    [Required]
    public string CertificatePassword { get; set; } = string.Empty;

    public bool AllowUnsolicitedAuthnResponse { get; set; } = false;

    public string? NameIdPolicyFormat { get; set; }

    public List<string> RequestedAttributes { get; set; } = new();

    public Dictionary<string, string> AttributeMapping { get; set; } = new();

    public bool ValidateCertificates { get; set; } = true;

    public int MinIncomingSigningAlgorithmStrength { get; set; } = 128;

    public int MaxIncomingSigningAlgorithmStrength { get; set; } = 256;
}