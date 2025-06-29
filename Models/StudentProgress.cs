using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public class StudentProgress
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student")]
        public string StudentId { get; set; } = string.Empty;

        public int? ResourceId { get; set; }

        public int? QuizId { get; set; }

        [StringLength(50)]
        [Display(Name = "Completion Status")]
        public string CompletionStatus { get; set; } = "Not Started"; // Not Started, In Progress, Completed

        [Display(Name = "Score Percentage")]
        public double? ScorePercentage { get; set; }

        [Display(Name = "Time Spent (minutes)")]
        public int TimeSpentMinutes { get; set; } = 0;

        [Display(Name = "Attempts")]
        public int AttemptCount { get; set; } = 0;

        [Display(Name = "Started Date")]
        public DateTime? StartedDate { get; set; }

        [Display(Name = "Completed Date")]
        public DateTime? CompletedDate { get; set; }

        [Display(Name = "Last Accessed")]
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; } = null!;

        [ForeignKey("ResourceId")]
        public virtual LearningResource? Resource { get; set; }

        [ForeignKey("QuizId")]
        public virtual Quiz? Quiz { get; set; }
    }

    public static class CompletionStatus
    {
        public const string NotStarted = "Not Started";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
