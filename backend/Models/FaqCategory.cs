namespace TraceAI.Api.Models;

public class FaqCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public ICollection<FaqItem> Items { get; set; } = [];
}
