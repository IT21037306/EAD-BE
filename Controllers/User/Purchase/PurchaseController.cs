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

        public PurchaseController(IMongoCollection<PurchaseModel> purchaseCollection, IMongoCollection<CheckoutModel> checkoutCollection, UserManager<CustomApplicationUser> userManager)
        {
            _purchaseCollection = purchaseCollection;
            _checkoutCollection = checkoutCollection;
            _userManager = userManager;
        }

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
            if (checkout.PaymentStatus != "Paid" )
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
                IsDelivered = false
            };

            // Insert the purchase object into the purchase collection
            await _purchaseCollection.InsertOneAsync(purchase);
            
            // Delete the checkout object
            await _checkoutCollection.DeleteOneAsync(c => c.CheckoutUuid == checkoutUuid);

            return Ok(new { Message = "Item/s has been purchased successfully", PurchaseId = purchase.PurchaseId });
        }
        
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

    }
}