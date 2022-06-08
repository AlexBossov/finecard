using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoyalWalletv2.Domain.Models;

public class Employee
{
    [Key] public int Id { get; set; }
    [Required] [MaxLength(100)] public string? Name { get; set; }
    [Required] [MaxLength(100)] public string? Surname { get; set; }
    [Required] public int CompanyId { get; set; }
    public bool Archived { get; set; }
    public string? Email { get; set; }
    public string? Position { get; set; }
    public Location? Location { get; set; }

    [ForeignKey(nameof(Location))]
    [Required] public int LocationId { get; set; }

    public uint CountOfStamps { get; set; }
    public uint CountOfPresents { get; set; }
}