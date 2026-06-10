using System.ComponentModel.DataAnnotations;

namespace NasIndexer.Dtos
{
    public class CreateDirectoryItemDto : IValidatableObject
    {
        [Required(ErrorMessage = "Directory name is required.")]
        [StringLength(40, ErrorMessage = "Directory name cannot be longer than 40 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Path is required.")]
        [StringLength(100, ErrorMessage = "Path cannot be longer than 100 characters.")]
        public string Path { get; set; } = string.Empty;

        public int? ScanJobId { get; set; }

        public int? ParentId { get; set; }

        [Required(ErrorMessage = "Created date is required.")]
        public DateTime? CreatedDate { get; set; }

        [Required(ErrorMessage = "Modified date is required.")]
        public DateTime? ModifiedDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CreatedDate.HasValue && ModifiedDate.HasValue && ModifiedDate.Value < CreatedDate.Value)
            {
                yield return new ValidationResult(
                    "Modified date cannot be before created date.",
                    new[] { nameof(ModifiedDate) });
            }
        }
    }
}
