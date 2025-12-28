namespace EnterpriseLeaveManagement.ViewModels.Leave;

public class LeaveBalanceViewModel
{
    public string LeaveType { get; set; } = default!;
    public int AllowanceDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays => AllowanceDays - UsedDays;
}


