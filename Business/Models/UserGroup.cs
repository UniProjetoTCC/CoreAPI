using System;

namespace Business.Models
{
    public class UserGroup
    {
        public int GroupId { get; set; }
        public string UserId { get; set; } = null!;
        public int SubscriptionPlanId { get; set; }
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
