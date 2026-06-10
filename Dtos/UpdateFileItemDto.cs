using System.ComponentModel.DataAnnotations;

namespace NasIndexer.Dtos
{
    public class UpdateFileItemDto : IValidatableObject
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "File name is required.")]
        [StringLength(120, ErrorMessage = "File name must be 120 characters or fewer.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Path is required.")]
        [StringLength(260, ErrorMessage = "Path must be 260 characters or fewer.")]
        public string Path { get; set; } = string.Empty;

        [Required(ErrorMessage = "Extension is required.")]
        [StringLength(20, ErrorMessage = "Extension must be 20 characters or fewer.")]
        public string Extension { get; set; } = string.Empty;

        [Range(0, long.MaxValue, ErrorMessage = "Size cannot be negative.")]
        public long Size { get; set; }

        [Required(ErrorMessage = "Directory is required.")]
        public int? DirectoryId { get; set; }

        [Required(ErrorMessage = "Created date is required.")]
        public DateTime? CreatedDate { get; set; }

        [Required(ErrorMessage = "Modified date is required.")]
        public DateTime? ModifiedDate { get; set; }

        public List<int> TagIds { get; set; } = new();

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
