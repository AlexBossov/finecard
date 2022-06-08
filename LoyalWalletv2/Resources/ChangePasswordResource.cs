using System.ComponentModel.DataAnnotations;

namespace LoyalWalletv2.Resources;

public class ChangePasswordResource
{
    public int CompanyId { get; set; }
    [Required(ErrorMessage = "Old password is required")]
    public string? OldPassword { get; set; }
    [Required(ErrorMessage = "New password is required")]
    public string? NewPassword { get; set; }
}