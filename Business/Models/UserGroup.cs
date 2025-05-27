namespace Business.Models
{
    public class UserGroup
    {
        public string GroupId { get; set; } = string.Empty;
        public string UserId { get; set; } = null!;
        public string SubscriptionPlanId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
