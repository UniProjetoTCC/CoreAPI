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
        [ForeignKey("Product")]
        public required int ProductId { get; set; }

        [Required]
        [ForeignKey("Promotion")]
        public required int PromotionId { get; set; }

        // Propriedades de navegação
        public virtual required ProductModel Product { get; set; }
        public virtual required PromotionModel Promotion { get; set; }
    }
}
