/*
 * File: UserManagement.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of CSR Operations for User Management
 */


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
        private readonly UserManager<CustomUserModel> _userManager;

        // Constructor
        public UserManagement(UserManager<CustomUserModel> userManager)
        {
            _userManager = userManager;
        }
        
        // Display All Users
        [HttpGet("all-users")]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            return Ok(users);
        }
        
        // Update User State
        [HttpPatch("update-user-state")]
        public async Task<IActionResult> UpdateUserState([FromBody] UpdateStateModel request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            if (user.State == "inactive")
            {
                user.State = "active";
            }
            else if (user.State == "active")
            {
                user.State = "inactive";
            }
            else
            {
                return BadRequest(new { Message = "Invalid user state" });
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Failed to update user state", Errors = result.Errors });
            }

            return Ok(new { Message = "User state updated successfully"});
        }
    }
}
