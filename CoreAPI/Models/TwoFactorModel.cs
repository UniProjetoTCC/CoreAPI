using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class Enable2faModel
    {
        [Required]
        [StringLength(6)]
        public string Code { get; set; } = string.Empty;
    }

    public class Verify2faModel
    {
        [Required]
        [StringLength(6)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;
    }

    public class TwoFactorResponse
    {
        public string QrCodeUrl { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;
    }
}
