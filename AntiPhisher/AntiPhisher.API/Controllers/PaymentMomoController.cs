using AntiPhisher.Application.Request.Payment;
using AntiPhisher.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentMomoController : ControllerBase
    {
        private readonly MoMoService _momoService;

        public PaymentMomoController(MoMoService momoService)
        {
            _momoService = momoService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentRequest model)
        {
            var result = await _momoService.CreatePayment(model, HttpContext);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("ipn")] // URL: /api/PaymentMomo/ipn
        public async Task<IActionResult> HandleCallback([FromBody] dynamic ipnData)
        {
            await _momoService.ProcessCallback(ipnData);
            return Ok(new { message = "Received" });
        }
    }
}