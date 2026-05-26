using AntiPhisher.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse> CreateOrderFromSubscription(int subscriptionId);
        Task<ApiResponse> GetOrderById(int orderId);
        Task<ApiResponse> GetUserOrders();
        Task<ApiResponse> CancelOrder(int orderId);
    }
}
