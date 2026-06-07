using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace AntiPhisher.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IClaimService _claimService;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            IClaimService claimService)
        {
            _subscriptionService = subscriptionService;
            _claimService = claimService;
        }

        /// <summary>
        /// Doanh nghiệp tiến hành mua/đăng ký gói dịch vụ mới.
        /// POST: api/Subscription
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _subscriptionService.CreateSubscriptionAsync(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Lấy lịch sử và thông tin tất cả các gói đăng ký thuộc về CÔNG TY của Manager hiện tại.
        /// GET: api/Subscription/my-subscriptions
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpGet("my-subscriptions")]
        public async Task<IActionResult> GetMySubscriptions()
        {
            var userClaim = _claimService.GetUserClaim();
            var response = await _subscriptionService.GetUserSubscriptionsAsync(userClaim.Id);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Lấy chi tiết thông tin gói đăng ký theo Id.
        /// GET: api/Subscription/{subscriptionId}
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpGet("{subscriptionId:int}")]
        public async Task<IActionResult> GetSubscriptionById(int subscriptionId)
        {
            var response = await _subscriptionService.GetSubscriptionByIdAsync(subscriptionId);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Cập nhật thông tin gói đăng ký dịch vụ của doanh nghiệp.
        /// PUT: api/Subscription/{subscriptionId}
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpPut("{subscriptionId:int}")]
        public async Task<IActionResult> UpdateSubscription(int subscriptionId, [FromBody] UpdateSubscriptionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _subscriptionService.UpdateSubscriptionAsync(subscriptionId, request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Hủy kích hoạt gói dịch vụ của công ty.
        /// POST: api/Subscription/{subscriptionId}/cancel
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpPost("{subscriptionId:int}/cancel")]
        public async Task<IActionResult> CancelSubscription(int subscriptionId)
        {
            var response = await _subscriptionService.CancelSubscriptionAsync(subscriptionId);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        // =====================================================================
        // PHẦN 1: Quota & Quản lý nhân viên (Chỉ dành cho Manager)
        // =====================================================================

        /// <summary>
        /// Mời / thêm nhân viên mới vào công ty. Kiểm tra quota slot của gói hiện tại.
        /// POST: api/Subscription/invite-employee
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpPost("invite-employee")]
        public async Task<IActionResult> InviteEmployee([FromBody] InviteEmployeeRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { errorMessage = string.Join("; ", errors) });
            }

            var claim = _claimService.GetUserClaim();
            var response = await _subscriptionService.InviteEmployeeAsync(request, claim.Id);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Xem thông tin số lượng slot nhân sự đã dùng / tổng số slot của gói dịch vụ công ty.
        /// GET: api/Subscription/slots-usage
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpGet("slots-usage")]
        public async Task<IActionResult> GetSlotsUsage()
        {
            var claim = _claimService.GetUserClaim();
            var response = await _subscriptionService.GetSlotsUsageAsync(claim.Id);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Xóa nhân viên khỏi công ty, giải phóng 1 vị trí slot trống cho gói.
        /// DELETE: api/Subscription/remove-employee/{employeeUserId}
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpDelete("remove-employee/{employeeUserId:int}")]
        public async Task<IActionResult> RemoveEmployee(int employeeUserId)
        {
            var claim = _claimService.GetUserClaim();
            var response = await _subscriptionService.RemoveEmployeeAsync(employeeUserId, claim.Id);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }
    }
}