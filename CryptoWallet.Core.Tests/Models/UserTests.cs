using Xunit;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Core.Tests.Models
{
    public class UserTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateUser()
        {
            // Arrange
            var name = "TestUser";
            var password = "TestPassword";

            // Act
            var user = new User(name, password);

            // Assert
            Assert.NotNull(user.Id);
            Assert.Equal(name, user.Name);
            Assert.Equal(password, user.Password);
            Assert.Equal(UserRole.User, user.Role);
            Assert.True(user.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithAdminRole_ShouldCreateAdminUser()
        {
            // Arrange
            var name = "AdminUser";
            var password = "AdminPassword";

            // Act
            var user = new User(name, password, UserRole.Admin);

            // Assert
            Assert.NotNull(user.Id);
            Assert.Equal(name, user.Name);
            Assert.Equal(password, user.Password);
            Assert.Equal(UserRole.Admin, user.Role);
            Assert.True(user.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var user1 = new User("User1", "Password1");
            var user2 = new User("User2", "Password2");

            // Assert
            Assert.NotEqual(user1.Id, user2.Id);
        }
    }
}