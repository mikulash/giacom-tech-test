using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Data
{
    public interface IOrderRepository
    {
        Task<IEnumerable<OrderSummary>> GetOrdersAsync();
        Task<IEnumerable<OrderSummary>> GetOrdersByStatusIdAsync(Guid statusId);

        Task<OrderDetail> GetOrderByIdAsync(Guid orderId);
        Task<Result<bool>> UpdateOrderStatusAsync(Guid orderId, Guid statusId);
    }
}
