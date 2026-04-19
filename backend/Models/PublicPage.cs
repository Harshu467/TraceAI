namespace TraceAI.Api.Models;

public class PublicPage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public SeoMetadata SeoMetadata { get; set; } = null!;
}
