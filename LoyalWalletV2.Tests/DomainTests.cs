using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LoyalWalletv2;
using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Resources;
using LoyalWalletv2.Services;
using LoyalWalletv2.Tools;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace LoyalWalletV2.Tests;

public class DomainTests
{
    private readonly Customer _customer;
    private readonly Employee _employee;
    private readonly ITestOutputHelper _testOutputHelper;
    private ITokenService _tokenService;
    private HttpClient _httpClient;

    public DomainTests(ITestOutputHelper testOutputHelper)
    {
        _httpClient = new HttpClient();
        _testOutputHelper = testOutputHelper;
        _tokenService = new TokenService(_httpClient);
        var company = new Company
        {
            MaxCountOfStamps = 6,
            Id = 1
        };

        _customer = new Customer
        {
            PhoneNumber = "+7 951 8270 540",
            CompanyId = company.Id,
            Company = company
        };

        _employee = new Employee
        {
            CompanyId = company.Id,
        };
    }

    [Fact]
    public void AddOneStamp()
    {
        const uint expectedStamps = 1;

        _customer.DoStamp(_employee);

        Assert.Equal(expectedStamps, _customer.CountOfStamps);
        Assert.Equal(expectedStamps, _employee.CountOfStamps);
    }

    [Fact]
    public void AddSixStamps_AndTakeOnePresent()
    {
        const uint expectedPresents = 0;
        const uint expectedGivenPresents = 1;

        var i = 0;
        for (; i < 6; i++) _customer.DoStamp(_employee);
        
        _customer.TakePresent(_employee);

        Assert.Equal(expectedPresents, _customer.CountOfStoredPresents);
        Assert.Equal(expectedGivenPresents, _employee.CountOfPresents);
    }

    [Fact]
    public async void TokenOsmi()
    {
        var httpClient = new HttpClient();
        var tokenService = new TokenService(httpClient);

        var token = await tokenService.GetTokenAsync();
        _testOutputHelper.WriteLine(token);
    }

    [Fact]
    public async void DeleteTemplate()
    {
        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Delete, OsmiInformation.HostPrefix
                                                      + $"/templates/finecard{1}");
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await _tokenService.GetTokenAsync());


        var response = new HttpResponseMessage();

        try
        {
            response = await _httpClient.SendAsync(requestMessage);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }

        _testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        // Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async void CreateTemplate()
    {
        DeleteTemplate();
        var cardOptions = new CardOptionsResource
        {
            CompanyId = 1,
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
        
        var serializedValues = JsonConvert.SerializeObject(values);
        _testOutputHelper.WriteLine(serializedValues);

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Post, OsmiInformation.HostPrefix
                                                    + $"/templates/{cardOptions.CompanyId}");
        requestMessage.Content = new StringContent(
            serializedValues,
            Encoding.UTF8,
            "application/json");

        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await _tokenService.GetTokenAsync());
        _testOutputHelper.WriteLine(await _tokenService.GetTokenAsync());

        var response = new HttpResponseMessage();
            
        try
        {
            response = await _httpClient.SendAsync(requestMessage);
        }
        catch (Exception e)
        {
            _testOutputHelper.WriteLine(e.Message);
            throw;
        }

        _testOutputHelper.WriteLine($"Response body - {await response.Content.ReadAsStringAsync()}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async void Push()
    {
        var pushResource = new PushResource
        {
            Message = "Hi",
            SerialNumbers = new List<string>()
            {
                "12345",
            }
        };
        var values = new Dictionary<string, object?>
        {
            {
                "message", pushResource.Message
            },
            {
                "serials", pushResource.SerialNumbers
            }
        };

        var serializedValues = System.Text.Json.JsonSerializer.Serialize(values);
        
        _testOutputHelper.WriteLine(serializedValues);

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Post, OsmiInformation.HostPrefix + 
                                                    "/marketing/pushmessage");
        requestMessage.Content = new StringContent(
            serializedValues,
            Encoding.UTF8,
            "application/json");

        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await _tokenService.GetTokenAsync());
    
        var response = await _httpClient.SendAsync(requestMessage);

        _testOutputHelper.WriteLine($"Response body - {await response.Content.ReadAsStringAsync()}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async void AddCustomer()
    {
        var existingCustomer = new Customer
        {
            PhoneNumber = "+79518270540",
            Company = new Company
            {
                Name = "A",
                Id = 1
            },
            CompanyId = 1,
            Confirmed = true,
            SerialNumber = 12345
        };

        if (existingCustomer is null) throw new LoyalWalletException("Client isn't exist");
        if (!existingCustomer.Confirmed) throw new LoyalWalletException("Client isn't confirmed");

        var barcode = OsmiInformation.HostPrefix
                      + $"/?serial_number={existingCustomer.SerialNumber}&company_id={existingCustomer.CompanyId}";

        Debug.Assert(existingCustomer.Company != null, "existingCustomer.Company != null");
        var values = new Dictionary<string, object>
        {
            { "noSharing", false },
            { "values", new object[]
            {
                new
                {
                    label = "Серийный номер клиента", 
                    value = $"{existingCustomer.Id}",
                    hideLabel = false,
                    forExistingCards = true,
                    key = "H1"
                },
                new
                {
                    label = "Количество штампов",
                    value = $"{existingCustomer.CountOfStamps} / {existingCustomer.Company.MaxCountOfStamps}", 
                    changeMsg = "ваши баллы %@",
                    hideLabel = false,
                    forExistingCards = false,
                    //key to change location on the card
                    key = "P1"
                },
                new
                {
                    label = "Номер телефона",
                    value = $"{existingCustomer.PhoneNumber}",
                    hideLabel = false,
                    forExistingCards = true,
                    key = "B1"
                }, 
            }},
            { 
                "barcode", 
                new
                {
                    message = barcode
                }
            }
        };

        var serializedValues = System.Text.Json.JsonSerializer.Serialize(values);

        _testOutputHelper.WriteLine($"values: {serializedValues}");

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Post, OsmiInformation.HostPrefix
                                                    + $"/passes/{existingCustomer.SerialNumber}" +
                                                    $"/{existingCustomer.Company.Id}?withValues=true");
        requestMessage.Content = new StringContent(
            serializedValues,
            Encoding.UTF8,
            "application/json");

        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await _tokenService.GetTokenAsync());

        var response = await _httpClient.SendAsync(requestMessage);
            
        _testOutputHelper.WriteLine($"Response body - {await response.Content.ReadAsStringAsync()}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    } 
}