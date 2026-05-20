using System.ComponentModel.DataAnnotations;
using NasIndexer.Utilities;

namespace NasIndexer.ViewModels
{
    public class SystemAdminFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(160)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Created Date")]
        [Required]
        public string CreatedDateText { get; set; } = string.Empty;

        [Display(Name = "Last Login")]
        [Required]
        public string LastLoginText { get; set; } = string.Empty;

        public List<int> SelectedNasServerIds { get; set; } = new();
        public List<NasServerCheckboxViewModel> AvailableNasServers { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasCreatedDate = DateTimeInputParser.TryParse(CreatedDateText, out var createdDate);
            var hasLastLogin = DateTimeInputParser.TryParse(LastLoginText, out var lastLogin);

            if (!string.IsNullOrWhiteSpace(CreatedDateText) && !hasCreatedDate)
            {
                yield return new ValidationResult(
                    "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.",
                    new[] { nameof(CreatedDateText) });
            }

            if (!string.IsNullOrWhiteSpace(LastLoginText) && !hasLastLogin)
            {
                yield return new ValidationResult(
                    "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.",
                    new[] { nameof(LastLoginText) });
            }

            if (hasCreatedDate && hasLastLogin && lastLogin < createdDate)
            {
                yield return new ValidationResult(
                    "Last login cannot be before created date.",
                    new[] { nameof(LastLoginText) });
            }
        }
    }

    public class NasServerCheckboxViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
