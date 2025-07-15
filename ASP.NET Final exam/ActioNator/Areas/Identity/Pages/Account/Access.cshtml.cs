using ActioNator.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.ApplicationUser;

namespace ActioNator.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class AccessModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;

        public AccessModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUserStore<ApplicationUser> userStore)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userStore = userStore;
            _emailStore = GetEmailStore(userStore);
        }


        [BindProperty]
        public RegisterInputModel RegisterInput { get; set; }

        [BindProperty]
        public LoginInputModel LoginInput { get; set; }

        public string ReturnUrl { get; set; } = "/Home/Index";

        public void OnGet(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect(ReturnUrl);
                return;
            }

            ReturnUrl = returnUrl ?? ReturnUrl;
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRegisterAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? ReturnUrl;

            if (TryValidateModel(RegisterInput, nameof(RegisterInput)))
            {
                return Page();
            }

            var user = CreateUser();

            await _userStore.SetUserNameAsync(user, RegisterInput.FirstName + RegisterInput.LastName, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, RegisterInput.Email, CancellationToken.None);

            user
                .FirstName = RegisterInput.FirstName;
            user
                .LastName = RegisterInput.LastName;

            var result = await _userManager.CreateAsync(user, RegisterInput.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(ReturnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLoginAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? ReturnUrl;

            if (TryValidateModel(LoginInput, nameof(LoginInput)))
            {
                return Page();
            }

            var user 
                = await _userManager.FindByEmailAsync(LoginInput.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                LoginInput.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return LocalRedirect(ReturnUrl);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore(IUserStore<ApplicationUser> store)
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The user store does not support email.");
            }

            return (IUserEmailStore<ApplicationUser>)store;
        }


        public class RegisterInputModel
        {
            [Required]
            [Display(Name = "First Name")]
            [MinLength(FirstNameMinLength)]
            [MaxLength(FirstNameMaxLength)]
            public string FirstName { get; set; } = null!;

            [Required]
            [Display(Name = "Last Name")]
            [MinLength(LastNameMinLength)]
            [MaxLength(LastNameMaxLength)]
            public string LastName { get; set; } = null!;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = null!;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = null!;

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = null!;
        }

        public class LoginInputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = null!;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = null!;
        }
    }
}
