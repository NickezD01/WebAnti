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
        public PaymentController(IVnPayService service)
        {
            _service = service;
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

            // Redirect the user directly to the front-end with status
            if (response.IsSuccess)
            {
                // Extract the redirect URL from the response and pass it as a query parameter to the FE
                var redirectUrl = "http://localhost:5173/paymentsuccess";
                return Redirect(redirectUrl);
            }
            else
            {
                var redirectUrl = "http://localhost:5173/paymentfail";
                return Redirect(redirectUrl);
            }
        }
    }
}
