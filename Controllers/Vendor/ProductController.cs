using EAD_BE.Models.Vendor.Product;
using EAD_BE.Data;
using EAD_BE.Models.User.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EAD_BE.Controllers.Vendor
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Vendor,Admin")]
    public class ProductController : ControllerBase
    {
        private readonly MongoDbContextProduct _context;
        private readonly UserManager<CustomApplicationUser> _userManager;
        private readonly IMongoCollection<CategoryModel> _categoryCollection;
        

        public ProductController(MongoDbContextProduct context, UserManager<CustomApplicationUser> userManager, IMongoCollection<CategoryModel> categoryCollection)
        {
            _context = context;
            _userManager = userManager;
            _categoryCollection = categoryCollection;
        }

        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct([FromBody] ProductModel product)
        {
            if (product == null)
            {
                return BadRequest(new { Message = "Product data is required" });
            }

            if (product.AddedByUserId == Guid.Empty)
            {
                return BadRequest(new { Message = "User ID is required" });
            }

            var user = await _userManager.FindByIdAsync(product.AddedByUserId.ToString());
            if (user == null)
            {
                return BadRequest(new { Message = "User does not exist" });
            }
            

            // Check if the category exists in the database
            var category = await _categoryCollection.Find(c => c.Id == product.Category).FirstOrDefaultAsync();
            if (category == null)
            {
                return BadRequest(new { Message = "Category does not exist" });
            }

            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.Products.InsertOneAsync(product);

            return Ok(new { Message = "Product added successfully", ProductId = product.Id });
        }
        
        [HttpPut("update-product/{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductModel product)
        {
            if (product == null)
            {
                return BadRequest(new { Message = "Product data is required" });
            }

            if (product.AddedByUserId == Guid.Empty)
            {
                return BadRequest(new { Message = "User ID is required" });
            }

            var user = await _userManager.FindByIdAsync(product.AddedByUserId.ToString());
            if (user == null)
            {
                return BadRequest(new { Message = "User does not exist" });
            }

            var existingProduct = await _context.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existingProduct == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Category = product.Category;
            existingProduct.UpdatedAt = DateTime.UtcNow;
            existingProduct.AddedByUserId = product.AddedByUserId;

            await _context.Products.ReplaceOneAsync(p => p.Id == id, existingProduct);

            return Ok(new { Message = "Product updated successfully", ProductId = existingProduct.Id });
        }
        
        [HttpDelete("delete-product/{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var existingProduct = await _context.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existingProduct == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            await _context.Products.DeleteOneAsync(p => p.Id == id);

            return Ok(new { Message = "Product deleted successfully" });
        }
        
    }
}