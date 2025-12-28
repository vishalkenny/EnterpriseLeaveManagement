using EnterpriseLeaveManagement.Infrastructure.UnitOfWork;
using EnterpriseLeaveManagement.Models;
using EnterpriseLeaveManagement.Services.Notifications;
using EnterpriseLeaveManagement.ViewModels.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EnterpriseLeaveManagement.Services.LeaveService;

public class LeaveService : ILeaveService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IMemoryCache _cache;
    private static readonly string ManagerPendingCacheKey = "Leave_PendingForManager";
    private static readonly string AllLeavesCacheKey = "Leave_AllForHr";

    private static string GetEmployeeLeavesCacheKey(string employeeId) => $"Leave_ForEmployee_{employeeId}";

    public LeaveService(IUnitOfWork unitOfWork, IEmailNotificationService emailNotificationService, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _emailNotificationService = emailNotificationService;
        _cache = cache;
    }

    public async Task<int> ApplyForLeaveAsync(string employeeId, LeaveApplyViewModel model)
    {
        if (model.EndDate < model.StartDate)
        {
            throw new InvalidOperationException("End date cannot be earlier than start date.");
        }

        var leave = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveType = model.LeaveType,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Reason = model.Reason,
            Status = LeaveStatus.Pending,
            CreatedOn = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.LeaveRequests.AddAsync(leave);

        var history = new LeaveStatusHistory
        {
            LeaveRequest = leave,
            LeaveId = leave.LeaveId,
            PreviousStatus = null,
            NewStatus = LeaveStatus.Pending,
            ChangedBy = employeeId,
            ChangedOn = DateTime.UtcNow,
            Remarks = "Leave applied"
        };

        await _unitOfWork.LeaveStatusHistories.AddAsync(history);

        await _unitOfWork.SaveChangesAsync();

        await _emailNotificationService.SendAsync(
            employeeId,
            "Leave request submitted",
            $"Leave {leave.LeaveId} from {model.StartDate:dd MMM yyyy} to {model.EndDate:dd MMM yyyy} submitted.");

        _cache.Remove(GetEmployeeLeavesCacheKey(employeeId));
        _cache.Remove(ManagerPendingCacheKey);
        _cache.Remove(AllLeavesCacheKey);

        return leave.LeaveId;
    }

    public async Task<bool> CancelLeaveAsync(int leaveId, string employeeId)
    {
        var leave = await _unitOfWork.LeaveRequests.GetByIdAsync(leaveId);
        if (leave == null)
        {
            return false;
        }

        if (leave.EmployeeId != employeeId || leave.Status != LeaveStatus.Pending)
        {
            return false;
        }

        leave.Status = LeaveStatus.Rejected;

        _unitOfWork.LeaveRequests.Update(leave);

        var history = new LeaveStatusHistory
        {
            LeaveId = leave.LeaveId,
            PreviousStatus = LeaveStatus.Pending,
            NewStatus = LeaveStatus.Rejected,
            ChangedBy = employeeId,
            ChangedOn = DateTime.UtcNow,
            Remarks = "Cancelled by employee"
        };

        await _unitOfWork.LeaveStatusHistories.AddAsync(history);

        await _unitOfWork.SaveChangesAsync();

        await _emailNotificationService.SendAsync(
            employeeId,
            "Leave request cancelled",
            $"Your leave request {leave.LeaveId} has been cancelled.");

        _cache.Remove(GetEmployeeLeavesCacheKey(employeeId));
        _cache.Remove(ManagerPendingCacheKey);
        _cache.Remove(AllLeavesCacheKey);

        return true;
    }

    public async Task<bool> ApproveLeaveAsync(int leaveId, string managerId, string? remarks)
    {
        var leave = await _unitOfWork.LeaveRequests.GetByIdAsync(leaveId);
        if (leave == null)
        {
            return false;
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            return false;
        }

        var previousStatus = leave.Status;
        leave.Status = LeaveStatus.Approved;
        _unitOfWork.LeaveRequests.Update(leave);

        var history = new LeaveStatusHistory
        {
            LeaveId = leave.LeaveId,
            PreviousStatus = previousStatus,
            NewStatus = LeaveStatus.Approved,
            ChangedBy = managerId,
            ChangedOn = DateTime.UtcNow,
            Remarks = remarks
        };

        await _unitOfWork.LeaveStatusHistories.AddAsync(history);
        await _unitOfWork.SaveChangesAsync();

        await _emailNotificationService.SendAsync(
            leave.EmployeeId,
            "Leave request approved",
            $"Your leave request {leave.LeaveId} has been approved.");

        _cache.Remove(GetEmployeeLeavesCacheKey(leave.EmployeeId));
        _cache.Remove(ManagerPendingCacheKey);
        _cache.Remove(AllLeavesCacheKey);

        return true;
    }

    public async Task<bool> RejectLeaveAsync(int leaveId, string managerId, string? remarks)
    {
        var leave = await _unitOfWork.LeaveRequests.GetByIdAsync(leaveId);
        if (leave == null)
        {
            return false;
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            return false;
        }

        var previousStatus = leave.Status;
        leave.Status = LeaveStatus.Rejected;
        _unitOfWork.LeaveRequests.Update(leave);

        var history = new LeaveStatusHistory
        {
            LeaveId = leave.LeaveId,
            PreviousStatus = previousStatus,
            NewStatus = LeaveStatus.Rejected,
            ChangedBy = managerId,
            ChangedOn = DateTime.UtcNow,
            Remarks = remarks
        };

        await _unitOfWork.LeaveStatusHistories.AddAsync(history);
        await _unitOfWork.SaveChangesAsync();

        await _emailNotificationService.SendAsync(
            leave.EmployeeId,
            "Leave request rejected",
            $"Your leave request {leave.LeaveId} has been rejected.");

        _cache.Remove(GetEmployeeLeavesCacheKey(leave.EmployeeId));
        _cache.Remove(ManagerPendingCacheKey);
        _cache.Remove(AllLeavesCacheKey);

        return true;
    }

    public async Task<bool> OverrideLeaveAsync(int leaveId, string hrId, string newStatus, string? remarks)
    {
        if (newStatus != LeaveStatus.Pending && newStatus != LeaveStatus.Approved && newStatus != LeaveStatus.Rejected)
        {
            throw new InvalidOperationException("Invalid status specified for override.");
        }

        var leave = await _unitOfWork.LeaveRequests.GetByIdAsync(leaveId);
        if (leave == null)
        {
            return false;
        }

        var previousStatus = leave.Status;
        if (previousStatus == newStatus)
        {
            return false;
        }

        leave.Status = newStatus;
        _unitOfWork.LeaveRequests.Update(leave);

        var history = new LeaveStatusHistory
        {
            LeaveId = leave.LeaveId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedBy = hrId,
            ChangedOn = DateTime.UtcNow,
            Remarks = remarks
        };

        await _unitOfWork.LeaveStatusHistories.AddAsync(history);
        await _unitOfWork.SaveChangesAsync();

        await _emailNotificationService.SendAsync(
            leave.EmployeeId,
            "Leave request status overridden",
            $"Your leave request {leave.LeaveId} status has been changed to {newStatus}.");

        _cache.Remove(GetEmployeeLeavesCacheKey(leave.EmployeeId));
        _cache.Remove(ManagerPendingCacheKey);
        _cache.Remove(AllLeavesCacheKey);

        return true;
    }

    public async Task<IReadOnlyList<LeaveSummaryViewModel>> GetLeavesForEmployeeAsync(string employeeId)
    {
        var cacheKey = GetEmployeeLeavesCacheKey(employeeId);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<LeaveSummaryViewModel>? cached) && cached is not null)
        {
            return cached;
        }

        var leaves = await _unitOfWork.LeaveRequests
            .FindAsync(l => l.EmployeeId == employeeId && !l.IsDeleted);

        var result = leaves
            .OrderByDescending(l => l.CreatedOn)
            .Select(l => new LeaveSummaryViewModel
            {
                LeaveId = l.LeaveId,
                EmployeeId = l.EmployeeId,
                LeaveType = l.LeaveType,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Status = l.Status,
                CreatedOn = l.CreatedOn
            })
            .ToList();

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(1)
        });

        return result;
    }

    public async Task<IReadOnlyList<LeaveSummaryViewModel>> GetPendingLeavesForManagerAsync(string managerId)
    {
        if (_cache.TryGetValue(ManagerPendingCacheKey, out IReadOnlyList<LeaveSummaryViewModel>? cached) && cached is not null)
        {
            return cached;
        }

        var leaves = await _unitOfWork.LeaveRequests
            .FindAsync(l => l.Status == LeaveStatus.Pending && !l.IsDeleted);

        var result = leaves
            .OrderBy(l => l.CreatedOn)
            .Select(l => new LeaveSummaryViewModel
            {
                LeaveId = l.LeaveId,
                EmployeeId = l.EmployeeId,
                LeaveType = l.LeaveType,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Status = l.Status,
                CreatedOn = l.CreatedOn
            })
            .ToList();

        _cache.Set(ManagerPendingCacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        });

        return result;
    }

    public async Task<IReadOnlyList<LeaveSummaryViewModel>> GetAllLeavesAsync()
    {
        if (_cache.TryGetValue(AllLeavesCacheKey, out IReadOnlyList<LeaveSummaryViewModel>? cached) && cached is not null)
        {
            return cached;
        }

        var leaves = await _unitOfWork.LeaveRequests
            .FindAsync(l => !l.IsDeleted);

        var result = leaves
            .OrderByDescending(l => l.CreatedOn)
            .Select(l => new LeaveSummaryViewModel
            {
                LeaveId = l.LeaveId,
                EmployeeId = l.EmployeeId,
                LeaveType = l.LeaveType,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Status = l.Status,
                CreatedOn = l.CreatedOn
            })
            .ToList();

        _cache.Set(AllLeavesCacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        });

        return result;
    }

    public async Task<EmployeeLeaveDashboardViewModel> GetEmployeeDashboardAsync(string employeeId)
    {
        var requests = await GetLeavesForEmployeeAsync(employeeId);

        var allowances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Annual"] = 20,
            ["Sick"] = 10,
            ["Casual"] = 7
        };

        var approved = requests.Where(r => r.Status == LeaveStatus.Approved);

        var usedByType = approved
            .GroupBy(r => r.LeaveType)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(r => (int)((r.EndDate.Date - r.StartDate.Date).TotalDays + 1)),
                StringComparer.OrdinalIgnoreCase);

        var balances = allowances.Select(kvp =>
        {
            var used = usedByType.TryGetValue(kvp.Key, out var days) ? days : 0;
            return new LeaveBalanceViewModel
            {
                LeaveType = kvp.Key,
                AllowanceDays = kvp.Value,
                UsedDays = used
            };
        }).ToList();

        return new EmployeeLeaveDashboardViewModel
        {
            Requests = requests,
            Balances = balances
        };
    }
}



