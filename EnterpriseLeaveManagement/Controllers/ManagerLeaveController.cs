using EnterpriseLeaveManagement.Services.LeaveService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace EnterpriseLeaveManagement.Controllers;

[Authorize(Roles = "Manager")]
public class ManagerLeaveController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly UserManager<Data.ApplicationUser> _userManager;

    public ManagerLeaveController(ILeaveService leaveService, UserManager<Data.ApplicationUser> userManager)
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

            var pending = await _leaveService.GetPendingLeavesForManagerAsync(user.Id);
            return View(pending);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ManagerLeaveController.Index for user {User}", User?.Identity?.Name);
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? remarks)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            await _leaveService.ApproveLeaveAsync(id, user.Id, remarks);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ManagerLeaveController.Approve for user {User}, leaveId {LeaveId}", User?.Identity?.Name, id);
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? remarks)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            await _leaveService.RejectLeaveAsync(id, user.Id, remarks);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ManagerLeaveController.Reject for user {User}, leaveId {LeaveId}", User?.Identity?.Name, id);
            throw;
        }
    }
}



