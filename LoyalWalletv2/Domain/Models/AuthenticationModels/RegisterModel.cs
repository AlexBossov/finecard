using System.ComponentModel.DataAnnotations;

namespace LoyalWalletv2.Domain.Models.AuthenticationModels;

public class RegisterModel
{
    [EmailAddress]  
    [Required(ErrorMessage = "Email is required")]  
    public string? Email { get; set; }  

    [Required(ErrorMessage = "Password is required")]  
    public string? Password { get; set; }  
}