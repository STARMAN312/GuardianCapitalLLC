using GuardianCapitalLLC.Data;
using GuardianCapitalLLC.Models;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuardianCapitalLLC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillingController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly string _maintenanceApiKey;

        public BillingController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _maintenanceApiKey = configuration["Maintenance:ApiKey"]
                ?? throw new InvalidOperationException("Missing Maintenance API Key");
        }

        [HttpPost("charge-maintenance")]
        public async Task<IActionResult> ChargeMaintenance([FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (apiKey != _maintenanceApiKey)
                return Unauthorized("Invalid API key");

            await ChargeMaintenanceAsync();
            return Ok("Maintenance fees charged.");
        }

        [HttpPost("accrue-interest-daily")]
        public async Task<IActionResult> AccrueDailyInterest([FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (apiKey != _maintenanceApiKey)
                return Unauthorized("Invalid API key");

            await AccrueInterestAsync();
            return Ok("Daily interest accrued.");
        }

        private async Task AccrueInterestAsync()
        {
            const decimal dailyInterestRate = 0.00057534m;

            var users = await _db.Users
                .Include(u => u.BankAccounts)
                .ThenInclude(b => b.Transactions)
                .Where(u => u.BankAccounts.Any(a => a.Type == BankAccount.AccountType.Savings))
                .ToListAsync();

            foreach (var user in users)
            {
                foreach (var account in user.BankAccounts.Where(a => a.Type == BankAccount.AccountType.Savings))
                {
                    var interest = account.Balance * dailyInterestRate;
                    if (interest <= 0) continue;

                    account.Balance += interest;

                    account.Transactions.Add(new Transaction
                    {
                        Amount = interest,
                        Type = TransactionType.Interest,
                        Description = "Daily Savings Interest",
                        Purpose = PurposeType.Other,
                        Date = DateTime.UtcNow,
                        BankAccountId = account.Id,
                        UserId = user.Id
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        private async Task ChargeMaintenanceAsync()
        {
            var users = await _db.Users
                .Include(u => u.BankAccounts)
                    .ThenInclude(b => b.Transactions)
                .Where(u => u.BankAccounts.Any())
                .ToListAsync();

            foreach (var user in users)
            {
                TryCharge(user, 15.00m);
            }

            await _db.SaveChangesAsync();
        }

        private bool TryCharge(ApplicationUser user, decimal fee)
        {
            // Order of priority: Checking > Savings > TrustFund
            var orderedAccounts = user.BankAccounts
                .OrderBy(a =>
                    a.Type == BankAccount.AccountType.Checking ? 0 :
                    a.Type == BankAccount.AccountType.Savings ? 1 :
                    a.Type == BankAccount.AccountType.TrustFund ? 2 : 3)
                .ToList();

            foreach (var account in orderedAccounts)
            {
                if (account.Balance >= fee)
                {
                    account.Balance -= fee;

                    account.Transactions.Add(new Transaction
                    {
                        Amount = fee,
                        Type = TransactionType.ServiceFee,
                        Description = "Monthly Maintenance Fee",
                        Purpose = PurposeType.Other,
                        Date = DateTime.UtcNow,
                        BankAccountId = account.Id,
                        UserId = user.Id
                    });

                    return true;
                }
            }

            // Not enough funds in any account
            return false;
        }
    }
}
