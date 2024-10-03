using EAD_BE.Models.User.Common;
using EAD_BE.Models.Vendor.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EAD_BE.Controllers.Vendor
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Vendor,Admin")]
    public class CategoryController : ControllerBase
    {
        private readonly IMongoCollection<CategoryModel> _categoryCollection;
        private readonly UserManager<CustomApplicationUser> _userManager;

        public CategoryController(IMongoCollection<CategoryModel> categoryCollection, UserManager<CustomApplicationUser> userManager)
        {
            _categoryCollection = categoryCollection;
            _userManager = userManager;
        }

        [HttpPut("update-category-status/{id}")]
        public async Task<IActionResult> UpdateCategoryStatus(Guid id)
        {
            var category = await _categoryCollection.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (category == null)
            {
                return NotFound(new { Message = "Category not found" });
            }

            // Toggle the IsActive state
            var newIsActiveState = !category.IsActive;
            var update = Builders<CategoryModel>.Update.Set(c => c.IsActive, newIsActiveState);
            await _categoryCollection.UpdateOneAsync(c => c.Id == id, update);

            return Ok(new { Message = "Category status updated successfully", IsActive = newIsActiveState });
        }
        
        [HttpGet("all-categories/{userEmail}")]
        public async Task<IActionResult> GetAllCategories(String userEmail)
        {
            try
            {
                if (string.IsNullOrEmpty(userEmail))
                {
                    return NotFound(new { Message = "User email not found" });
                }
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { Message = "User not logged in" });
                }
            
                // Check if the email matches the logged-in user's email
                if (currentUser.Email != userEmail)
                {
                    return BadRequest(new { Message = "You are not authorized to view categories." });
                }


                var categories = await _categoryCollection.Find(_ => true).ToListAsync();
                if (categories == null || !categories.Any())
                {
                    return NotFound(new { Message = "No categories found" });
                }

                return Ok(new { UserEmail = userEmail, Categories = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving categories", Details = ex.Message });
            }
        }
    }
}