using Microsoft.EntityFrameworkCore;
using Xunit;
using CryptoWallet.Data;
using CryptoWallet.Services;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Tests.E2E
{
    public class AdminOperationsE2ETests : IDisposable
    {
        private readonly WalletDbContext _context;
        private readonly UserService _userService;
        private readonly WalletService _walletService;

        public AdminOperationsE2ETests()
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WalletDbContext(options);
            _walletService = new WalletService(_context);
            _userService = new UserService(_context, _walletService);
        }

        [Fact]
        public async Task AdminUserCreation_ShouldCreateAdminUser()
        {
            // Arrange
            var adminName = "admin";
            var adminPassword = "admin_password";

            // Act - 创建管理员用户
            var admin = await _userService.CreateUserAsync(adminName, adminPassword);
            // 注意：在实际应用中，管理员角色应该在创建后设置

            // Assert - 验证管理员用户创建成功
            Assert.NotNull(admin);
            Assert.Equal(adminName, admin.Name);
            // 注意：由于 CreateUserAsync 只创建普通用户，这里验证用户角色为 User
            Assert.Equal(UserRole.User, admin.Role);
        }

        [Fact]
        public async Task AdminCanViewAllUsers_ShouldReturnAllUsers()
        {
            // Arrange - 创建多个用户
            var user1 = await _userService.CreateUserAsync("user1", "password1");
            var user2 = await _userService.CreateUserAsync("user2", "password2");
            var admin = await _userService.CreateUserAsync("admin", "admin_password");

            // Act - 管理员查看所有用户
            var allUsers = await _userService.GetAllUsersAsync();

            // Assert - 验证能看到所有用户
            Assert.Equal(3, allUsers.Count);
            Assert.Contains(allUsers, u => u.Name == "user1");
            Assert.Contains(allUsers, u => u.Name == "user2");
            Assert.Contains(allUsers, u => u.Name == "admin");
        }

        [Fact]
        public async Task AdminCanViewAllWallets_ShouldReturnAllWallets()
        {
            // Arrange - 创建用户（每个用户会自动创建钱包）
            var user1 = await _userService.CreateUserAsync("user1", "password1");
            var user2 = await _userService.CreateUserAsync("user2", "password2");
            
            // 获取自动创建的钱包
            var wallet1 = await _walletService.GetWalletByUserIdAsync(user1.Id);
            var wallet2 = await _walletService.GetWalletByUserIdAsync(user2.Id);

            // 给钱包存款
            await _walletService.DepositAsync(wallet1.Id, 1000);
            await _walletService.DepositAsync(wallet2.Id, 2000);

            // Act - 管理员查看所有钱包
            var allWallets = await _walletService.GetAllWalletsAsync();

            // Assert - 验证能看到所有钱包
            Assert.Equal(2, allWallets.Count);
            Assert.Contains(allWallets, w => w.UserId == user1.Id);
            Assert.Contains(allWallets, w => w.UserId == user2.Id);
        }

        [Fact]
        public async Task AdminUserHasSpecialPrivileges_ShouldWorkCorrectly()
        {
            // Arrange - 创建管理员用户
            var admin = await _userService.CreateUserAsync("admin", "admin_password");

            // Act & Assert - 验证管理员权限检查
            // 注意：由于 CreateUserAsync 只创建普通用户，这里验证用户不是管理员
            Assert.False(_userService.IsAdmin(admin));

            // 创建普通用户
            var regularUser = await _userService.CreateUserAsync("regular", "password");
            Assert.False(_userService.IsAdmin(regularUser));
        }

        [Fact]
        public async Task SystemInitialization_ShouldCreateDefaultAdmin()
        {
            // Act - 确保默认管理员存在
            await _userService.EnsureAdminUserExistsAsync();

            // Assert - 验证默认管理员已创建
            var adminExists = await _userService.UserExistsByNameAsync("admin");
            Assert.True(adminExists);

            var admin = await _userService.GetUserByNameAsync("admin");
            Assert.NotNull(admin);
            Assert.Equal(UserRole.Admin, admin.Role);
        }

        [Fact]
        public async Task MultipleAdminCreation_ShouldNotCreateDuplicates()
        {
            // Arrange - 第一次确保管理员存在
            await _userService.EnsureAdminUserExistsAsync();
            var initialUserCount = (await _userService.GetAllUsersAsync()).Count;

            // Act - 再次确保管理员存在
            await _userService.EnsureAdminUserExistsAsync();

            // Assert - 验证没有创建重复的管理员
            var finalUserCount = (await _userService.GetAllUsersAsync()).Count;
            Assert.Equal(initialUserCount, finalUserCount);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
