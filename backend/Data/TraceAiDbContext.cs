using Microsoft.EntityFrameworkCore;
using TraceAI.Api.Models;

namespace TraceAI.Api.Data;

public class TraceAiDbContext(DbContextOptions<TraceAiDbContext> options) : DbContext(options)
{
    public DbSet<TaskRun> Tasks => Set<TaskRun>();
    public DbSet<TaskStep> Steps => Set<TaskStep>();
    public DbSet<StepLog> StepLogs => Set<StepLog>();
    public DbSet<PublicPage> PublicPages => Set<PublicPage>();
    public DbSet<SeoMetadata> SeoMetadataEntries => Set<SeoMetadata>();
    public DbSet<FaqCategory> FaqCategories => Set<FaqCategory>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();

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

        modelBuilder.Entity<PublicPage>(entity =>
        {
            entity.ToTable("PublicPages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Slug).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Title).IsRequired();
            entity.Property(x => x.Body).IsRequired();
            entity.HasOne(x => x.SeoMetadata)
                .WithOne(x => x.PublicPage)
                .HasForeignKey<SeoMetadata>(x => x.PublicPageId);
        });

        modelBuilder.Entity<SeoMetadata>(entity =>
        {
            entity.ToTable("SeoMetadata");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.OgImageUrl).IsRequired();
        });

        modelBuilder.Entity<FaqCategory>(entity =>
        {
            entity.ToTable("FaqCategories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<FaqItem>(entity =>
        {
            entity.ToTable("FaqItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Question).IsRequired();
            entity.Property(x => x.Answer).IsRequired();
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.CategoryId);
        });

        modelBuilder.Entity<ContactSubmission>(entity =>
        {
            entity.ToTable("ContactSubmissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.Message).IsRequired();
        });

        SeedPublicContent(modelBuilder);
    }

    private static void SeedPublicContent(ModelBuilder modelBuilder)
    {
        var homeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var howItWorksId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var faqId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var contactId = Guid.Parse("00000000-0000-0000-0000-000000000004");

        modelBuilder.Entity<PublicPage>().HasData(
            new PublicPage { Id = homeId, Slug = string.Empty, Title = "Home", Body = "TraceAI helps teams execute AI-assisted workflows with confidence.", DisplayOrder = 1 },
            new PublicPage { Id = howItWorksId, Slug = "how-it-works", Title = "How It Works", Body = "Design prompts, run guided stages, and inspect every step in real time.", DisplayOrder = 2 },
            new PublicPage { Id = faqId, Slug = "faq", Title = "FAQ", Body = "Browse answers by category or search across all common questions.", DisplayOrder = 3 },
            new PublicPage { Id = contactId, Slug = "contact", Title = "Contact", Body = "Need help? Reach out and our support team will follow up.", DisplayOrder = 4 }
        );

        modelBuilder.Entity<SeoMetadata>().HasData(
            new SeoMetadata { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), PublicPageId = homeId, Title = "TraceAI | Home", Description = "AI workflow orchestration and trace-first execution.", OgImageUrl = "/og-home.png" },
            new SeoMetadata { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), PublicPageId = howItWorksId, Title = "TraceAI | How It Works", Description = "Understand TraceAI's step-by-step execution engine.", OgImageUrl = "/og-how-it-works.png" },
            new SeoMetadata { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), PublicPageId = faqId, Title = "TraceAI | FAQ", Description = "Answers to the most common TraceAI questions.", OgImageUrl = "/og-faq.png" },
            new SeoMetadata { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), PublicPageId = contactId, Title = "TraceAI | Contact", Description = "Contact TraceAI support.", OgImageUrl = "/og-contact.png" }
        );

        var gettingStartedId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var securityId = Guid.Parse("20000000-0000-0000-0000-000000000002");

        modelBuilder.Entity<FaqCategory>().HasData(
            new FaqCategory { Id = gettingStartedId, Name = "Getting Started" },
            new FaqCategory { Id = securityId, Name = "Security" }
        );

        modelBuilder.Entity<FaqItem>().HasData(
            new FaqItem { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), CategoryId = gettingStartedId, Question = "How quickly can I run my first workflow?", Answer = "Most teams are running their first TraceAI workflow in under 10 minutes.", DisplayOrder = 1 },
            new FaqItem { Id = Guid.Parse("30000000-0000-0000-0000-000000000002"), CategoryId = gettingStartedId, Question = "Can I retry only failed steps?", Answer = "Yes, TraceAI supports targeted step retries without rerunning successful stages.", DisplayOrder = 2 },
            new FaqItem { Id = Guid.Parse("30000000-0000-0000-0000-000000000003"), CategoryId = securityId, Question = "How is execution data stored?", Answer = "Execution metadata is stored in your configured database with full traceability.", DisplayOrder = 1 }
        );
    }
}
