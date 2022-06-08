using System.ComponentModel.DataAnnotations;

namespace LoyalWalletv2.Resources;

public class SaveCustomerResource
{
    [Required]
    [MaxLength(20)] 
    public string? PhoneNumber { get; set; }

    [Required]
    public int CompanyId { get; set; }
}