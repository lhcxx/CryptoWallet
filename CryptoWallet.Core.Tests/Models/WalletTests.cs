using Xunit;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Core.Tests.Models
{
    public class WalletTests
    {
        [Fact]
        public void Constructor_WithValidUserId_ShouldCreateWallet()
        {
            // Arrange
            var userId = "test-user-id";

            // Act
            var wallet = new Wallet(userId);

            // Assert
            Assert.NotNull(wallet.Id);
            Assert.Equal(userId, wallet.UserId);
            Assert.Equal(0, wallet.Balance);
            Assert.True(wallet.CreatedAt <= DateTime.UtcNow);
            Assert.NotNull(wallet.Transactions);
            Assert.Empty(wallet.Transactions);
        }

        [Fact]
        public void Constructor_ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var wallet1 = new Wallet("user1");
            var wallet2 = new Wallet("user2");

            // Assert
            Assert.NotEqual(wallet1.Id, wallet2.Id);
        }
    }
}