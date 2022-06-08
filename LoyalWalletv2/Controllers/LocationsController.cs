using LoyalWalletv2.Contexts;
using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Domain.Models.AuthenticationModels;
using LoyalWalletv2.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoyalWalletv2.Controllers;

[Authorize(Roles = nameof(EUserRoles.User))]
public class LocationsController : BaseApiController
{
    private readonly AppDbContext _context;

    public LocationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{companyId:int}")]
    public async Task<IEnumerable<Location>> ListAsync(int companyId)
    {
        var company = await _context.Companies.Include(c => c.Locations)
                          .FirstOrDefaultAsync(c => c.Id == companyId) ??
                      throw new LoyalWalletException("Company not found");
        return company.Locations;
    }

    [HttpGet]
    [Route("{companyId:int}/{address}")]
    public async Task<Location> GetByAddress(int companyId, string? address)
    {
        var locations = await ListAsync(companyId);
        return locations.FirstOrDefault(l => l.Address == address) ??
               throw new LoyalWalletException("Location not found");
    }
    
    [HttpGet]
    [Route("{companyId:int}/{name}")]
    public async Task<Location> GetByName(int companyId, string? name)
    {
        var locations = await ListAsync(companyId);
        return locations.FirstOrDefault(l => l.Name == name) ??
               throw new LoyalWalletException("Location not found");
    }
    
    [HttpPut]
    public async Task<Location> UpdateAsync([FromBody] Location location)
    {
        var existLocation = await GetByName(location.CompanyId, location.Address);
        existLocation.Archived = location.Archived;
        await _context.SaveChangesAsync();

        return existLocation;
    }

    [HttpPost]
    public async Task<Location> CreateAsync([FromBody] Location location)
    {
        var result = await _context.Locations.AddAsync(location);
        await _context.SaveChangesAsync();

        return result.Entity;
    }

    [HttpDelete("{id:int}")]
    public async Task<Location> DeleteAsync(int id)
    {
        var model = await _context.Locations.FindAsync(id) ??
                    throw new LoyalWalletException("Location not found");
        var result = _context.Locations.Remove(model);
        await _context.SaveChangesAsync();

        return result.Entity;
    }
}