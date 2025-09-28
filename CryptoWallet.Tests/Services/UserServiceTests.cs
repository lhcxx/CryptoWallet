using Microsoft.EntityFrameworkCore;
using Xunit;
using CryptoWallet.Data;
using CryptoWallet.Services;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Tests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly WalletDbContext _context;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WalletDbContext(options);
            var walletService = new WalletService(_context);
            _userService = new UserService(_context, walletService);
        }

        [Fact]
        public async Task CreateUserAsync_WithValidParameters_ShouldCreateUser()
        {
            // Arrange
            var name = "TestUser";
            var password = "TestPassword";

            // Act
            var user = await _userService.CreateUserAsync(name, password);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(name, user.Name);
            Assert.Equal(password, user.Password);
            Assert.Equal(UserRole.User, user.Role);
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateName_ShouldThrowException()
        {
            // Arrange
            var name = "TestUser";
            var password = "TestPassword";
            await _userService.CreateUserAsync(name, password);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _userService.CreateUserAsync(name, "AnotherPassword"));
        }

        [Theory]
        [InlineData("", "password")]
        [InlineData(null, "password")]
        [InlineData("username", "")]
        [InlineData("username", null)]
        public async Task CreateUserAsync_WithInvalidParameters_ShouldThrowException(string name, string password)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _userService.CreateUserAsync(name, password));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}