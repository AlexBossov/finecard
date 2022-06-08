using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using LoyalWalletv2;
using LoyalWalletv2.Domain.Models;
using LoyalWalletv2.Domain.Models.AuthenticationModels;
using LoyalWalletv2.Services;
using LoyalWalletV2.Tests.Extensions;
using LoyalWalletv2.Tools;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace LoyalWalletV2.Tests;

public class ApiTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ITokenService _tokenService;
    private readonly HttpClient _httpClient;

    public ApiTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _httpClient = new HttpClient();
        _tokenService = new TokenService(_httpClient);
    }

    [Fact]
    public async void AllStampCount()
    {
        // var application = new CustomWebApplicationFactory<Program>();
        // var client = application.CreateClient();
        //
        // var loginResponse = await Login(client);
        //
        // using var requestMessage =
        //     new HttpRequestMessage(HttpMethod.Get, "/api/Customer/1/all-stamps-count");
        //
        // requestMessage.Headers.Authorization =
        //     new AuthenticationHeaderValue("Bearer", loginResponse.Token);
        //
        // var response = await client.SendAsync(requestMessage);
        //
        // Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Assert.Equal("1", await response.Content.ReadAsStringAsync());
        
        await using var application = new CustomWebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var loginRequest = new LoginModel
        {
            Email = "kostya.adrianov@gmail.com",
            Password = "Password123#"
        };

        var serializedValues = JsonConvert.SerializeObject(loginRequest);
        
        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Post, "api/Authenticate/login");

        requestMessage.Content = new StringContent(
            serializedValues,
            Encoding.UTF8,
            "application/json");
        
        var response = await client.SendAsync(requestMessage);
        
        var responseSerialised = await response.Content.ReadAsStringAsync();

        var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseSerialised);
        _testOutputHelper.WriteLine(responseSerialised);
    
        using var requestMessage2 =
            new HttpRequestMessage(HttpMethod.Get, "/api/Customer/1/all-stamps-count");
        
        requestMessage2.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResponse.Token);
        
        response = await client.SendAsync(requestMessage2);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("1", await response.Content.ReadAsStringAsync());
    }
    
    // [Fact]
    // public async void AllPresentsCount()
    // {
    //     await using var application = new CustomWebApplicationFactory<Program>();
    //     using var client = application.CreateClient();
    //
    //     var loginResponse = await Login(client);
    //
    //     using var requestMessage =
    //         new HttpRequestMessage(HttpMethod.Get, "/api/Customer/1/all-presents-count");
    //     
    //     requestMessage.Headers.Authorization =
    //         new AuthenticationHeaderValue("Bearer", loginResponse.Token);
    //     HttpResponseMessage response;
    //     try
    //     {
    //         response = await client.SendAsync(requestMessage);
    //     }
    //     catch (Exception e)
    //     {
    //         _testOutputHelper.WriteLine(e.Message);
    //         throw;
    //     }
    //
    //     Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    //     Assert.Equal("1", await response.Content.ReadAsStringAsync());
    //     // _testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
    // }
    //
    // private async Task<LoginResponse> Login(HttpClient client)
    // {
    //     var loginRequest = new LoginModel
    //     {
    //         Email = "kostya.adrianov@gmail.com",
    //         Password = "Password123#"
    //     };
    //
    //     var serializedValues = JsonConvert.SerializeObject(loginRequest);
    //     
    //     using var requestMessage =
    //         new HttpRequestMessage(HttpMethod.Post, "api/Authenticate/login");
    //
    //     requestMessage.Content = new StringContent(
    //         serializedValues,
    //         Encoding.UTF8,
    //         "application/json");
    //     
    //     var response = await client.SendAsync(requestMessage);
    //     
    //     var responseSerialised = await response.Content.ReadAsStringAsync();
    //
    //     _testOutputHelper.WriteLine(responseSerialised);
    //     return JsonConvert.DeserializeObject<LoginResponse>(responseSerialised);
    // }

    [Fact]
    public async void CodeGenerate()
    {
        const string phoneNumber = "+79518270540";

        var values = new Dictionary<string, string>
        {
            { "smsText", "Ваш пинкод для транзакции {pin}" },
            { "length", "4" }
        };

        var serializedValues = System.Text.Json.JsonSerializer.Serialize(values);
        
        _testOutputHelper.WriteLine(serializedValues);

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Post, OsmiInformation.HostPrefix
                                                    + $"/activation/sendpin/{phoneNumber}");
        requestMessage.Content = new StringContent(
            serializedValues,
            Encoding.UTF8,
            "application/json");
        requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await _tokenService.GetTokenAsync());

        var response = await _httpClient.SendAsync(requestMessage);
        
        var responseSerialised = await response.Content.ReadAsStringAsync();
        var token = JsonConvert.DeserializeObject<Container>(responseSerialised)?.Token;

        _testOutputHelper.WriteLine($"Generated code: {token}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async void CheckCode()
    {
        var values = new Dictionary<string, string>
        {
            { "token", "1bd29cf9d4d720b7d17ffa50d42309bf2b3c05d2" },
            { "pin", "2784" }
        };

        var serializedValues = System.Text.Json.JsonSerializer.Serialize(values);

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Post, OsmiInformation.HostPrefix
                                                    + "/activation/checkpin");
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
    public async void ScanCard()
    {
        const string uri = "https://api-dev.osmicards.com/v2t/?serial_number=12345&company_id=1";
        var uriParam = new Uri(uri);
        var uriCollection = HttpUtility.ParseQueryString(uriParam.Query);
        var serialNumberQuery = uriCollection.Get("serial_number");
        var companyIdQuery = uriCollection.Get("company_id");

        if (!int.TryParse(serialNumberQuery, out var serialNumber))
            throw new LoyalWalletException($"Invalid value of serial number {serialNumberQuery}");
        
        if (!int.TryParse(companyIdQuery, out var companyId))
            throw new LoyalWalletException($"Invalid value of company id {companyIdQuery}");
        
        _testOutputHelper.WriteLine($"{serialNumber} {companyId}");

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

        var employee = new Employee
        {
            Id = 1,
            CompanyId = 1,
        };

        existingCustomer.DoStamp(employee);

        Debug.Assert(existingCustomer.Company != null, "existingCustomer.Company != null");
        var values = new Dictionary<string, object>
        {
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
        };

        var serializedValues = System.Text.Json.JsonSerializer.Serialize(values);

        _testOutputHelper.WriteLine($"values: {serializedValues}");

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Put, OsmiInformation.HostPrefix
                                                   + $"/passes/{existingCustomer.SerialNumber}" +
                                                   $"/{existingCustomer.Company.Name}?push=true");
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

public class Container
{
    public string? Token { get; set; }
}

public class LoginResponse
{
    public string? Token { get; set; }
    public string? Expiration { get; set; }
    public int CompanyId { get; set; }
}