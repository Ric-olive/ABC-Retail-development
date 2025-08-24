using System.ComponentModel.DataAnnotations;

namespace ABC_Retail.Models.ViewModels
{
    public class LoginAdminViewModel
    {
        [Required(ErrorMessage = "Admin email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Admin Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Admin Password")]
        public string Password { get; set; }

    }
}
