using System.ComponentModel.DataAnnotations;
using System.Net;

namespace NasIndexer.Dtos
{
    public class UpdateNasServerDto : IValidatableObject
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(120, ErrorMessage = "Name must be 120 characters or fewer.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "IP address is required.")]
        [StringLength(45, ErrorMessage = "IP address must be 45 characters or fewer.")]
        public string IpAddress { get; set; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        public int Port { get; set; } = 445;

        [StringLength(80, ErrorMessage = "Username must be 80 characters or fewer.")]
        public string? Username { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Last scan is required.")]
        public DateTime? LastScan { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(IpAddress) && !IPAddress.TryParse(IpAddress.Trim(), out _))
            {
                yield return new ValidationResult(
                    "Enter a valid IPv4 or IPv6 address.",
                    new[] { nameof(IpAddress) });
            }
        }
    }
}
