using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order.Service;
using System;
using System.Net;
using System.Threading.Tasks;
using Order.Model;

namespace OrderService.WebAPI.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
        }
        
        [HttpGet("by-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetOrdersByStatus([FromQuery] Guid statusId)
        {
            if (statusId == Guid.Empty)
            {
                return BadRequest("Order status ID cannot be empty.");
            }
            
            var orders = await _orderService.GetOrdersByStatusIdAsync(statusId);
            return Ok(orders);
        }
        
        

        [HttpGet("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                return Ok(order);
            }
            else
            {
                return NotFound();
            }
        }
        
        [HttpPatch("{orderId}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            Console.WriteLine("PATCH STATUS, orderID: " + orderId + ", statusId: " + request.StatusId);
            Console.WriteLine("PATCH STATUS, REQUEST: " + request);
            
            if (orderId == Guid.Empty)
            {
                return BadRequest("Order ID cannot be empty.");
            }

            if (request.StatusId == Guid.Empty)
            {
                return BadRequest("Valid status ID is required.");
            }

            var result = await _orderService.UpdateOrderStatusAsync(orderId, request.StatusId);
            
            return result.StatusCode switch
            {
                HttpStatusCode.NoContent => NoContent(),
                HttpStatusCode.NotFound => NotFound(result.Message),
                HttpStatusCode.BadRequest => BadRequest(result.Message),
                _ => StatusCode(500, "An unexpected error occurred.")
            };
        }
        
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderService.CreateOrderAsync(request);
    
            return result.StatusCode switch
            {
                HttpStatusCode.Created => CreatedAtAction(nameof(GetOrderById), new {orderId = result.Data}, result.Data),
                HttpStatusCode.BadRequest => BadRequest(result.Message),
                _ => StatusCode(500, "An unexpected error occurred.")
            };
        }
        
        [HttpGet("profit/monthly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMonthlyProfit([FromQuery] int? year = null)
        {
            var result = await _orderService.GetMonthlyProfitAsync();
    
            return result.StatusCode switch
            {
                HttpStatusCode.OK => Ok(result.Data),
                HttpStatusCode.BadRequest => BadRequest(result.Message),
                _ => StatusCode(500, "An unexpected error occurred.")
            };
        }
    }
}
