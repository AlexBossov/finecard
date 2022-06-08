using System;
using System.Diagnostics;
using System.Linq;
using LoyalWalletv2.Contexts;
using LoyalWalletv2.Domain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoyalWalletV2.Tests.Extensions;

public class CustomWebApplicationFactory<TStartup>
    : WebApplicationFactory<TStartup> where TStartup: class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();
            var logger = scopedServices
                .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();
            // db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            try
            {
                Debug.Assert(db.Locations != null, "db.Locations != null");
                db.Locations.Add(new Location
                {
                    CompanyId = 1,
                    Address = "a",
                    Name = "a1"
                });
                db.SaveChanges();
                Debug.Assert(db.Employees != null, "db.Employees != null");
                var employee = new Employee
                {
                    CompanyId = 1,
                    LocationId = 1,
                    Name = "a",
                    Surname = "a"
                };
                db.Employees.Add(employee);
                Debug.Assert(db.Customers != null, "db.Customers != null");
                Debug.Assert(db.Companies != null, "db.Companies != null");
                var customer = new Customer
                {
                    CompanyId = 1,
                    PhoneNumber = "+79518270540",
                    Company = db.Companies.Find(1)
                };
                Debug.Assert(db.Scans != null, "db.Scans != null");
                db.Scans.Add(new Scan
                {
                    CompanyId = 1,
                    EmployeeId = 1,
                    CustomerId = 1,
                    // ScanDate = DateTime.Now
                });
                customer.DoStamp(employee);
                // customer.DoStamp(employee);
                // customer.DoStamp(employee);
                // customer.DoStamp(employee);
                // customer.DoStamp(employee);
                // customer.DoStamp(employee);
                // customer.TakePresent(employee);
                // logger.LogInformation("count {Count}", customer.CountOfGivenPresents);
                // db.Scans.Add(new Scan
                // {
                //     CompanyId = 1,
                //     EmployeeId = 1,
                //     CustomerId = 1,
                //     ScanDate = DateTime.Now
                // });
                db.Customers.Add(customer);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the " +
                                    "database with test messages. Error: {Message}", ex.Message);
            }
        });
    }
}