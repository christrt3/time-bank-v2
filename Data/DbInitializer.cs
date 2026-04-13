using Microsoft.AspNetCore.Identity;
using Time_Bank_V1.Data.Entities;

namespace Time_Bank_V1.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure DB is created
            await context.Database.EnsureCreatedAsync();

            // Seed Roles
            string[] roles = { "Admin", "Member", "Organization" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed Admin user
            if (await userManager.FindByEmailAsync("admin@acc.org") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@acc.org",
                    Email = "admin@acc.org",
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "ACC",
                    AccountType = "Admin",
                    City = "Gainesville",
                    ZipCode = "32601",
                    TimeBalance = 100m,
                    MemberSince = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(admin, "Admin@12345!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Seed Categories if none exist
            if (!context.Categories.Any())
            {
                var parentCategories = new[]
                {
                    new Category { Name = "Education & Tutoring", Description = "Teaching, tutoring, and educational support" },
                    new Category { Name = "Elder Care", Description = "Support services for seniors and elderly community members" },
                    new Category { Name = "Technology", Description = "Tech support, computer help, and digital literacy" },
                    new Category { Name = "Transportation", Description = "Rides, errands, and transportation assistance" },
                    new Category { Name = "Home & Garden", Description = "Yardwork, home repair, and maintenance" },
                    new Category { Name = "Health & Wellness", Description = "Fitness, nutrition guidance, and wellness support" },
                    new Category { Name = "Arts & Crafts", Description = "Creative arts, crafts, and artistic skills" },
                    new Category { Name = "Professional Services", Description = "Legal advice, financial literacy, and professional skills" },
                    new Category { Name = "Child Care", Description = "Babysitting, childcare, and after-school help" },
                    new Category { Name = "Community & Events", Description = "Event planning, volunteering, and community building" },
                };
                context.Categories.AddRange(parentCategories);
                await context.SaveChangesAsync();

                // Sub-categories
                var eduCat = context.Categories.First(c => c.Name == "Education & Tutoring");
                var elderCat = context.Categories.First(c => c.Name == "Elder Care");
                var techCat = context.Categories.First(c => c.Name == "Technology");
                var transCat = context.Categories.First(c => c.Name == "Transportation");
                var homeCat = context.Categories.First(c => c.Name == "Home & Garden");
                var profCat = context.Categories.First(c => c.Name == "Professional Services");

                var subCategories = new[]
                {
                    new Category { Name = "Math Tutoring", ParentCategoryId = eduCat.Id },
                    new Category { Name = "Language Learning", ParentCategoryId = eduCat.Id },
                    new Category { Name = "Tax Assistance", ParentCategoryId = eduCat.Id },
                    new Category { Name = "Companionship", ParentCategoryId = elderCat.Id },
                    new Category { Name = "Grocery Shopping", ParentCategoryId = elderCat.Id },
                    new Category { Name = "Medication Reminders", ParentCategoryId = elderCat.Id },
                    new Category { Name = "Computer Help", ParentCategoryId = techCat.Id },
                    new Category { Name = "Phone & Tablet Help", ParentCategoryId = techCat.Id },
                    new Category { Name = "Medical Appointments", ParentCategoryId = transCat.Id },
                    new Category { Name = "Errands", ParentCategoryId = transCat.Id },
                    new Category { Name = "Lawn Mowing", ParentCategoryId = homeCat.Id },
                    new Category { Name = "Home Repair", ParentCategoryId = homeCat.Id },
                    new Category { Name = "Gardening", ParentCategoryId = homeCat.Id },
                    new Category { Name = "Legal Guidance", ParentCategoryId = profCat.Id },
                    new Category { Name = "Financial Literacy", ParentCategoryId = profCat.Id },
                };
                context.Categories.AddRange(subCategories);
                await context.SaveChangesAsync();

                // Skills
                var skills = new[]
                {
                    new Skill { Name = "Math", CategoryId = eduCat.Id },
                    new Skill { Name = "English / Writing", CategoryId = eduCat.Id },
                    new Skill { Name = "Spanish (Bilingual)", CategoryId = eduCat.Id },
                    new Skill { Name = "Tax Preparation", CategoryId = eduCat.Id },
                    new Skill { Name = "Companionship / Visiting", CategoryId = elderCat.Id },
                    new Skill { Name = "Grocery Runs", CategoryId = elderCat.Id },
                    new Skill { Name = "Computer Troubleshooting", CategoryId = techCat.Id },
                    new Skill { Name = "Smartphone Tutorials", CategoryId = techCat.Id },
                    new Skill { Name = "Driving / Rides", CategoryId = transCat.Id },
                    new Skill { Name = "Errands", CategoryId = transCat.Id },
                    new Skill { Name = "Lawn Care", CategoryId = homeCat.Id },
                    new Skill { Name = "Home Repair (basic)", CategoryId = homeCat.Id },
                    new Skill { Name = "Gardening", CategoryId = homeCat.Id },
                };
                context.Skills.AddRange(skills);
                await context.SaveChangesAsync();
            }
        }
    }
}
