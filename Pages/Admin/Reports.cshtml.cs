using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Time_Bank_V1.Services;

namespace Time_Bank_V1.Pages.Admin
{
    [Authorize(Policy = "RequireAdmin")]
    public class ReportsModel : PageModel
    {
        private readonly IAdminService _adminService;

        public ReportsModel(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public AdminReportData? Report { get; set; }

        public async Task OnGetAsync()
        {
            Report = await _adminService.GenerateReportAsync();
        }

        public async Task<IActionResult> OnGetDownloadUsersAsync()
        {
            var users = await _adminService.GetAllUsersAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Name,Email,AccountType,City,Balance,MemberSince,Locked,MonthlyGrant");
            foreach (var u in users)
            {
                sb.AppendLine($"\"{u.FullName}\",{u.Email},{u.AccountType},{u.City},{u.TimeBalance},{u.MemberSince:yyyy-MM-dd},{u.IsBalanceLocked},{u.MonthlyGrantHours}");
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"ACC_Users_{DateTime.Now:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> OnGetDownloadTransactionsAsync()
        {
            var report = await _adminService.GenerateReportAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Date,Description,Provider,Receiver,Hours,Status,Type");
            foreach (var tx in report.RecentTransactions)
            {
                sb.AppendLine($"{tx.ServiceDate:yyyy-MM-dd},\"{tx.Description}\",\"{tx.Provider?.FullName}\",\"{tx.Receiver?.FullName}\",{tx.Hours},{tx.Status},{tx.TransactionType}");
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"ACC_Transactions_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
