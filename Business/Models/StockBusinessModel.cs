using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Models
{
    public class StockBusinessModel
    {
        public string Id { get; set; } = null!;
        public string ProductId { get; set; } = null!;
        public string GroupId { get; set; } = null!;
        public int Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ProductBusinessModel? Product { get; set; }
    }
}
