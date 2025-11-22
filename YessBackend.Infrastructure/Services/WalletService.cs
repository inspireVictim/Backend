using Microsoft.EntityFrameworkCore;
using YessBackend.Application.Services;
using YessBackend.Domain.Entities;
using YessBackend.Infrastructure.Data;

namespace YessBackend.Infrastructure.Services;

/// <summary>
/// Сервис кошелька
/// Реализует логику из Python WalletService
/// </summary>
public class WalletService : IWalletService
{
    private readonly ApplicationDbContext _context;

    public WalletService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetWalletByUserIdAsync(int userId)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task<decimal> GetBalanceAsync(int userId)
    {
        var wallet = await GetWalletByUserIdAsync(userId);
        return wallet?.Balance ?? 0.0m;
    }

    public async Task<decimal> GetYescoinBalanceAsync(int userId)
    {
        var wallet = await GetWalletByUserIdAsync(userId);
        return wallet?.YescoinBalance ?? 0.0m;
    }

    public async Task<List<Transaction>> GetUserTransactionsAsync(int userId, int limit = 50, int offset = 0)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Transaction> CreateTransactionAsync(
        int userId,
        string type,
        decimal amount,
        int? partnerId = null,
        int? orderId = null,
        string? description = null)
    {
        var wallet = await GetWalletByUserIdAsync(userId);
        if (wallet == null)
        {
            throw new InvalidOperationException("Кошелек не найден");
        }

        var balanceBefore = wallet.Balance;
        var balanceAfter = balanceBefore;

        // Обновляем баланс в зависимости от типа транзакции
        if (type == "topup" || type == "bonus")
        {
            balanceAfter = balanceBefore + amount;
            wallet.TotalEarned += amount;
        }
        else if (type == "payment" || type == "withdrawal")
        {
            if (balanceBefore < amount)
            {
                throw new InvalidOperationException("Недостаточно средств");
            }
            balanceAfter = balanceBefore - amount;
            wallet.TotalSpent += amount;
        }

        wallet.Balance = balanceAfter;
        wallet.LastUpdated = DateTime.UtcNow;

        var transaction = new Transaction
        {
            UserId = userId,
            PartnerId = partnerId,
            OrderId = orderId,
            Type = type,
            Amount = amount,
            Status = "completed",
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return transaction;
    }
}
