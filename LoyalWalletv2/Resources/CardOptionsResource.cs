using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace LoyalWalletv2.Resources;

public class CardOptionsResource
{
    [Required]
    public int CompanyId { get; set; }
    public int BackgroundColor { get; set; } = Color.White.ToArgb();
    public int TextColor { get; set; } = Color.Black.ToArgb();
    public byte[]? LogotypeImg { get; set; }
}