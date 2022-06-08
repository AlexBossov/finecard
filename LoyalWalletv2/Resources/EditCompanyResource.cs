namespace LoyalWalletv2.Resources;

public class EditCompanyResource
{
    public string? CompanyName { get; set; }
    public int CompanyId { get; set; }
    public uint MaxCountOfStamps { get; set; } = 6;
    public string? InstagramName { get; set; }
}