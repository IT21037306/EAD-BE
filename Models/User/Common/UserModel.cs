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
public class CustomApplicationUser : MongoIdentityUser<Guid>
{
    public string State { get; set; }
        
    public string Address { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}