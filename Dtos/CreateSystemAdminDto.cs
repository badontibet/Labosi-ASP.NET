using System.ComponentModel.DataAnnotations;

namespace NasIndexer.Dtos
{
    public class CreateSystemAdminDto : IValidatableObject
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(80, ErrorMessage = "Username must be 80 characters or fewer.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(160, ErrorMessage = "Email must be 160 characters or fewer.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [StringLength(80, ErrorMessage = "Role must be 80 characters or fewer.")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "Created date is required.")]
        public DateTime? CreatedDate { get; set; }

        [Required(ErrorMessage = "Last login is required.")]
        public DateTime? LastLogin { get; set; }

        public List<int> ManagedNasServerIds { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CreatedDate.HasValue && LastLogin.HasValue && LastLogin.Value < CreatedDate.Value)
            {
                yield return new ValidationResult(
                    "Last login cannot be before created date.",
                    new[] { nameof(LastLogin) });
            }
        }
    }
}
