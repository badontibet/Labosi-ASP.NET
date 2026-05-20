using System.ComponentModel.DataAnnotations;
using NasIndexer.Utilities;

namespace NasIndexer.ViewModels
{
    public class DirectoryItemFormViewModel : IValidatableObject
    {
        public const int NameSoftLimit = 20;
        public const int NameHardLimit = NameSoftLimit * 2;
        public const int PathSoftLimit = 50;
        public const int PathHardLimit = PathSoftLimit * 2;

        public int Id { get; set; }

        [Required(ErrorMessage = "Directory name is required.")]
        [StringLength(NameHardLimit, ErrorMessage = "Directory name cannot be longer than 40 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Path is required.")]
        [StringLength(PathHardLimit, ErrorMessage = "Path cannot be longer than 100 characters.")]
        public string Path { get; set; } = string.Empty;

        public int? ScanJobId { get; set; }

        [Display(Name = "Scan Job")]
        public string? ScanJobLabel { get; set; }

        public int? ParentDirectoryId { get; set; }

        [Display(Name = "Parent Directory")]
        public string? ParentDirectoryLabel { get; set; }

        [Required(ErrorMessage = "Created date is required.")]
        [Display(Name = "Created Date")]
        public string CreatedDateText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Modified date is required.")]
        [Display(Name = "Modified Date")]
        public string ModifiedDateText { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasCreatedDate = DateTimeInputParser.TryParse(CreatedDateText, out var createdDate);
            var hasModifiedDate = DateTimeInputParser.TryParse(ModifiedDateText, out var modifiedDate);

            if (!string.IsNullOrWhiteSpace(CreatedDateText) && !hasCreatedDate)
            {
                yield return new ValidationResult(
                    "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.",
                    new[] { nameof(CreatedDateText) });
            }

            if (!string.IsNullOrWhiteSpace(ModifiedDateText) && !hasModifiedDate)
            {
                yield return new ValidationResult(
                    "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.",
                    new[] { nameof(ModifiedDateText) });
            }

            if (hasCreatedDate && hasModifiedDate && modifiedDate < createdDate)
            {
                yield return new ValidationResult(
                    "Modified date cannot be before created date.",
                    new[] { nameof(ModifiedDateText) });
            }

            if (Id > 0 && ParentDirectoryId == Id)
            {
                yield return new ValidationResult(
                    "A directory cannot be its own parent.",
                    new[] { nameof(ParentDirectoryId) });
            }
        }
    }
}
