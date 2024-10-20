/*
 * File: InventoryController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of Vendor Operations for Inventory
 */

using EAD_BE.Config.Vendor;
using EAD_BE.Models.User.Common;
using EAD_BE.Models.Vendor.Inventory;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using EAD_BE.Models.Vendor.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using DotNetEnv;
using MailKit.Security;

namespace EAD_BE.Controllers.Vendor
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Vendor,Admin")]
    public class InventoryController : ControllerBase
    {
        private readonly MongoDbContextProduct _context;
        private readonly IMongoCollection<CategoryModel> _categoryCollection;
        private readonly UserManager<CustomUserModel> _userManager;
        
        private readonly string SMTPServer = Environment.GetEnvironmentVariable("SMTP_SERVER");
        private readonly int SMTPPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT"));
        private readonly string SMTPUsername = Environment.GetEnvironmentVariable("SMTP_USER");
        private readonly string SMTPPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");

        // Constructor
        public InventoryController(MongoDbContextProduct context, IMongoCollection<CategoryModel> categoryCollection, UserManager<CustomUserModel> userManager)
        {
            _context = context;
            _categoryCollection = categoryCollection;
            _userManager = userManager;
        }

        // Display All Products of a Vendor
        [HttpGet("user/{userEmail}/products")]
        public async Task<IActionResult> GetUserProducts(string userEmail)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var isCurrentUser = currentUser.Email.ToLower() == userEmail.ToLower();

                if (!isCurrentUser)
                {
                    return BadRequest(new { Message = "You do not have permission to view products for this user" });
                }

                var products = await _context.Products.Find(p => p.AddedByUserEmail.ToLower() == userEmail.ToLower()).ToListAsync();
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

                    if (product.StockQuantity <= 10)
                    {
                        product.Notification = new Notification
                        {
                            Message = $"The stock for product '{product.Name}' is low. Current stock quantity is {product.StockQuantity}.",
                            currentStock = product.StockQuantity,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        var emailSender = new EmailNotificationModel
                            (SMTPServer, 
                                SMTPPort, 
                                SMTPUsername, 
                                SMTPPassword,
                                SecureSocketOptions.SslOnConnect);
                        
                        var subject = $"Low Stock Alert: {product.Name}";
                        var htmlMessage = $@"
                                <html>
                                <body>
                                    <h1>Low Stock Alert</h1>
                                    <p>The stock for product <strong>{product.Name}</strong> is low.</p>
                                    <p>Current stock quantity is <strong>{product.StockQuantity}</strong>.</p>
                                    <p>Please restock as soon as possible.</p>
                                </body>
                                </html>";

                        await emailSender.SendEmailAsync(userEmail, subject, htmlMessage);

                        // Save the product to the database
                        //await _context.Products.ReplaceOneAsync(p => p.Id == product.Id, product, new ReplaceOptions { IsUpsert = true });
                    }

                    var productDetails = new
                    {
                        product.Id,
                        product.Name,
                        product.Description,
                        product.Price,
                        product.StockQuantity,
                        Category = category != null ? new { category.Id, category.Name } : null,
                        product.CreatedAt,
                        product.UpdatedAt,
                        product.Notification
                    };

                    ((List<object>)((dynamic)userProducts[categoryName]).products).Add(productDetails);
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