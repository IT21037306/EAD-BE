/*
 * File: LoginModel.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Model class of User Operations for Authentication
 */

namespace EAD_BE.Models.UserManagement;

public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}