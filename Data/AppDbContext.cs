using Microsoft.EntityFrameworkCore;
using VzOverFlow.Models;

namespace VzOverFlow.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Question> Questions { get; set; } = default!;
        public DbSet<Answer> Answers { get; set; } = default!;
        public DbSet<Comment> Comments { get; set; } = default!;
        public DbSet<Tag> Tags { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Vote> Votes { get; set; } = default!;
        public DbSet<OneTimeCode> OneTimeCodes { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Question>()
                .HasMany(q => q.Tags)
                .WithMany(t => t.Questions)
                .UsingEntity(j => j.ToTable("QuestionTags"));

            modelBuilder.Entity<Question>()
                .HasOne(q => q.AcceptedAnswer)
                .WithOne()
                .HasForeignKey<Question>(q => q.AcceptedAnswerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.Name)
                .IsUnique();

            modelBuilder.Entity<Vote>()
                .HasOne(v => v.User)
                .WithMany(u => u.Votes)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OneTimeCode>()
                .HasOne(o => o.User)
                .WithMany(u => u.OneTimeCodes)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
