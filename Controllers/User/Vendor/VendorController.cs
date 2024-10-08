/*
 * File: VendorController.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Controller class of User Operations for Vendors
 */


namespace EAD_BE.Controllers.User.Vendor;

using EAD_BE.Models.User.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

public class VendorController : ControllerBase
{
    private readonly UserManager<CustomUserModel> _userManager;

    public VendorController(UserManager<CustomUserModel> userManager)
    {
        _userManager = userManager;
    }

    // Rate a vendor
    [HttpPost("rate-vendor/{vendorId}")]
    public async Task<IActionResult> RateVendor(Guid vendorId, [FromBody] int rating)
    {
        if (rating < 1 || rating > 5)
        {
            return BadRequest(new { Message = "Rating must be between 1 and 5" });
        }

        var existingVendor = await _userManager.FindByIdAsync(vendorId.ToString());
        if (existingVendor == null)
        {
            return NotFound(new { Message = "Vendor not found" });
        }

        // Update the vendor's rating and rating count
        existingVendor.Ranking = ((existingVendor.Ranking * existingVendor.RankingCount) + rating) / (existingVendor.RankingCount + 1);
        existingVendor.RankingCount += 1;

        var result = await _userManager.UpdateAsync(existingVendor);
        if (!result.Succeeded)
        {
            return StatusCode(500, new { Message = "An error occurred while updating the vendor rating" });
        }

        return Ok(new { Message = "Vendor rated successfully" });
    }

    // Add a comment to a vendor
    [HttpPost("add-comment/{vendorId}")]
    public async Task<IActionResult> AddComment(Guid vendorId, [FromBody] CommentVendor comment)
    {
        if (string.IsNullOrEmpty(comment.UserEmail) || string.IsNullOrEmpty(comment.Text))
        {
            return BadRequest(new { Message = "User email and comment text are required" });
        }
        
        var user = await _userManager.FindByEmailAsync(comment.UserEmail);
        if (user == null)
        {
            return BadRequest(new { Message = "User does not exist" });
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized(new { Message = "User not logged in" });
        }

        var existingVendor = await _userManager.FindByIdAsync(vendorId.ToString());
        if (existingVendor == null)
        {
            return NotFound(new { Message = "Vendor not found" });
        }

        comment.commentID = Guid.NewGuid();
        comment.CreatedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;
        existingVendor.Comments.Add(comment);

        var result = await _userManager.UpdateAsync(existingVendor);
        if (!result.Succeeded)
        {
            return StatusCode(500, new { Message = "An error occurred while adding the comment" });
        }

        return Ok(new { Message = "Comment added successfully" });
    }

    // Update a comment for a vendor
    [HttpPut("update-comment/{vendorId}")]
    public async Task<IActionResult> UpdateComment(Guid vendorId, Guid commentId, [FromBody] CommentVendor updatedComment)
    {
        if (string.IsNullOrEmpty(updatedComment.Text))
        {
            return BadRequest(new { Message = "Comment text is required" });
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized(new { Message = "User not logged in" });
        }
        
        var user = await _userManager.FindByEmailAsync(updatedComment.UserEmail);
        if (user == null)
        {
            return BadRequest(new { Message = "User does not exist" });
        }

        var existingVendor = await _userManager.FindByIdAsync(vendorId.ToString());
        if (existingVendor == null)
        {
            return NotFound(new { Message = "Vendor not found" });
        }

        var comment = existingVendor.Comments.FirstOrDefault(c => c.commentID == commentId && c.UserEmail == updatedComment.UserEmail);
        if (comment == null)
        {
            return NotFound(new { Message = "Comment not found" });
        }

        comment.Text = updatedComment.Text;
        comment.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(existingVendor);
        if (!result.Succeeded)
        {
            return StatusCode(500, new { Message = "An error occurred while updating the comment" });
        }

        return Ok(new { Message = "Comment updated successfully" });
    }

    // Delete a comment for a vendor
    [HttpDelete("delete-comment/{vendorId}/{commentId}/{userEmail}")]
    public async Task<IActionResult> DeleteComment(Guid vendorId, Guid commentId, string userEmail)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized(new { Message = "User not logged in" });
        }
        
        var user = await _userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            return BadRequest(new { Message = "User does not exist" });
        }

        if (currentUser.Email != userEmail)
        {
            return BadRequest(new { Message = "You are not authorized to delete this comment." });
        }

        var existingVendor = await _userManager.FindByIdAsync(vendorId.ToString());
        if (existingVendor == null)
        {
            return NotFound(new { Message = "Vendor not found" });
        }

        var comment = existingVendor.Comments.FirstOrDefault(c => c.commentID == commentId && c.UserEmail == userEmail);
        if (comment == null)
        {
            return NotFound(new { Message = "Comment not found" });
        }

        existingVendor.Comments.Remove(comment);

        var result = await _userManager.UpdateAsync(existingVendor);
        if (!result.Succeeded)
        {
            return StatusCode(500, new { Message = "An error occurred while deleting the comment" });
        }

        return Ok(new { Message = "Comment deleted successfully" });
    }
}