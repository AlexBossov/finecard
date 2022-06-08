namespace LoyalWalletv2.Services;

public interface ITokenService
{
    Task<string?> GetTokenAsync();
}