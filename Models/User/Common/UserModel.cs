/*
 * File: UserModel.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Model class of User Operations for Common User Management
 */


namespace EAD_BE.Models.User.Common;

using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
using System;

[CollectionName("Users")]
public class CustomUserModel : MongoIdentityUser<Guid>
{
    public string State { get; set; }
        
    public string Address { get; set; }
    
    public int Ranking { get; set; } = 0;
    
    public int RankingCount { get; set; } = 0;
    
    public List<CommentVendor> Comments { get; set; } = new List<CommentVendor>();
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


public class CommentVendor
{
    public Guid commentID { get; set; }
    public string UserEmail { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}