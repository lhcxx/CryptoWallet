using System;

namespace CryptoWallet.Core.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserRole Role { get; set; }

        public User(string name, string password, UserRole role = UserRole.User)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Password = password;
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
