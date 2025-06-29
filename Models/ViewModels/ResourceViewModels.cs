using System.ComponentModel.DataAnnotations;

namespace LearnSphere.Models.ViewModels
{
    public class ResourceListViewModel
    {
        public List<LearningResource> Resources { get; set; } = new List<LearningResource>();
        public string SearchTerm { get; set; } = string.Empty;
        public string SelectedCategory { get; set; } = string.Empty;
        public string SelectedResourceType { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> ResourceTypes { get; set; } = new List<string>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ResourceUploadViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Resource Type")]
        public string ResourceType { get; set; } = string.Empty;

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(200)]
        public string Tags { get; set; } = string.Empty;

        [Display(Name = "Upload File")]
        public IFormFile? UploadedFile { get; set; }

        [StringLength(500)]
        [Display(Name = "External URL")]
        public string? ExternalUrl { get; set; }
    }

    public class QuizCreateViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Time Limit (minutes)")]
        [Range(1, 300)]
        public int? TimeLimitMinutes { get; set; }

        [Display(Name = "Passing Score")]
        [Range(0, 100)]
        public int PassingScore { get; set; } = 70;

        [Display(Name = "Max Attempts")]
        [Range(1, 10)]
        public int MaxAttempts { get; set; } = 3;

        public int? ResourceId { get; set; }

        public List<QuestionCreateViewModel> Questions { get; set; } = new List<QuestionCreateViewModel>();
    }

    public class QuestionCreateViewModel
    {
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
        [Display(Name = "Correct Answer")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [StringLength(500)]
        public string Explanation { get; set; } = string.Empty;

        public int Points { get; set; } = 1;
    }

    public class QuizTakeViewModel
    {
        public Quiz Quiz { get; set; } = null!;
        public List<Question> Questions { get; set; } = new List<Question>();
        public Dictionary<int, string> StudentAnswers { get; set; } = new Dictionary<int, string>();
        public int AttemptNumber { get; set; }
        public DateTime StartTime { get; set; }
        public int? TimeRemainingMinutes { get; set; }
    }

    public class QuizResultViewModel
    {
        public Quiz Quiz { get; set; } = null!;
        public int Score { get; set; }
        public double ScorePercentage { get; set; }
        public bool Passed { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public TimeSpan TimeTaken { get; set; }
        public List<QuestionResultViewModel> QuestionResults { get; set; } = new List<QuestionResultViewModel>();
    }

    public class QuestionResultViewModel
    {
        public Question Question { get; set; } = null!;
        public string StudentAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
}
