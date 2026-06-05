using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.SubscriptionPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AntiPhisher.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionPlanController : ControllerBase
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly ILogger<SubscriptionPlanController> _logger;

        public SubscriptionPlanController(ISubscriptionPlanService subscriptionPlanService, ILogger<SubscriptionPlanController> logger)
        {
            _subscriptionPlanService = subscriptionPlanService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPlans()
        {
            var response = await _subscriptionPlanService.GetAllPlansAsync();
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }


        // Public — chỉ field bán hàng (trang pricing)
        [HttpGet("{planId}")]
        public async Task<IActionResult> GetPlanById(int planId)
        {
            var response = await _subscriptionPlanService.GetPlanByIdAsync(planId);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        // Admin-only — kèm dữ liệu nội bộ (subscriber count + revenue)
        [Authorize(Roles = "Admin")]
        [HttpGet("{planId}/stats")]
        public async Task<IActionResult> GetPlanStats(int planId)
        {
            var response = await _subscriptionPlanService.GetPlanStatsAsync(planId);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanRequest request)
        {
            try
            {
                _logger.LogInformation($"User Claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}"))}");
                _logger.LogInformation($"Is User Authenticated: {User.Identity?.IsAuthenticated}");
                _logger.LogInformation($"User Roles: {string.Join(", ", User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");

                var response = await _subscriptionPlanService.CreatePlanAsync(request);
                return response.IsSuccess ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreatePlan: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{planId}")]
        public async Task<IActionResult> UpdatePlan(int planId, [FromBody] UpdateSubscriptionPlanRequest request)
        {
            var resposne = await _subscriptionPlanService.UpdatePlanAsync(planId, request);
            return resposne.IsSuccess ? Ok(resposne) : BadRequest(resposne);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{planId}")]
        public async Task<IActionResult> DeletePlan(int planId)
        {
            var response = await _subscriptionPlanService.DeletePlanAsync(planId);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        //DashBoard

        [Authorize(Roles = "Admin")]
        [HttpGet("NumberOfSubscribers")]
        public async Task<IActionResult> countPlan()
        {
            var response = await _subscriptionPlanService.CountPlan();
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("CalculateTotalRevenue")]
        public async Task<IActionResult> CalculateTotalRevenue()
        {
            var response = await _subscriptionPlanService.CalculateTotalRevenue();
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("TotalPrice")]
        public async Task<IActionResult> TotalPrice()
        {
            var response = await _subscriptionPlanService.TotalPrice();
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

    }
}
