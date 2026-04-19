using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TraceAI.Api.Data;
using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicContentController(TraceAiDbContext dbContext, IContactNotificationService notificationService, IConfiguration configuration)
    : ControllerBase
{
    [HttpGet("pages")]
    public async Task<IActionResult> GetPages(CancellationToken cancellationToken)
    {
        var pages = await dbContext.PublicPages
            .Include(x => x.SeoMetadata)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new
            {
                x.Slug,
                x.Title,
                x.Body,
                Seo = new
                {
                    x.SeoMetadata.Title,
                    x.SeoMetadata.Description,
                    x.SeoMetadata.OgImageUrl
                }
            })
            .ToListAsync(cancellationToken);

        return Ok(pages);
    }

    [HttpGet("pages/{slug}")]
    public async Task<IActionResult> GetPageBySlug(string slug, CancellationToken cancellationToken)
    {
        var page = await dbContext.PublicPages
            .Include(x => x.SeoMetadata)
            .Where(x => x.Slug == slug)
            .Select(x => new
            {
                x.Slug,
                x.Title,
                x.Body,
                Seo = new
                {
                    x.SeoMetadata.Title,
                    x.SeoMetadata.Description,
                    x.SeoMetadata.OgImageUrl
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        return page is null ? NotFound() : Ok(page);
    }

    [HttpGet("faq/categories")]
    public async Task<IActionResult> GetFaqCategories(CancellationToken cancellationToken)
    {
        var categories = await dbContext.FaqCategories
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("faq")]
    public async Task<IActionResult> GetFaqItems([FromQuery] Guid? categoryId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.FaqItems
            .Include(x => x.Category)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Question.Contains(search) || x.Answer.Contains(search));
        }

        var results = await query
            .OrderBy(x => x.Category.Name)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => new
            {
                x.Id,
                x.Question,
                x.Answer,
                Category = new { x.CategoryId, x.Category.Name }
            })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

    [HttpPost("contact")]
    public async Task<IActionResult> SubmitContact([FromBody] ContactRequest request, CancellationToken cancellationToken)
    {
        var submission = new ContactSubmission
        {
            Name = request.Name,
            Email = request.Email,
            Message = request.Message,
            SubmittedAtUtc = DateTime.UtcNow
        };

        dbContext.ContactSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);
        await notificationService.SendSupportNotificationAsync(submission, cancellationToken);

        return Accepted(new { submission.Id, submission.SubmittedAtUtc });
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> GetSiteMap(CancellationToken cancellationToken)
    {
        var baseUrl = configuration["PublicSite:BaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host.Value}";
        var pages = await dbContext.PublicPages
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.Slug)
            .ToListAsync(cancellationToken);

        var urls = pages
            .Select(slug => $"{baseUrl}/{slug}")
            .Append($"{baseUrl}/faq")
            .Append($"{baseUrl}/contact")
            .Distinct()
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var url in urls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(url)}</loc>");
            sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}
