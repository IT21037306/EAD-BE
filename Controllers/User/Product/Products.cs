using EAD_BE.Data;
using EAD_BE.Models.User.Common;
using EAD_BE.Models.Vendor.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EAD_BE.Controllers.User.Product;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User,Admin,Vendor,CSR")]
public class Products : ControllerBase
{
    private readonly MongoDbContextProduct _context;
    private readonly IMongoCollection<CategoryModel> _categoryCollection;

    public Products(MongoDbContextProduct context , IMongoCollection<CategoryModel> categoryCollection)
    {
        _context = context;
        _categoryCollection = categoryCollection;
    }

    [HttpGet("all-products")]
    public async Task<IActionResult> GetAllProducts()
    {
        try
        {
            var products = await _context.Products.Find(_ => true).ToListAsync();
            if (products == null || !products.Any())
            {
                return NotFound(new { Message = "No products found" });
            }

            var productWithCategoryDetails = new List<object>();

            foreach (var product in products)
            {
                var category = await _categoryCollection.Find(c => c.Id == product.Category && c.IsActive).FirstOrDefaultAsync();
                if (category != null)
                {
                    productWithCategoryDetails.Add(new
                    {
                        product.Id,
                        product.Name,
                        product.Description,
                        product.Price,
                        product.StockQuantity,
                        Category = new { category.Id, category.Name },
                        product.CreatedAt,
                        product.UpdatedAt,
                        AddedByUserEmail = product.AddedByUserEmail
                    });
                }
            }

            if (!productWithCategoryDetails.Any())
            {
                return NotFound(new { Message = "No products found with active categories" });
            }

            return Ok(productWithCategoryDetails);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while retrieving products", Details = ex.Message });
        }
    }
}