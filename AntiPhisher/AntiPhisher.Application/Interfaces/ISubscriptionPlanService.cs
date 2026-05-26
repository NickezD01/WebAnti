using AntiPhisher.Application.Request.SubscriptionPlan;
using AntiPhisher.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface ISubscriptionPlanService
    {
        // Plan CRUD operations
        Task<ApiResponse> CreatePlanAsync(CreateSubscriptionPlanRequest request);
        Task<ApiResponse> UpdatePlanAsync(int Id, UpdateSubscriptionPlanRequest request);
        Task<ApiResponse> DeletePlanAsync(int planId);
        Task<ApiResponse> GetPlanByIdAsync(int planId);

        // Plan listing operations
        Task<ApiResponse> GetAllPlansAsync();
        // Đã xóa: Task<ApiResponse> GetActivePlansAsync();

        // Plan detail operations



        // Plan management operations



        //Admin Dashboard
        Task<ApiResponse> CountPlan();
        Task<ApiResponse> CalculateTotalRevenue();
        Task<ApiResponse> TotalPrice();
    }
}
