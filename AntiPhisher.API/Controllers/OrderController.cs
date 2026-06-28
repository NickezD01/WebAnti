using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [Route("api/orders")]
    [ApiController]
    [Authorize] // Yêu cầu xác thực JWT
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// Tạo đơn hàng từ đăng ký Subscription
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder(OrderRequest request)
        {
            var result = await _orderService.CreateOrderFromSubscription(request.SubscriptionId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// Lấy thông tin đơn hàng theo ID
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            var result = await _orderService.GetOrderById(orderId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// Lấy danh sách đơn hàng của người dùng hiện tại
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetUserOrders()
        {
            var result = await _orderService.GetUserOrders();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// Hủy đơn hàng (chỉ có thể hủy nếu chưa thanh toán)
        [HttpPut("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var result = await _orderService.CancelOrder(orderId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
