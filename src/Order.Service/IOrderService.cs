using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Service
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderSummary>> GetOrdersAsync();
        Task<IEnumerable<OrderSummary>> GetOrdersByStatusIdAsync(Guid statusId);
        
        Task<OrderDetail> GetOrderByIdAsync(Guid orderId);
        Task<Result<bool>> UpdateOrderStatusAsync(Guid orderId, Guid statusId);
        Task<Result<Guid>> CreateOrderAsync(CreateOrderDto order);
    }
}
