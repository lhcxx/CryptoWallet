using Microsoft.EntityFrameworkCore;
using CryptoWallet.Core.Models;
using CryptoWallet.Core.Services;
using CryptoWallet.Data;

namespace CryptoWallet.Services
{
    public class WalletService : BaseWalletService
    {
        private readonly WalletDbContext _context;

        public WalletService(WalletDbContext context)
        {
            _context = context;
        }

        protected override DbSet<User> Users => _context.Users;
        protected override DbSet<Wallet> Wallets => _context.Wallets;
        protected override DbSet<Transaction> Transactions => _context.Transactions;
        protected override Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
        protected override Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync() => _context.Database.BeginTransactionAsync();
    }
}