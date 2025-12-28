using EnterpriseLeaveManagement.Services.LeaveService;
using EnterpriseLeaveManagement.ViewModels.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace EnterpriseLeaveManagement.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class LeaveApiController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveApiController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

  
    [HttpGet]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<IReadOnlyList<LeaveSummaryViewModel>>> GetAll()
    {
        try
        {
            var leaves = await _leaveService.GetAllLeavesAsync();
            return Ok(leaves);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in LeaveApiController.GetAll for user {User}", User?.Identity?.Name);
            throw;
        }
    }
}



