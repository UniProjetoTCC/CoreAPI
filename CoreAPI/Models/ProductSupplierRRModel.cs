using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    /// <summary>
    /// Modelo para associar um fornecedor a um produto, criando uma entrada de SupplierPrice.
    /// </summary>
    public class AddSupplierToProductRequest
    {
        [Required]
        [StringLength(36)]
        public required string SupplierId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "The unit price must be greater than 0.")]
        public required decimal UnitPrice { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "The minimum quantity must be at least 1.")]
        public required int MinimumQuantity { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "The supplier's SKU must have a maximum of 50 characters.")]
        public required string SupplierSku { get; set; }

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
    }
}