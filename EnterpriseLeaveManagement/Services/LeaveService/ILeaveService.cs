using EnterpriseLeaveManagement.Models;
using EnterpriseLeaveManagement.ViewModels.Leave;

namespace EnterpriseLeaveManagement.Services.LeaveService;

public interface ILeaveService
{
    Task<int> ApplyForLeaveAsync(string employeeId, LeaveApplyViewModel model);
    Task<bool> CancelLeaveAsync(int leaveId, string employeeId);

    Task<bool> ApproveLeaveAsync(int leaveId, string managerId, string? remarks);
    Task<bool> RejectLeaveAsync(int leaveId, string managerId, string? remarks);
    Task<bool> OverrideLeaveAsync(int leaveId, string hrId, string newStatus, string? remarks);

    Task<IReadOnlyList<LeaveSummaryViewModel>> GetLeavesForEmployeeAsync(string employeeId);
    Task<IReadOnlyList<LeaveSummaryViewModel>> GetPendingLeavesForManagerAsync(string managerId);
    Task<IReadOnlyList<LeaveSummaryViewModel>> GetAllLeavesAsync();

    Task<EmployeeLeaveDashboardViewModel> GetEmployeeDashboardAsync(string employeeId);
}



