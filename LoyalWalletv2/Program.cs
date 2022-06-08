using System.Text;
using LoyalWalletv2;
using LoyalWalletv2.Contexts;
using LoyalWalletv2.Controllers;
using LoyalWalletv2.Domain.Models.AuthenticationModels;
using LoyalWalletv2.Services;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Formatting = Formatting.Indented;
    });

var connection = builder.Configuration.GetConnectionString("DefaultConnectionMySQL");
var version = ServerVersion.AutoDetect(connection);
builder.Services.AddDbContext<AppDbContext>(options =>
     options.UseMySql(connection, version));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddHttpClient<OsmiController>();
builder.Services.AddHttpClient<AuthenticateController>();
builder.Services.AddHttpClient<TokenService>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()  
    .AddEntityFrameworkStores<AppDbContext>()  
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(options =>  
    {  
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;  
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;  
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;  
    })

    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {  
            ValidateIssuer = true,  
            ValidateAudience = true,  
            ValidAudience = builder.Configuration["JWT:ValidAudience"],  
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],  
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))  
        };
    });

builder.Services.AddCors();

var app = builder.Build();
var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();

try
{
    await SampleData.Initialize(context, userManager, roleManager);
}
catch (Exception e)
{
    Console.WriteLine(e.Message + "An error occurred seeding the DB.");
    throw;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(_ => true) // allow any origin
                .AllowCredentials());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }