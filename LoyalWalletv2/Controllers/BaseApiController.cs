using Microsoft.AspNetCore.Mvc;

namespace LoyalWalletv2.Controllers;

[Route("/api/[controller]")]
[Produces("application/json")]
[ApiController]
public class BaseApiController : ControllerBase
{
}