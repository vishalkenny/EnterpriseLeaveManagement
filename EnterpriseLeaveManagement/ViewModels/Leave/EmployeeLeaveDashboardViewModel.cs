namespace EnterpriseLeaveManagement.ViewModels.Leave;

public class EmployeeLeaveDashboardViewModel
{
    public IReadOnlyList<LeaveSummaryViewModel> Requests { get; set; } = Array.Empty<LeaveSummaryViewModel>();
    public IReadOnlyList<LeaveBalanceViewModel> Balances { get; set; } = Array.Empty<LeaveBalanceViewModel>();
}


