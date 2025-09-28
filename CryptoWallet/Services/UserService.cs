using Microsoft.EntityFrameworkCore;
using CryptoWallet.Core.Models;
using CryptoWallet.Core.Services;
using CryptoWallet.Data;

namespace CryptoWallet.Services
{
    public class UserService : BaseUserService
    {
        private readonly WalletDbContext _context;
        private readonly WalletService _walletService;

        public UserService(WalletDbContext context, WalletService walletService)
        {
            _context = context;
            _walletService = walletService;
        }

        protected override DbSet<User> Users => _context.Users;
        protected override DbSet<Wallet> Wallets => _context.Wallets;
        protected override Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
        protected override IWalletService WalletService => _walletService;
    }
}