using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            //var userClaim = _claimService.GetUserClaim();
            //    request.AccountId = userClaim.Id; // Set the authenticated user's ID

            var response = await _subscriptionService.CreateSubscriptionAsync(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        [HttpGet("my-subscriptions")]
        public async Task<IActionResult> GetMySubscriptions()
        {
            var userClaim = _claimService.GetUserClaim();
            var response = await _subscriptionService.GetUserSubscriptionsAsync(userClaim.Id);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }


        [HttpGet("{subscriptionId}")]
        public async Task<IActionResult> GetSubscriptionById(int subscriptionId)
        {
            var response = await _subscriptionService.GetSubscriptionByIdAsync(subscriptionId);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        [HttpPut("{subscriptionId}")]
        public async Task<IActionResult> UpdateSubscription(int subscriptionId, [FromBody] UpdateSubscriptionRequest request)
        {
            var resposne = await _subscriptionService.UpdateSubscriptionAsync(subscriptionId, request);
            return resposne.IsSuccess ? Ok(resposne) : BadRequest(resposne);
        }

        [HttpPost("{subscriptionId}/cancel")]
        public async Task<IActionResult> CancelSubscription(int subscriptionId)
        {
            var response = await _subscriptionService.CancelSubscriptionAsync(subscriptionId);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        // =====================================================================
        // PHẦN 1: Quota & Quản lý nhân viên (Manager only)
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
        /// Xem thông tin slot đã dùng / tổng / còn lại.
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
        /// Xóa nhân viên khỏi công ty, giải phóng 1 slot.
        /// DELETE: api/Subscription/remove-employee/{userId}
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
