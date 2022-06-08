using System.ComponentModel.DataAnnotations;

namespace LoyalWalletv2.Domain.Models;

public class Location
{    
    [Key] public int Id { get; set; }
    [Required] [MaxLength(100)] public string? Address { get; set; }
    public string? Name { get; set; }
    [Required] public int CompanyId { get; set; }
    public List<Employee>? Employees { get; set; }
    public DateTime PayUpDate { get; set; }
    public bool Archived { get; set; }
}