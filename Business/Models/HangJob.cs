namespace Business.Models
{
    public class HangJob
    {
        public string Id { get; set; } = string.Empty;
        public required string HangfireJobId { get; set; }
        public required string JobType { get; set; }
        public string GroupId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public required string Status { get; set; } // Scheduled, Executed, Cancelled
    }

    public static class JobTypes
    {
        public const string LinkedUsersDeactivation = "LinkedUsersDeactivation";
        public const string LinkedUsersActivation = "LinkedUsersActivation";
        // Adicione outros tipos de jobs aqui conforme necessário
    }
}
