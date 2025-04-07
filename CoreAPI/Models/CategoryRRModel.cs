using System.ComponentModel.DataAnnotations;
using System;

namespace CoreAPI.Models
{
    public class CategoryCreateModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool Active { get; set; } = true;
    }

    public class CategoryUpdateModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool? Active { get; set; } = true;
    }
}