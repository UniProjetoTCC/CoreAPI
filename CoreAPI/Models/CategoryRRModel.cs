using Business.Models;
using System.ComponentModel.DataAnnotations;

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

    public class CategorySearchResponse
    {
        public List<CategoryDto> Items { get; set; } = new List<CategoryDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
        public bool FromCache { get; set; }
    }

    public class CachedCategorySearch
    {
        public List<CategoryBusinessModel> Categories { get; set; } = new List<CategoryBusinessModel>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class CategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}