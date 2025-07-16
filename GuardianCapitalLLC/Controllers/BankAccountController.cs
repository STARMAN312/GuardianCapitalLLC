using GuardianCapitalLLC.Data;
using GuardianCapitalLLC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuardianCapitalLLC.Controllers
{
    public class BankAccountController(UserManager<ApplicationUser> userManager, ApplicationDbContext context) : Controller
    {

        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ApplicationDbContext _context = context;

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string Id)
        {
            ApplicationUser? user = await _context.Users
                .Include(u => u.BankAccounts!)
                .ThenInclude(a => a.Transactions)
                .FirstOrDefaultAsync(u => u.Id == Id);

            if (user == null)
                return NotFound();

            BankAccountsView BankAccounts = new BankAccountsView
            {
                UserId = user.Id,
                FullName = user.FullName!,
                TotalBalance = user.BankAccounts!.Sum(a => a.Balance),
                BankAccounts = user.BankAccounts!,
            };

            if (TempData["ActiveTab"] == null)
            {
                TempData["ActiveTab"] = "Overview";
            }
            return View(BankAccounts);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(string UserId, int fromAccountId, int toAccountId, decimal amount)
        {

            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Amount must be greater than 0.";
                TempData["ActiveTab"] = "Transfer";
                return RedirectToAction("Index", new { Id = UserId });
            }

            if (fromAccountId == toAccountId)
            {
                ModelState.AddModelError("", "You must choose different accounts.");
                TempData["ActiveTab"] = "Transfer";
                return RedirectToAction("Index", new { Id = UserId });
            }

            ApplicationUser? user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null)
                return Unauthorized();

            List<BankAccount> accounts = await _context.BankAccounts
                .Where(a => a.UserId == user.Id && (a.Id == fromAccountId || a.Id == toAccountId))
                .ToListAsync();

            BankAccount? fromAccount = accounts.FirstOrDefault(a => a.Id == fromAccountId);
            BankAccount? toAccount = accounts.FirstOrDefault(a => a.Id == toAccountId);

            if (fromAccount == null || toAccount == null)
            {
                ModelState.AddModelError("", "One or both accounts were not found.");
                TempData["ActiveTab"] = "Transfer";
                return RedirectToAction("Index", new { Id = UserId });
            }
            if (fromAccount == null || toAccount == null)
            {
                ModelState.AddModelError("", "One or both accounts were not found.");
                TempData["ActiveTab"] = "Transfer";
                return RedirectToAction("Index", new { Id = UserId });
            }

            if (fromAccount.Balance < amount)
            {
                TempData["ErrorMessage"] = "Insufficient funds in source account.";
                TempData["ActiveTab"] = "Transfer";
                return RedirectToAction("Index", new { Id = UserId });
            }

            fromAccount.Balance -= amount;
            toAccount.Balance += amount;

            _context.Transactions.AddRange(new[]
            {
                new Transaction
                {
                    Amount = amount,
                    Type = TransactionType.Transfer,
                    Description = $"Transfer to {toAccount.Type} account",
                    BankAccountId = fromAccount.Id,
                    UserId = user.Id
                },
                new Transaction
                {
                    Amount = amount,
                    Type = TransactionType.Deposit,
                    Description = $"Transfer from {fromAccount.Type} account",
                    BankAccountId = toAccount.Id,
                    UserId = user.Id
                }
            });

            await _context.SaveChangesAsync();

            TempData["ActiveTab"] = "Overview";
            return RedirectToAction("Index", new { Id = UserId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBalance(string UserId, int AccountId, decimal amount)
        {

            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Amount must be greater than 0.";
                TempData["ActiveTab"] = "AddBalance";
                return RedirectToAction("Index", new { Id = UserId });
            }

            ApplicationUser? user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null)
                return Unauthorized();

            List<BankAccount> accounts = await _context.BankAccounts
                .Where(a => a.UserId == user.Id && (a.Id == AccountId))
                .ToListAsync();

            BankAccount? Account = accounts.FirstOrDefault(a => a.Id == AccountId);

            if (Account == null)
            {
                TempData["ErrorMessage"] = "The account was not found.";
                TempData["ActiveTab"] = "AddBalance";
                return RedirectToAction("Index", new { Id = UserId });
            }

            Account.Balance += amount;

            _context.Transactions.AddRange(new[]
            {
                new Transaction
                {
                    Amount = amount,
                    Type = TransactionType.Deposit,
                    Description = $"Deposit",
                    BankAccountId = Account.Id,
                    UserId = user.Id
                }
            });

            await _context.SaveChangesAsync();

            TempData["ActiveTab"] = "Overview";
            return RedirectToAction("Index", new { Id = UserId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawBalance(string UserId, int AccountId, decimal amount)
        {

            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Amount must be greater than 0.";
                TempData["ActiveTab"] = "WithdrawBalance";
                return RedirectToAction("Index", new { Id = UserId });
            }

            ApplicationUser? user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null)
                return Unauthorized();

            List<BankAccount> accounts = await _context.BankAccounts
                .Where(a => a.UserId == user.Id && (a.Id == AccountId))
                .ToListAsync();

            BankAccount? Account = accounts.FirstOrDefault(a => a.Id == AccountId);

            if (Account == null)
            {
                TempData["ErrorMessage"] = "The account was not found.";
                TempData["ActiveTab"] = "WithdrawBalance";
                return RedirectToAction("Index", new { Id = UserId });
            }

            Account.Balance -= amount;

            _context.Transactions.AddRange(new[]
            {
                new Transaction
                {
                    Amount = amount,
                    Type = TransactionType.Withdrawal,
                    Description = $"Withdrawal",
                    BankAccountId = Account.Id,
                    UserId = user.Id
                }
            });

            await _context.SaveChangesAsync();

            TempData["ActiveTab"] = "Overview";
            return RedirectToAction("Index", new { Id = UserId });
        }
    }
}
