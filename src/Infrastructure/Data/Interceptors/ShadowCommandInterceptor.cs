using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using TodoApi.Application.Common.Constants;
using TodoApi.Domain.Entities;
using Unleash;

namespace TodoApi.Infrastructure.Data.Interceptors;

public class ShadowCommandInterceptor : SaveChangesInterceptor
{
    private readonly ILogger _logger;
    private readonly IUnleash _unleash;
    private readonly ShadowDbContext _shadowDbContext;

    public ShadowCommandInterceptor(
        IUnleash unleash,
        ShadowDbContext shadowDbContext,
        ILogger<ShadowDbContext> logger)
    {
        _logger = logger;
        _unleash = unleash;
        _shadowDbContext = shadowDbContext;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (_unleash.IsEnabled(UnleashConstants.UseShadowOperations))
        {
            AttachEntitiesToShadowTracker(eventData.Context);
        }        

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void AttachEntitiesToShadowTracker(DbContext? context)
    {
        if (context == null) return;

        AttachEntities<TodoList>(context);
        AttachEntities<TodoItem>(context);
    }

    private void AttachEntities<T>(DbContext context) where T : class
    {
        try
        {
            var entires = context.ChangeTracker.Entries<T>().ToList();

            var set = _shadowDbContext.Set<T>();

            foreach (var entry in entires)
            {
                set.Attach(entry.Entity);
                _shadowDbContext.Entry(entry.Entity).State = entry.State;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public async override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _shadowDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return result;
    }
}
