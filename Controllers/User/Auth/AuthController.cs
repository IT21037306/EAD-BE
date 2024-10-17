/*
 * File: AuthController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of User Operations for Authentication
 */

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AspNetCore.Identity.MongoDbCore.Models;
using EAD_BE.Models.User.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EAD_BE.Models.UserManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<CustomUserModel> _userManager;
    private static readonly List<SignUpModel> _signUpModels = new List<SignUpModel>();
    private readonly SignInManager<CustomUserModel> _signInManager;
    private readonly IConfiguration _configuration;
    private string[] roles = { "Admin", "User", "CSR" };

    // Constructor
    public AuthController(UserManager<CustomUserModel> userManager, SignInManager<CustomUserModel> signInManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    // Signup
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignUpModel request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Message = "Invalid data provided" });
        }

        if (request.Role == null)
        {
            request.Role = "User";
        }

        if (string.IsNullOrEmpty(request.Role))
        {
            return BadRequest(new { Message = "Role is required" });
        }

        if (!roles.Select(r => r.ToLower()).Contains(request.Role.ToLower()))
        {
            return BadRequest(new { Message = "Invalid role provided" });
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { Message = "Email is already in use" });
        }

        var user = new CustomUserModel
        {
            UserName = request.UserName,
            Email = request.Email,
            State = request.Role != null && (request.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || request.Role.Equals("CSR", StringComparison.OrdinalIgnoreCase)) ? "active" : "inactive",
            Address = "",
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(new { Message = "User creation failed", Errors = ModelState });
        }

        await _userManager.AddToRoleAsync(user, request.Role);

        if (request.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _userManager.AddToRoleAsync(user, "CSR");
            await _userManager.AddToRoleAsync(user, "Admin");
            await _userManager.AddToRoleAsync(user, "Vendor");
        }
        else if (request.Role.Equals("CSR", StringComparison.OrdinalIgnoreCase))
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _userManager.AddToRoleAsync(user, "CSR");
        }
        else if (request.Role == null)
        {
            await _userManager.AddToRoleAsync(user, "User");
        }

        // Store the user details as SignUpModel object with default state
        _signUpModels.Add(request);

        return Ok(new { Message = "User created successfully" });
    }

    // Login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Message = "Invalid data provided" });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { Message = "Email not found" });
        }

        if (user.State == "inactive")
        {
            return Unauthorized(new { Message = "Account is suspended. Please contact CSR." });
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName, request.Password, isPersistent: false, lockoutOnFailure: false);

        if (result.IsLockedOut)
        {
            return Unauthorized(new { Message = "User account is locked out" });
        }
        else if (result.IsNotAllowed)
        {
            return Unauthorized(new { Message = "User is not allowed to sign in" });
        }
        else if (!result.Succeeded)
        {
            return Unauthorized(new { Message = "Invalid password" });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        foreach (var role in roles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]);
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(authClaims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        var userDto = new
        {
            user.Id,
            user.UserName,
            user.Email,
            Roles = roles
        };

        return Ok(new { Message = "Login successful", Token = tokenString, User = userDto });
    }

    // Logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { Message = "Logout successful" });
    }
}
