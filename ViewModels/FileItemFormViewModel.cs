using System.ComponentModel.DataAnnotations;
using NasIndexer.Utilities;

namespace NasIndexer.ViewModels
{
    public class FileItemFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "File name is required.")]
        [StringLength(120, ErrorMessage = "File name must be 120 characters or fewer.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Path is required.")]
        [StringLength(260, ErrorMessage = "Path must be 260 characters or fewer.")]
        public string Path { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Extension must be 20 characters or fewer.")]
        public string? Extension { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Size cannot be negative.")]
        public long Size { get; set; }

        [Required(ErrorMessage = "Choose a directory.")]
        [Display(Name = "Directory")]
        public int? DirectoryId { get; set; }

        [Required(ErrorMessage = "Choose a directory.")]
        [Display(Name = "Directory")]
        public string DirectoryLabel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Created date is required.")]
        [Display(Name = "Created Date")]
        public string CreatedDateText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Modified date is required.")]
        [Display(Name = "Modified Date")]
        public string ModifiedDateText { get; set; } = string.Empty;

        public List<int> SelectedTagIds { get; set; } = new();
        public List<TagCheckboxViewModel> AvailableTags { get; set; } = new();

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
        }
    }

    public class TagCheckboxViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
