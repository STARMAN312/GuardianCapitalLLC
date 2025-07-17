using GuardianCapitalLLC.Data;
using GuardianCapitalLLC.Models;
using GuardianCapitalLLC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;

namespace GuardianCapitalLLC.Controllers
{
    public class AccountController(SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, MarketDataService marketDataService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<HomeController> _logger = logger;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly MarketDataService _marketDataService = marketDataService;

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Index()
        {
            ApplicationUser? currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login");

            ApplicationUser? user = await _context.Users
                .Include(u => u.BankAccounts!)
                .ThenInclude(a => a.Transactions)
                .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            if (user == null)
                return RedirectToAction("Login");

            List<TransactionVM> allTransactions = user.BankAccounts!
                .SelectMany(account => account.Transactions.Select(t => new TransactionVM
                {
                    AccountName = account.Type.ToString(),
                    Type = t.Type,
                    Amount = t.Amount,
                    Date = t.Date,
                }))
                .OrderByDescending(t => t.Date)
                .ToList();

            decimal totalBalance = user.BankAccounts!.Sum(a => a.Balance);

            var httpClient = new HttpClient();
            string url = $"https://api.exchangerate-api.com/v4/latest/USD";

            ExchangeRatesResponse? ratesResponse = null;
            try
            {
                ratesResponse = await httpClient.GetFromJsonAsync<ExchangeRatesResponse>(url);
            }
            catch
            {
                return NotFound();
            }

            Dictionary<string, decimal> convertedBalances = new Dictionary<string, decimal>();

            if (ratesResponse != null)
            {
                string[] targetCurrencies = new[] { "USD", "CAD", "EUR", "MXN", "GBP", "JPY", "KWD" };

                foreach (string currency in targetCurrencies)
                {
                    if (currency == "USD")
                    {
                        convertedBalances["USD"] = Math.Round(totalBalance, 2);
                    }
                    else if (ratesResponse.Rates.TryGetValue(currency, out var rate))
                    {
                        convertedBalances[currency] = Math.Round(totalBalance * rate, 2);
                    }
                }
            }

            var marketData = await _marketDataService.GetMarketDataAsync();

            AccountViewVM userView = new AccountViewVM
            {
                FullName = user.FullName!,
                TotalBalance = totalBalance,
                BankAccounts = user.BankAccounts!,
                Transactions = allTransactions,
                ConvertedBalances = convertedBalances,
                MarketData = marketData,
            };

            return View(userView);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> TransferFundsToInternalAccount()
        {
            ViewBag.HideBanner = true;

            ApplicationUser? currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login");

            ApplicationUser? user = await _context.Users
                .Include(u => u.BankAccounts)
                .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            InternalTransferFundsVM transferFundsVM = new InternalTransferFundsVM
            {
                BankAccounts = user!.BankAccounts
            };

            ViewBag.HideBanner = true;

            return View(transferFundsVM);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewInternalTransfer(InternalTransferFundsVM model)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login");

            ICollection<BankAccount> bankAccounts = await _context.BankAccounts
                    .Where(a => a.UserId == user.Id)
                    .ToListAsync();

            bool isPinValid = await VerifyUserPinAsync(user, model.Pin);

            if (!ModelState.IsValid || !isPinValid)
            {
                model.BankAccounts = bankAccounts;

                ModelState.AddModelError(string.Empty, "Fill in all the required input fields correctly.");

                ViewBag.HideBanner = true;
                return View("TransferFundsToInternalAccount", model);
            }

            ViewBag.HideBanner = true;

            model.FromAccount = bankAccounts.FirstOrDefault(a => a.Id == model.FromAccountId);
            model.ToAccount = bankAccounts.FirstOrDefault(a => a.Id == model.ToAccountId);

            return View(model);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmInternalTransfer(InternalTransferFundsVM model)
        {
            ViewBag.HideBanner = true;

            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            List<BankAccount> bankAccounts = await _context.BankAccounts
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            model.BankAccounts = bankAccounts;

            if (!ModelState.IsValid)
            {
                return View("TransferFundsToInternalAccount", model);
            }

            // Find the source account
            BankAccount? fromAccount = bankAccounts.FirstOrDefault(a => a.Id == model.FromAccountId);
            if (fromAccount == null)
            {
                ModelState.AddModelError(string.Empty, "Selected source account not found.");
                return View("TransferFundsToInternalAccount", model);
            }

            // Example: Verify the PIN (assuming you have a way to verify it securely)
            bool isPinValid = await VerifyUserPinAsync(user, model.Pin);
            if (!isPinValid)
            {
                ModelState.AddModelError(nameof(model.Pin), "Invalid security PIN.");
                return View("TransferFundsToInternalAccount", model);
            }

            // Check sufficient balance
            if (fromAccount.Balance < model.Amount)
            {
                ModelState.AddModelError(string.Empty, "Insufficient funds in the source account.");
                return View("TransferFundsToInternalAccount", model);
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid transfer amount.");
                return View("TransferFundsToInternalAccount", model);
            }

            if (model.FromAccountId == model.ToAccountId)
            {
                ModelState.AddModelError("", "You must choose different accounts.");
                return View("TransferFundsToInternalAccount", model);
            }

            List<BankAccount> accounts = await _context.BankAccounts
                .Where(a => a.UserId == user.Id && (a.Id == model.FromAccountId || a.Id == model.ToAccountId))
                .ToListAsync();

            BankAccount? toAccount = accounts.FirstOrDefault(a => a.Id == model.ToAccountId);

            if (fromAccount == null || toAccount == null)
            {
                ModelState.AddModelError("", "One or both accounts were not found.");
                return View("TransferFundsToInternalAccount", model);
            }
            if (fromAccount == null || toAccount == null)
            {
                ModelState.AddModelError("", "One or both accounts were not found.");
                TempData["ActiveTab"] = "Transfer";
                return View("TransferFundsToInternalAccount", model);
            }

            if (fromAccount.Balance < model.Amount)
            {
                TempData["ErrorMessage"] = "Insufficient funds in source account.";
                TempData["ActiveTab"] = "Transfer";
                return View("TransferFundsToInternalAccount", model);
            }

            fromAccount.Balance -= model.Amount;
            toAccount.Balance += model.Amount;

            _context.Transactions.AddRange(new[]
            {
                new Transaction
                {
                    Amount = model.Amount,
                    Type = TransactionType.Transfer,
                    Description = $"Transfer to {toAccount.Type} account",
                    BankAccountId = fromAccount.Id,
                    UserId = user.Id
                },
                new Transaction
                {
                    Amount = model.Amount,
                    Type = TransactionType.Deposit,
                    Description = $"Transfer from {fromAccount.Type} account",
                    BankAccountId = toAccount.Id,
                    UserId = user.Id
                }
            });

            await _context.SaveChangesAsync();

            TempData["InternalTransferModal"] = "Active";

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> TransferFundsToExternalAccount()
        {
            ViewBag.HideBanner = true;

            ApplicationUser? currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return RedirectToAction("Login");

            ApplicationUser? user = await _context.Users
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

            ExternalTransferFundsVM transferFundsVM = new ExternalTransferFundsVM
            {
                BankAccounts = user!.BankAccounts
            };

            return View(transferFundsVM);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewExternalTransfer(ExternalTransferFundsVM model)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login");

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

                return View("TransferFundsToExternalAccount", model);
            }

            ViewBag.HideBanner = true;

            model.FromAccount = bankAccounts.FirstOrDefault(a => a.Id == model.AccountId);

            return View(model);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmExternalTransfer(ExternalTransferFundsVM model)
        {
            ViewBag.HideBanner = true;

            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            List<BankAccount> bankAccounts = await _context.BankAccounts
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            model.BankAccounts = bankAccounts;

            if (!ModelState.IsValid)
            {
                return View("TransferFundsToExternalAccount", model);
            }

            // Find the source account
            BankAccount? fromAccount = bankAccounts.FirstOrDefault(a => a.Id == model.AccountId);
            if (fromAccount == null)
            {
                ModelState.AddModelError(string.Empty, "Selected source account not found.");
                return View("TransferFundsToExternalAccount", model);
            }

            // Example: Verify the PIN (assuming you have a way to verify it securely)
            bool isPinValid = await VerifyUserPinAsync(user, model.Pin);
            if (!isPinValid)
            {
                ModelState.AddModelError(nameof(model.Pin), "Invalid security PIN.");
                return View("TransferFundsToExternalAccount", model);
            }

            // Check sufficient balance
            if (fromAccount.Balance < model.Amount)
            {
                ModelState.AddModelError(string.Empty, "Insufficient funds in the source account.");
                return View("TransferFundsToExternalAccount", model);
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid transfer amount.");
                return View("TransferFundsToExternalAccount", model);
            }

            // Deduct amount from source account
            fromAccount.Balance -= model.Amount;

            // Add transaction record
            Transaction transaction = new Transaction
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

            TempData["ExternalTransferModal"] = "Active";

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Login()
        {

            // Ensure roles exist
            if (!_roleManager.Roles.Any())
            {
                string[] roles = new[] { "Admin", "Client" };
                foreach (string role in roles)
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Ensure default admin user exists
            if (!_userManager.Users.Any())
            {
                ApplicationUser adminUser = new ApplicationUser
                {
                    UserName = "Irvin",
                    Email = "irvinarielmadrid@gmail.com",
                };

                string adminPassword = "5VQ4=R0I£#rU;lqs'H>p6S(N18gGTxl6G;Z/(@UkIic!PjGv";

                IdentityResult result = await _userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    foreach (IdentityError error in result.Errors)
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
                Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, true, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    ApplicationUser? user = await _userManager.FindByNameAsync(model.Username);

                    if(user != null)
                    {
                        IList<string> roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "Home");
                        }

                        return RedirectToAction("Index", "Account");
                    }
                }
                else
                {
                    _context.FailedLoginLog.Add(new FailedLoginLog
                    {
                        EmailOrUsername = model.Username,
                        AttemptedAt = DateTime.UtcNow,
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
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
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(user?.PasswordHash))
                    return false;

                var passwordHasher = new PasswordHasher<ApplicationUser>();
                var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, pin);
                return result == PasswordVerificationResult.Success;
            });
        }

    }
}
