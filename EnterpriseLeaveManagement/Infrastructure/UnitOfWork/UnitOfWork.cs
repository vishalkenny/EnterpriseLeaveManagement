using EnterpriseLeaveManagement.Data;
using EnterpriseLeaveManagement.Infrastructure.Repositories;
using EnterpriseLeaveManagement.Models;

namespace EnterpriseLeaveManagement.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        LeaveRequests = new Repository<LeaveRequest>(_context);
        LeaveStatusHistories = new Repository<LeaveStatusHistory>(_context);
    }

    public IRepository<LeaveRequest> LeaveRequests { get; }
    public IRepository<LeaveStatusHistory> LeaveStatusHistories { get; }

    public Task<int> SaveChangesAsync()
        => _context.SaveChangesAsync();

    public ValueTask DisposeAsync()
        => _context.DisposeAsync();
}



