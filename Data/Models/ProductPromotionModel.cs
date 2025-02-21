using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("ProductPromotions")]
    public class ProductPromotionModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public required int ProductId { get; set; }

        [Required]
        [ForeignKey("Promotion")]
        public required int PromotionId { get; set; }

        // Propriedades de navegação
        public virtual ProductModel? Product { get; set; }
        public virtual PromotionModel? Promotion { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
    }
}
