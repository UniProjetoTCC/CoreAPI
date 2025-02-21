using System;

namespace Business.Models
{
    public class LinkedUser
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string CreatedByUserId { get; set; } = null!;
        public bool CanPerformTransactions { get; set; }
        public bool CanGenerateReports { get; set; }
        public bool CanManageProducts { get; set; }
        public bool CanAlterStock { get; set; }
        public bool CanManagePromotions { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
