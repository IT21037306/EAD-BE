/*
 * File: OrderController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of Admin Operations for Orders
 */


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.Threading.Tasks;
using EAD_BE.Models.User.Cart;
using EAD_BE.Models.User.Checkout;

namespace EAD_BE.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize (Roles = "Admin")]
    public class OrderController : ControllerBase
    {
        private readonly IMongoCollection<CheckoutModel> _orderCollection;

        public OrderController(IMongoCollection<CheckoutModel> orderCollection)
        {
            _orderCollection = orderCollection;
        }

        [HttpGet("all-orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderCollection.Find(_ => true).ToListAsync();
                if (orders == null || !orders.Any())
                {
                    return NotFound(new { Message = "No orders found" });
                }
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving orders", Details = ex.Message });
            }
        }
        
    }
    
}