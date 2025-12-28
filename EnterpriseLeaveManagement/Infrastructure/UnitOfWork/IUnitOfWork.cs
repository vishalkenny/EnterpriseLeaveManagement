using EnterpriseLeaveManagement.Models;

namespace EnterpriseLeaveManagement.Infrastructure.UnitOfWork;

using EnterpriseLeaveManagement.Infrastructure.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<LeaveRequest> LeaveRequests { get; }
    IRepository<LeaveStatusHistory> LeaveStatusHistories { get; }

    Task<int> SaveChangesAsync();
}



