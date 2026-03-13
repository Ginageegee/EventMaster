using System.ComponentModel.DataAnnotations;

namespace EventMaster.Models.ViewModels
{
    public class CompleteProfileViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";
    }
}