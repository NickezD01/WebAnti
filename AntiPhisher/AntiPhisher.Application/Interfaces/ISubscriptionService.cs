using AntiPhisher.Application.Request.Subscription;
using AntiPhisher.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface ISubscriptionService
    {
        // Subscription CRUD operations
        Task<ApiResponse> CreateSubscriptionAsync(CreateSubscriptionRequest request);
        Task<ApiResponse> UpdateSubscriptionAsync(int Id, UpdateSubscriptionRequest request);
        Task<ApiResponse> CancelSubscriptionAsync(int subscriptionId);
        Task<ApiResponse> GetSubscriptionByIdAsync(int subscriptionId);
        Task<ApiResponse> DeleteSubPlanData(int Id);
        // User-related subscription operations
        Task<ApiResponse> GetUserSubscriptionsAsync(int accountId);

        // Subscription management operations
        Task<ApiResponse> ProcessSubscriptionPaymentAsync(int subscriptionId);

        // Subscription status operations

        Task<ApiResponse> HandleExpiredSubscriptionsAsync();
    }
}
