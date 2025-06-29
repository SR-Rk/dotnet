using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LearnSphere.Data;
using LearnSphere.Models;
using LearnSphere.Models.ViewModels;

namespace LearnSphere.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Dashboard()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalStudents = await _userManager.GetUsersInRoleAsync("Student").ContinueWith(t => t.Result.Count),
                TotalAdmins = await _userManager.GetUsersInRoleAsync("Admin").ContinueWith(t => t.Result.Count),
                TotalResources = await _context.LearningResources.CountAsync(),
                TotalQuizzes = await _context.Quizzes.CountAsync(),
                ActiveResources = await _context.LearningResources.CountAsync(r => r.IsActive),
                RecentRegistrations = await _userManager.Users
                    .Where(u => u.RegistrationDate >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync(),
                RecentResources = await _context.LearningResources
                    .Include(r => r.UploadedBy)
                    .OrderByDescending(r => r.UploadDate)
                    .Take(5)
                    .ToListAsync(),
                RecentUsers = await _userManager.Users
                    .OrderByDescending(u => u.RegistrationDate)
                    .Take(5)
                    .ToListAsync()
            };

            // Get resource type statistics
            viewModel.ResourceTypeStats = await _context.LearningResources
                .GroupBy(r => r.ResourceType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            return View(viewModel);
        }

        public async Task<IActionResult> ManageUsers(string searchTerm = "", string role = "", int page = 1)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            const int pageSize = 10;
            var totalUsers = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedRole = role;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            return View(users);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRoles = userRoles;

            var progress = await _context.StudentProgress
                .Include(sp => sp.Resource)
                .Include(sp => sp.Quiz)
                .Where(sp => sp.StudentId == id)
                .OrderByDescending(sp => sp.LastAccessed)
                .ToListAsync();

            ViewBag.StudentProgress = progress;

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Don't allow deleting the current admin
            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser?.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Delete associated student progress
            var progress = _context.StudentProgress.Where(sp => sp.StudentId == userId);
            _context.StudentProgress.RemoveRange(progress);

            await _userManager.DeleteAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }

        public async Task<IActionResult> ManageResources(string searchTerm = "", string category = "", int page = 1)
        {
            var query = _context.LearningResources
                .Include(r => r.UploadedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => r.Title.Contains(searchTerm) || r.Description.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(r => r.Category == category);
            }

            const int pageSize = 10;
            var totalResources = await query.CountAsync();
            var resources = await query
                .OrderByDescending(r => r.UploadDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.LearningResources
                .Select(r => r.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedCategory = category;
            ViewBag.Categories = categories;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalResources / (double)pageSize);

            return View(resources);
        }

        public IActionResult CreateResource()
        {
            var viewModel = new ResourceUploadViewModel();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateResource(ResourceUploadViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var resource = new LearningResource
                {
                    Title = model.Title,
                    Description = model.Description,
                    ResourceType = model.ResourceType,
                    Category = model.Category,
                    Tags = model.Tags,
                    UploadedById = currentUser!.Id,
                    ExternalUrl = model.ExternalUrl
                };

                // Handle file upload
                if (model.UploadedFile != null && model.UploadedFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.UploadedFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.UploadedFile.CopyToAsync(fileStream);
                    }

                    resource.FilePath = "/uploads/" + uniqueFileName;
                }

                _context.LearningResources.Add(resource);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Resource created successfully.";
                return RedirectToAction(nameof(ManageResources));
            }

            return View(model);
        }

        public async Task<IActionResult> EditResource(int id)
        {
            var resource = await _context.LearningResources.FindAsync(id);
            if (resource == null)
                return NotFound();

            var viewModel = new ResourceUploadViewModel
            {
                Title = resource.Title,
                Description = resource.Description,
                ResourceType = resource.ResourceType,
                Category = resource.Category,
                Tags = resource.Tags,
                ExternalUrl = resource.ExternalUrl
            };

            ViewBag.ResourceId = id;
            ViewBag.CurrentFilePath = resource.FilePath;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditResource(int id, ResourceUploadViewModel model)
        {
            var resource = await _context.LearningResources.FindAsync(id);
            if (resource == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                resource.Title = model.Title;
                resource.Description = model.Description;
                resource.ResourceType = model.ResourceType;
                resource.Category = model.Category;
                resource.Tags = model.Tags;
                resource.ExternalUrl = model.ExternalUrl;
                resource.LastModified = DateTime.UtcNow;

                // Handle file upload
                if (model.UploadedFile != null && model.UploadedFile.Length > 0)
                {
                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(resource.FilePath))
                    {
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, resource.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.UploadedFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.UploadedFile.CopyToAsync(fileStream);
                    }

                    resource.FilePath = "/uploads/" + uniqueFileName;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Resource updated successfully.";
                return RedirectToAction(nameof(ManageResources));
            }

            ViewBag.ResourceId = id;
            ViewBag.CurrentFilePath = resource.FilePath;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.LearningResources.FindAsync(id);
            if (resource == null)
                return NotFound();

            // Delete associated file
            if (!string.IsNullOrEmpty(resource.FilePath))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, resource.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            // Delete associated progress records
            var progressRecords = _context.StudentProgress.Where(sp => sp.ResourceId == id);
            _context.StudentProgress.RemoveRange(progressRecords);

            _context.LearningResources.Remove(resource);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Resource deleted successfully.";
            return RedirectToAction(nameof(ManageResources));
        }

        public async Task<IActionResult> ViewReports()
        {
            var studentProgress = await _context.StudentProgress
                .Include(sp => sp.Student)
                .Include(sp => sp.Resource)
                .Include(sp => sp.Quiz)
                .OrderByDescending(sp => sp.LastAccessed)
                .Take(50)
                .ToListAsync();

            return View(studentProgress);
        }
    }
}
