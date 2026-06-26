using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.Orders;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimService _claimService;

        public OrderService(IUnitOfWork unitOfWork, IClaimService claimService)
        {
            _unitOfWork = unitOfWork;
            _claimService = claimService;
        }

        public async Task<ApiResponse> CreateOrderFromSubscription(int subscriptionId)
        {
            ApiResponse response = new ApiResponse();
            var claim = _claimService.GetUserClaim();
            var userId = claim.Id;

            try
            {
                // 🔹 Lấy thông tin Subscription đầy đủ
                var subscription = await _unitOfWork.Subscriptions.GetSubscriptionWithDetails(subscriptionId);
                if (subscription == null)
                {
                    return response.SetNotFound("Subscription not found");
                }

                // 🔹 Kiểm tra Subscription có hợp lệ
                if (subscription.Status != SubscriptionStatus.Active)
                {
                    return response.SetBadRequest("Subscription is not active");
                }

                // 🔹 Kiểm tra Subscription đã có đơn hàng chưa
                var existingOrder = await _unitOfWork.Orders.GetAsync(o => o.SubscriptionId == subscriptionId);
                if (existingOrder != null)
                {
                    return response.SetBadRequest("An order for this subscription already exists.");
                }

                // 🔹 Tạo đơn hàng mới từ Subscription
                var order = new Order
                {
                    AccountId = userId,
                    SubscriptionId = subscriptionId,
                    Price = subscription.Price,
                    //Status = OrderStatus.Pending, // Chờ thanh toán
                    Note = "Order created from subscription",
                    IsDelete = false
                };

                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangeAsync();

                // 🔹 Trả về OrderResponse chỉ chứa các trường cần thiết
                var orderResponse = new OrderResponse
                {
                    Id = order.Id,
                    SubscriptionId = order.SubscriptionId,
                    Price = order.Price
                };

                return response.SetOk(orderResponse);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest($"An error occurred: {ex.Message}");
            }
        }

        public async Task<ApiResponse> GetOrderById(int orderId)
        {
            ApiResponse response = new ApiResponse();

            var order = await _unitOfWork.Orders.GetAsync(x => x.Id == orderId);
            if (order == null)
            {
                return response.SetNotFound("Order not found");
            }

            return response.SetOk(order);
        }

        public async Task<ApiResponse> GetUserOrders()
        {
            ApiResponse response = new ApiResponse();
            var claim = _claimService.GetUserClaim();
            var userId = claim.Id;

            var orders = await _unitOfWork.Orders.GetAllAsync(x => x.AccountId == userId);
            return response.SetOk(orders);
        }

        public async Task<ApiResponse> CancelOrder(int orderId)
        {
            ApiResponse response = new ApiResponse();
            var claim = _claimService.GetUserClaim();
            var userId = claim.Id;

            var order = await _unitOfWork.Orders.GetAsync(x => x.Id == orderId && x.AccountId == userId);
            if (order == null)
            {
                return response.SetNotFound("Order not found or does not belong to the user.");
            }

            //if (order.Status == OrderStatus.Paid)
            //{
            //    return response.SetBadRequest("Cannot cancel a paid order.");
            //}

            //order.Status = OrderStatus.Canceled;
            await _unitOfWork.SaveChangeAsync();

            return response.SetOk("Order canceled successfully.");
        }
    }
}
