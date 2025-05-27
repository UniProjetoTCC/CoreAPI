namespace Business.Models
{
    public class SubscriptionPlan
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = null!;
        public int LinkedUserLimit { get; set; }
        public bool IsActive { get; set; }
        public bool Requires2FA { get; set; }
        public bool RequiresEmailVerification { get; set; }
        public bool HasPremiumSupport { get; set; }
        public bool HasAdvancedAnalytics { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
