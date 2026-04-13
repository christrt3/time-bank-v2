using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Time_Bank_V1.Services;

namespace Time_Bank_V1.Pages.Admin
{
    [Authorize(Policy = "RequireAdmin")]
    public class IndexModel : PageModel
    {
        private readonly IAdminService _adminService;

        public IndexModel(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public AdminReportData? Report { get; set; }

        public async Task OnGetAsync()
        {
            Report = await _adminService.GenerateReportAsync();
        }
    }
}
