using System; 
using System.Collections.Generic; 
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace Data.Models
{
    [Table("Taxes")]
    public class TaxModel 
    { 
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        [Required]
        [ForeignKey("UserGroup")]
        public required int GroupId { get; set; }

        [Required]
        [StringLength(100)] 
        public required string Name { get; set; } = string.Empty; 

        [Range(0, 100)] 
        [Column(TypeName = "decimal(5,2)")]
        public required decimal Rate { get; set; } 

        public virtual required UserGroupModel UserGroup { get; set; }
        public virtual required ICollection<ProductTaxModel> ProductTaxes { get; set; } = new List<ProductTaxModel>();
    } 
}
