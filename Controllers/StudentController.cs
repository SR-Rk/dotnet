using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LearnSphere.Data;
using LearnSphere.Models;
using LearnSphere.Models.ViewModels;

namespace LearnSphere.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var studentProgress = await _context.StudentProgress
                .Where(sp => sp.StudentId == currentUser.Id)
                .Include(sp => sp.Resource)
                .Include(sp => sp.Quiz)
                .ToListAsync();

            var viewModel = new StudentDashboardViewModel
            {
                Student = currentUser,
                TotalResourcesAccessed = studentProgress.Where(sp => sp.ResourceId != null).Count(),
                QuizzesCompleted = studentProgress.Where(sp => sp.QuizId != null && sp.CompletionStatus == CompletionStatus.Completed).Count(),
                QuizzesPassed = studentProgress.Where(sp => sp.QuizId != null && sp.ScorePercentage >= 70).Count(),
                AverageScore = studentProgress.Where(sp => sp.ScorePercentage.HasValue).Any() 
                    ? studentProgress.Where(sp => sp.ScorePercentage.HasValue).Average(sp => sp.ScorePercentage.Value) 
                    : 0,
                TotalTimeSpent = studentProgress.Sum(sp => sp.TimeSpentMinutes),
                RecentProgress = studentProgress
                    .OrderByDescending(sp => sp.LastAccessed)
                    .Take(5)
                    .ToList(),
                InProgressItems = studentProgress
                    .Where(sp => sp.CompletionStatus == CompletionStatus.InProgress)
                    .OrderByDescending(sp => sp.LastAccessed)
                    .Take(5)
                    .ToList()
            };

            // Get recommended resources (not yet accessed)
            var accessedResourceIds = studentProgress.Where(sp => sp.ResourceId.HasValue).Select(sp => sp.ResourceId.Value).ToList();
            viewModel.RecommendedResources = await _context.LearningResources
                .Where(r => r.IsActive && !accessedResourceIds.Contains(r.Id))
                .OrderByDescending(r => r.ViewCount)
                .Take(5)
                .ToListAsync();

            return View(viewModel);
        }

        public async Task<IActionResult> ViewResources(string searchTerm = "", string category = "", string resourceType = "", int page = 1)
        {
            var query = _context.LearningResources
                .Where(r => r.IsActive)
                .Include(r => r.UploadedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => r.Title.Contains(searchTerm) || r.Description.Contains(searchTerm) || r.Tags.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(r => r.Category == category);
            }

            if (!string.IsNullOrEmpty(resourceType))
            {
                query = query.Where(r => r.ResourceType == resourceType);
            }

            const int pageSize = 12;
            var totalResources = await query.CountAsync();
            var resources = await query
                .OrderByDescending(r => r.UploadDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.LearningResources
                .Where(r => r.IsActive)
                .Select(r => r.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var resourceTypes = await _context.LearningResources
                .Where(r => r.IsActive)
                .Select(r => r.ResourceType)
                .Distinct()
                .OrderBy(rt => rt)
                .ToListAsync();

            var viewModel = new ResourceListViewModel
            {
                Resources = resources,
                SearchTerm = searchTerm,
                SelectedCategory = category,
                SelectedResourceType = resourceType,
                Categories = categories,
                ResourceTypes = resourceTypes,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalResources / (double)pageSize),
                PageSize = pageSize
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ResourceDetails(int id)
        {
            var resource = await _context.LearningResources
                .Include(r => r.UploadedBy)
                .Include(r => r.Quizzes.Where(q => q.IsActive))
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (resource == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // Update view count
                resource.ViewCount++;

                // Track or update student progress
                var existingProgress = await _context.StudentProgress
                    .FirstOrDefaultAsync(sp => sp.StudentId == currentUser.Id && sp.ResourceId == id);

                if (existingProgress == null)
                {
                    var progress = new StudentProgress
                    {
                        StudentId = currentUser.Id,
                        ResourceId = id,
                        CompletionStatus = CompletionStatus.InProgress,
                        StartedDate = DateTime.UtcNow,
                        LastAccessed = DateTime.UtcNow
                    };
                    _context.StudentProgress.Add(progress);
                }
                else
                {
                    existingProgress.LastAccessed = DateTime.UtcNow;
                    if (existingProgress.CompletionStatus == CompletionStatus.NotStarted)
                    {
                        existingProgress.CompletionStatus = CompletionStatus.InProgress;
                        existingProgress.StartedDate = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
            }

            return View(resource);
        }

        [HttpPost]
        public async Task<IActionResult> CompleteResource(int resourceId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Json(new { success = false, message = "User not found" });

            var progress = await _context.StudentProgress
                .FirstOrDefaultAsync(sp => sp.StudentId == currentUser.Id && sp.ResourceId == resourceId);

            if (progress != null)
            {
                progress.CompletionStatus = CompletionStatus.Completed;
                progress.CompletedDate = DateTime.UtcNow;
                progress.LastAccessed = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Resource marked as completed" });
            }

            return Json(new { success = false, message = "Progress record not found" });
        }

        public async Task<IActionResult> SelfAssessments()
        {
            var quizzes = await _context.Quizzes
                .Where(q => q.IsActive)
                .Include(q => q.Resource)
                .Include(q => q.Questions)
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var studentProgress = new Dictionary<int, StudentProgress>();

            if (currentUser != null)
            {
                var progressList = await _context.StudentProgress
                    .Where(sp => sp.StudentId == currentUser.Id && sp.QuizId != null)
                    .ToListAsync();

                studentProgress = progressList.ToDictionary(sp => sp.QuizId.Value, sp => sp);
            }

            ViewBag.StudentProgress = studentProgress;
            return View(quizzes);
        }

        public async Task<IActionResult> TakeQuiz(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Resource)
                .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

            if (quiz == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            // Check if student has exceeded max attempts
            var attempts = await _context.StudentProgress
                .Where(sp => sp.StudentId == currentUser.Id && sp.QuizId == id)
                .CountAsync();

            if (attempts >= quiz.MaxAttempts)
            {
                TempData["Error"] = "You have exceeded the maximum number of attempts for this quiz.";
                return RedirectToAction(nameof(SelfAssessments));
            }

            var viewModel = new QuizTakeViewModel
            {
                Quiz = quiz,
                Questions = quiz.Questions.OrderBy(q => q.OrderIndex).ToList(),
                AttemptNumber = attempts + 1,
                StartTime = DateTime.UtcNow,
                TimeRemainingMinutes = quiz.TimeLimitMinutes
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitQuiz(int quizId, Dictionary<int, string> answers, DateTime startTime)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId && q.IsActive);

            if (quiz == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var timeTaken = DateTime.UtcNow - startTime;
            var correctAnswers = 0;
            var totalQuestions = quiz.Questions.Count;
            var questionResults = new List<QuestionResultViewModel>();

            foreach (var question in quiz.Questions)
            {
                var studentAnswer = answers.ContainsKey(question.Id) ? answers[question.Id] : "";
                var isCorrect = question.CorrectAnswer.Equals(studentAnswer, StringComparison.OrdinalIgnoreCase);
                
                if (isCorrect)
                    correctAnswers++;

                questionResults.Add(new QuestionResultViewModel
                {
                    Question = question,
                    StudentAnswer = studentAnswer,
                    IsCorrect = isCorrect,
                    Explanation = question.Explanation
                });
            }

            var scorePercentage = totalQuestions > 0 ? (double)correctAnswers / totalQuestions * 100 : 0;
            var passed = scorePercentage >= quiz.PassingScore;

            // Save student progress
            var progress = new StudentProgress
            {
                StudentId = currentUser.Id,
                QuizId = quizId,
                CompletionStatus = passed ? CompletionStatus.Completed : CompletionStatus.Failed,
                ScorePercentage = scorePercentage,
                TimeSpentMinutes = (int)timeTaken.TotalMinutes,
                AttemptCount = 1,
                StartedDate = startTime,
                CompletedDate = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow
            };

            _context.StudentProgress.Add(progress);
            await _context.SaveChangesAsync();

            var resultViewModel = new QuizResultViewModel
            {
                Quiz = quiz,
                Score = correctAnswers,
                ScorePercentage = scorePercentage,
                Passed = passed,
                CorrectAnswers = correctAnswers,
                TotalQuestions = totalQuestions,
                TimeTaken = timeTaken,
                QuestionResults = questionResults
            };

            return View("QuizResult", resultViewModel);
        }

        public async Task<IActionResult> Profile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            return View(currentUser);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ApplicationUser model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                currentUser.FullName = model.FullName;
                currentUser.Bio = model.Bio;
                currentUser.Department = model.Department;

                var result = await _userManager.UpdateAsync(currentUser);
                if (result.Succeeded)
                {
                    TempData["Success"] = "Profile updated successfully.";
                    return RedirectToAction(nameof(Profile));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
    }
}
