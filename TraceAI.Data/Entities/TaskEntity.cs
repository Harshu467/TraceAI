using System;
using System.Collections.Generic;

namespace TraceAI.Data.Entities
{
    public sealed class TaskEntity
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public IList<StepEntity> Steps { get; set; } = new List<StepEntity>();
    }
}
