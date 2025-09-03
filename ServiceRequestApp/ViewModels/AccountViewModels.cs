using System.ComponentModel.DataAnnotations;

namespace ServiceRequestApp.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords don't match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string? Address { get; set; }

        [Required]
        public string UserType { get; set; } // "Provider" or "Requester"

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Zipcode { get; set; }

        [Required]
        public string NationalId { get; set; }

        // Only for providers
        public string? BusinessCredentials { get; set; }
        public string? BusinessImagePath { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}