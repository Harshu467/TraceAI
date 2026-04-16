using Microsoft.EntityFrameworkCore;
using TraceAI.Api.Models;

namespace TraceAI.Api.Data;

public class TraceAiDbContext(DbContextOptions<TraceAiDbContext> options) : DbContext(options)
{
    public DbSet<TaskRun> Tasks => Set<TaskRun>();
    public DbSet<TaskStep> Steps => Set<TaskStep>();
    public DbSet<StepLog> StepLogs => Set<StepLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskRun>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Prompt).IsRequired();
            entity.HasMany(x => x.Steps)
                  .WithOne(x => x.TaskRun)
                  .HasForeignKey(x => x.TaskRunId);
        });

        modelBuilder.Entity<TaskStep>(entity =>
        {
            entity.ToTable("Steps");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StepName).IsRequired();
            entity.Property(x => x.Input).IsRequired();
            entity.Property(x => x.Output).IsRequired();
            entity.Property(x => x.PromptUsed).IsRequired();
            entity.Property(x => x.RawAiResponse).IsRequired();
            entity.HasMany(x => x.Logs)
                  .WithOne(x => x.TaskStep)
                  .HasForeignKey(x => x.TaskStepId);
        });

        modelBuilder.Entity<StepLog>(entity =>
        {
            entity.ToTable("StepLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Message).IsRequired();
        });
    }
}
