namespace Business.Models
{
    public class CustomerBusinessModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GroupId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? LoyaltyProgramId { get; set; }
        public int LoyaltyPoints { get; set; } = 0;
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navegação
        public LoyaltyProgramBusinessModel? LoyaltyProgram { get; set; }
    }
}
