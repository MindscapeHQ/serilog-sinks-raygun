using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Serilog.Sinks.Raygun.SampleWebApp.Models;

namespace Serilog.Sinks.Raygun.SampleWebApp.Controllers;

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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    public IActionResult ThrowException()
    {
        throw new Exception("Serilog.Sinks.Raygun.SampleWebApp");
    }
    
    public IActionResult ThrowExceptionInAction()
    {
        try
        {
            throw new Exception("Serilog.Sinks.Raygun.SampleWebApp");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Captured an error");
        }

        return Content("HI");
    }
    
    public IActionResult ThrowWithUser()
    {
        // login user (faked)
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new(ClaimTypes.Name, "test"),
            new(ClaimTypes.Email, "hello-world@banana.com"),
            new(ClaimTypes.Role, "Admin")
        }, "Test"));
        
        try
        {
            throw new Exception("An exception with a user i hope....");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Captured an error with custom data {Thing}", "Banana");
        }

        return Content("HI");
    }
}