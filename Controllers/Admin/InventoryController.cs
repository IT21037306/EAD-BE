/*
 * File: InventoryController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of Admin Operations for Inventory
 */

using EAD_BE.Config.Vendor;
using EAD_BE.Models.User.Common;
using EAD_BE.Models.Vendor.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;

namespace EAD_BE.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class InventoryController : ControllerBase
    {
        private readonly MongoDbContextProduct _context;
        private readonly IMongoCollection<CategoryModel> _categoryCollection;
        private readonly UserManager<CustomUserModel> _userManager;

        // Constructor
        public InventoryController(MongoDbContextProduct context, IMongoCollection<CategoryModel> categoryCollection, UserManager<CustomUserModel> userManager)
        {
            _context = context;
            _categoryCollection = categoryCollection;
            _userManager = userManager;
        }
        
        // Display All User Products
        [HttpGet("admin/products")]
        public async Task<IActionResult> GetAllUserProducts()
        {
            try
            {
                var users = _userManager.Users.ToList();
                if (users == null || !users.Any())
                {
                    return NotFound(new { Message = "No users found" });
                }

                var allUserProducts = new List<object>();

                foreach (var user in users)
                {
                    var products = await _context.Products.Find(p => p.AddedByUserEmail.ToLower() == user.Email.ToLower()).ToListAsync();
                    var userProducts = new Dictionary<string, object>();

                    if (products != null && products.Any())
                    {
                        foreach (var product in products)
                        {
                            var category = await _categoryCollection.Find(c => c.Id == product.Category).FirstOrDefaultAsync();
                            var categoryName = category != null ? category.Name : "Uncategorized";
                            var categoryState = category != null && category.IsActive ? "active" : "inactive";

                            if (!userProducts.ContainsKey(categoryName))
                            {
                                userProducts[categoryName] = new
                                {
                                    status = categoryState,
                                    products = new List<object>()
                                };
                            }

                            ((List<object>)((dynamic)userProducts[categoryName]).products).Add(new
                            {
                                product.Id,
                                product.Name,
                                product.Description,
                                product.Price,
                                product.StockQuantity,
                                Category = category != null ? new { category.Id, category.Name } : null,
                                product.CreatedAt,
                                product.UpdatedAt
                            });
                        }
                    }

                    allUserProducts.Add(new
                    {
                        user = user.Email,
                        categories = userProducts
                    });
                }

                return Ok(allUserProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving products", Details = ex.Message });
            }
        }
    }
}