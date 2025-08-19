using System;
using System.ComponentModel.DataAnnotations;

namespace Order.Model;

public class UpdateOrderStatusRequest
{
    [Required]
    public Guid StatusId { get; set; }
}