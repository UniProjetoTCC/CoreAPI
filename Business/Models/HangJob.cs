namespace Business.Models
{
    public class HangJob
    {
        public int Id { get; set; }
        public required string HangfireJobId { get; set; }
        public required string JobType { get; set; }
        public int GroupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public required string Status { get; set; } // Scheduled, Executed, Cancelled
    }

    public static class JobTypes
    {
        public const string LinkedUsersDeactivation = "LinkedUsersDeactivation";
        // Adicione outros tipos de jobs aqui conforme necessário
    }
}
