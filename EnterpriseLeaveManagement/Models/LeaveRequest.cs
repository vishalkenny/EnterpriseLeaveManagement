namespace EnterpriseLeaveManagement.Models;

public class LeaveRequest
{
    public int LeaveId { get; set; }

    public string EmployeeId { get; set; } = default!;

    public string LeaveType { get; set; } = default!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public ICollection<LeaveStatusHistory> StatusHistory { get; set; } = new List<LeaveStatusHistory>();
}



