using System;
using System.Collections.Generic;

namespace CryptoWallet.Models
{
    public class Wallet
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<Transaction> Transactions { get; set; }

        public Wallet(string userId)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            Balance = 0;
            CreatedAt = DateTime.UtcNow;
            Transactions = new List<Transaction>();
        }
    }

    public class Transaction
    {
        public string Id { get; set; }
        public string WalletId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string? RelatedUserId { get; set; } // For transfers

        public Transaction(string walletId, TransactionType type, decimal amount, string description, string? relatedUserId = null)
        {
            Id = Guid.NewGuid().ToString();
            WalletId = walletId;
            Type = type;
            Amount = amount;
            Description = description;
            Timestamp = DateTime.UtcNow;
            RelatedUserId = relatedUserId;
        }
    }

    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        TransferIn,
        TransferOut
    }
}
