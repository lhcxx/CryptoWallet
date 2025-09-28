using CryptoWallet.Core.Models;

namespace CryptoWallet.Core.Services
{
    public interface IWalletService
    {
        Task<Wallet> CreateWalletAsync(string userId);
        Task<Wallet?> GetWalletAsync(string walletId);
        Task<Wallet?> GetWalletByUserIdAsync(string userId);
        Task<decimal> GetBalanceAsync(string walletId);
        Task DepositAsync(string walletId, decimal amount, string description = "Deposit");
        Task WithdrawAsync(string walletId, decimal amount, string description = "Withdrawal");
        Task TransferAsync(string fromWalletId, string toWalletId, decimal amount, string description = "Transfer");
        Task<List<Transaction>> GetTransactionHistoryAsync(string walletId);
        Task<List<Wallet>> GetAllWalletsAsync();
    }
}
