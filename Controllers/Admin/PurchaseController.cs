/*
 * File: PurchaseController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of Admin Operations for Orders
 */

using EAD_BE.Config.Vendor;
using EAD_BE.Models.User.Checkout;
using EAD_BE.Models.User.Common;
using EAD_BE.Models.User.Purchased;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EAD_BE.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PurchaseController : ControllerBase
    {
        private readonly IMongoCollection<PurchaseModel> _purchaseCollection;
        private readonly IMongoCollection<CheckoutModel> _checkoutCollection;
        private readonly UserManager<CustomUserModel> _userManager;
        private readonly MongoDbContextProduct _productCollection;

        // Constructor
        public PurchaseController(IMongoCollection<PurchaseModel> purchaseCollection, IMongoCollection<CheckoutModel> checkoutCollection, UserManager<CustomUserModel> userManager, MongoDbContextProduct productCollection)
        {
            _purchaseCollection = purchaseCollection;
            _checkoutCollection = checkoutCollection;
            _userManager = userManager;
            _productCollection = productCollection;
        }
        
        // Get All Purchases
        [HttpGet("all-purchases/admin")]
        public async Task<IActionResult> GetAllPurchases()
        {
            // Fetch the current logged-in user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "User not logged in" });
            }

            // Fetch all purchases
            var purchases = await _purchaseCollection.Find(_ => true).ToListAsync();

            if (purchases == null || !purchases.Any())
            {
                return NotFound(new { Message = "No purchases found" });
            }

            return Ok(purchases);
        }
    }
}
