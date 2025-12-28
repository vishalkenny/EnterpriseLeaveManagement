namespace EnterpriseLeaveManagement.ViewModels.Leave;

public class LeaveSummaryViewModel
{
    public int LeaveId { get; set; }
    public string EmployeeId { get; set; } = default!;
    public string? EmployeeName { get; set; }
    public string LeaveType { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
}



