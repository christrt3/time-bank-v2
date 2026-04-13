using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;

namespace Time_Bank_V1.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _context = context;
            _env = env;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public List<Category> Categories { get; set; } = new();
        public List<Skill> AllSkills { get; set; } = new();
        public List<int> SelectedSkillIds { get; set; } = new();
        public List<Feedback> ReceivedFeedback { get; set; } = new();
        public int FeedbackCount { get; set; }

        public class ProfileInputModel
        {
            [Required] public string FirstName { get; set; } = string.Empty;
            [Required] public string LastName { get; set; } = string.Empty;
            [Required, EmailAddress] public string Email { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? City { get; set; }
            public string? ZipCode { get; set; }
            public string? Bio { get; set; }
            public string? Licensure { get; set; }
            public bool ShowLicensure { get; set; }
            public string PreferredLanguage { get; set; } = "English";
            public string? OrganizationName { get; set; }

            // Notification preferences
            public bool NotifyInApp { get; set; }
            public bool NotifyEmail { get; set; }
            public bool NotifySms { get; set; }
            public bool NotifyMatchAlerts { get; set; }

            // Availability (Day index 0=Mon…6=Sun, slot 0=Morning,1=Afternoon,2=Evening)
            public bool[] MorningAvailability { get; set; } = new bool[7];
            public bool[] AfternoonAvailability { get; set; } = new bool[7];
            public bool[] EveningAvailability { get; set; } = new bool[7];

            // Which fields to show publicly
            public bool ShowCity { get; set; } = true;
            public bool ShowPhone { get; set; } = false;
            public bool ShowAvailability { get; set; } = true;
            public bool ShowSkills { get; set; } = true;
            public bool ShowBio { get; set; } = true;

            // Selected skill IDs (comma-separated from form)
            public List<int> SkillIds { get; set; } = new();
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? ProfilePicture { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            CurrentUser = await _context.Users
                .Include(u => u.UserSkills).ThenInclude(us => us.Skill)
                .Include(u => u.Availabilities)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (CurrentUser == null) return RedirectToPage("/Index");

            await LoadDropdownsAsync();
            PopulateInput(CurrentUser);

            ReceivedFeedback = await _context.Feedbacks
                .Include(f => f.FromUser)
                .Where(f => f.ToUserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .ToListAsync();
            FeedbackCount = ReceivedFeedback.Count;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            CurrentUser = await _context.Users
                .Include(u => u.UserSkills)
                .Include(u => u.Availabilities)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (CurrentUser == null) return RedirectToPage("/Index");

            await LoadDropdownsAsync();

            if (!ModelState.IsValid) return Page();

            // Profile picture upload
            if (ProfilePicture != null && ProfilePicture.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(ProfilePicture.ContentType))
                {
                    ModelState.AddModelError(nameof(ProfilePicture), "Only JPG, PNG, GIF, or WEBP images are allowed.");
                    return Page();
                }

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{userId}_{DateTime.UtcNow.Ticks}{Path.GetExtension(ProfilePicture.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = System.IO.File.Create(filePath))
                    await ProfilePicture.CopyToAsync(stream);

                // Delete old picture if exists
                if (!string.IsNullOrEmpty(CurrentUser.ProfilePicturePath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, CurrentUser.ProfilePicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                CurrentUser.ProfilePicturePath = $"/uploads/avatars/{fileName}";
            }

            // Update user fields
            CurrentUser.FirstName = Input.FirstName;
            CurrentUser.LastName = Input.LastName;
            CurrentUser.Email = Input.Email;
            CurrentUser.UserName = Input.Email;
            CurrentUser.PhoneNumber = Input.Phone;
            CurrentUser.City = Input.City;
            CurrentUser.ZipCode = Input.ZipCode;
            CurrentUser.Bio = Input.Bio;
            CurrentUser.Licensure = Input.Licensure;
            CurrentUser.ShowLicensure = Input.ShowLicensure;
            CurrentUser.PreferredLanguage = Input.PreferredLanguage;
            CurrentUser.OrganizationName = Input.OrganizationName;
            CurrentUser.NotifyInApp = Input.NotifyInApp;
            CurrentUser.NotifyEmail = Input.NotifyEmail;
            CurrentUser.NotifySms = Input.NotifySms;
            CurrentUser.NotifyMatchAlerts = Input.NotifyMatchAlerts;

            // Build public visibility string
            var vis = new List<string> { "Name" };
            if (Input.ShowCity) vis.Add("City");
            if (Input.ShowPhone) vis.Add("Phone");
            if (Input.ShowAvailability) vis.Add("Availability");
            if (Input.ShowSkills) vis.Add("Skills");
            if (Input.ShowBio) vis.Add("Bio");
            CurrentUser.PublicFieldVisibility = string.Join(",", vis);

            // Update skills
            _context.UserSkills.RemoveRange(CurrentUser.UserSkills);
            foreach (var skillId in Input.SkillIds.Distinct())
            {
                _context.UserSkills.Add(new UserSkill { UserId = userId, SkillId = skillId });
            }

            // Update availability
            _context.Availabilities.RemoveRange(CurrentUser.Availabilities);
            string[] days = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            DayOfWeek[] daysEnum = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            for (int i = 0; i < 7; i++)
            {
                if (Input.MorningAvailability[i])
                    _context.Availabilities.Add(new Availability { UserId = userId, DayOfWeek = daysEnum[i], TimeSlot = TimeSlot.Morning });
                if (Input.AfternoonAvailability[i])
                    _context.Availabilities.Add(new Availability { UserId = userId, DayOfWeek = daysEnum[i], TimeSlot = TimeSlot.Afternoon });
                if (Input.EveningAvailability[i])
                    _context.Availabilities.Add(new Availability { UserId = userId, DayOfWeek = daysEnum[i], TimeSlot = TimeSlot.Evening });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToPage();
        }

        private async Task LoadDropdownsAsync()
        {
            Categories = await _context.Categories
                .Where(c => c.ParentCategoryId == null && c.IsActive)
                .Include(c => c.SubCategories)
                .Include(c => c.Skills)
                .OrderBy(c => c.Name)
                .ToListAsync();

            AllSkills = await _context.Skills.Include(s => s.Category).OrderBy(s => s.Name).ToListAsync();
        }

        private void PopulateInput(ApplicationUser user)
        {
            Input.FirstName = user.FirstName;
            Input.LastName = user.LastName;
            Input.Email = user.Email ?? string.Empty;
            Input.Phone = user.PhoneNumber;
            Input.City = user.City;
            Input.ZipCode = user.ZipCode;
            Input.Bio = user.Bio;
            Input.Licensure = user.Licensure;
            Input.ShowLicensure = user.ShowLicensure;
            Input.PreferredLanguage = user.PreferredLanguage;
            Input.OrganizationName = user.OrganizationName;
            Input.NotifyInApp = user.NotifyInApp;
            Input.NotifyEmail = user.NotifyEmail;
            Input.NotifySms = user.NotifySms;
            Input.NotifyMatchAlerts = user.NotifyMatchAlerts;
            Input.SkillIds = user.UserSkills.Select(us => us.SkillId).ToList();
            SelectedSkillIds = Input.SkillIds;

            // Parse visibility
            var vis = user.PublicFieldVisibility?.Split(',') ?? Array.Empty<string>();
            Input.ShowCity = vis.Contains("City");
            Input.ShowPhone = vis.Contains("Phone");
            Input.ShowAvailability = vis.Contains("Availability");
            Input.ShowSkills = vis.Contains("Skills");
            Input.ShowBio = vis.Contains("Bio");

            // Availability
            DayOfWeek[] daysEnum = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            for (int i = 0; i < 7; i++)
            {
                Input.MorningAvailability[i] = user.Availabilities.Any(a => a.DayOfWeek == daysEnum[i] && a.TimeSlot == TimeSlot.Morning);
                Input.AfternoonAvailability[i] = user.Availabilities.Any(a => a.DayOfWeek == daysEnum[i] && a.TimeSlot == TimeSlot.Afternoon);
                Input.EveningAvailability[i] = user.Availabilities.Any(a => a.DayOfWeek == daysEnum[i] && a.TimeSlot == TimeSlot.Evening);
            }
        }
    }
}
