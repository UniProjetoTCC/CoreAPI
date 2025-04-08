using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("ProductTaxes")]
    public class ProductTaxModel
    {
        [Key]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("Product")]
        [StringLength(36)]
        public required string ProductId { get; set; }

        [Required]
        [ForeignKey("Tax")]
        [StringLength(36)]
        public required string TaxId { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ProductModel? Product { get; set; }
        public virtual TaxModel? Tax { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
    }
}
