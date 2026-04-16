using System;

namespace TraceAI.Data.Entities
{
    public sealed class StepLogEntity
    {
        public int Id { get; set; }

        public int StepId { get; set; }

        public StepEntity Step { get; set; } = null!;

        public string Message { get; set; } = null!;

        public DateTime Timestamp { get; set; }
    }
}
