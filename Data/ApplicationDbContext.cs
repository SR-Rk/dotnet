using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LearnSphere.Models;

namespace LearnSphere.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<LearningResource> LearningResources { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<StudentProgress> StudentProgress { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationships
        builder.Entity<LearningResource>()
            .HasOne(lr => lr.UploadedBy)
            .WithMany(u => u.UploadedResources)
            .HasForeignKey(lr => lr.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Quiz>()
            .HasOne(q => q.Resource)
            .WithMany(lr => lr.Quizzes)
            .HasForeignKey(q => q.ResourceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Question>()
            .HasOne(q => q.Quiz)
            .WithMany(quiz => quiz.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProgress>()
            .HasOne(sp => sp.Student)
            .WithMany(u => u.StudentProgress)
            .HasForeignKey(sp => sp.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProgress>()
            .HasOne(sp => sp.Resource)
            .WithMany(lr => lr.StudentProgress)
            .HasForeignKey(sp => sp.ResourceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<StudentProgress>()
            .HasOne(sp => sp.Quiz)
            .WithMany(q => q.StudentProgress)
            .HasForeignKey(sp => sp.QuizId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure indexes for better performance
        builder.Entity<LearningResource>()
            .HasIndex(lr => lr.Category);

        builder.Entity<LearningResource>()
            .HasIndex(lr => lr.ResourceType);

        builder.Entity<LearningResource>()
            .HasIndex(lr => lr.UploadDate);

        builder.Entity<StudentProgress>()
            .HasIndex(sp => new { sp.StudentId, sp.ResourceId });

        builder.Entity<StudentProgress>()
            .HasIndex(sp => new { sp.StudentId, sp.QuizId });

        // Configure decimal precision
        builder.Entity<StudentProgress>()
            .Property(sp => sp.ScorePercentage)
            .HasPrecision(5, 2);
    }
}
