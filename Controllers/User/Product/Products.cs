using EAD_BE.Data;
using EAD_BE.Models.User.Common;
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

    public Products(MongoDbContextProduct context)
    {
        _context = context;
    }

    [HttpGet("all-products")]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _context.Products.Find(_ => true).ToListAsync();
        return Ok(products);
    }
}