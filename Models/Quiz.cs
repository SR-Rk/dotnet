using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public class Quiz
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Time Limit (minutes)")]
        public int? TimeLimitMinutes { get; set; }

        [Display(Name = "Passing Score")]
        public int PassingScore { get; set; } = 70;

        [Display(Name = "Max Attempts")]
        public int MaxAttempts { get; set; } = 3;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int? ResourceId { get; set; }

        // Navigation properties
        [ForeignKey("ResourceId")]
        public virtual LearningResource? Resource { get; set; }
        
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<StudentProgress> StudentProgress { get; set; } = new List<StudentProgress>();
    }
}
