using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EnterpriseLeaveManagement.Models;

namespace EnterpriseLeaveManagement.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return RedirectToAction("Login", "Account");
        }

        if (User.IsInRole("HR"))
        {
            return RedirectToAction("Index", "HrLeave");
        }

        if (User.IsInRole("Manager"))
        {
            return RedirectToAction("Index", "ManagerLeave");
        }

        if (User.IsInRole("Employee"))
        {
            return RedirectToAction("Index", "EmployeeLeave");
        }

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
}
