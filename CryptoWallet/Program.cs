using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CryptoWallet.Models;
using CryptoWallet.Services;
using CryptoWallet.Data;
using CryptoWallet.Configuration;

namespace CryptoWallet
{
    class Program
    {
        private static IServiceProvider? _serviceProvider;
        private static User? _currentUser;

        static async Task Main(string[] args)
        {
            InitializeServices();
            await InitializeDatabase();
            ShowWelcomeMessage();
            
            while (true)
            {
                try
                {
                    if (_currentUser == null)
                    {
                        await ShowLoginMenu();
                    }
                    else
                    {
                        await ShowMainMenu();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        private static void InitializeServices()
        {
            var services = new ServiceCollection();
            
            // Add database services (using InMemory by default)
            services.AddDatabaseServices("", "InMemory");
            
            // Add application services
            services.AddScoped<WalletService>();
            services.AddScoped<UserService>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

        private static async Task InitializeDatabase()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            
            await context.Database.EnsureCreatedAsync();
            await userService.EnsureAdminUserExistsAsync();
        }

        private static void ShowWelcomeMessage()
        {
            Console.Clear();
            Console.WriteLine("=== CryptoWallet Console Application ===");
            Console.WriteLine("Welcome to the centralized wallet system!");
            Console.WriteLine();
        }

        private static async Task ShowLoginMenu()
        {
            Console.WriteLine("Please select an option:");
            Console.WriteLine("1. Create new user");
            Console.WriteLine("2. Login with email");
            Console.WriteLine("3. Exit");
            Console.Write("Enter your choice (1-3): ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await CreateNewUser();
                    break;
                case "2":
                    await LoginUser();
                    break;
                case "3":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }

        private static async Task ShowMainMenu()
        {
            using var scope = _serviceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            
            var isAdmin = userService.IsAdmin(_currentUser);
            var roleText = isAdmin ? " (Admin)" : "";
            
            Console.WriteLine($"\n=== Welcome, {_currentUser.Name}!{roleText} ===");
            
            if (isAdmin)
            {
                // Admin menu - only view operations
                Console.WriteLine("Please select an option:");
                Console.WriteLine("1. View all users (Admin only)");
                Console.WriteLine("2. View all transactions (Admin only)");
                Console.WriteLine("3. Logout");
                Console.Write("Enter your choice (1-3): ");
            }
            else
            {
                // Regular user menu - full functionality
                Console.WriteLine("Please select an option:");
                Console.WriteLine("1. Check wallet balance");
                Console.WriteLine("2. Deposit money");
                Console.WriteLine("3. Withdraw money");
                Console.WriteLine("4. Send money to another user");
                Console.WriteLine("5. View transaction history");
                Console.WriteLine("6. Logout");
                Console.Write("Enter your choice (1-6): ");
            }

            var choice = Console.ReadLine();
            Console.WriteLine();

            if (isAdmin)
            {
                switch (choice)
                {
                    case "1":
                        await ViewAllUsers();
                        break;
                    case "2":
                        await ViewAllTransactions();
                        break;
                    case "3":
                        Logout();
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
            else
            {
                switch (choice)
                {
                    case "1":
                        await CheckBalance();
                        break;
                    case "2":
                        await DepositMoney();
                        break;
                    case "3":
                        await WithdrawMoney();
                        break;
                    case "4":
                        await SendMoney();
                        break;
                    case "5":
                        await ViewTransactionHistory();
                        break;
                    case "6":
                        Logout();
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private static async Task CreateNewUser()
        {
            Console.Write("Enter your name: ");
            var name = Console.ReadLine();

            Console.Write("Enter your email: ");
            var email = Console.ReadLine();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                
                var user = await userService.CreateUserAsync(name, email);
                _currentUser = user;
                Console.WriteLine($"User created successfully! Your user ID is: {user.Id}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private static async Task LoginUser()
        {
            Console.Write("Enter your email: ");
            var email = Console.ReadLine();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                
                var user = await userService.GetUserByEmailAsync(email);
                if (user != null)
                {
                    _currentUser = user;
                    Console.WriteLine($"Welcome back, {user.Name}!");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine("User not found. Please check your email or create a new account.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private static async Task CheckBalance()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
                
                var wallet = await walletService.GetWalletByUserIdAsync(_currentUser.Id);
                if (wallet != null)
                {
                    Console.WriteLine($"Your current balance: ${wallet.Balance:F2}");
                }
                else
                {
                    Console.WriteLine("Wallet not found!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async Task DepositMoney()
        {
            Console.Write("Enter amount to deposit: $");
            if (decimal.TryParse(Console.ReadLine(), out var amount))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
                    
                    var wallet = await walletService.GetWalletByUserIdAsync(_currentUser.Id);
                    await walletService.DepositAsync(wallet.Id, amount);
                    Console.WriteLine($"Successfully deposited ${amount:F2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid amount entered.");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async Task WithdrawMoney()
        {
            Console.Write("Enter amount to withdraw: $");
            if (decimal.TryParse(Console.ReadLine(), out var amount))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
                    
                    var wallet = await walletService.GetWalletByUserIdAsync(_currentUser.Id);
                    await walletService.WithdrawAsync(wallet.Id, amount);
                    Console.WriteLine($"Successfully withdrew ${amount:F2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid amount entered.");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async Task SendMoney()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
                
                Console.WriteLine("Available users:");
                var users = await userService.GetAllUsersAsync();
                
                // Filter users based on current user's role
                var otherUsers = users.Where(u => u.Id != _currentUser.Id).ToList();
                
                // If current user is not admin, filter out admin users
                if (!userService.IsAdmin(_currentUser))
                {
                    otherUsers = otherUsers.Where(u => u.Role != UserRole.Admin).ToList();
                }
                
                if (!otherUsers.Any())
                {
                    Console.WriteLine("No other users found.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
                }

                for (int i = 0; i < otherUsers.Count; i++)
                {
                    var roleText = otherUsers[i].Role == UserRole.Admin ? " (Admin)" : "";
                    Console.WriteLine($"{i + 1}. {otherUsers[i].Name}{roleText} ({otherUsers[i].Email})");
                }

                Console.Write("Select user to send money to (enter number): ");
                if (int.TryParse(Console.ReadLine(), out var userIndex) && userIndex > 0 && userIndex <= otherUsers.Count)
                {
                    var recipient = otherUsers[userIndex - 1];
                    Console.Write($"Enter amount to send to {recipient.Name}: $");
                    
                    if (decimal.TryParse(Console.ReadLine(), out var amount))
                    {
                        var senderWallet = await walletService.GetWalletByUserIdAsync(_currentUser.Id);
                        var recipientWallet = await walletService.GetWalletByUserIdAsync(recipient.Id);
                        
                        await walletService.TransferAsync(senderWallet.Id, recipientWallet.Id, amount);
                        Console.WriteLine($"Successfully sent ${amount:F2} to {recipient.Name}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid amount entered.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid user selection.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async Task ViewTransactionHistory()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
                
                var wallet = await walletService.GetWalletByUserIdAsync(_currentUser.Id);
                if (wallet != null)
                {
                    var transactions = await walletService.GetTransactionHistoryAsync(wallet.Id);
                    
                    Console.WriteLine("Transaction History:");
                    Console.WriteLine("==================");
                    
                    if (transactions.Any())
                    {
                        foreach (var transaction in transactions)
                        {
                            var sign = transaction.Type == TransactionType.Deposit || transaction.Type == TransactionType.TransferIn ? "+" : "-";
                            Console.WriteLine($"{transaction.Timestamp:yyyy-MM-dd HH:mm:ss} | {transaction.Type} | {sign}${transaction.Amount:F2} | {transaction.Description}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No transactions found.");
                    }
                }
                else
                {
                    Console.WriteLine("Wallet not found!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async Task ViewAllUsers()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
                
                var users = await userService.GetAllUsersAsync();
                
                // Filter out the current admin user
                var otherUsers = users.Where(u => u.Id != _currentUser.Id).ToList();
                
                Console.WriteLine("All Users (Admin View):");
                Console.WriteLine("======================");
                
                if (!otherUsers.Any())
                {
                    Console.WriteLine("No other users found.");
                }
                else
                {
                    foreach (var user in otherUsers)
                    {
                        var wallet = await walletService.GetWalletByUserIdAsync(user.Id);
                        var balance = wallet?.Balance ?? 0;
                        var roleText = user.Role == UserRole.Admin ? " (Admin)" : "";
                        Console.WriteLine($"Name: {user.Name}{roleText} | Email: {user.Email} | Balance: ${balance:F2}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async Task ViewAllTransactions()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
                var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                
                var allWallets = await walletService.GetAllWalletsAsync();
                
                // Filter out admin user's wallet
                var otherWallets = allWallets.Where(w => w.UserId != _currentUser.Id).ToList();
                
                Console.WriteLine("All Transactions (Admin View - Excluding Admin):");
                Console.WriteLine("===============================================");
                
                var allTransactions = new List<Transaction>();
                foreach (var wallet in otherWallets)
                {
                    var transactions = await walletService.GetTransactionHistoryAsync(wallet.Id);
                    allTransactions.AddRange(transactions);
                }
                
                var sortedTransactions = allTransactions.OrderByDescending(t => t.Timestamp).ToList();
                
                if (sortedTransactions.Any())
                {
                    foreach (var transaction in sortedTransactions)
                    {
                        var sign = transaction.Type == TransactionType.Deposit || transaction.Type == TransactionType.TransferIn ? "+" : "-";
                        Console.WriteLine($"{transaction.Timestamp:yyyy-MM-dd HH:mm:ss} | Wallet: {transaction.WalletId} | {transaction.Type} | {sign}${transaction.Amount:F2} | {transaction.Description}");
                    }
                }
                else
                {
                    Console.WriteLine("No transactions found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Logout()
        {
            _currentUser = null!;
            Console.WriteLine("Logged out successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}