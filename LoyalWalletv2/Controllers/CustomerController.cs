using System.Diagnostics;
using AutoMapper;
using LoyalWalletv2.Contexts;
using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Domain.Models.AuthenticationModels;
using LoyalWalletv2.Resources;
using LoyalWalletv2.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoyalWalletv2.Controllers;

[Route("/api/[controller]/{companyId:int}")]
[Authorize(Roles = nameof(EUserRoles.User))]
public class CustomerController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public CustomerController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IEnumerable<CustomerResource>> ListAsync(int companyId)
    {
        var query = await CustomerList(companyId);
        var queryResource = _mapper
            .Map<IEnumerable<Customer>, IEnumerable<CustomerResource>>(query);
        return queryResource;
    }

    [HttpGet("phone-number={phoneNumber}")]
    public async Task<CustomerResource> GetByPhoneNumber(string phoneNumber, int companyId)
    {
        var queryList = await CustomerList(companyId);
        var model = queryList.FirstOrDefault(c => c.PhoneNumber == phoneNumber) ??
                    throw new LoyalWalletException("Customer not found");

        var resultResource = _mapper.Map<Customer, CustomerResource>(model);
        return resultResource;
    }

    [HttpGet("all-cards-count")]
    public async Task<int> AllCardsCount(int companyId, string? locationName, DateTime? startDate, DateTime? endDate)
    {
        var query = await ScansList(companyId, locationName, startDate, endDate);
        return query.Count;
    }

    [HttpGet("all-stamps-count")]
    public async Task<long> AllStampsCount(int companyId, string? locationName, DateTime? startDate, DateTime? endDate)
    {
        var scans = await ScansList(companyId, locationName, startDate, endDate);
        Debug.Assert(_context.Customers != null, "_context.Customers != null");
        var query = _context.Customers.ToList()
            .Where(c => scans.Any(s => s.CustomerId == c.Id)).ToList();
        return query.Sum(q => q.CountOfStamps);
    }

    [HttpGet("all-presents-count")]
    public async Task<long> AllPresentsCount(int companyId, string? locationName, DateTime? startDate, DateTime? endDate)
    {
        var scans = await ScansList(companyId, locationName, startDate, endDate);
        Debug.Assert(_context.Customers != null, "_context.Customers != null");
        var query = _context.Customers.ToList()
            .Where(c => scans.Any(s => s.CustomerId == c.Id)).ToList();

        return query.Sum(q => q.CountOfGivenPresents);
    }

    [HttpPut("take-present/{id:int}/{employeeId:int}")]
    public async Task<CustomerResource> TakeAsync(int id, int companyId, int employeeId)
    {
        Debug.Assert(_context.Customers != null, "_context.Customers != null");
        var customer = await _context.Customers.FindAsync(id) ??
                    throw new LoyalWalletException("Customer not found");

        Debug.Assert(_context.Employees != null, "_context.Employees != null");
        var employee = await _context.Employees.FindAsync(employeeId) ??
                       throw new LoyalWalletException($"Employee by id: {employeeId} not found");
        customer.TakePresent(employee);
        
        var scan = new Scan
        {
            CustomerId = customer.Id,
            EmployeeId = employee.Id,
            CompanyId = companyId,
            ScanDate = DateTime.Now
        };

        Debug.Assert(_context.Scans != null, "_context.Stamps != null");
        await _context.Scans.AddAsync(scan);
        
        await _context.SaveChangesAsync();

        var resultResource = _mapper.Map<Customer, CustomerResource>(customer);
        return resultResource;
    }

    private async Task<List<Customer>> CustomerList(int companyId)
    {
        Debug.Assert(_context.Customers != null, "_context.Customers != null");
        return await _context.Customers
            .Where(c => c.CompanyId == companyId)
            .ToListAsync();
    }

    private async Task<List<Scan>> ScansList(int companyId, string? locationName, DateTime? startDate,
        DateTime? endDate)
    {
        Debug.Assert(_context.Scans != null, "_context.Scans != null");
        Debug.Assert(_context.Locations != null, "_context.Locations != null");
        var scans = _context.Scans
            .Where(s => s.CompanyId == companyId);
        
        if (locationName != null)
        {
            var location = await _context.Locations
                               .FirstOrDefaultAsync(l => l.Name == locationName) ??
                           throw new LoyalWalletException($"location this name {locationName} not found");
            scans = scans.Where(s => s.CompanyId == location.CompanyId);
        }

        if (startDate != null && endDate != null)
            scans = scans.Where(s => s.ScanDate >= startDate && s.ScanDate <= endDate);
        return scans.ToList();
    }
}