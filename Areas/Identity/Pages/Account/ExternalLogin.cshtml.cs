using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NasIndexer.Model;

namespace NasIndexer.Areas.Identity.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<ExternalLoginModel> logger;

        public ExternalLoginModel(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            ILogger<ExternalLoginModel> logger)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ProviderDisplayName { get; set; }

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

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
        }

        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Page(
                "./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl });

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"External provider error: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl });
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "External login information could not be loaded.";
                return RedirectToPage("./Login", new { ReturnUrl });
            }

            var signInResult = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                logger.LogInformation("User signed in with {LoginProvider}.", info.LoginProvider);
                return LocalRedirect(ReturnUrl);
            }

            ProviderDisplayName = info.ProviderDisplayName;
            Input.Email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "External login information could not be loaded.";
                return RedirectToPage("./Login", new { ReturnUrl });
            }

            ProviderDisplayName = info.ProviderDisplayName;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim();
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                OIB = Input.OIB.Trim(),
                JMBG = Input.JMBG.Trim()
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                AddErrors(createResult);
                return Page();
            }

            var loginResult = await userManager.AddLoginAsync(user, info);
            if (!loginResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                AddErrors(loginResult);
                return Page();
            }

            logger.LogInformation("A new AppUser account was created with {LoginProvider}.", info.LoginProvider);
            await signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: info.LoginProvider);
            return LocalRedirect(ReturnUrl);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
