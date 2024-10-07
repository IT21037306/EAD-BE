/*
 * File: PurchaseController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of User Operations for Purchases
 */


using EAD_BE.Config.Vendor;
using EAD_BE.Models.User.Purchased;
using EAD_BE.Models.User.Checkout;
using EAD_BE.Models.User.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;

namespace EAD_BE.Controllers.User.Purchase
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize (Roles = "User,Admin,Vendor,CSR")]
    public class PurchaseController : ControllerBase
    {
        private readonly IMongoCollection<PurchaseModel> _purchaseCollection;
        private readonly IMongoCollection<CheckoutModel> _checkoutCollection;
        private readonly UserManager<CustomApplicationUser> _userManager;
        private readonly MongoDbContextProduct _productCollection;

        // Constructor
        public PurchaseController(IMongoCollection<PurchaseModel> purchaseCollection, IMongoCollection<CheckoutModel> checkoutCollection, UserManager<CustomApplicationUser> userManager, MongoDbContextProduct productCollection)
        {
            _purchaseCollection = purchaseCollection;
            _checkoutCollection = checkoutCollection;
            _userManager = userManager;
            _productCollection = productCollection;
        }

        // Add Checkout items to Purchase Table
        [HttpPost("add-to-purchase/{checkoutUuid}/{userEmail}")]
        public async Task<IActionResult> AddToPurchaseTable(Guid checkoutUuid, string userEmail)
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
                return BadRequest(new { Message = "You are not authorized to add this checkout to purchase table" });
            }

            // Fetch the checkout record based on the CheckoutUuid and UserEmail
            var checkout = await _checkoutCollection.Find(c => c.CheckoutUuid == checkoutUuid && c.UserEmail == userEmail).FirstOrDefaultAsync();
            if (checkout == null)
            {
                return NotFound(new { Message = "Checkout record not found" });
            }
            
            // Check if the payment status is "Paid"
            if (checkout.PaymentStatus != "Paid")
            {
                return BadRequest(new { Message = "Payment not completed. Please complete the payment first." });
            }

            // Check if the purchase status is "Purchased"
            if (checkout.PurchaseStatus != "Purchased")
            {
                return BadRequest(new { Message = "Purchase not completed. Please complete the purchase first." });
            }

            // Map CheckoutModel to PurchaseModel
            var purchase = new PurchaseModel
            {
                PurchaseId = Guid.NewGuid(),
                userEmail = currentUser.Email,
                PurchaseDate = DateTime.UtcNow,
                Items = checkout.Items.Select(i => new PurchasedItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList(),
                IsShipped = false,
                IsDelivered = false,
                isOrderCancelled = false,
                UserDetails = null,
                IsUserDataAvailable = false
            };
            

            // Insert the purchase object into the purchase collection
            await _purchaseCollection.InsertOneAsync(purchase);
            
            // Delete the checkout object
            await _checkoutCollection.DeleteOneAsync(c => c.CheckoutUuid == checkoutUuid);

            return Ok(new { Message = "Item/s has been purchased successfully", PurchaseId = purchase.PurchaseId });
        }
        
        // Update Delivery Status
        [HttpPut("update-delivery-status/{purchaseId}/{userEmail}")]
        public async Task<IActionResult> UpdateDeliveryStatus(Guid purchaseId, string userEmail)
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
            
            var purchase = await _purchaseCollection.Find(p => p.PurchaseId == purchaseId).FirstOrDefaultAsync();

            if (purchase.isOrderCancelled)
            {
                return BadRequest(new {Message = "Delivery status cannot be updated after order cancellation"});
            }

            // Fetch the purchase record based on the PurchaseId and UserEmail
            var filter = Builders<PurchaseModel>.Filter.And(
                Builders<PurchaseModel>.Filter.Eq(p => p.PurchaseId, purchaseId),
                Builders<PurchaseModel>.Filter.Eq(p => p.userEmail, currentUser.Email)
            );

            var update = Builders<PurchaseModel>.Update.Set(p => p.IsDelivered, true);

            var result = await _purchaseCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                return NotFound(new { Message = "Purchase record not found or delivery status unchanged" });
            }

            return Ok(new { Message = "Delivery status updated successfully" });
        }
        
        // Add User Details to Shipping
        [HttpPut("add-user-details/{purchaseId}")]
        public async Task<IActionResult> AddUserDetails(Guid purchaseId, [FromBody] UserData userDetails)
        {
            if (userDetails == null || string.IsNullOrEmpty(userDetails.UserName) || string.IsNullOrEmpty(userDetails.UserPhoneNumber) || string.IsNullOrEmpty(userDetails.UserAddress))
            {
                return BadRequest(new { Message = "User details are required" });
            }

            var purchase = await _purchaseCollection.Find(p => p.PurchaseId == purchaseId).FirstOrDefaultAsync();
            if (purchase == null)
            {
                return NotFound(new { Message = "Purchase record not found" });
            }

            if (purchase.IsShipped)
            {
                return BadRequest(new {Message = "User details cannot be added after shipping"});
            }

            if (purchase.IsDelivered)
            {
                return BadRequest(new {Message = "User details cannot be added after delivery"});
            }
            

            if (purchase.IsUserDataAvailable)
            {
                return BadRequest(new { Message = "User details are already available" });
            }

            if (purchase.isOrderCancelled)
            {
                return BadRequest(new {Message  = "User details cannot be added after order cancellation"});
            }

            var filter = Builders<PurchaseModel>.Filter.Eq(p => p.PurchaseId, purchaseId);
            var update = Builders<PurchaseModel>.Update
                .Set(p => p.UserDetails, userDetails)
                .Set(p => p.IsUserDataAvailable, true);

            var result = await _purchaseCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                return NotFound(new { Message = "Purchase record not found or user details unchanged" });
            }

            return Ok(new { Message = "User details added successfully" });
        }
        
        // Update User Details to Shipping
        [HttpPut ("update-user-details/{purchaseId}")]
        public async Task<IActionResult> UpdateUserDetails(Guid purchaseId, [FromBody] UserData userDetails)
        {
            if (userDetails == null || string.IsNullOrEmpty(userDetails.UserName) || string.IsNullOrEmpty(userDetails.UserPhoneNumber) || string.IsNullOrEmpty(userDetails.UserAddress))
            {
                return BadRequest(new { Message = "User details are required" });
            }

            var purchase = await _purchaseCollection.Find(p => p.PurchaseId == purchaseId).FirstOrDefaultAsync();
            if (purchase == null)
            {
                return NotFound(new { Message = "Purchase record not found" });
            }

            if (purchase.IsShipped)
            {
                return BadRequest(new { Message = "User details cannot be updated after shipping" });
            }

            if (purchase.IsDelivered)
            {
                return BadRequest(new { Message = "User details cannot be updated after delivery" });
            }

            if (purchase.isOrderCancelled)
            {
                return BadRequest(new {Message  = "User details cannot be updated after order cancellation"});
            }

            var filter = Builders<PurchaseModel>.Filter.Eq(p => p.PurchaseId, purchaseId);
            var update = Builders<PurchaseModel>.Update
                .Set(p => p.UserDetails, userDetails)
                .Set(p => p.IsUserDataAvailable, true);

            var result = await _purchaseCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                return NotFound(new { Message = "Purchase record not found or user details unchanged" });
            }

            return Ok(new { Message = "User details updated successfully" });
        }
        
        // Request to Cancel Order
        [HttpPatch ("request-cancel-order/{purchaseId}")]
        public async Task<IActionResult> CancelOrder(Guid purchaseId)
        {
            var purchase = await _purchaseCollection.Find(p => p.PurchaseId == purchaseId).FirstOrDefaultAsync();
            if (purchase == null)
            {
                return NotFound(new { Message = "Purchase record not found" });
            }

            if (purchase.IsShipped)
            {
                return BadRequest(new { Message = "Order cancel request can't be made after shipping" });
            }

            if (purchase.IsDelivered)
            {
                return BadRequest(new { Message = "Order cancel request can't be made after delivery" });
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
            var update = Builders<PurchaseModel>.Update.Set(p => p.requestToCancelOrder, true);

            var result = await _purchaseCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                return NotFound(new { Message = "Purchase record not found or order cancellation unchanged" });
            }

            return Ok(new { Message = "Order cancel request has been made successfully" });
        }
        
        

    }
}