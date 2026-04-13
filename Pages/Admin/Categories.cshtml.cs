using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Time_Bank_V1.Data.Entities;
using Time_Bank_V1.Services;

namespace Time_Bank_V1.Pages.Admin
{
    [Authorize(Policy = "RequireAdmin")]
    public class CategoriesModel : PageModel
    {
        private readonly IAdminService _adminService;

        public CategoriesModel(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public List<Category> Categories { get; set; } = new();

        public class CategoryInput
        {
            [Required, MaxLength(100)]
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int? ParentId { get; set; }
        }

        [BindProperty]
        public CategoryInput Input { get; set; } = new();

        public async Task OnGetAsync()
        {
            Categories = await _adminService.GetAllCategoriesAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                Categories = await _adminService.GetAllCategoriesAsync();
                return Page();
            }
            await _adminService.AddCategoryAsync(Input.Name, Input.Description, Input.ParentId);
            TempData["SuccessMessage"] = $"Category \"{Input.Name}\" added.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAsync(int categoryId, bool isActive)
        {
            var cats = await _adminService.GetAllCategoriesAsync();
            var cat = cats.FirstOrDefault(c => c.Id == categoryId);
            if (cat != null)
                await _adminService.UpdateCategoryAsync(categoryId, cat.Name, cat.Description, isActive);
            TempData["SuccessMessage"] = isActive ? "Category activated." : "Category deactivated.";
            return RedirectToPage();
        }
    }
}
