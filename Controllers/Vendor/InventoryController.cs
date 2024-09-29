using EAD_BE.Data;
using EAD_BE.Models.User.Common;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using EAD_BE.Models.Vendor.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace EAD_BE.Controllers.Vendor
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Vendor,Admin")]
    public class InventoryController : ControllerBase
    {
        private readonly MongoDbContextProduct _context;
        private readonly IMongoCollection<CategoryModel> _categoryCollection;
        private readonly UserManager<CustomApplicationUser> _userManager;

        public InventoryController(MongoDbContextProduct context, IMongoCollection<CategoryModel> categoryCollection, UserManager<CustomApplicationUser> userManager)
        {
            _context = context;
            _categoryCollection = categoryCollection;
            _userManager = userManager;
        }

        [HttpGet("user/{userId}/products")]
        public async Task<IActionResult> GetUserProducts(Guid userId)
        {
            try
            {
                var products = await _context.Products.Find(p => p.AddedByUserId == userId).ToListAsync();
                if (products == null || !products.Any())
                {
                    return NotFound(new { Message = "No products found for the specified user" });
                }

                var userProducts = new Dictionary<string, object>();

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

                return Ok(userProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving products", Details = ex.Message });
            }
        }
        
    }
}