using Microsoft.EntityFrameworkCore.ChangeTracking;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Common.Interfaces;

public interface IApplicationDbContext
{    
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    ChangeTracker ChangeTracker { get; }
}
