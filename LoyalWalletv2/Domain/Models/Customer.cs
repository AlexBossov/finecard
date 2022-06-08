using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LoyalWalletv2.Domain.Models;

public class Customer
{
    private uint _countOfStamps;
    private uint _countOfPurchases;
    private uint _countOfStoredPresents;
    private uint _countOfGivenPresents;

    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public int SerialNumber { get; set; }

    [Required] 
    public int CompanyId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public Company? Company { get; set; }

    public bool Confirmed { get; set; }
    
    [BackingField(nameof(_countOfStamps))]
    public uint CountOfStamps => _countOfStamps;

    [BackingField(nameof(_countOfPurchases))]
    public uint CountOfPurchases => _countOfPurchases;

    [BackingField(nameof(_countOfStoredPresents))]
    public uint CountOfStoredPresents => _countOfStoredPresents;

    [BackingField(nameof(_countOfGivenPresents))]
    public uint CountOfGivenPresents => _countOfGivenPresents;
    public DateTime FirstTimePurchase { get; set; }
    public DateTime LastTimePurchase { get; set; }

    public void DoStamp(Employee employee)
    {
        if (CountOfStamps + 1 == Company.MaxCountOfStamps)
        {
            _countOfStamps = 0;
            _countOfStoredPresents++;
        }
        else
        {
            _countOfStamps++;
        }

        if (_countOfPurchases == 0)
            FirstTimePurchase = DateTime.Now;

        LastTimePurchase = DateTime.Now;
        _countOfPurchases++;

        employee.CountOfStamps += 1;
    }

    public void TakePresent(Employee employee)
    {
        _countOfStoredPresents--;
        _countOfGivenPresents++;
        employee.CountOfPresents += 1;
    }
}