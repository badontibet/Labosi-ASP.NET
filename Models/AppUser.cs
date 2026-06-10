using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace NasIndexer.Model
{
    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "OIB is required.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "OIB must contain exactly 11 numeric characters.")]
        public string OIB { get; set; } = string.Empty;

        [Required(ErrorMessage = "JMBG is required.")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "JMBG must contain exactly 13 numeric characters.")]
        public string JMBG { get; set; } = string.Empty;
    }
}
