namespace LoyalWalletv2.Tools;

public class LoyalWalletException : Exception
{
    public LoyalWalletException()
    {
    }

    public LoyalWalletException(string message) :
        base(message)
    {
    }
    
    public LoyalWalletException(string message, Exception innerException) :
        base(message, innerException)
    {
    }
}