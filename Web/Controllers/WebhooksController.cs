using System.IO;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly IWebhookProcessingService _webhookProcessingService;

        public WebhooksController(IWebhookProcessingService webhookProcessingService)
        {
            _webhookProcessingService = webhookProcessingService;
        }

        [HttpPost("{provider}")]
        public async Task<IActionResult> Post(string provider)
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];

            var result = await _webhookProcessingService.ProcessWebhookAsync(payload, signature, provider);

            if (result)
            {
                return Ok();
            }

            return BadRequest();
        }
    }
}