using Microsoft.EntityFrameworkCore;
using TraceAI.Data.Entities;

namespace TraceAI.Data
{
    public sealed class TraceAIDbContext : DbContext
    {
        public TraceAIDbContext(DbContextOptions<TraceAIDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
        public DbSet<StepEntity> Steps => Set<StepEntity>();
        public DbSet<StepLogEntity> StepLogs => Set<StepLogEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=traceai.db");
            }

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskEntity>(entity =>
            {
                entity.ToTable("Tasks");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(x => x.Description)
                    .HasMaxLength(1000);
                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .IsRequired();

                entity.HasMany(x => x.Steps)
                    .WithOne(x => x.Task)
                    .HasForeignKey(x => x.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StepEntity>(entity =>
            {
                entity.ToTable("Steps");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(x => x.Status)
                    .HasMaxLength(100);
                entity.Property(x => x.Sequence)
                    .IsRequired();

                entity.HasMany(x => x.Logs)
                    .WithOne(x => x.Step)
                    .HasForeignKey(x => x.StepId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StepLogEntity>(entity =>
            {
                entity.ToTable("StepLogs");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Message)
                    .IsRequired()
                    .HasMaxLength(2000);
                entity.Property(x => x.Timestamp)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
