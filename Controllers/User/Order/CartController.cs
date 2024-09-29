using EAD_BE.Data;
using EAD_BE.Models.User.Cart;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;

namespace EAD_BE.Controllers.User.Order
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize (Roles = "User,Admin,Vendor,CSR")]
    public class CartController : ControllerBase
    {
        private readonly IMongoCollection<Cart> _cartCollection;
        private readonly MongoDbContextProduct _context;

        public CartController(IMongoCollection<Cart> cartCollection , MongoDbContextProduct context)
        {
            _cartCollection = cartCollection;
            _context = context;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(Guid userId, [FromBody] CartItemInput cartItemInput)
        {
            if (cartItemInput == null || cartItemInput.ProductId == Guid.Empty || cartItemInput.Quantity <= 0)
            {
                return BadRequest(new { Message = "Invalid product details" });
            }

            // Fetch the product details from the product collection
            var product = await _context.Products.Find(p => p.Id == cartItemInput.ProductId).FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            // Check if the product has enough stock
            if (product.StockQuantity < cartItemInput.Quantity)
            {
                return BadRequest(new { Message = "Insufficient stock for the product" });
            }

            // Fetch the cart based on the UserId
            var cart = await _cartCollection.Find(c => c.UserId == userId).FirstOrDefaultAsync();

            // If no cart exists, create a new one
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CartUuid = Guid.NewGuid(), // Ensure CartUuid is set
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Items = new List<CartItem>() // Initialize the Items list
                };
            }

            // Find if the product already exists in the cart
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == cartItemInput.ProductId);

            if (existingItem != null)
            {
                // If the item exists, update the quantity
                existingItem.Quantity += cartItemInput.Quantity;
            }
            else
            {
                // Add the product with its details to the cart
                cart.Items.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = cartItemInput.Quantity
                });
            }

            // Deduct the stock quantity of the product
            product.StockQuantity -= cartItemInput.Quantity;
            await _context.Products.ReplaceOneAsync(p => p.Id == product.Id, product);

            // Update the last modified time
            cart.UpdatedAt = DateTime.UtcNow;

            // Upsert the cart based on the CartUuid
            await _cartCollection.ReplaceOneAsync(
                c => c.CartUuid == cart.CartUuid,
                cart,
                new ReplaceOptions { IsUpsert = true }
            );

            return Ok(new { Message = "Item added to cart successfully" });
        }
        
        [HttpPut("update-quantity-add")]
        public async Task<IActionResult> UpdateCartItemQuantityAdd(Guid userId, [FromBody] CartItemInput cartItemInput)
        {
            if (cartItemInput == null || cartItemInput.ProductId == Guid.Empty || cartItemInput.Quantity <= 0)
            {
                return BadRequest(new { Message = "Invalid product details" });
            }

            // Fetch the product details from the product collection
            var product = await _context.Products.Find(p => p.Id == cartItemInput.ProductId).FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            // Fetch the cart based on the UserId
            var cart = await _cartCollection.Find(c => c.UserId == userId).FirstOrDefaultAsync();

            if (cart == null)
            {
                return NotFound(new { Message = "Cart not found" });
            }

            // Find the item in the cart
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == cartItemInput.ProductId);

            if (existingItem == null)
            {
                return NotFound(new { Message = "Item not found in cart" });
            }

            // Calculate the difference in quantity
            int quantityDifference = cartItemInput.Quantity;

            // Check if the product has enough stock for the update
            if (product.StockQuantity < quantityDifference)
            {
                return BadRequest(new { Message = "Insufficient stock for the product" });
            }

            // Update the quantity of the item in the cart
            existingItem.Quantity += cartItemInput.Quantity;

            // Adjust the stock quantity of the product
            product.StockQuantity -= quantityDifference;
            await _context.Products.ReplaceOneAsync(p => p.Id == product.Id, product);

            // Update the last modified time
            cart.UpdatedAt = DateTime.UtcNow;

            // Save the updated cart
            await _cartCollection.ReplaceOneAsync(
                c => c.CartUuid == cart.CartUuid,
                cart,
                new ReplaceOptions { IsUpsert = true }
            );

            return Ok(new { Message = "Item quantity updated successfully" });
        }
        
        [HttpDelete("update-quantity-remove")]
        public async Task<IActionResult> UpdateCartItemQuantityRemove(Guid userId, [FromBody] CartItemInput cartItemInput)
        {
            if (cartItemInput == null || cartItemInput.ProductId == Guid.Empty || cartItemInput.Quantity <= 0)
            {
                return BadRequest(new { Message = "Invalid product details" });
            }

            // Fetch the product details from the product collection
            var product = await _context.Products.Find(p => p.Id == cartItemInput.ProductId).FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            // Fetch the cart based on the UserId
            var cart = await _cartCollection.Find(c => c.UserId == userId).FirstOrDefaultAsync();

            if (cart == null)
            {
                return NotFound(new { Message = "Cart not found" });
            }

            // Find the item in the cart
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == cartItemInput.ProductId);

            if (existingItem == null)
            {
                return NotFound(new { Message = "Item not found in cart" });
            }

            // Calculate the difference in quantity
            int quantityDifference = cartItemInput.Quantity;

            // Check if the decreased amount is larger than the quantity in the cart
            if (cartItemInput.Quantity > existingItem.Quantity)
            {
                return BadRequest(new { Message = $"You only have {existingItem.Quantity} of this item in your cart" });
            }

            // Update the quantity of the item in the cart
            existingItem.Quantity -= cartItemInput.Quantity;

            // Adjust the stock quantity of the product
            product.StockQuantity += quantityDifference;
            await _context.Products.ReplaceOneAsync(p => p.Id == product.Id, product);

            // Remove the item from the cart if the quantity is zero
            if (existingItem.Quantity == 0)
            {
                cart.Items.Remove(existingItem);
            }

            // Update the last modified time
            cart.UpdatedAt = DateTime.UtcNow;

            // Save the updated cart
            await _cartCollection.ReplaceOneAsync(
                c => c.CartUuid == cart.CartUuid,
                cart,
                new ReplaceOptions { IsUpsert = true }
            );

            return Ok(new { Message = "Item quantity updated successfully" });
        }        

    }
}