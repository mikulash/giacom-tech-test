using System;

namespace Order.Model;

public class CreateOrderRequest
{
    public Guid ResellerId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid StatusId { get; set; }
}