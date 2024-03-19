using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Data;
public class ShadowDbContext : DbContext
{
    public ShadowDbContext(DbContextOptions<ShadowDbContext> options) : base(options) { }

    public DbSet<TodoList> TodoLists => Set<TodoList>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TodoList>()
               .Property(t => t.Title)
               .HasMaxLength(200)
               .IsRequired();

        builder.Entity<TodoList>()
               .OwnsOne(b => b.Colour);

        builder.Entity<TodoItem>()
               .Property(t => t.Title)
               .HasMaxLength(200)
               .IsRequired();
    }
}
