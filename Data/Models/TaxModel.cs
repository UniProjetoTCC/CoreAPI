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
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(36)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [ForeignKey("UserGroup")]
        [StringLength(36)]
        public required string GroupId { get; set; }

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
