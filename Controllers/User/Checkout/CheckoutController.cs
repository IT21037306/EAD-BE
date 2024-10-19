/*
 * File: CheckoutController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of User Operations for Checkout
 */


using EAD_BE.Config.Vendor;
using EAD_BE.Models.User.Checkout;
using EAD_BE.Models.User.Cart;
using EAD_BE.Models.User.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;

namespace EAD_BE.Controllers.User.Checkout
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize (Roles = "User,Admin,Vendor,CSR")]
    public class CheckoutController : ControllerBase
    {
        private readonly IMongoCollection<Cart> _cartCollection;
        private readonly IMongoCollection<CheckoutModel> _checkoutCollection;
        private readonly UserManager<CustomUserModel> _userManager;
        private readonly MongoDbContextProduct _context;

        // Constructor
        public CheckoutController(IMongoCollection<Cart> cartCollection, IMongoCollection<CheckoutModel> checkoutCollection, UserManager<CustomUserModel> userManager, MongoDbContextProduct context)
        {
            _cartCollection = cartCollection;
            _checkoutCollection = checkoutCollection;
            _userManager = userManager;
            _context = context;
        }
        
        // Get All Checkouts
        [HttpGet("all-checkouts/{userEmail}")]
        public async Task<IActionResult> GetAllCheckouts(String userEmail)
        {
            // Fetch the current logged-in user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "User not logged in" });
            }
    
            if (currentUser.Email != userEmail)
            {
                return BadRequest(new { Message = "You are not authorized to perform this action" });
            }

            // Fetch all checkouts based on the UserEmail
            var checkouts = await _checkoutCollection.Find(c => c.UserEmail == currentUser.Email).ToListAsync();

            if (checkouts == null || !checkouts.Any())
            {
                return NotFound(new { Message = "No checkouts found for the specified user" });
            }
    
            // Get the checkouts
            var checkout = await _checkoutCollection.Find(c => c.UserEmail == userEmail).FirstOrDefaultAsync();
            if (checkout == null)
            {
                return NotFound(new { Message = "Checkout not found for the specified user" });
            }

            // Fetch product details for each item in the cart
            var checkoutItems = new List<object>();
            foreach (var item in checkout.Items)
            {
                var product = await _context.Products.Find(p => p.Id == item.ProductId).FirstOrDefaultAsync();
                if (product != null)
                {
                    checkoutItems.Add(new
                    {
                        item.ProductId,
                        item.ProductName,
                        item.Price,
                        item.Quantity,
                        product.ProductPicture 
                    });
                }
            }

            return Ok(new
            {
                checkout.UserEmail,
                checkout.CheckoutUuid,
                checkout.CreatedAt,
                Items = checkoutItems
            });
        }

        // Checkout items in Cart
        [HttpPost("checkout/{userEmail}")]
        public async Task<IActionResult> Checkout(String userEmail)
        {
            // Fetch the current logged-in user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "User not logged in" });
            }

            // Fetch the cart based on the UserEmail
            var cart = await _cartCollection.Find(c => c.UserEmail == currentUser.Email).FirstOrDefaultAsync();

            if (cart == null)
            {
                return NotFound(new { Message = "Cart not found" });
            }
            
            // Check if the email matches the logged-in user's email
            if (currentUser.Email != userEmail)
            {
                return BadRequest(new { Message = "You are not authorized to view this cart" });
            }

            // Create a new checkout object
            var checkout = new CheckoutModel
            {
                UserEmail = cart.UserEmail,
                Items = cart.Items.Select(i => new CheckoutItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList(),
                CreatedAt = DateTime.UtcNow,
                PurchaseStatus = "Pending",
                PaymentStatus = "Pending"
            };

            // Insert the checkout object into the checkout collection
            await _checkoutCollection.InsertOneAsync(checkout);

            // Remove the cart from the cart collection
            await _cartCollection.DeleteOneAsync(c => c.CartUuid == cart.CartUuid);

            return Ok(new { Message = "Checkout successful", CheckoutId = checkout.CheckoutUuid });
        }
        
        // Approve Payment Status
        [HttpPut("approve-payment-status/{checkoutUuid}/{userEmail}")]
        public async Task<IActionResult> UpdatePaymentStatus(Guid checkoutUuid, string userEmail)
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
                return BadRequest(new { Message = "You are not authorized to update this payment status" });
            }

            // Fetch the checkout record based on the CheckoutUuid and UserEmail
            var filter = Builders<CheckoutModel>.Filter.And(
                Builders<CheckoutModel>.Filter.Eq(c => c.CheckoutUuid, checkoutUuid),
                Builders<CheckoutModel>.Filter.Eq(c => c.UserEmail, userEmail)
            );
            var update = Builders<CheckoutModel>.Update
                .Set(c => c.PaymentStatus, "Paid")
                .Set(c => c.PurchaseStatus, "Purchased");

            var result = await _checkoutCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                return NotFound(new { Message = "Checkout record not found or payment status unchanged" });
            }

            return Ok(new { Message = "Payment status updated to Paid and purchase status updated to Purchased successfully" });
        }
        
        // Cancel Payment Status
        [HttpPut("cancel-payment-status/{checkoutUuid}/{userEmail}")]
        public async Task<IActionResult> CancelPaymentStatus(Guid checkoutUuid, string userEmail)
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
                return BadRequest(new { Message = "You are not authorized to update this payment status" });
            }

            // Fetch the checkout record based on the CheckoutUuid and UserEmail
            var filter = Builders<CheckoutModel>.Filter.And(
                Builders<CheckoutModel>.Filter.Eq(c => c.CheckoutUuid, checkoutUuid),
                Builders<CheckoutModel>.Filter.Eq(c => c.UserEmail, userEmail)
            );
            var update = Builders<CheckoutModel>.Update
                .Set(c => c.PaymentStatus, "Cancelled")
                .Set(c => c.PurchaseStatus, "Cancelled");

            var result = await _checkoutCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                return NotFound(new { Message = "Checkout record not found or payment status unchanged" });
            }

            return Ok(new { Message = "Payment status and purchase status updated to Cancelled successfully" });
        }
    }
}