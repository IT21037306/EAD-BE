/*
 * File: SignUpModel.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Model class of User Operations for Authentication
 */

namespace EAD_BE.Models.UserManagement
{
    public class SignUpModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Role {get; set;}
        public string? State { get; set; }
        
        public string? Address { get; set; }
        
        public string? PhoneNumber { get; set; }
    }
}

