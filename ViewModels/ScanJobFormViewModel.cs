using System.ComponentModel.DataAnnotations;
using NasIndexer.Model;
using NasIndexer.Utilities;

namespace NasIndexer.ViewModels
{
    public class ScanJobFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Choose a NAS server.")]
        [Display(Name = "NAS Server")]
        public int? NasServerId { get; set; }

        [Required(ErrorMessage = "Choose a NAS server.")]
        [Display(Name = "NAS Server")]
        public string NasServerLabel { get; set; } = string.Empty;

        [Required]
        public ScanStatus Status { get; set; } = ScanStatus.Pending;

        [Required(ErrorMessage = "Start time is required.")]
        [Display(Name = "Start Time")]
        public string StartTimeText { get; set; } = string.Empty;

        [Display(Name = "End Time")]
        public string? EndTimeText { get; set; }

        [Required(ErrorMessage = "Root path is required.")]
        [StringLength(180, ErrorMessage = "Root path must be 180 characters or fewer.")]
        [Display(Name = "Root Path")]
        public string RootPath { get; set; } = string.Empty;

        [Range(0, long.MaxValue, ErrorMessage = "Total files cannot be negative.")]
        [Display(Name = "Total Files")]
        public long TotalFiles { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Processed files cannot be negative.")]
        [Display(Name = "Processed Files")]
        public long ProcessedFiles { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasStartTime = DateTimeInputParser.TryParse(StartTimeText, out var startTime);
            var hasEndTime = DateTimeInputParser.TryParseOptional(EndTimeText, out var endTime);

            if (!string.IsNullOrWhiteSpace(StartTimeText) && !hasStartTime)
            {
                yield return new ValidationResult(
                    "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.",
                    new[] { nameof(StartTimeText) });
            }

            if (!hasEndTime)
            {
                yield return new ValidationResult(
                    "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.",
                    new[] { nameof(EndTimeText) });
            }

            if (hasStartTime && hasEndTime && endTime.HasValue && endTime.Value < startTime)
            {
                yield return new ValidationResult(
                    "End time cannot be before start time.",
                    new[] { nameof(EndTimeText) });
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
