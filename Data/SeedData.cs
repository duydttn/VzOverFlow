using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VzOverFlow.Models;

namespace VzOverFlow.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            if (await context.Users.AnyAsync())
            {
                return;
            }

            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            const string adminRoleName = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(adminRoleName));
            }

            var admin = new User
            {
                UserName = "admin",
                Email = "admin@vzoverflow.dev",
                Reputation = 1500,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                TwoFactorEnabled = true
            };
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, adminRoleName);

            var member = new User
            {
                UserName = "linh.nguyen",
                Email = "linh.nguyen@vzoverflow.dev",
                Reputation = 320,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                TwoFactorEnabled = false
            };
            await userManager.CreateAsync(member, "Member@123");

            var tags = new[]
            {
                new Tag { Name = "c#", Description = "Ngôn ngữ lập trình C#." },
                new Tag { Name = "asp.net-core", Description = "Framework xây dựng web hiện đại." },
                new Tag { Name = "entity-framework-core", Description = "ORM phổ biến trên .NET." }
            };

            var question = new Question
            {
                Title = "Làm sao tạo ứng dụng hỏi đáp như StackOverflow bằng ASP.NET Core?",
                Body = "Mình muốn tạo clone đơn giản của StackOverflow với MVC + EF Core. Nên bắt đầu từ đâu?",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                User = admin,
                Tags = new List<Tag> { tags[0], tags[1], tags[2] },
                Answers = new List<Answer>
                {
                    new()
            {
                        Body = "Bắt đầu từ việc thiết kế model domain (Question, Answer, Tag) rồi xây dựng controller/view.",
                        User = member,
                        CreatedAt = DateTime.UtcNow.AddDays(-9),
                IsAccepted = true
                    }
                }
            };

            await context.Tags.AddRangeAsync(tags);
            await context.Questions.AddAsync(question);
            await context.SaveChangesAsync();
        }
    }
}
