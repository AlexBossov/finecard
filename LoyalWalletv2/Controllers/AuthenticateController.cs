using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LoyalWalletv2.Contexts;
using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Domain.Models.AuthenticationModels;
using LoyalWalletv2.Resources;
using LoyalWalletv2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LoyalWalletv2.Controllers;

public class AuthenticateController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly ILogger<AuthenticateController> _logger;
    private readonly IEmailService _emailService;
    private readonly HttpClient _httpClient;
    private ITokenService _tokenService;

    public AuthenticateController
    (
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        AppDbContext context,
        ILogger<AuthenticateController> logger,
        IEmailService emailService,
        HttpClient httpClient, 
        ITokenService tokenService
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _httpClient = httpClient;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        ApplicationUser user = await _userManager.FindByNameAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password) || !user.EmailConfirmed)
            return Unauthorized();
        IList<string> userRoles = await _userManager.GetRolesAsync(user);

        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        authClaims.
            AddRange(userRoles.
                Select(userRole => 
                    new Claim(ClaimTypes.Role, userRole)));

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

        var token = new JwtSecurityToken(
            _configuration["JWT:ValidIssuer"],
            _configuration["JWT:ValidAudience"],
            expires: DateTime.Now.AddHours(3),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            companyId = user.CompanyId,
            expiration = token.ValidTo
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterModel model)
    {
        try
        {
            ApplicationUser userExists = await _userManager.FindByNameAsync(model.Email);
            if (userExists != null)
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new Response
                    {
                        Status = "Error",
                        Message = "User already exists!"
                    });

            var newCompany = await CreateCompanyAsync();

            var user = new ApplicationUser
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email,
                CompanyId = newCompany.Id
            };

            IdentityResult result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new Response
                    {
                        Status = "Error",
                        Message = "User creation failed! Please check user details and try again."
                    });

            if (!await _roleManager.RoleExistsAsync(nameof(EUserRoles.User)))
                await _roleManager.CreateAsync(new IdentityRole(nameof(EUserRoles.User)));

            if (await _roleManager.RoleExistsAsync(nameof(EUserRoles.User)))
                await _userManager.AddToRoleAsync(user, nameof(EUserRoles.User));
            
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Authenticate",
                new
                { user.Id, code },
                HttpContext.Request.Scheme);
            await _emailService.SendEmailAsync(model.Email, "Confirm your account",
                $"Подтвердите регистрацию, перейдя по ссылке: <a href='{callbackUrl}'>link</a>");

            return Ok(new Response { Status = "Success", Message = "User created successfully!" });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred when registering user {Message}", e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string? id, string? code)
    {
        if (id == null || code == null)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new Response
                {
                    Status = "Error",
                    Message = $"User confirmation failed! {nameof(id)} or {nameof(code)} is invalid "
                });

        var userExist = await _userManager.FindByIdAsync(id);

        if (userExist == null)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new Response
                {
                    Status = "Error",
                    Message = "User not found."
                });

        var result = await _userManager.ConfirmEmailAsync(userExist, code);

        if (!result.Succeeded)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new Response
                {
                    Status = "Error",
                    Message = "User confirmation failed! Please check user details and try again."
                });

        return Ok(new Response { Status = "Success", Message = "User confirmed successfully!" });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordResource changePasswordResource)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.CompanyId == changePasswordResource.CompanyId);

        if (user == null || !await _userManager
                .CheckPasswordAsync(user, changePasswordResource.OldPassword) || !user.EmailConfirmed)
            return Unauthorized();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(user, token, changePasswordResource.NewPassword);
        
        if (!result.Succeeded)
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new Response
                {
                    Status = "Error",
                    Message = "User confirmation failed! Please check user details and try again."
                });

        return Ok(new Response { Status = "Success", Message = "User confirmed successfully!" });
    }

    private async Task<Company> CreateCompanyAsync()
    {
        var model = new Company();
        var result = await _context.Companies.AddAsync(model);
        await _context.SaveChangesAsync();

        var cardOptions = new CardOptionsResource
        {
            CompanyId = result.Entity.Id,
        };
        
        var values = new Dictionary<string, object>
        {
            { "noSharing", false },
            { "limit", "-empty-" },
            { "logoText", "Test" },
            { "description", "Основная карта" },
            { "style", "storeCard" },
            { "transitType", "-empty-" },
            {
                "values", new object[]
                {
                    new
                    {
                        label = "Серийный номер клиента", 
                        value = "-empty-",
                        hideLabel = false,
                        forExistingCards = true,
                        key = "H1"
                    },
                    new
                    {
                        label = "Количество штампов",
                        value = "0 / 6", 
                        changeMsg = "ваши баллы %@",
                        hideLabel = false,
                        forExistingCards = false,
                        //key to change location on the card
                        key = "P1"
                    },
                    new
                    {
                        label = "Номер телефона",
                        value = "-empty-",
                        hideLabel = false,
                        forExistingCards = true,
                        key = "B1"
                    },
                    new
                    {
                        label = "Id ресторана",
                        value = $"{cardOptions.CompanyId}",
                        hideLabel = true,
                        forExistingCards = true,
                        key = "B2"
                    },
                }
            },
            {
                "barcode", new
                {
                    show = true,
                    showSignature = true,
                    message = "-serial-",
                    signature = "-serial-",
                    format = "QR",
                    encoding = "iso-8859-1"
                }
            },
            { 
                "colors", new 
                {
                    label = $"{ColorTranslator.ToHtml(Color.FromArgb(cardOptions.TextColor))}",
                    background = $"{ColorTranslator.ToHtml(Color.FromArgb(cardOptions.BackgroundColor))}",
                    foreground = "#00BBCC"
            }},
            {
            "images", new {
                strip = "-empty-",
                icon = "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AP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//Gq1ARsEoGAWjYBSQBQAAAAD//xqtQEbBKBgFo2AUkAUAAAAA//8arUBGwSgYBaNgFJAFAAAAAP//AwDS0nGp47f0xwAAAABJRU5ErkJggg==",
                // logo = $"{cardOptions.LogotypeImg}"
                logo = "-empty-"
            }},
        };

        var serializedValues = JsonSerializer.Serialize(values);

        using (var requestMessage =
               new HttpRequestMessage(HttpMethod.Post, OsmiInformation.HostPrefix
                                                       + $"/templates/finecard{cardOptions.CompanyId}"))
        {
            requestMessage.Content = new StringContent(
                serializedValues,
                Encoding.UTF8,
                "application/json");

            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", await _tokenService.GetTokenAsync());

            try
            {
                await _httpClient.SendAsync(requestMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        return result.Entity;
    }
}  