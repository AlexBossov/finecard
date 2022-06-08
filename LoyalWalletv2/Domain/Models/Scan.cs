using System.ComponentModel.DataAnnotations;

namespace LoyalWalletv2.Domain.Models;

public class Scan
{
    [Key] public int Id { get; set; }
    public DateTime ScanDate { get; set; }
    public int EmployeeId { get; set; }
    public int CustomerId { get; set; }
    public int CompanyId { get; set; }
    
}