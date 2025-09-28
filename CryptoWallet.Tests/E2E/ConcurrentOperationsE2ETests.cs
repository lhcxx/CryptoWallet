using Microsoft.EntityFrameworkCore;
using Xunit;
using CryptoWallet.Data;
using CryptoWallet.Services;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Tests.E2E
{
    public class ConcurrentOperationsE2ETests : IDisposable
    {
        private readonly WalletDbContext _context;
        private readonly UserService _userService;
        private readonly WalletService _walletService;

        public ConcurrentOperationsE2ETests()
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WalletDbContext(options);
            _walletService = new WalletService(_context);
            _userService = new UserService(_context, _walletService);
        }

        [Fact]
        public async Task MultipleUsersSimultaneousOperations_ShouldWorkCorrectly()
        {
            // Arrange - 创建多个用户
            var user1 = await _userService.CreateUserAsync("user1", "password1");
            var user2 = await _userService.CreateUserAsync("user2", "password2");
            var user3 = await _userService.CreateUserAsync("user3", "password3");

            var wallet1 = await _walletService.CreateWalletAsync(user1.Id);
            var wallet2 = await _walletService.CreateWalletAsync(user2.Id);
            var wallet3 = await _walletService.CreateWalletAsync(user3.Id);

            // Act - 同时执行多个操作
            var tasks = new List<Task>
            {
                _walletService.DepositAsync(wallet1.Id, 1000),
                _walletService.DepositAsync(wallet2.Id, 2000),
                _walletService.DepositAsync(wallet3.Id, 1500)
            };

            await Task.WhenAll(tasks);

            // Assert - 验证所有操作都成功
            var finalWallet1 = await _walletService.GetWalletAsync(wallet1.Id);
            var finalWallet2 = await _walletService.GetWalletAsync(wallet2.Id);
            var finalWallet3 = await _walletService.GetWalletAsync(wallet3.Id);

            Assert.NotNull(finalWallet1);
            Assert.NotNull(finalWallet2);
            Assert.NotNull(finalWallet3);
            Assert.Equal(1000, finalWallet1.Balance);
            Assert.Equal(2000, finalWallet2.Balance);
            Assert.Equal(1500, finalWallet3.Balance);
        }

        [Fact]
        public async Task ConcurrentTransfersBetweenUsers_ShouldMaintainDataIntegrity()
        {
            // Arrange - 创建用户和钱包
            var user1 = await _userService.CreateUserAsync("user1", "password1");
            var user2 = await _userService.CreateUserAsync("user2", "password2");
            var user3 = await _userService.CreateUserAsync("user3", "password3");

            var wallet1 = await _walletService.CreateWalletAsync(user1.Id);
            var wallet2 = await _walletService.CreateWalletAsync(user2.Id);
            var wallet3 = await _walletService.CreateWalletAsync(user3.Id);

            // 给用户1存款
            await _walletService.DepositAsync(wallet1.Id, 2000);

            // Act - 同时进行多个转账
            var tasks = new List<Task>
            {
                _walletService.TransferAsync(wallet1.Id, wallet2.Id, 500),
                _walletService.TransferAsync(wallet1.Id, wallet3.Id, 300)
            };

            await Task.WhenAll(tasks);

            // Assert - 验证转账结果
            var finalWallet1 = await _walletService.GetWalletAsync(wallet1.Id);
            var finalWallet2 = await _walletService.GetWalletAsync(wallet2.Id);
            var finalWallet3 = await _walletService.GetWalletAsync(wallet3.Id);

            Assert.NotNull(finalWallet1);
            Assert.NotNull(finalWallet2);
            Assert.NotNull(finalWallet3);
            Assert.Equal(1200, finalWallet1.Balance); // 2000 - 500 - 300
            Assert.Equal(500, finalWallet2.Balance);
            Assert.Equal(300, finalWallet3.Balance);
        }

        [Fact]
        public async Task MultipleDepositsToSameWallet_ShouldAccumulateCorrectly()
        {
            // Arrange - 创建用户和钱包
            var user = await _userService.CreateUserAsync("user", "password");
            var wallet = await _walletService.CreateWalletAsync(user.Id);

            // Act - 多次存款到同一个钱包
            var depositTasks = new List<Task>
            {
                _walletService.DepositAsync(wallet.Id, 100),
                _walletService.DepositAsync(wallet.Id, 200),
                _walletService.DepositAsync(wallet.Id, 300),
                _walletService.DepositAsync(wallet.Id, 400)
            };

            await Task.WhenAll(depositTasks);

            // Assert - 验证总余额
            var finalWallet = await _walletService.GetWalletAsync(wallet.Id);
            Assert.NotNull(finalWallet);
            Assert.Equal(1000, finalWallet.Balance); // 100 + 200 + 300 + 400
        }

        [Fact]
        public async Task ConcurrentUserRegistration_ShouldCreateUniqueUsers()
        {
            // Act - 同时创建多个用户
            var tasks = new List<Task<User>>
            {
                _userService.CreateUserAsync("user1", "password1"),
                _userService.CreateUserAsync("user2", "password2"),
                _userService.CreateUserAsync("user3", "password3"),
                _userService.CreateUserAsync("user4", "password4"),
                _userService.CreateUserAsync("user5", "password5")
            };

            var users = await Task.WhenAll(tasks);

            // Assert - 验证所有用户都创建成功且ID唯一
            Assert.Equal(5, users.Length);
            var userIds = users.Select(u => u.Id).ToList();
            Assert.Equal(userIds.Count, userIds.Distinct().Count()); // 所有ID都是唯一的

            // 验证所有用户都存在
            var allUsers = await _userService.GetAllUsersAsync();
            Assert.Equal(5, allUsers.Count);
        }

        [Fact]
        public async Task StressTest_MultipleOperationsOnSameWallet_ShouldHandleCorrectly()
        {
            // Arrange - 创建用户和钱包
            var user = await _userService.CreateUserAsync("stress_user", "password");
            var wallet = await _walletService.CreateWalletAsync(user.Id);

            // 初始存款
            await _walletService.DepositAsync(wallet.Id, 10000);

            // Act - 执行大量操作
            var operations = new List<Task>();
            
            // 50次存款操作
            for (int i = 0; i < 50; i++)
            {
                operations.Add(_walletService.DepositAsync(wallet.Id, 10));
            }

            // 30次取款操作
            for (int i = 0; i < 30; i++)
            {
                operations.Add(_walletService.WithdrawAsync(wallet.Id, 5));
            }

            await Task.WhenAll(operations);

            // Assert - 验证最终余额
            var finalWallet = await _walletService.GetWalletAsync(wallet.Id);
            Assert.NotNull(finalWallet);
            var expectedBalance = 10000 + (50 * 10) - (30 * 5); // 10000 + 500 - 150 = 10350
            Assert.Equal(expectedBalance, finalWallet.Balance);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
