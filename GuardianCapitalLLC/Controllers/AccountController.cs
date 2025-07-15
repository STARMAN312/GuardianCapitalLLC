using GuardianCapitalLLC.Data;
using GuardianCapitalLLC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;

namespace GuardianCapitalLLC.Controllers
{
    public class AccountController(SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<HomeController> _logger = logger;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var user = await _context.Users
                .Include(u => u.BankAccounts)
                .ThenInclude(a => a.Transactions)
                .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            List<TransactionVM> allTransactions = user.BankAccounts
                .SelectMany(account => account.Transactions.Select(t => new TransactionVM
                {
                    AccountName = account.Type.ToString(),
                    Type = t.Type,
                    Amount = t.Amount,
                    Date = t.Date,
                }))
                .OrderByDescending(t => t.Date)
                .ToList();

            AccountViewVM userView = new AccountViewVM
            {
                FullName = user.FullName,
                TotalBalance = user.BankAccounts.Sum(a => a.Balance),
                BankAccounts = user.BankAccounts,
                Transactions = allTransactions
            };

            return View(userView);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> TransferFunds()
        {
            ViewBag.HideBanner = true;

            var currentUser = await _userManager.GetUserAsync(User);

            var user = await _context.Users
                .Include(u => u.BankAccounts)
                .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            var purposeList = Enum.GetValues(typeof(PurposeType))
                .Cast<PurposeType>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = Regex.Replace(e.ToString(), "(\\B[A-Z])", " $1")
                })
                .ToList();

            ViewBag.PurposeList = purposeList;

            TransferFundsVM transferFundsVM = new TransferFundsVM
            {
                BankAccounts = user.BankAccounts
            };

            return View(transferFundsVM);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewTransfer(TransferFundsVM model)
        {
            var user = await _userManager.GetUserAsync(User);

            ICollection<BankAccount> bankAccounts = await _context.BankAccounts
                    .Where(a => a.UserId == user.Id)
                    .ToListAsync();

            bool isPinValid = await VerifyUserPinAsync(user, model.Pin);

            if (!ModelState.IsValid || !isPinValid)
            {
                model.BankAccounts = bankAccounts;

                var purposeList = Enum.GetValues(typeof(PurposeType))
               .Cast<PurposeType>()
               .Select(e => new SelectListItem
               {
                   Value = e.ToString(),
                   Text = Regex.Replace(e.ToString(), "(\\B[A-Z])", " $1")
               })
               .ToList();

                ViewBag.PurposeList = purposeList;

                ModelState.AddModelError(string.Empty, "Fill in all the required input fields correctly.");

                return View("TransferFunds", model);
            }

            ViewBag.HideBanner = true;

            model.FromAccount = bankAccounts.FirstOrDefault(a => a.Id == model.AccountId);

            return View(model);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(TransferFundsVM model)
        {
            ViewBag.HideBanner = true;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var bankAccounts = await _context.BankAccounts
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            model.BankAccounts = bankAccounts;

            if (!ModelState.IsValid)
            {
                return View("TransferFunds", model);
            }

            // Find the source account
            var fromAccount = bankAccounts.FirstOrDefault(a => a.Id == model.AccountId);
            if (fromAccount == null)
            {
                ModelState.AddModelError(string.Empty, "Selected source account not found.");
                return View("TransferFunds", model);
            }

            // Example: Verify the PIN (assuming you have a way to verify it securely)
            bool isPinValid = await VerifyUserPinAsync(user, model.Pin);
            if (!isPinValid)
            {
                ModelState.AddModelError(nameof(model.Pin), "Invalid security PIN.");
                return View("TransferFunds", model);
            }

            // Check sufficient balance
            if (fromAccount.Balance < model.Amount)
            {
                ModelState.AddModelError(string.Empty, "Insufficient funds in the source account.");
                return View("TransferFunds", model);
            }

            if (model.Amount <= 0 || model.Amount > 1_000_000)
            {
                ModelState.AddModelError(string.Empty, "Invalid transfer amount.");
                return View("TransferFunds", model);
            }

            // Deduct amount from source account
            fromAccount.Balance -= model.Amount;

            // Add transaction record
            var transaction = new Transaction
            {
                Amount = model.Amount,
                Type = Enum.Parse<TransactionType>(model.TransferType),
                Description = model.Description ?? $"Transfer to {model.ToAccountNumber}",
                BankAccountId = fromAccount.Id,
                UserId = user.Id,
                Date = DateTime.UtcNow,
                ToAccountNumber = model.ToAccountNumber,
                Recipient = model.RecipientName,
                Purpose = Enum.Parse<PurposeType>(model.Purpose),
            };

            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            TempData["Modal"] = "Active";

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Login()
        {

            // Ensure roles exist
            if (!_roleManager.Roles.Any())
            {
                string[] roles = new[] { "Admin", "Client" };
                foreach (var role in roles)
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Ensure default admin user exists
            if (!_userManager.Users.Any())
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "Irvin",
                    Email = "irvinarielmadrid@gmail.com",
                };

                string adminPassword = "5VQ4=R0I£#rU;lqs'H>p6S(N18gGTxl6G;Z/(@UkIic!PjGv";

                var result = await _userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Admin user creation error: {error.Description}");
                    }
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, true, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);

                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin"))
                    {
                        return RedirectToAction("Index", "Home");
                    }

                    return RedirectToAction("Index", "Account");
                }
                else
                {
                    _context.FailedLoginLog.Add(new FailedLoginLog
                    {
                        EmailOrUsername = model.Username,
                        AttemptedAt = DateTime.UtcNow,
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });

                    await _context.SaveChangesAsync();
                }
            }

            ModelState.AddModelError(string.Empty, "Incorrect username or password.");
            return View(model);
        }

        public async Task<ActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        private async Task<bool> VerifyUserPinAsync(ApplicationUser user, string pin)
        {
            var passwordHasher = new PasswordHasher<ApplicationUser>();

            // This compares the hashed stored password with the provided PIN
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, pin);

            return result == PasswordVerificationResult.Success;
        }

    }
}
