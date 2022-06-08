using System.Diagnostics;
using LoyalWalletv2.Contexts;
using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Domain.Models.AuthenticationModels;
using LoyalWalletv2.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoyalWalletv2.Controllers;

[Authorize(Roles = nameof(EUserRoles.User))]
public class EmployeeController : BaseApiController
{
    private readonly AppDbContext _context;

    public EmployeeController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("{companyId:int}")]
    public async Task<IEnumerable<Employee>> ListAsync(int companyId)
    {
        Debug.Assert(_context.Companies != null, "_context.Companies != null");
        var company = await _context.Companies.Include(c => c.Employees)
                          .FirstOrDefaultAsync(c => c.Id == companyId) ??
                      throw new LoyalWalletException("Company not found");
        return company.Employees;
    }

    [HttpGet]
    [Route("{companyId:int}/{employeeName}&{employeeSurname}")]
    public async Task<Employee> GetByName(int companyId, string? employeeName, string? employeeSurname)
    {
        var employees = await ListAsync(companyId);
        return employees.FirstOrDefault(e => e.Name == employeeName && e.Surname == employeeSurname) ??
               throw new LoyalWalletException("Employee not found");
    }

    [HttpPost]
    public async Task<Employee> CreateAsync([FromBody] Employee employee)
    {
        Debug.Assert(_context.Employees != null, "_context.Employees != null");
        var result = await _context.Employees.AddAsync(employee);
        await _context.SaveChangesAsync();

        return result.Entity;
    }

    [HttpPut]
    public async Task<Employee> UpdateAsync([FromBody] Employee employee)
    {
        var existEmployee = await GetByName(employee.CompanyId, employee.Name, employee.Surname);
        existEmployee.Archived = employee.Archived;
        await _context.SaveChangesAsync();

        return existEmployee;
    }

    [HttpDelete("{id:int}")]
    public async Task<Employee> DeleteAsync(int id)
    {
        Debug.Assert(_context.Employees != null, "_context.Employees != null");
        var model = await _context.Employees.FindAsync(id) ??
                    throw new LoyalWalletException("Employee not found");
        var result = _context.Employees.Remove(model);
        await _context.SaveChangesAsync();

        return result.Entity;
    }

    [HttpGet("count-of-stamps/{employeeId:int}")]
    public async Task<uint> CountOfStamps(int employeeId, DateTime? startDate, DateTime? endDate)
    {
        var scans = await ScansList(employeeId, startDate, endDate);
        Debug.Assert(_context.Employees != null, "_context.Customers != null");
        var query = _context.Employees.ToList()
                        .FirstOrDefault(c => scans.Any(s => s.EmployeeId == c.Id))
                    ?? throw new LoyalWalletException("Employee not found");
        return query.CountOfStamps;
    }
    
    [HttpGet("count-of-presents/{employeeId:int}")]
    public async Task<uint> CountOfPresents(int employeeId, DateTime? startDate, DateTime? endDate)
    {
        var scans = await ScansList(employeeId, startDate, endDate);
        Debug.Assert(_context.Employees != null, "_context.Customers != null");
        var query = _context.Employees.ToList()
                        .FirstOrDefault(c => scans.Any(s => s.EmployeeId == c.Id))
                    ?? throw new LoyalWalletException("Employee not found");
        return query.CountOfPresents;
    }
    
    private async Task<List<Scan>> ScansList(int employeeId, DateTime? startDate, DateTime? endDate)
    {
        Debug.Assert(_context.Scans != null, "_context.Scans != null");
        Debug.Assert(_context.Locations != null, "_context.Locations != null");
        var scans = _context.Scans
            .Where(s => s.EmployeeId == employeeId);

        if (startDate != null && endDate != null)
            scans = scans.Where(s => s.ScanDate >= startDate && s.ScanDate <= endDate);
        return scans.ToList();
    }
}