using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace LoyalWalletv2.Domain.Models.AuthenticationModels;

public class ApplicationUser : IdentityUser
{
    public int CompanyId { get; set; }
    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }
}