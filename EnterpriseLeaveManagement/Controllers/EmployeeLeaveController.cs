using EnterpriseLeaveManagement.Services.LeaveService;
using EnterpriseLeaveManagement.ViewModels.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace EnterpriseLeaveManagement.Controllers;

[Authorize(Roles = "Employee")]
public class EmployeeLeaveController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly UserManager<Data.ApplicationUser> _userManager;

    public EmployeeLeaveController(ILeaveService leaveService, UserManager<Data.ApplicationUser> userManager)
    {
        _leaveService = leaveService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var dashboard = await _leaveService.GetEmployeeDashboardAsync(user.Id);
            return View(dashboard);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in EmployeeLeaveController.Index for user {User}", User?.Identity?.Name);
            throw;
        }
    }

    public IActionResult Apply()
    {
        return View(new LeaveApplyViewModel
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(LeaveApplyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        try
        {
            await _leaveService.ApplyForLeaveAsync(user.Id, model);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex, "Business validation failed in EmployeeLeaveController.Apply for user {User}", User?.Identity?.Name);
            ModelState.AddModelError(nameof(model.EndDate), ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in EmployeeLeaveController.Apply for user {User}", User?.Identity?.Name);
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            await _leaveService.CancelLeaveAsync(id, user.Id);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in EmployeeLeaveController.Cancel for user {User}, leaveId {LeaveId}", User?.Identity?.Name, id);
            throw;
        }
    }
}



