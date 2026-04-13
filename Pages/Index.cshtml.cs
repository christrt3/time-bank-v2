using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Time_Bank_V1.Data.Entities;

namespace Time_Bank_V1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public string? ErrorMessage { get; set; }

        // ── Login ────────────────────────────────────────────────────────────
        public class LoginInputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        [BindProperty]
        public LoginInputModel LoginInput { get; set; } = new();

        // ── Register ─────────────────────────────────────────────────────────
        public class RegisterInputModel
        {
            [Required, Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required, Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            public string? Phone { get; set; }
            public string? City { get; set; }

            [Required]
            public string AccountType { get; set; } = "Member";

            [Required]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Display(Name = "Guardian account")]
            public bool IsGuardian { get; set; }

            [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms.")]
            [Display(Name = "Agree to Terms")]
            public bool AgreeToTerms { get; set; }
        }

        [BindProperty]
        public RegisterInputModel RegisterInput { get; set; } = new();

        // ── OnGet ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> OnGetAsync()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToPage("/Dashboard");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return Page();
        }

        // ── Login handler ─────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostLoginAsync()
        {
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("RegisterInput")))
                ModelState.Remove(key);

            if (!ModelState.IsValid) return Page();

            var result = await _signInManager.PasswordSignInAsync(
                LoginInput.Email,
                LoginInput.Password,
                LoginInput.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToPage("/Dashboard");

            if (result.IsLockedOut)
            {
                ErrorMessage = "Your account has been locked out due to multiple failed attempts. Please try again in 15 minutes.";
                return Page();
            }

            ErrorMessage = "Invalid email or password. Please try again.";
            return Page();
        }

        // ── Register handler ──────────────────────────────────────────────────
        public async Task<IActionResult> OnPostRegisterAsync()
        {
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("LoginInput")))
                ModelState.Remove(key);

            if (!ModelState.IsValid) return Page();

            var user = new ApplicationUser
            {
                UserName = RegisterInput.Email,
                Email = RegisterInput.Email,
                FirstName = RegisterInput.FirstName,
                LastName = RegisterInput.LastName,
                PhoneNumber = RegisterInput.Phone,
                City = RegisterInput.City,
                AccountType = RegisterInput.AccountType,
                IsGuardian = RegisterInput.IsGuardian,
                MemberSince = DateTime.UtcNow,
                EmailConfirmed = true // No email verification in this version
            };

            var result = await _userManager.CreateAsync(user, RegisterInput.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, RegisterInput.AccountType);
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["SuccessMessage"] = $"Welcome to ACC, {user.FirstName}! Complete your profile to get started.";
                return RedirectToPage("/Profile");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        // ── Logout handler ────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Index");
        }
    }
}
