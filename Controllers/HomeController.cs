using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LearnSphere.Models;

namespace LearnSphere.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(string name, string email, string subject, string message)
    {
        // In a real application, you would save this to a database or send an email
        _logger.LogInformation("Contact form submitted: {Name}, {Email}, {Subject}", name, email, subject);

        // For now, just return a success response
        return Json(new { success = true, message = "Thank you for your message! We will get back to you soon." });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
