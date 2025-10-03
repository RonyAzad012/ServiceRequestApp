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
        public string? Street { get; set; }
        public string? City { get; set; }

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

    public class TaskerRegisterViewModel
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
        public string? Street { get; set; }
        public string? City { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Zipcode { get; set; }

        [Required]
        public string NationalId { get; set; }

        // Tasker-specific fields
        [Required]
        public string Skills { get; set; } // Comma-separated skills

        public string? PortfolioUrl { get; set; }

        public string? ProfileDescription { get; set; }

        public int? PrimaryCategoryId { get; set; }

        public string? ServiceAreas { get; set; }
    }

    public class EditRequesterProfileViewModel
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string? Address { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Zipcode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class EditProviderProfileViewModel
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string? Address { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Zipcode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ShopName { get; set; }
        public string? ShopDescription { get; set; }
        public string? ShopAddress { get; set; }
        public string? ShopPhone { get; set; }
        public string? BusinessCredentials { get; set; }
        public string? BusinessImagePath { get; set; }
        public string? ServiceTypes { get; set; }
        public string? WorkingHours { get; set; }
        public string? GalleryImages { get; set; }
        public decimal? StartingPrice { get; set; }
        public string? Experience { get; set; }
        public string? Certifications { get; set; }
    }

    public class EditTaskerProfileViewModel
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string? Address { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Zipcode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Skills { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? ProfileDescription { get; set; }
    }

    public class RequesterRegisterViewModel
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

        [Required]
        public string Address { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Zipcode { get; set; }

        [Required]
        public string NationalId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class ProviderRegisterViewModel
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

        [Required]
        public string Address { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string Zipcode { get; set; }

        [Required]
        public string NationalId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Provider-specific fields
        [Required]
        public string ShopName { get; set; }

        public string? ShopDescription { get; set; }

        [Required]
        public string ShopAddress { get; set; }

        [Required]
        public string ShopPhone { get; set; }

        public string? BusinessCredentials { get; set; }

        public string? BusinessImagePath { get; set; }

        public int? PrimaryCategoryId { get; set; }

        public string? ServiceAreas { get; set; }

        [Url]
        public string? BusinessWebsite { get; set; }

        public IFormFileCollection? BusinessDocuments { get; set; }
    }
}