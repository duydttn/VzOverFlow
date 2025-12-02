using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VzOverFlow.Models;

namespace VzOverFlow.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            if (context.Users.Any())
            {
                return;
            }

            var passwordHasher = new PasswordHasher<User>();

            var admin = new User
            {
                UserName = "admin",
                Email = "admin@vzoverflow.dev",
                Reputation = 1500,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                TwoFactorEnabled = true
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123");

            var member = new User
            {
                UserName = "linh.nguyen",
                Email = "linh.nguyen@vzoverflow.dev",
                Reputation = 320,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                TwoFactorEnabled = false
            };
            member.PasswordHash = passwordHasher.HashPassword(member, "Member@123");

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

            context.Users.AddRange(admin, member);
            context.Tags.AddRange(tags);
            context.Questions.Add(question);
            context.SaveChanges();
        }
    }
}
