namespace TraceAI.Api.Models;

public class SeoMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PublicPageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OgImageUrl { get; set; } = string.Empty;

    public PublicPage PublicPage { get; set; } = null!;
}
