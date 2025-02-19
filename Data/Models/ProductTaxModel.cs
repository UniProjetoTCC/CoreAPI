using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("ProductTaxes")]
    public class ProductTaxModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Product")]
        public required int ProductId { get; set; }

        [Required]
        [ForeignKey("Tax")]
        public required int TaxId { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual required ProductModel Product { get; set; }
        public virtual required TaxModel Tax { get; set; }
        public virtual required UserGroupModel UserGroup { get; set; }
    }
}
