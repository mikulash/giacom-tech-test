using Microsoft.EntityFrameworkCore;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Order.Data.Entities;

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

        public async Task<Result<IEnumerable<MonthlyProfit>>> GetMonthlyProfitAsync()
        {
            try
            {
                var completedStatusId = await _orderContext.Set<OrderStatus>()
                    .Where(s => s.Name.ToLower() == "completed")
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();
                
                var orderItemData = await _orderContext.OrderItem
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.Order.StatusId == completedStatusId)
                    .Where(oi => oi.Quantity.HasValue)
                    .Select(oi => new
                    {
                        oi.Order.CreatedDate,
                        ProfitOnItem = (oi.Product.UnitPrice - oi.Product.UnitCost) * oi.Quantity.Value
                    })
                    .ToListAsync(); 
                
                var monthlyProfits = orderItemData
                    .GroupBy(x => new { x.CreatedDate.Year, x.CreatedDate.Month })
                    .Select(g => new MonthlyProfit
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Profit = g.Sum(x => x.ProfitOnItem)
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();
                
                return Result<IEnumerable<MonthlyProfit>>.Success(monthlyProfits, $"Retrieved monthly profit data for {monthlyProfits.Count()} months.");
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<MonthlyProfit>>.BadRequest($"Error calculating monthly profit: {ex.Message}");
            } 
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

        public async Task<Result<Guid>> CreateOrderAsync(CreateOrderRequest newOrderRequest)
        {
            var resellerIdBytes = newOrderRequest.ResellerId.ToByteArray();
            var customerIdBytes = newOrderRequest.CustomerId.ToByteArray();
            var statusIdBytes = newOrderRequest.StatusId.ToByteArray();
            
            var statusExists = await _orderContext.OrderStatus.AnyAsync(s => _orderContext.Database.IsInMemory()
                ? s.Id.SequenceEqual(statusIdBytes)
                : s.Id == statusIdBytes);

            if (!statusExists)
            {
                return Result<Guid>.BadRequest("Status ID is not existing");
            }
            
            var order = new Data.Entities.Order {
                Id = Guid.NewGuid().ToByteArray(),
                ResellerId = resellerIdBytes,
                CustomerId = customerIdBytes,
                StatusId = statusIdBytes,
                CreatedDate = DateTime.UtcNow
            };

            _orderContext.Order.Add(order);
            
            await _orderContext.SaveChangesAsync();
            
            if (order.Id == null)
            {
                return Result<Guid>.BadRequest("Failed to create order.");
            }
            
            return Result<Guid>.Created(new Guid(order.Id));

        }
    }
}