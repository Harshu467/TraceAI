namespace TraceAI.Api.Models;

public class FaqItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public FaqCategory Category { get; set; } = null!;
}
