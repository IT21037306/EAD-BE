/*
 * File: CategoryController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of Admin Operations for Categories
 */


using EAD_BE.Models.User.Common;
using EAD_BE.Models.Vendor.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EAD_BE.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
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
        
    }
}