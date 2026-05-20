using System.ComponentModel.DataAnnotations;

namespace NasIndexer.ViewModels
{
    public class FileTagFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tag name is required.")]
        [StringLength(60, ErrorMessage = "Tag name must be 60 characters or fewer.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(220, ErrorMessage = "Description must be 220 characters or fewer.")]
        public string? Description { get; set; }

        [RegularExpression(@"^$|^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a hex value like #AABBCC.")]
        public string? Color { get; set; }
    }
}
