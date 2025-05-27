namespace Business.Models
{
    public class LinkedUser
    {
        public string Id { get; set; } = string.Empty;
        public string LinkedUserId { get; set; } = null!;
        public string ParentUserId { get; set; } = null!;
        public string GroupId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool CanPerformTransactions { get; set; }
        public bool CanGenerateReports { get; set; }
        public bool CanManageProducts { get; set; }
        public bool CanAlterStock { get; set; }
        public bool CanManagePromotions { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
