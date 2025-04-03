using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("BackgroundJobs")]
    public class BackgroundJobsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public required string HangfireJobId { get; set; }

        [Required]
        [StringLength(50)]
        public required string JobType { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; }

        public DateTime? ExecutedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        [Required]
        [StringLength(20)]
        public required string Status { get; set; } // Scheduled, Executed, Cancelled

        [ForeignKey("GroupId")]
        public virtual UserGroupModel? UserGroup { get; set; }
    }
}
