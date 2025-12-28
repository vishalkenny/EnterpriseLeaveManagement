using EnterpriseLeaveManagement.Services.LeaveService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace EnterpriseLeaveManagement.Controllers;

[Authorize(Roles = "HR")]
public class HrLeaveController : Controller
{
    private readonly ILeaveService _leaveService;

    public HrLeaveController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var all = await _leaveService.GetAllLeavesAsync();
            return View(all);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in HrLeaveController.Index for user {User}", User?.Identity?.Name);
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Override(int id, string newStatus, string? remarks)
    {
        try
        {
            await _leaveService.OverrideLeaveAsync(id, User.Identity?.Name ?? "HR", newStatus, remarks);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in HrLeaveController.Override for user {User}, leaveId {LeaveId}", User?.Identity?.Name, id);
            throw;
        }
    }
}



