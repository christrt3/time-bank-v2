using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Time_Bank_V1.Data.Entities;
using Time_Bank_V1.Services;

namespace Time_Bank_V1.Pages.Admin
{
    [Authorize(Policy = "RequireAdmin")]
    public class UsersModel : PageModel
    {
        private readonly IAdminService _adminService;
        private readonly ITransactionService _txService;

        public UsersModel(IAdminService adminService, ITransactionService txService)
        {
            _adminService = adminService;
            _txService = txService;
        }

        public List<ApplicationUser> Users { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public async Task OnGetAsync()
        {
            var all = await _adminService.GetAllUsersAsync();
            if (!string.IsNullOrEmpty(Search))
            {
                var s = Search.ToLower();
                all = all.Where(u => u.FullName.ToLower().Contains(s) || (u.Email?.ToLower().Contains(s) ?? false)).ToList();
            }
            Users = all;
        }

        public async Task<IActionResult> OnPostGrantHoursAsync(string userId, decimal hours, string reason)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (success, error) = await _txService.GrantHoursAsync(adminId, userId, hours, reason);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? $"Granted {hours}h to user." : error;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetGrantAsync(string userId, bool eligible, decimal hours)
        {
            await _adminService.SetMonthlyGrantAsync(userId, eligible, hours);
            TempData["SuccessMessage"] = "Monthly grant settings updated.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFlagUserAsync(string userId, string reason)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _adminService.FlagUserAsync(adminId, userId, FlagType.ProfileContent, reason);
            TempData["SuccessMessage"] = "User flagged for review.";
            return RedirectToPage();
        }
    }
}
