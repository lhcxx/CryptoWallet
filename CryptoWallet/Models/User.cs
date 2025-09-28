using System;

namespace CryptoWallet.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserRole Role { get; set; }

        public User(string name, string email, UserRole role = UserRole.User)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Email = email;
            CreatedAt = DateTime.UtcNow;
            Role = role;
        }
    }

    public enum UserRole
    {
        User,
        Admin
    }
}
