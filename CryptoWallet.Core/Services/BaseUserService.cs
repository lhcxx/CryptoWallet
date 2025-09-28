using Microsoft.EntityFrameworkCore;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Core.Services
{
    public abstract class BaseUserService : IUserService
    {
        protected abstract DbSet<User> Users { get; }
        protected abstract DbSet<Wallet> Wallets { get; }
        protected abstract Task<int> SaveChangesAsync();
        protected abstract IWalletService WalletService { get; }

        public async Task<User> CreateUserAsync(string name, string password)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            if (await Users.AnyAsync(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("User with this name already exists");

            var user = new User(name, password);
            Users.Add(user);
            await SaveChangesAsync();

            // Create a wallet for the new user
            await WalletService.CreateWalletAsync(user.Id);

            return user;
        }

        public async Task<User?> GetUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

            return await Users.FindAsync(userId);
        }

        public async Task<User?> GetUserByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            return await Users
                .FirstOrDefaultAsync(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await Users.ToListAsync();
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            return !string.IsNullOrEmpty(userId) && 
                   await Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<bool> UserExistsByNameAsync(string name)
        {
            return !string.IsNullOrEmpty(name) && 
                   await Users.AnyAsync(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task EnsureAdminUserExistsAsync()
        {
            var adminExists = await Users.AnyAsync(u => u.Name == "admin");
            if (!adminExists)
            {
                var adminUser = new User("admin", "admin", UserRole.Admin);
                Users.Add(adminUser);
                await SaveChangesAsync();

                // Create wallet for admin user
                await WalletService.CreateWalletAsync(adminUser.Id);
            }
        }

        public bool IsAdmin(User user)
        {
            return user?.Role == UserRole.Admin;
        }
    }
}
