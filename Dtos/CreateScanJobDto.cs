using System.ComponentModel.DataAnnotations;
using NasIndexer.Model;

namespace NasIndexer.Dtos
{
    public class CreateScanJobDto : IValidatableObject
    {
        [Required(ErrorMessage = "NAS server is required.")]
        public int? NasServerId { get; set; }

        [Required]
        [EnumDataType(typeof(ScanStatus))]
        public ScanStatus Status { get; set; } = ScanStatus.Pending;

        [Required(ErrorMessage = "Start time is required.")]
        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required(ErrorMessage = "Root path is required.")]
        [StringLength(180, ErrorMessage = "Root path must be 180 characters or fewer.")]
        public string RootPath { get; set; } = string.Empty;

        [Range(0, long.MaxValue, ErrorMessage = "Total files cannot be negative.")]
        public long TotalFiles { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Processed files cannot be negative.")]
        public long ProcessedFiles { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartTime.HasValue && EndTime.HasValue && EndTime.Value < StartTime.Value)
            {
                yield return new ValidationResult(
                    "End time cannot be before start time.",
                    new[] { nameof(EndTime) });
            }

            if (ProcessedFiles > TotalFiles)
            {
                yield return new ValidationResult(
                    "Processed files cannot be greater than total files.",
                    new[] { nameof(ProcessedFiles) });
            }
        }
    }
}
