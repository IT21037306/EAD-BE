using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AspNetCore.Identity.MongoDbCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EAD_BE.Models.UserManagement;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<MongoIdentityUser<Guid>> _userManager;
    private static readonly List<SignUpModel> _signUpModels = new List<SignUpModel>();
    private readonly SignInManager<MongoIdentityUser<Guid>> _signInManager;
    private string[] roles = ["Admin", "User", "Vendor", "CSR"];


    public AuthController(UserManager<MongoIdentityUser<Guid>> userManager, SignInManager<MongoIdentityUser<Guid>> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignUpModel request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Message = "Invalid data provided" });
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

        var user = new MongoIdentityUser<Guid>
        {
            UserName = request.UserName,
            Email = request.Email,
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

        // Store the user details as SignUpModel object
        _signUpModels.Add(request);

        return Ok(new { Message = "User created successfully" });
    }
    
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

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET"));
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
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
}