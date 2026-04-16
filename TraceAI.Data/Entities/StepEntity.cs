using System.Collections.Generic;

namespace TraceAI.Data.Entities
{
    public sealed class StepEntity
    {
        public int Id { get; set; }

        public int TaskId { get; set; }

        public TaskEntity Task { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Status { get; set; }

        public int Sequence { get; set; }

        public IList<StepLogEntity> Logs { get; set; } = new List<StepLogEntity>();
    }
}
