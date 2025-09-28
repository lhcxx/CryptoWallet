using CryptoWallet.Core.Models;

namespace CryptoWallet.Core.Services
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(string name, string password);
        Task<User?> GetUserAsync(string userId);
        Task<User?> GetUserByNameAsync(string name);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> UserExistsAsync(string userId);
        Task<bool> UserExistsByNameAsync(string name);
        Task EnsureAdminUserExistsAsync();
        bool IsAdmin(User user);
    }
}
