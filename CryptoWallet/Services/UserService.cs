using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CryptoWallet.Models;
using CryptoWallet.Data;

namespace CryptoWallet.Services
{
    public class UserService
    {
        private readonly WalletDbContext _context;
        private readonly WalletService _walletService;

        public UserService(WalletDbContext context, WalletService walletService)
        {
            _context = context;
            _walletService = walletService;
        }

        public async Task<User> CreateUserAsync(string name, string email)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            if (await _context.Users.AnyAsync(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("User with this email already exists");

            var user = new User(name, email);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create a wallet for the new user
            await _walletService.CreateWalletAsync(user.Id);

            return user;
        }

        public async Task<User> GetUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            return !string.IsNullOrEmpty(userId) && 
                   await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<bool> UserExistsByEmailAsync(string email)
        {
            return !string.IsNullOrEmpty(email) && 
                   await _context.Users.AnyAsync(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public async Task EnsureAdminUserExistsAsync()
        {
            var adminExists = await _context.Users.AnyAsync(u => u.Email == "admin@admin");
            if (!adminExists)
            {
                var adminUser = new User("admin", "admin@admin", UserRole.Admin);
                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                // Create wallet for admin user
                await _walletService.CreateWalletAsync(adminUser.Id);
            }
        }

        public bool IsAdmin(User user)
        {
            return user?.Role == UserRole.Admin;
        }
    }
}