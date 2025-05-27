namespace Business.Models
{
    public class StockMovementBusinessModel
    {
        public string Id { get; set; } = null!;
        public string StockId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string GroupId { get; set; } = null!;
        public string MovementType { get; set; } = null!;
        public int Quantity { get; set; }
        public string? Reason { get; set; }
        public DateTime MovementDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public StockBusinessModel? Stock { get; set; }
    }
}
