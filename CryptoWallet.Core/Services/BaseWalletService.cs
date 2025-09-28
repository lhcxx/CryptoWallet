using Microsoft.EntityFrameworkCore;
using CryptoWallet.Core.Models;

namespace CryptoWallet.Core.Services
{
    public abstract class BaseWalletService : IWalletService
    {
        protected abstract DbSet<User> Users { get; }
        protected abstract DbSet<Wallet> Wallets { get; }
        protected abstract DbSet<Transaction> Transactions { get; }
        protected abstract Task<int> SaveChangesAsync();
        protected abstract Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();

        public async Task<Wallet> CreateWalletAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

            var wallet = new Wallet(userId);
            Wallets.Add(wallet);
            await SaveChangesAsync();
            return wallet;
        }

        public async Task<Wallet?> GetWalletAsync(string walletId)
        {
            if (string.IsNullOrEmpty(walletId))
                throw new ArgumentException("WalletId cannot be null or empty", nameof(walletId));

            return await Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == walletId);
        }

        public async Task<Wallet?> GetWalletByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));

            return await Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<decimal> GetBalanceAsync(string walletId)
        {
            var wallet = await GetWalletAsync(walletId);
            if (wallet == null)
                throw new InvalidOperationException("Wallet not found");

            return wallet.Balance;
        }

        public async Task DepositAsync(string walletId, decimal amount, string description = "Deposit")
        {
            if (string.IsNullOrEmpty(walletId))
                throw new ArgumentException("WalletId cannot be null or empty", nameof(walletId));

            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));

            using var transaction = await BeginTransactionAsync();
            try
            {
                var wallet = await Wallets.FindAsync(walletId);
                if (wallet == null)
                    throw new InvalidOperationException("Wallet not found");

                wallet.Balance += amount;
                var transactionRecord = new Transaction(walletId, TransactionType.Deposit, amount, description);
                Transactions.Add(transactionRecord);

                await SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task WithdrawAsync(string walletId, decimal amount, string description = "Withdrawal")
        {
            if (string.IsNullOrEmpty(walletId))
                throw new ArgumentException("WalletId cannot be null or empty", nameof(walletId));

            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));

            using var transaction = await BeginTransactionAsync();
            try
            {
                var wallet = await Wallets.FindAsync(walletId);
                if (wallet == null)
                    throw new InvalidOperationException("Wallet not found");

                if (wallet.Balance < amount)
                    throw new InvalidOperationException("Insufficient funds");

                wallet.Balance -= amount;
                var transactionRecord = new Transaction(walletId, TransactionType.Withdrawal, amount, description);
                Transactions.Add(transactionRecord);

                await SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task TransferAsync(string fromWalletId, string toWalletId, decimal amount, string description = "Transfer")
        {
            if (string.IsNullOrEmpty(fromWalletId))
                throw new ArgumentException("FromWalletId cannot be null or empty", nameof(fromWalletId));

            if (string.IsNullOrEmpty(toWalletId))
                throw new ArgumentException("ToWalletId cannot be null or empty", nameof(toWalletId));

            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));

            if (fromWalletId == toWalletId)
                throw new InvalidOperationException("Cannot transfer to the same wallet");

            using var transaction = await BeginTransactionAsync();
            try
            {
                var fromWallet = await Wallets.FindAsync(fromWalletId);
                var toWallet = await Wallets.FindAsync(toWalletId);

                if (fromWallet == null)
                    throw new InvalidOperationException("Source wallet not found");

                if (toWallet == null)
                    throw new InvalidOperationException("Destination wallet not found");

                if (fromWallet.Balance < amount)
                    throw new InvalidOperationException("Insufficient funds");

                // Perform the transfer with ACID properties
                fromWallet.Balance -= amount;
                toWallet.Balance += amount;

                // Get user names for transaction descriptions
                var fromUser = await Users.FindAsync(fromWallet.UserId);
                var toUser = await Users.FindAsync(toWallet.UserId);
                
                // Create transactions for both wallets
                var fromTransaction = new Transaction(fromWalletId, TransactionType.TransferOut, amount, 
                    $"{description} to {toUser?.Name ?? "Unknown User"}", toWallet.UserId);
                var toTransaction = new Transaction(toWalletId, TransactionType.TransferIn, amount, 
                    $"{description} from {fromUser?.Name ?? "Unknown User"}", fromWallet.UserId);

                Transactions.AddRange(fromTransaction, toTransaction);

                await SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Transaction>> GetTransactionHistoryAsync(string walletId)
        {
            var wallet = await GetWalletAsync(walletId);
            if (wallet == null)
                throw new InvalidOperationException("Wallet not found");

            return await Transactions
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        public async Task<List<Wallet>> GetAllWalletsAsync()
        {
            return await Wallets
                .Include(w => w.Transactions)
                .ToListAsync();
        }
    }
}
