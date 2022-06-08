using System.Diagnostics;
using LoyalWalletv2.Contexts;
using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Domain.Models.AuthenticationModels;
using LoyalWalletv2.Tools;
using Microsoft.AspNetCore.Identity;


namespace LoyalWalletv2;

public static class SampleData
{
    public static async Task Initialize(
        AppDbContext context,
        UserManager<ApplicationUser>? userManager,
        RoleManager<IdentityRole>? roleManager)
    {
        var company = new Company
        {
            Name = "#default_company",
        };

        var companies = context.Companies;
        Debug.Assert(companies != null, nameof(companies) + " != null");
        if (!companies.Any())
            await companies.AddAsync(company);
        await context.SaveChangesAsync();
        
        await RegisterAdmin(userManager, roleManager, company.Id);
        //
        // Debug.Assert(context.Customers != null, "context.Customers != null");
        // if (context.Customers.Any())
        //     return;
        //
        // Debug.Assert(context.Locations != null, "db.Locations != null");
        // context.Locations.Add(new Location
        // {
        //     CompanyId = 1,
        //     Address = "a",
        //     Name = "a1"
        // });
        // context.SaveChanges();
        // Debug.Assert(context.Employees != null, "db.Employees != null");
        // var employee = new Employee
        // {
        //     CompanyId = 1,
        //     LocationId = 1,
        //     Name = "a",
        //     Surname = "a"
        // };
        // context.Employees.Add(employee);
        // Debug.Assert(context.Customers != null, "db.Customers != null");
        // Debug.Assert(context.Companies != null, "db.Companies != null");
        // var customer = new Customer
        // {
        //     CompanyId = 1,
        //     PhoneNumber = "+79518270540",
        //     Company = context.Companies.Find(1)
        // };
        // Debug.Assert(context.Scans != null, "db.Scans != null");
        // context.Scans.Add(new Scan
        // {
        //     CompanyId = 1,
        //     EmployeeId = 1,
        //     CustomerId = 1,
        //     // ScanDate = DateTime.Now
        // });
        // customer.DoStamp(employee);
        // customer.DoStamp(employee);
        // customer.DoStamp(employee);
        // customer.DoStamp(employee);
        // customer.DoStamp(employee);
        // customer.DoStamp(employee);
        // customer.TakePresent(employee);
        // // logger.LogInformation("count {Count}", customer.CountOfGivenPresents);
        // context.Scans.Add(new Scan
        // {
        //     CompanyId = 1,
        //     EmployeeId = 1,
        //     CustomerId = 1,
        //     ScanDate = DateTime.Now
        // });
        // context.Customers.Add(customer);
        // context.SaveChanges();
    }

    public static async Task RegisterAdmin(
        UserManager<ApplicationUser>? userManager,
        RoleManager<IdentityRole>? roleManager,
        int companyId)
    {
        //Only one admin!
        if (await roleManager.RoleExistsAsync(nameof(EUserRoles.Admin)))
            return;

        var model = new RegisterModel
        {
            Email = "kostya.adrianov@gmail.com",
            Password = "Password123#"
        };

        var user = new ApplicationUser
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.Email,
            CompanyId = companyId,
            EmailConfirmed = true
        };

        IdentityResult result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            throw new LoyalWalletException(
                "Admin creation failed! Please check user details and try again.");

        if (!await roleManager.RoleExistsAsync(nameof(EUserRoles.Admin)))
            await roleManager.CreateAsync(new IdentityRole(nameof(EUserRoles.Admin)));
        if (!await roleManager.RoleExistsAsync(nameof(EUserRoles.User)))
            await roleManager.CreateAsync(new IdentityRole(nameof(EUserRoles.User)));

        if (await roleManager.RoleExistsAsync(nameof(EUserRoles.Admin)))
            await userManager.AddToRoleAsync(user, nameof(EUserRoles.Admin));
        if (await roleManager.RoleExistsAsync(nameof(EUserRoles.User)))
            await userManager.AddToRoleAsync(user, nameof(EUserRoles.User));
    }
}