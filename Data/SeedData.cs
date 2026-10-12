using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using VzOverFlow.Models;

namespace VzOverFlow.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // 1. Create Admin Role if not exists
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole<int>("Admin"));
            }

            // 2. Create User Role if not exists
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole<int>("User"));
            }

            // 3. Create Specific Admin User
            string adminUsername = "adminnehehe";
            string adminEmail = "bevanduy265@gmail.com";
            string? adminPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");

            var existingAdmin = await userManager.FindByNameAsync(adminUsername);
            if (existingAdmin == null)
            {
                if (string.IsNullOrWhiteSpace(adminPassword)) return;

                var adminUser = new User
                {
                    UserName = adminUsername,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    Reputation = 99999
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                // Ensure existing user has Admin role
                if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                {
                    await userManager.AddToRoleAsync(existingAdmin, "Admin");
                }
            }
        }
    }
}