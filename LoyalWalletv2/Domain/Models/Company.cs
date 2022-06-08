using System.ComponentModel.DataAnnotations;
using LoyalWalletv2.Domain.Models.AuthenticationModels;

namespace LoyalWalletv2.Domain.Models;

public class Company
{
    [Key] public int Id { get; set; }
    [MaxLength(100)] public string? Name { get; set; }
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    public IList<Location> Locations { get; set; } = new List<Location>();
    public IList<Employee> Employees { get; set; } = new List<Employee>();
    public IList<Customer> Customers { get; set; } = new List<Customer>();
    public uint MaxCountOfStamps { get; set; } = 6;
    public ApplicationUser? Account { get; set; }
    public string? InstagramName { get; set; }
}