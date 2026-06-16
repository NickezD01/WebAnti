using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        public IVnPayService _service;
        private readonly IConfiguration _config;

        public PaymentController(IVnPayService service, IConfiguration config)
        {
            _service = service;
            _config = config;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePaymentUrl(PaymentRequest model)
        {
            var response = await _service.CreatePaymentUrl(model, HttpContext);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> PaymentCallback()
        {
            var response = await _service.PaymentExecute(Request.Query);
            var feBase = _config["Frontend:BaseUrl"]!;

            return Redirect(response.IsSuccess
                ? $"{feBase}/paymentsuccess"
                : $"{feBase}/paymentfail");
        }
    }
}
