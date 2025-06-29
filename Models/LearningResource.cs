using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public class LearningResource
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Resource Type")]
        public string ResourceType { get; set; } = string.Empty; // PDF, Video, Quiz, Document, etc.

        [StringLength(500)]
        [Display(Name = "File Path")]
        public string? FilePath { get; set; }

        [StringLength(500)]
        [Display(Name = "External URL")]
        public string? ExternalUrl { get; set; }

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(200)]
        public string Tags { get; set; } = string.Empty;

        [Display(Name = "Upload Date")]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public int ViewCount { get; set; } = 0;

        [Required]
        [Display(Name = "Uploaded By")]
        public string UploadedById { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("UploadedById")]
        public virtual ApplicationUser UploadedBy { get; set; } = null!;
        
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public virtual ICollection<StudentProgress> StudentProgress { get; set; } = new List<StudentProgress>();
    }

    public static class ResourceTypes
    {
        public const string PDF = "PDF";
        public const string Video = "Video";
        public const string Quiz = "Quiz";
        public const string Document = "Document";
        public const string Presentation = "Presentation";
        public const string Simulation = "Simulation";
        public const string ExternalLink = "External Link";
    }
}
