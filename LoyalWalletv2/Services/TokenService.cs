using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
namespace LoyalWalletv2.Services;

public class TokenService : ITokenService
{
    private readonly HttpClient _httpClient;

    public TokenService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<string?> GetTokenAsync()
    {
        var values = new Dictionary<string, object?>
        {
            {
                "apiId", OsmiInformation.ApiId
            },
            {
                "apiKey", OsmiInformation.ApiKey 
            }
        };

        var serializedValues = JsonConvert.SerializeObject(values);

        using var requestMessage =
            new HttpRequestMessage(HttpMethod.Post, OsmiInformation.HostPrefix + "/getToken");
        requestMessage.Content = new StringContent(
            serializedValues,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(requestMessage);

        var responseSerialised = await response.Content.ReadAsStringAsync();
        var container = JsonConvert.DeserializeObject<Container>(responseSerialised);

        Debug.Assert(container != null, nameof(container) + " != null");
        return container.Token;
    }
}

public class Container
{
    public string? Token { get; set; }
}