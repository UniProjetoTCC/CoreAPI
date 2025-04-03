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
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

        [Required]
        [ForeignKey("Category")]
        [StringLength(36)]
        public required string CategoryId { get; set; }

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
        public virtual CategoryModel? Category { get; set; }
        public virtual UserGroupModel? UserGroup { get; set; }
        public virtual ICollection<StockModel>? Stock { get; set; } = new List<StockModel>();
        public virtual ICollection<PriceHistoryModel>? PriceHistories { get; set; } = new List<PriceHistoryModel>();
        public virtual ICollection<ProductTaxModel>? ProductTaxes { get; set; } = new List<ProductTaxModel>();
        public virtual ICollection<SaleItemModel>? SaleItems { get; set; } = new List<SaleItemModel>();
        public virtual ICollection<SupplierPriceModel>? SupplierPrices { get; set; } = new List<SupplierPriceModel>();
        public virtual ICollection<ProductExpirationModel>? ProductExpirations { get; set; } = new List<ProductExpirationModel>();
        public virtual ICollection<PurchaseOrderItemModel>? PurchaseOrderItems { get; set; } = new List<PurchaseOrderItemModel>();
        public virtual ICollection<ProductPromotionModel>? ProductPromotions { get; set; } = new List<ProductPromotionModel>();
    }
}
