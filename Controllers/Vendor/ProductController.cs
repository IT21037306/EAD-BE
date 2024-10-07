/*
 * File: ProductController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of Vendor Operations for Products
 */


using EAD_BE.Config.Vendor;
using EAD_BE.Models.Vendor.Product;
using EAD_BE.Models.User.Cart;
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
        private readonly IMongoCollection<Cart> _cartCollection;
        

        // Constructor
        public ProductController(MongoDbContextProduct context, UserManager<CustomApplicationUser> userManager, IMongoCollection<CategoryModel> categoryCollection, IMongoCollection<Cart> cartCollection)
        {
            _context = context;
            _userManager = userManager;
            _categoryCollection = categoryCollection;
            _cartCollection = cartCollection;
        }

        // Add a new product
        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct([FromBody] ProductModel product)
        {
            if (product == null)
            {
                return BadRequest(new { Message = "Product data is required" });
            }
            
            if (string.IsNullOrEmpty(product.Name))
            {
                return BadRequest(new{Message = "Product name is required"});
            }

            if (string.IsNullOrEmpty(product.Description))
            {
                return BadRequest(new{Message = "Product description is required"});
            }
            
            if (string.IsNullOrEmpty(product.ProductPicture))
            {
                return BadRequest(new{Message = "Product picture is required"});
            }
            
            if (product.Price == default)
            {
                return BadRequest(new{Message = "Product price is required"});
            }
            
            if (product.StockQuantity == default)
            {
                return BadRequest(new{Message = "Product stock quantity is required"});
            }
            
            if (product.Category == default)
            {
                return BadRequest(new{Message = "Product category is required"});
            }

            if (string.IsNullOrEmpty(product.AddedByUserEmail))
            {
                return BadRequest(new { Message = "User email is required" });
            }

            var user = await _userManager.FindByEmailAsync(product.AddedByUserEmail);
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
        
        // Update an existing product
        [HttpPut("update-product/{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductModel product)
        {
            if (product == null)
            {
                return BadRequest(new { Message = "Product data is required" });
            }

            if (string.IsNullOrEmpty(product.AddedByUserEmail))
            {
                return BadRequest(new { Message = "User email is required" });
            }

            var user = await _userManager.FindByEmailAsync(product.AddedByUserEmail);
            if (user == null)
            {
                return BadRequest(new { Message = "User does not exist" });
            }

            var existingProduct = await _context.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existingProduct == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var isVendor = existingProduct.AddedByUserEmail == currentUser.Email;

            if (!isAdmin && !isVendor)
            {
                return BadRequest(new { Message = "You do not have permission to update this product" });
            }

            existingProduct.Name = product.Name != default ? product.Name : existingProduct.Name;
            existingProduct.Description = product.Description != default ? product.Description : existingProduct.Description;
            existingProduct.Price = product.Price != default ? product.Price : existingProduct.Price;
            existingProduct.StockQuantity = product.StockQuantity != default ? product.StockQuantity : existingProduct.StockQuantity;
            existingProduct.Category = product.Category != default ? product.Category : existingProduct.Category;
            existingProduct.UpdatedAt = DateTime.UtcNow;
            existingProduct.AddedByUserEmail = product.AddedByUserEmail;
            existingProduct.ProductPicture = product.ProductPicture != default ? product.ProductPicture : existingProduct.ProductPicture; ;


            await _context.Products.ReplaceOneAsync(p => p.Id == id, existingProduct);

            return Ok(new { Message = "Product updated successfully", ProductId = existingProduct.Id });
        }
        
        // Delete an existing product
        [HttpDelete("delete-product/{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var existingProduct = await _context.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existingProduct == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var isVendor = existingProduct.AddedByUserEmail == currentUser.Email;

            if (!isAdmin && !isVendor)
            {
                return BadRequest(new { Message = "You do not have permission to delete this product" });
            }

            // Check if the product exists in any cart's items array
            var productInCart = await _cartCollection.Find(c => c.Items.Any(i => i.ProductId == id)).FirstOrDefaultAsync();
            if (productInCart != null)
            {
                return BadRequest(new { Message = "Product exists in a cart and cannot be deleted" });
            }

            await _context.Products.DeleteOneAsync(p => p.Id == id);

            return Ok(new { Message = "Product deleted successfully" });
        }
        
        
    }
    
    
}