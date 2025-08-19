using Microsoft.EntityFrameworkCore;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Data
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderContext _orderContext;

        public OrderRepository(OrderContext orderContext)
        {
            _orderContext = orderContext;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
        {
            var orders = await _orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .Select(x => new OrderSummary
                {
                    Id = new Guid(x.Id),
                    ResellerId = new Guid(x.ResellerId),
                    CustomerId = new Guid(x.CustomerId),
                    StatusId = new Guid(x.StatusId),
                    StatusName = x.Status.Name,
                    ItemCount = x.Items.Count,
                    TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                    TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                    CreatedDate = x.CreatedDate
                })
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return orders;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusIdAsync(Guid statusId)
        {
           var statusIdBytes = statusId.ToByteArray();
               
           var orders = await _orderContext.Order
               .Where(x => _orderContext.Database.IsInMemory() ? x.StatusId.SequenceEqual(statusIdBytes) : x.StatusId == statusIdBytes)
               .Include(x => x.Items)
               .Include(x => x.Status)
               .Select(x => new OrderSummary
               {
                   Id = new Guid(x.Id),
                   ResellerId = new Guid(x.ResellerId),
                   CustomerId = new Guid(x.CustomerId),
                   StatusId = new Guid(x.StatusId),
                   StatusName = x.Status.Name,
                   ItemCount = x.Items.Count,
                   TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                   TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                   CreatedDate = x.CreatedDate
               })
               .OrderByDescending(x => x.CreatedDate)
               .ToListAsync();

           return orders;
        }

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var orderIdBytes = orderId.ToByteArray();

            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory()
                    ? x.Id.SequenceEqual(orderIdBytes)
                    : x.Id == orderIdBytes)
                .Select(x => new OrderDetail
                {
                    Id = new Guid(x.Id),
                    ResellerId = new Guid(x.ResellerId),
                    CustomerId = new Guid(x.CustomerId),
                    StatusId = new Guid(x.StatusId),
                    StatusName = x.Status.Name,
                    CreatedDate = x.CreatedDate,
                    TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                    TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                    Items = x.Items.Select(i => new Model.OrderItem
                    {
                        Id = new Guid(i.Id),
                        OrderId = new Guid(i.OrderId),
                        ServiceId = new Guid(i.ServiceId),
                        ServiceName = i.Service.Name,
                        ProductId = new Guid(i.ProductId),
                        ProductName = i.Product.Name,
                        UnitCost = i.Product.UnitCost,
                        UnitPrice = i.Product.UnitPrice,
                        TotalCost = i.Product.UnitCost * i.Quantity.Value,
                        TotalPrice = i.Product.UnitPrice * i.Quantity.Value,
                        Quantity = i.Quantity.Value
                    })
                }).SingleOrDefaultAsync();

            return order;
        }

        public async Task<Result<bool>> UpdateOrderStatusAsync(Guid orderId, Guid statusId)
        {
            var orderIdBytes = orderId.ToByteArray();
            var statusIdBytes = statusId.ToByteArray();

            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory()
                    ? x.Id.SequenceEqual(orderIdBytes)
                    : x.Id == orderIdBytes)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return Result<bool>.NotFound("Order not found.");
            }
            
            var statusExists = await _orderContext.OrderStatus.AnyAsync(s => _orderContext.Database.IsInMemory()
                    ? s.Id.SequenceEqual(statusIdBytes)
                    : s.Id == statusIdBytes);

            if (!statusExists)
            {
                return Result<bool>.BadRequest("Status ID is not existing");
            }

            order.StatusId = statusIdBytes;

            try
            {
                await _orderContext.SaveChangesAsync();
                return Result<bool>.NoContent();
            }
            catch (Exception ex)
            {
                return Result<bool>.BadRequest(ex.Message);
            }
        }
    }
}