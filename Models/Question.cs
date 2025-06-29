using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }

        [Required]
        [StringLength(1000)]
        [Display(Name = "Question Text")]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Option A")]
        public string OptionA { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Option B")]
        public string OptionB { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Option C")]
        public string OptionC { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Option D")]
        public string OptionD { get; set; } = string.Empty;

        [Required]
        [StringLength(1)]
        [Display(Name = "Correct Answer")]
        public string CorrectAnswer { get; set; } = string.Empty; // A, B, C, or D

        [StringLength(500)]
        public string Explanation { get; set; } = string.Empty;

        public int OrderIndex { get; set; } = 0;

        public int Points { get; set; } = 1;

        // Navigation property
        [ForeignKey("QuizId")]
        public virtual Quiz Quiz { get; set; } = null!;
    }
}
