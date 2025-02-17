using System.ComponentModel.DataAnnotations;

namespace Business.Models
{
    public class EmailMessage
    {
        [Required]
        [EmailAddress]
        public required string To { get; set; }

        [Required]
        public required string Subject { get; set; }

        [Required]
        public required string Body { get; set; }
    }
}
