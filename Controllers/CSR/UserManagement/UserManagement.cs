using AspNetCore.Identity.MongoDbCore.Models;
using EAD_BE.Models.CSR.UserManagement;
using EAD_BE.Models.User.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace EAD_BE.Controllers.CSR.UserManagement
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize (Roles = "CSR,Admin")]
    public class UserManagement : ControllerBase
    {
        private readonly UserManager<CustomApplicationUser> _userManager;

        public UserManagement(UserManager<CustomApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        
        [HttpGet("all-users")]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            return Ok(users);
        }
    }
}
