/*
 * File: ProductController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of User Operations for Products
 */


using EAD_BE.Config.Vendor;
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
public class ProductController : ControllerBase
{
    private readonly MongoDbContextProduct _context;
    private readonly IMongoCollection<CategoryModel> _categoryCollection;
    private readonly UserManager<CustomUserModel> _userManager;

    // Constructor
    public ProductController(MongoDbContextProduct context , IMongoCollection<CategoryModel> categoryCollection, UserManager<CustomUserModel> userManager)
    {
        _context = context;
        _categoryCollection = categoryCollection;
        _userManager = userManager;
    }

    // Display All Products
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
                        product.AddedByUserEmail,
                        product.ProductPicture
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
    
    // Display Products by Category Name
    [HttpGet("products-by-category/{categoryName}")]
    public async Task<IActionResult> GetProductsByCategoryName(string categoryName)
    {
        try
        {
            // Fetch the category to ensure it exists and is active
            var category = await _categoryCollection.Find(c => c.Name == categoryName && c.IsActive).FirstOrDefaultAsync();
            if (category == null)
            {
                return NotFound(new { Message = "Category not found or inactive" });
            }

            // Fetch products based on the category ID
            var products = await _context.Products.Find(p => p.Category == category.Id).ToListAsync();
            if (products == null || !products.Any())
            {
                return NotFound(new { Message = "No products found for the specified category" });
            }

            var productWithCategoryDetails = products.Select(product => new
            {
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                Category = new { category.Id, category.Name },
                product.CreatedAt,
                product.UpdatedAt,
                product.AddedByUserEmail,
                product.ProductPicture
            }).ToList();

            return Ok(productWithCategoryDetails);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while retrieving products", Details = ex.Message });
        }
    }
    
        // Rate a product
        [HttpPost("rate-product/{productId}")]
        public async Task<IActionResult> RateProduct(Guid productId, [FromBody] int rating)
        {
            if (rating < 1 || rating > 5)
            {
                return BadRequest(new { Message = "Rating must be between 1 and 5" });
            }

            var existingProduct = await _context.Products.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (existingProduct == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            // Update the product's rating and rating count
            existingProduct.Ranking = ((existingProduct.Ranking * existingProduct.RankingCount) + rating) / (existingProduct.RankingCount + 1);
            existingProduct.RankingCount += 1;

            await _context.Products.ReplaceOneAsync(p => p.Id == productId, existingProduct);

            return Ok(new { Message = "Product rated successfully" });
        }
        
        // Add a comment to a product
        [HttpPost("add-comment/{productId}")]
        public async Task<IActionResult> AddComment(Guid productId, [FromBody] Comment comment)
        {
            if (string.IsNullOrEmpty(comment.UserEmail) || string.IsNullOrEmpty(comment.Text))
            {
                return BadRequest(new { Message = "User email and comment text are required" });
            }
            
            var currentUser = await _userManager.GetUserAsync(User);
            
            var user = await _userManager.FindByEmailAsync(comment.UserEmail);
            if (user == null)
            {
                return BadRequest(new { Message = "User does not exist" });
            }
            
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "User not logged in" });
            }
            
            // Check if the email matches the logged-in user's email
            if (currentUser.Email != comment.UserEmail)
            {
                return BadRequest(new { Message = "You are not authorized to add comment to this product." });
            }

            var existingProduct = await _context.Products.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (existingProduct == null)
            {
                return NotFound(new { Message = "Product not found" });
            }
            
            comment.commentID = Guid.NewGuid();
            comment.CreatedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;
            existingProduct.Comments.Add(comment);

            await _context.Products.ReplaceOneAsync(p => p.Id == productId, existingProduct);

            return Ok(new { Message = "Comment added successfully" });
        }
        
        // Update a comment
        [HttpPut("update-comment/{productId}")]
        public async Task<IActionResult> UpdateComment(Guid productId, Guid commentId,[FromBody] Comment updatedComment)
        {
            if (string.IsNullOrEmpty(updatedComment.Text))
            {
                return BadRequest(new { Message = "Comment text is required" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "User not logged in" });
            }

            if (currentUser.Email != updatedComment.UserEmail)
            {
                return BadRequest(new { Message = "You are not authorized to update this comment." });
            }

            var existingProduct = await _context.Products.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (existingProduct == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            var comment = existingProduct.Comments.FirstOrDefault(c => c.commentID == commentId && c.UserEmail == updatedComment.UserEmail);
            if (comment == null)
            {
                return NotFound(new { Message = "Comment not found" });
            }
            
            comment.Text = updatedComment.Text;
            comment.CreatedAt = comment.CreatedAt;
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.Products.ReplaceOneAsync(p => p.Id == productId, existingProduct);

            return Ok(new { Message = "Comment updated successfully" });
        }
        // Delete a comment
        [HttpDelete("delete-comment/{productId}/{commentId}/{userEmail}")]
        public async Task<IActionResult> DeleteComment(Guid productId, Guid commentId, string userEmail)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { Message = "User not logged in" });
            }

            if (currentUser.Email != userEmail)
            {
                return BadRequest(new { Message = "You are not authorized to delete this comment." });
            }

            var existingProduct = await _context.Products.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (existingProduct == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            var comment = existingProduct.Comments.FirstOrDefault(c => c.commentID == commentId && c.UserEmail == userEmail);
            if (comment == null)
            {
                return NotFound(new { Message = "Comment not found" });
            }

            existingProduct.Comments.Remove(comment);

            await _context.Products.ReplaceOneAsync(p => p.Id == productId, existingProduct);

            return Ok(new { Message = "Comment deleted successfully" });
        }
}