using EAD_BE.Data;
using EAD_BE.Models.User.Common;
using EAD_BE.Models.Vendor.Product;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;

namespace EAD_BE.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
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
                    var products = await _context.Products.Find(p => p.AddedByUserId == user.Id).ToListAsync();
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