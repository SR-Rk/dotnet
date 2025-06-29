using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LearnSphere.Data;
using LearnSphere.Models;
using LearnSphere.Models.ViewModels;

namespace LearnSphere.Controllers
{
    public class ResourceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ResourceController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // Public access to browse resources (limited view)
        public async Task<IActionResult> Index(string searchTerm = "", string category = "", string resourceType = "", int page = 1)
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
                .OrderByDescending(r => r.ViewCount)
                .ThenByDescending(r => r.UploadDate)
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

        // Resource details - limited access for non-authenticated users
        public async Task<IActionResult> Details(int id)
        {
            var resource = await _context.LearningResources
                .Include(r => r.UploadedBy)
                .Include(r => r.Quizzes.Where(q => q.IsActive))
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (resource == null)
                return NotFound();

            // Check if user is authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    // Update view count
                    resource.ViewCount++;

                    // Track student progress only for students
                    if (await _userManager.IsInRoleAsync(currentUser, "Student"))
                    {
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
                    }

                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // For non-authenticated users, just update view count
                resource.ViewCount++;
                await _context.SaveChangesAsync();
            }

            return View(resource);
        }

        // Download file - requires authentication
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var resource = await _context.LearningResources
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (resource == null || string.IsNullOrEmpty(resource.FilePath))
                return NotFound();

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, resource.FilePath.TrimStart('/'));
            
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileName = Path.GetFileName(resource.FilePath);
            var contentType = GetContentType(fileName);

            return PhysicalFile(filePath, contentType, fileName);
        }

        // View file - requires authentication
        [Authorize]
        public async Task<IActionResult> View(int id)
        {
            var resource = await _context.LearningResources
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (resource == null)
                return NotFound();

            // If it's an external URL, redirect
            if (!string.IsNullOrEmpty(resource.ExternalUrl))
            {
                return Redirect(resource.ExternalUrl);
            }

            // If it's a file, serve it for viewing
            if (!string.IsNullOrEmpty(resource.FilePath))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, resource.FilePath.TrimStart('/'));
                
                if (!System.IO.File.Exists(filePath))
                    return NotFound();

                var fileName = Path.GetFileName(resource.FilePath);
                var contentType = GetContentType(fileName);

                // For PDF files, display inline
                if (contentType == "application/pdf")
                {
                    return PhysicalFile(filePath, contentType);
                }

                // For other files, force download
                return PhysicalFile(filePath, contentType, fileName);
            }

            return NotFound();
        }

        // Quiz access - requires authentication and student role
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Quiz(int resourceId)
        {
            var resource = await _context.LearningResources
                .Include(r => r.Quizzes.Where(q => q.IsActive))
                .FirstOrDefaultAsync(r => r.Id == resourceId && r.IsActive);

            if (resource == null || !resource.Quizzes.Any())
                return NotFound();

            var quiz = resource.Quizzes.First();
            return RedirectToAction("TakeQuiz", "Student", new { id = quiz.Id });
        }

        // API endpoint for AJAX requests
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UpdateProgress(int resourceId, string status)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Json(new { success = false, message = "User not found" });

            var progress = await _context.StudentProgress
                .FirstOrDefaultAsync(sp => sp.StudentId == currentUser.Id && sp.ResourceId == resourceId);

            if (progress != null)
            {
                progress.CompletionStatus = status;
                progress.LastAccessed = DateTime.UtcNow;
                
                if (status == CompletionStatus.Completed && !progress.CompletedDate.HasValue)
                {
                    progress.CompletedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Progress updated successfully" });
            }

            return Json(new { success = false, message = "Progress record not found" });
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                ".zip" => "application/zip",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}
