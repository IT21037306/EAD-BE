/*
 * File: PurchaseController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of CSR Operations for Purchases
 */


namespace EAD_BE.Controllers.CSR.Purchase
{
    using EAD_BE.Models.User.Purchased;
    using EAD_BE.Models.User.Checkout;
    using EAD_BE.Models.User.Common;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using MongoDB.Driver;
    using Microsoft.AspNetCore.Identity;
    using System;
    using System.Threading.Tasks;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize (Roles = "CSR,Admin,Vendor")]
    public class PurchaseController : ControllerBase
    {
        private readonly IMongoCollection<PurchaseModel> _purchaseCollection;
        private readonly UserManager<CustomUserModel> _userManager;

        // Constructor
        public PurchaseController(IMongoCollection<PurchaseModel> purchaseCollection, UserManager<CustomUserModel> userManager)
        {
            _purchaseCollection = purchaseCollection;
            _userManager = userManager;
        }

        // Update Shipping Status of a Purchase
        [HttpPut("update-shipping-status/{purchaseId}/{userEmail}")]
        public async Task<IActionResult> UpdateShippingStatus(Guid purchaseId, string userEmail)
        {
            // Fetch the current logged-in user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "User not logged in" });
            }

            // Check if the email matches the logged-in user's email
            if (currentUser.Email != userEmail)
            {
                return BadRequest(new { Message = "You are not authorized to update this purchase" });
            }

            // Fetch the purchase record based on the PurchaseId and UserEmail
            var filter = Builders<PurchaseModel>.Filter.And(
                Builders<PurchaseModel>.Filter.Eq(p => p.PurchaseId, purchaseId),
                Builders<PurchaseModel>.Filter.Eq(p => p.userEmail, currentUser.Email)
            );

            var update = Builders<PurchaseModel>.Update.Set(p => p.IsShipped, true);

            var result = await _purchaseCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                return NotFound(new { Message = "Purchase record not found or shipping status unchanged" });
            }

            return Ok(new { Message = "Shipping status updated to true successfully" });
        }
        
        // Cancel an order before dispatch
        [HttpPatch ("cancel-order/{purchaseId}")]
        public async Task<IActionResult> CancelOrder(Guid purchaseId)
        {
            var purchase = await _purchaseCollection.Find(p => p.PurchaseId == purchaseId).FirstOrDefaultAsync();
            if (purchase == null)
            {
                return NotFound(new { Message = "Purchase record not found" });
            }

            if (purchase.IsShipped)
            {
                return BadRequest(new { Message = "Order cannot be cancelled after shipping" });
            }

            if (purchase.IsDelivered)
            {
                return BadRequest(new { Message = "Order cannot be cancelled after delivery" });
            }

            if (purchase.requestToCancelOrder)
            {
                return BadRequest(new { Message = "Order is already requested to cancel" });
            }

            if (purchase.isOrderCancelled)
            {
                return BadRequest(new { Message = "Order is already cancelled" });
            }

            var filter = Builders<PurchaseModel>.Filter.Eq(p => p.PurchaseId, purchaseId);
            var update = Builders<PurchaseModel>.Update.Set(p => p.isOrderCancelled, true);

            var result = await _purchaseCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                return NotFound(new { Message = "Purchase record not found or order cancellation unchanged" });
            }

            return Ok(new { Message = "Order cancelled successfully" });
        }
        
        // View all orders requested to cancel
        [HttpGet("view-orders-requested-to-cancel/{userEmail}")]
        public async Task<IActionResult> GetAllOrdersRequestedToCancel(String userEmail)
        {
            try
            {
                // Fetch the current logged-in user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { Message = "User not logged in" });
                }

                // Check if the email matches the logged-in user's email
                if (currentUser.Email != userEmail)
                {
                    return BadRequest(new { Message = "You are not authorized to update this purchase" });
                }
                
                var filter = Builders<PurchaseModel>.Filter.Eq(p => p.requestToCancelOrder, true);
                var orders = await _purchaseCollection.Find(filter).ToListAsync();

                if (orders == null || !orders.Any())
                {
                    return NotFound(new { Message = "No orders found with cancel requests" });
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