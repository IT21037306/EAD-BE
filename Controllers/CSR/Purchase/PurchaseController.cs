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
    [Authorize (Roles = "CSR,Admin")]
    public class PurchaseController : ControllerBase
    {
        private readonly IMongoCollection<PurchaseModel> _purchaseCollection;
        private readonly UserManager<CustomApplicationUser> _userManager;

        public PurchaseController(IMongoCollection<PurchaseModel> purchaseCollection, UserManager<CustomApplicationUser> userManager)
        {
            _purchaseCollection = purchaseCollection;
            _userManager = userManager;
        }

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
    }
}