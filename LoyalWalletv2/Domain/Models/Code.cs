using System.ComponentModel.DataAnnotations;

namespace LoyalWalletv2.Domain.Models;

public class Code
{
    [Key]
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ConfirmationCode { get; set; }
}