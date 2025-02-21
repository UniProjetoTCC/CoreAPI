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
        [StringLength(50)] 
        public required string Name { get; set; } = string.Empty; 

        [Range(0, 100)] 
        [Column(TypeName = "decimal(5,2)")]
        public required decimal Rate { get; set; } 

        public virtual UserGroupModel? UserGroup { get; set; }
        public virtual ICollection<ProductTaxModel>? ProductTaxes { get; set; } = new List<ProductTaxModel>();
    } 
}
