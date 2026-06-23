using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LandIt.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required]
        [DataType(DataType.Text)]
        [StringLength(50)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        [ProtectedPersonalData]
        [Display(Name = "Phone Number")]
        public virtual string? PhoneNumber { get; set; }

        [Required]
        public Region Region { get; set; }


        [Required]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Compare("NewPassword")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

        // for displaying current image
        public string? ExistingPhotoPath { get; set; }

        public IFormFile? Photo { get; set; }

    }
}
