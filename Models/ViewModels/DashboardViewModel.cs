namespace LearnSphere.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalResources { get; set; }
        public int TotalQuizzes { get; set; }
        public int ActiveResources { get; set; }
        public int RecentRegistrations { get; set; }
        public List<LearningResource> RecentResources { get; set; } = new List<LearningResource>();
        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();
        public Dictionary<string, int> ResourceTypeStats { get; set; } = new Dictionary<string, int>();
    }

    public class StudentDashboardViewModel
    {
        public ApplicationUser Student { get; set; } = null!;
        public int TotalResourcesAccessed { get; set; }
        public int QuizzesCompleted { get; set; }
        public int QuizzesPassed { get; set; }
        public double AverageScore { get; set; }
        public int TotalTimeSpent { get; set; }
        public List<StudentProgress> RecentProgress { get; set; } = new List<StudentProgress>();
        public List<LearningResource> RecommendedResources { get; set; } = new List<LearningResource>();
        public List<StudentProgress> InProgressItems { get; set; } = new List<StudentProgress>();
    }
}
