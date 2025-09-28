using Microsoft.EntityFrameworkCore;
using Xunit;
using CryptoWallet.Data;
using CryptoWallet.Services;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Tests.E2E
{
    public class WalletOperationsE2ETests : IDisposable
    {
        private readonly WalletDbContext _context;
        private readonly UserService _userService;
        private readonly WalletService _walletService;

        public WalletOperationsE2ETests()
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WalletDbContext(options);
            _walletService = new WalletService(_context);
            _userService = new UserService(_context, _walletService);
        }

        [Fact]
        public async Task CompleteDepositAndWithdrawalFlow_ShouldWorkCorrectly()
        {
            // Arrange - 创建用户和钱包
            var user = await _userService.CreateUserAsync("test_user", "password");
            var wallet = await _walletService.CreateWalletAsync(user.Id);

            // Act - 存款操作
            var depositAmount = 1000.50m;
            await _walletService.DepositAsync(wallet.Id, depositAmount);

            // Assert - 验证存款成功
            var walletAfterDeposit = await _walletService.GetWalletAsync(wallet.Id);
            Assert.Equal(depositAmount, walletAfterDeposit.Balance);

            // Act - 取款操作
            var withdrawalAmount = 300.25m;
            await _walletService.WithdrawAsync(wallet.Id, withdrawalAmount);

            // Assert - 验证取款成功
            var walletAfterWithdrawal = await _walletService.GetWalletAsync(wallet.Id);
            Assert.Equal(depositAmount - withdrawalAmount, walletAfterWithdrawal.Balance);
        }

        [Fact]
        public async Task TransferBetweenUsers_ShouldWorkCorrectly()
        {
            // Arrange - 创建两个用户和钱包
            var user1 = await _userService.CreateUserAsync("sender", "password");
            var user2 = await _userService.CreateUserAsync("receiver", "password");
            
            var wallet1 = await _walletService.CreateWalletAsync(user1.Id);
            var wallet2 = await _walletService.CreateWalletAsync(user2.Id);

            // 给发送者钱包存款
            await _walletService.DepositAsync(wallet1.Id, 1000);

            // Act - 转账操作
            var transferAmount = 500m;
            await _walletService.TransferAsync(wallet1.Id, wallet2.Id, transferAmount);

            // Assert - 验证转账成功
            var updatedWallet1 = await _walletService.GetWalletAsync(wallet1.Id);
            var updatedWallet2 = await _walletService.GetWalletAsync(wallet2.Id);

            Assert.NotNull(updatedWallet1);
            Assert.NotNull(updatedWallet2);
            Assert.Equal(1000 - transferAmount, updatedWallet1.Balance);
            Assert.Equal(transferAmount, updatedWallet2.Balance);
        }

        [Fact]
        public async Task InsufficientFundsWithdrawal_ShouldFail()
        {
            // Arrange - 创建用户和钱包
            var user = await _userService.CreateUserAsync("poor_user", "password");
            var wallet = await _walletService.CreateWalletAsync(user.Id);

            // Act & Assert - 尝试从空钱包取款应该失败
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _walletService.WithdrawAsync(wallet.Id, 100));
        }

        [Fact]
        public async Task InsufficientFundsTransfer_ShouldFail()
        {
            // Arrange - 创建两个用户和钱包
            var user1 = await _userService.CreateUserAsync("sender", "password");
            var user2 = await _userService.CreateUserAsync("receiver", "password");
            
            var wallet1 = await _walletService.CreateWalletAsync(user1.Id);
            var wallet2 = await _walletService.CreateWalletAsync(user2.Id);

            // Act & Assert - 尝试从空钱包转账应该失败
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _walletService.TransferAsync(wallet1.Id, wallet2.Id, 100));
        }

        [Fact]
        public async Task ComplexTransactionHistory_ShouldTrackAllOperations()
        {
            // Arrange - 创建用户和钱包
            var user = await _userService.CreateUserAsync("active_user", "password");
            var wallet = await _walletService.CreateWalletAsync(user.Id);

            // Act - 执行一系列操作
            await _walletService.DepositAsync(wallet.Id, 1000);  // 存款 1000
            await _walletService.WithdrawAsync(wallet.Id, 200);   // 取款 200
            await _walletService.DepositAsync(wallet.Id, 500);    // 存款 500

            // Assert - 验证最终余额
            var finalWallet = await _walletService.GetWalletAsync(wallet.Id);
            Assert.NotNull(finalWallet);
            Assert.Equal(1300, finalWallet.Balance);

            // Assert - 验证交易历史
            var transactions = await _walletService.GetTransactionHistoryAsync(wallet.Id);
            Assert.Equal(3, transactions.Count);
            
            // 验证交易类型
            var depositTransactions = transactions.Where(t => t.Type == TransactionType.Deposit).ToList();
            var withdrawalTransactions = transactions.Where(t => t.Type == TransactionType.Withdrawal).ToList();
            
            Assert.Equal(2, depositTransactions.Count);
            Assert.Single(withdrawalTransactions);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
