/*
 * File: UserManagementController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of User Operations for User Management
 */


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using EAD_BE.Models.CSR.UserManagement;
using EAD_BE.Models.User.Common;
using Microsoft.AspNetCore.Authorization;

namespace EAD_BE.Controllers.User.UserManagement
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize (Roles = "User")]
    public class UserManagementController : ControllerBase
    {
        private readonly UserManager<CustomApplicationUser> _userManager;

        public UserManagementController(UserManager<CustomApplicationUser>  userManager)
        {
            _userManager = userManager;
        }

        [HttpPut("update-account")]
        public async Task<IActionResult> UpdateUserAccount([FromBody] UpdateUserModel model)
        {
            if (model == null)
            {
                return BadRequest(new { Message = "Invalid user data" });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Email != model.Email)
            {
                return Unauthorized(new { Message = "You are not authorized to update this account" });
            }

            if (!string.IsNullOrEmpty(model.UserName))
            {
                user.UserName = model.UserName;
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                if (model.PhoneNumber.Length != 10)
                {
                    return BadRequest(new { Message = "Phone number must be exactly 10 digits long" });
                }
                
                user.PhoneNumber = model.PhoneNumber;
            }

            if (!string.IsNullOrEmpty(model.Address))
            {
                user.Address = model.Address;
            }

            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(new { Message = "Failed to update user account", Errors = updateResult.Errors });
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                {
                    return BadRequest(new { Message = "Failed to update user password", Errors = passwordResult.Errors });
                }
            }

            return Ok(new { Message = "User account updated successfully" });
        }
        
        [HttpPut("deactivate-account/{email}")]
        public async Task<IActionResult> DeactivateAccount(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Email != email)
            {
                return Unauthorized(new { Message = "You are not authorized to deactivate this account" });
            }

            user.State = "inactive";
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Failed to deactivate user account", Errors = result.Errors });
            }

            return Ok(new { Message = "User account deactivated successfully" });
        }
    }
}