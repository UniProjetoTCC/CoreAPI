using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterLinkedUserModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanPerformTransactions { get; set; }
        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanGenerateReports { get; set; }
        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanManageProducts { get; set; }
        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanAlterStock { get; set; }
        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanManagePromotions { get; set; }
    }

    public class UpdateLinkedUserModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanPerformTransactions { get; set; }
        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanGenerateReports { get; set; }
        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanManageProducts { get; set; }
        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanAlterStock { get; set; }
        [Required(ErrorMessage = "You need set linked user permissions.")]
        public bool CanManagePromotions { get; set; }
    }

    public class EmailModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public required string Password { get; set; }
    }

    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ConfirmEmailModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;
    }

    public class RequestDeleteUserModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    public class DeleteUserModel
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;
    }

    public class DeleteLinkedUserModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    public class TokenModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public bool RequiresTwoFactor { get; set; }
        public string? Email { get; set; }
    }
}
