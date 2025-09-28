using Microsoft.EntityFrameworkCore;
using Xunit;
using CryptoWallet.Data;
using CryptoWallet.Services;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Tests.Services
{
    public class WalletServiceTests : IDisposable
    {
        private readonly WalletDbContext _context;
        private readonly WalletService _walletService;

        public WalletServiceTests()
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WalletDbContext(options);
            _walletService = new WalletService(_context);
        }

        [Fact]
        public async Task CreateWalletAsync_WithValidUserId_ShouldCreateWallet()
        {
            // Arrange
            var userId = "test-user-id";

            // Act
            var wallet = await _walletService.CreateWalletAsync(userId);

            // Assert
            Assert.NotNull(wallet);
            Assert.Equal(userId, wallet.UserId);
            Assert.Equal(0, wallet.Balance);
            Assert.True(wallet.CreatedAt <= DateTime.UtcNow);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task CreateWalletAsync_WithInvalidUserId_ShouldThrowException(string userId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _walletService.CreateWalletAsync(userId));
        }

        [Fact]
        public async Task DepositAsync_WithValidAmount_ShouldIncreaseBalance()
        {
            // Arrange
            var userId = "test-user-id";
            var wallet = await _walletService.CreateWalletAsync(userId);
            var amount = 100.50m;

            // Act
            await _walletService.DepositAsync(wallet.Id, amount);

            // Assert
            var updatedWallet = await _walletService.GetWalletAsync(wallet.Id);
            Assert.NotNull(updatedWallet);
            Assert.Equal(amount, updatedWallet.Balance);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task DepositAsync_WithInvalidAmount_ShouldThrowException(decimal amount)
        {
            // Arrange
            var userId = "test-user-id";
            var wallet = await _walletService.CreateWalletAsync(userId);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _walletService.DepositAsync(wallet.Id, amount));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}