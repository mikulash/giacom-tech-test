using Order.Data;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
        {
            var orders = await _orderRepository.GetOrdersAsync();
            return orders;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusIdAsync(Guid statusId)
        {
            var orders = await _orderRepository.GetOrdersByStatusIdAsync(statusId);
            return orders;
        }

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            return order;
        }

        public async Task<Result<bool>> UpdateOrderStatusAsync(Guid orderId, Guid statusId)
        {
            if (orderId == Guid.Empty)
            {
                return Result<bool>.BadRequest("Order ID cannot be empty.");
            }

            if (statusId == Guid.Empty)
            {
                return Result<bool>.BadRequest("Status ID cannot be empty.");
            }
            
            return await _orderRepository.UpdateOrderStatusAsync(orderId, statusId);
        }

        public async Task<Result<Guid>> CreateOrderAsync(CreateOrderDto order)
        {
            if (order.ResellerId == Guid.Empty)
            {
                return Result<Guid>.BadRequest("Reseller ID cannot be empty.");
            }

            if (order.CustomerId == Guid.Empty)
            {
                return Result<Guid>.BadRequest("Customer ID cannot be empty.");
            }

            if (order.StatusId == Guid.Empty)
            {
                return Result<Guid>.BadRequest("Status ID cannot be empty.");           
            }
            
            return await _orderRepository.CreateOrderAsync(order);
        }
    }
}
