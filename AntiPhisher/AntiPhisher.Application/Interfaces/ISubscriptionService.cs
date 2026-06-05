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
        Task<ApiResponse> HandleExpiredSubscriptionsAsync();

        // =====================================================
        // PHẦN 1: Quota & Quản lý nhân viên (dành cho Manager)
        // =====================================================

        /// <summary>Mời / thêm nhân viên vào công ty, kiểm tra quota slot.</summary>
        Task<ApiResponse> InviteEmployeeAsync(InviteEmployeeRequest request, int managerId);

        /// <summary>Lấy thông tin slot đã dùng / còn lại của gói hiện tại.</summary>
        Task<ApiResponse> GetSlotsUsageAsync(int managerId);

        /// <summary>Xóa nhân viên ra khỏi công ty, giải phóng 1 slot.</summary>
        Task<ApiResponse> RemoveEmployeeAsync(int employeeUserId, int managerId);
    }
}
