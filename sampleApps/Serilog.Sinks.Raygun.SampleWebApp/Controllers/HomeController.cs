using System.Diagnostics;
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
}