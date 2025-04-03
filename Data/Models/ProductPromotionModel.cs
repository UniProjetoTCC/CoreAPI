using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("ProductPromotions")]
    public class ProductPromotionModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        [ForeignKey("Product")]
        [StringLength(36)]
        public required string ProductId { get; set; }

        [Required]
        [ForeignKey("Promotion")]
        [StringLength(36)]
        public required string PromotionId { get; set; }

        // Propriedades de navegação
        public virtual ProductModel? Product { get; set; }
        public virtual PromotionModel? Promotion { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
    }
}
