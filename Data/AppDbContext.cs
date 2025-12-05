using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Models;

namespace VzOverFlow.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Question> Questions { get; set; } = default!;
        public DbSet<Answer> Answers { get; set; } = default!;
        public DbSet<Comment> Comments { get; set; } = default!;
        public DbSet<Tag> Tags { get; set; } = default!;
        public new DbSet<User> Users { get; set; } = default!;
        public DbSet<Vote> Votes { get; set; } = default!;
        public DbSet<OneTimeCode> OneTimeCodes { get; set; } = default!;
        public DbSet<Report> Reports { get; set; } = default!;
        public DbSet<UserActivity> UserActivities { get; set; } = default!;
        public DbSet<UserBadge> UserBadges { get; set; } = default!;
        public DbSet<DailyMission> DailyMissions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>().ToTable("Users");
            builder.Entity<IdentityRole<int>>().ToTable("Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

            builder.Entity<Question>()
                .HasMany(q => q.Tags)
                .WithMany(t => t.Questions)
                .UsingEntity(j => j.ToTable("QuestionTags"));

            builder.Entity<Question>()
                .HasOne(q => q.AcceptedAnswer)
                .WithOne()
                .HasForeignKey<Question>(q => q.AcceptedAnswerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Tag>()
                .HasIndex(t => t.Name)
                .IsUnique();

            builder.Entity<Vote>()
                .HasOne(v => v.User)
                .WithMany(u => u.Votes)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OneTimeCode>()
                .HasOne(o => o.User)
                .WithMany(u => u.OneTimeCodes)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Report>()
                .HasOne(r => r.Question)
                .WithMany()
                .HasForeignKey(r => r.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Report>()
                .HasOne(r => r.Answer)
                .WithMany()
                .HasForeignKey(r => r.AnswerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Report>()
                .HasOne(r => r.ResolvedBy)
                .WithMany()
                .HasForeignKey(r => r.ResolvedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Report>()
                .ToTable(t => t.HasCheckConstraint("CK_Report_Content", 
          "(QuestionId IS NOT NULL AND AnswerId IS NULL) OR (QuestionId IS NULL AND AnswerId IS NOT NULL)"));

            builder.Entity<UserActivity>()
                .HasOne(a => a.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserActivity>()
                .HasIndex(a => new { a.UserId, a.CreatedAt });

            builder.Entity<UserBadge>()
                .HasOne(b => b.User)
                .WithMany(u => u.Badges)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserBadge>()
                .HasIndex(b => new { b.UserId, b.Badge })
                .IsUnique();

            builder.Entity<DailyMission>()
                .HasOne(d => d.User)
                .WithMany(u => u.DailyMissions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DailyMission>()
                .HasIndex(d => new { d.UserId, d.Date })
                .IsUnique();
        }
    }
}
