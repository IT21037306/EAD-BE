/*
 * File: VendorController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of Admin Operations for Vendors
 */


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using EAD_BE.Models.User.Common;
using EAD_BE.Models.UserManagement;

namespace EAD_BE.Controllers.Admin.Vendor
{
    [ApiController]
    [Route("api/admin/vendor")]
    [Authorize(Roles = "Admin")]
    public class VendorController : ControllerBase
    {
        private readonly UserManager<CustomApplicationUser> _userManager;
        private static readonly List<SignUpModel> _signUpModels = new List<SignUpModel>();
        private string address;
        private string phoneNumber;
        

        // Constructor
        public VendorController( UserManager<CustomApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        
        // Create Vendor
        [HttpPost("create-vendor")]    
        public async Task<IActionResult> Signup([FromBody] SignUpModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid data provided" });
            }

            if (string.IsNullOrEmpty(request.Address))
            {
                return BadRequest(new { Message = "Address is required" });
            }
            
            if (string.IsNullOrEmpty(request.PhoneNumber))
            {
                return BadRequest(new { Message = "Phone number is required" });
            }
            
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "Email is already in use" });
            }
            
            var user = new CustomApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                State = "active",
                Address = "",
                UpdatedAt = DateTime.UtcNow
            };
            
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                if (request.PhoneNumber.Length != 10)
                {
                    return BadRequest(new { Message = "Phone number must be exactly 10 digits long" });
                }
                    
                user.PhoneNumber = request.PhoneNumber;
            }

            if (!string.IsNullOrEmpty(request.Address))
            {
                user.Address = request.Address;
            }



            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }
                return BadRequest(new { Message = "User creation failed", Errors = ModelState });
            }

            await _userManager.AddToRoleAsync(user, "Vendor");
            await _userManager.AddToRoleAsync(user, "User");

            // Store the user details as SignUpModel object
            _signUpModels.Add(request);

            return Ok(new { Message = "Vendor created successfully" });
        }
    
        // Delete Vendor
        [HttpDelete("delete-vendor/{email}")]
        public async Task<IActionResult> DeleteVendor(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { Message = "Vendor not found" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the vendor" });
            }

            return Ok(new { Message = "Vendor deleted successfully" });
        }
        
        // Display All Vendors
        [HttpGet("all-vendors")]
        public async Task<IActionResult> GetAllVendors()
        {
            try
            {
                var vendors = await _userManager.GetUsersInRoleAsync("Vendor");
                if (vendors == null || vendors.Count == 0)
                {
                    return NotFound(new { Message = "No vendors found" });
                }

                return Ok(vendors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving vendors", Details = ex.Message });
            }
        }
    }
}