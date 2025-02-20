using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("Products")]
    public class ProductModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

        [Required]
        [ForeignKey("Category")]
        public required int CategoryId { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        [Required]
        [StringLength(50)]
        public required string SKU { get; set; }

        [Required]
        [StringLength(50)]
        public required string BarCode { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal Price { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal Cost { get; set; }

        [Required]
        public required bool Active { get; set; } = true;

        [Required]
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Propriedades de navegação
        public virtual required ICollection<StockModel> Stock { get; set; } = new List<StockModel>();
        public virtual required ICollection<PriceHistoryModel> PriceHistories { get; set; } = new List<PriceHistoryModel>();
        public virtual required ICollection<ProductTaxModel> ProductTaxes { get; set; } = new List<ProductTaxModel>();
        public virtual required ICollection<SaleItemModel> SaleItems { get; set; } = new List<SaleItemModel>();
        public virtual required ICollection<SupplierPriceModel> SupplierPrices { get; set; } = new List<SupplierPriceModel>();
        public virtual required ICollection<ProductExpirationModel> ProductExpirations { get; set; } = new List<ProductExpirationModel>();
        public virtual required ICollection<PurchaseOrderItemModel> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItemModel>();

        public virtual required UserGroupModel UserGroup { get; set; }
        public virtual required CategoryModel Category { get; set; }
    }
}
