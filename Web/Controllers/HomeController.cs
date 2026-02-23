using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
public class HomeController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _contextAccessor;

    public HomeController(IConfiguration configuration, IHttpContextAccessor contextAccessor)
    {
        _configuration = configuration;
        _contextAccessor = contextAccessor;
    }
    
    [HttpGet("")]
    [HttpGet("/")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult RedirectToSwagger()
    {
        return Redirect("/swagger");
    }
}