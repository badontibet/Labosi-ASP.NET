using System.ComponentModel.DataAnnotations;
using System.Net;
using NasIndexer.Utilities;

namespace NasIndexer.ViewModels
{
    public class NasServerFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "IP Address")]
        [Required]
        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; } = 445;

        [StringLength(80)]
        public string? Username { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Last Scan")]
        [Required]
        public string LastScanText { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(IpAddress) && !IPAddress.TryParse(IpAddress.Trim(), out _))
            {
                yield return new ValidationResult(
                    "Enter a valid IPv4 or IPv6 address.",
                    new[] { nameof(IpAddress) });
            }

            if (string.IsNullOrWhiteSpace(LastScanText))
            {
                yield break;
            }

            if (!DateTimeInputParser.TryParse(LastScanText, out _))
            {
                yield return new ValidationResult(
                    "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.",
                    new[] { nameof(LastScanText) });
            }
        }
    }
}
