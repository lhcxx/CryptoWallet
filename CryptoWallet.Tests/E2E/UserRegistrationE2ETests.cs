using Microsoft.EntityFrameworkCore;
using Xunit;
using CryptoWallet.Data;
using CryptoWallet.Services;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Tests.E2E
{
    public class UserRegistrationE2ETests : IDisposable
    {
        private readonly WalletDbContext _context;
        private readonly UserService _userService;
        private readonly WalletService _walletService;

        public UserRegistrationE2ETests()
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WalletDbContext(options);
            _walletService = new WalletService(_context);
            _userService = new UserService(_context, _walletService);
        }

        [Fact]
        public async Task CompleteUserRegistrationFlow_ShouldCreateUserAndWallet()
        {
            // Arrange
            var username = "john_doe";
            var password = "secure_password123";

            // Act - 用户注册流程
            var user = await _userService.CreateUserAsync(username, password);
            var wallet = await _walletService.CreateWalletAsync(user.Id);

            // Assert - 验证用户和钱包创建成功
            Assert.NotNull(user);
            Assert.Equal(username, user.Name);
            Assert.Equal(UserRole.User, user.Role);
            
            Assert.NotNull(wallet);
            Assert.Equal(user.Id, wallet.UserId);
            Assert.Equal(0, wallet.Balance);
        }

        [Fact]
        public async Task UserRegistrationWithDuplicateUsername_ShouldFail()
        {
            // Arrange
            var username = "duplicate_user";
            var password1 = "password1";
            var password2 = "password2";

            // Act - 创建第一个用户
            var user1 = await _userService.CreateUserAsync(username, password1);

            // Act & Assert - 尝试创建同名用户应该失败
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.CreateUserAsync(username, password2));
        }

        [Fact]
        public async Task MultipleUsersRegistration_ShouldCreateSeparateWallets()
        {
            // Arrange
            var user1Name = "user1";
            var user2Name = "user2";
            var password = "password";

            // Act - 创建两个用户
            var user1 = await _userService.CreateUserAsync(user1Name, password);
            var user2 = await _userService.CreateUserAsync(user2Name, password);
            
            var wallet1 = await _walletService.CreateWalletAsync(user1.Id);
            var wallet2 = await _walletService.CreateWalletAsync(user2.Id);

            // Assert - 验证两个用户都有独立的钱包
            Assert.NotEqual(user1.Id, user2.Id);
            Assert.NotEqual(wallet1.Id, wallet2.Id);
            Assert.Equal(user1.Id, wallet1.UserId);
            Assert.Equal(user2.Id, wallet2.UserId);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
