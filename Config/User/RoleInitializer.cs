/*
 * File: RoleInitializer.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Configuration class for User Role Initialization
 */

namespace EAD_BE.Config.User;

using AspNetCore.Identity.MongoDbCore.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

public class RoleInitializer
{
    private readonly RoleManager<MongoIdentityRole<Guid>> _roleManager;
    private readonly string[] _roles = { "Admin", "User", "Vendor", "CSR" };

    public RoleInitializer(RoleManager<MongoIdentityRole<Guid>> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task InitializeRoles()
    {
        foreach (var role in _roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new MongoIdentityRole<Guid>(role));
            }
        }
    }
}