using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NasIndexer.Model;

namespace NasIndexer.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<RegisterModel> logger;

        public RegisterModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email address.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "OIB is required.")]
            [RegularExpression(@"^\d{11}$", ErrorMessage = "OIB must contain exactly 11 numeric characters.")]
            [Display(Name = "OIB")]
            public string OIB { get; set; } = string.Empty;

            [Required(ErrorMessage = "JMBG is required.")]
            [RegularExpression(@"^\d{13}$", ErrorMessage = "JMBG must contain exactly 13 numeric characters.")]
            [Display(Name = "JMBG")]
            public string JMBG { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(100, ErrorMessage = "The password must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new AppUser
            {
                UserName = Input.Email.Trim(),
                Email = Input.Email.Trim(),
                OIB = Input.OIB.Trim(),
                JMBG = Input.JMBG.Trim()
            };

            var result = await userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                logger.LogInformation("A local AppUser account was created.");
                await signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(ReturnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
