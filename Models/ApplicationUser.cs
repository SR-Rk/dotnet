using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LearnSphere.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Role { get; set; } = "Student";

        [StringLength(500)]
        public string Bio { get; set; } = string.Empty;

        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<LearningResource> UploadedResources { get; set; } = new List<LearningResource>();
        public virtual ICollection<StudentProgress> StudentProgress { get; set; } = new List<StudentProgress>();
    }
}
