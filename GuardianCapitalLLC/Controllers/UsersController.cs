using GuardianCapitalLLC.Data;
using GuardianCapitalLLC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GuardianCapitalLLC.Controllers
{
    public class UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ApplicationDbContext _context = context;

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync("Client");

            foreach (var user in usersInRole)
            {
                _context.Entry(user).Collection(u => u.BankAccounts).Load();
            }

            return View(usersInRole);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            ApplicationUser user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);

            EditUserVM editUser = new EditUserVM
            {
                Id = user.Id,
                FullName = user.FullName,
                Address = user.Address,
                WorkEmail = user.WorkEmail,
                PersonalEmail = user.PersonalEmail,
                WorkPhone = user.WorkPhone,
                PersonalPhone = user.PersonalPhone
            };

            return View(editUser);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(string id)
        {
            ApplicationUser user = await _userManager.Users
                .Include(u => u.BankAccounts)
                .ThenInclude(a => a.Transactions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

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

            UserViewVM viewModel = new UserViewVM
            {
                Id = user.Id,
                FullName = user.FullName,
                Address = user.Address,
                WorkEmail = user.WorkEmail,
                PersonalEmail = user.PersonalEmail,
                WorkPhone = user.WorkPhone,
                PersonalPhone = user.PersonalPhone,
                BankAccounts = user.BankAccounts,
                Transactions = allTransactions
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM User)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = new ApplicationUser
                {
                    UserName = User.FullName.Replace(" ", ""),
                    PhoneNumber = User.PersonalPhone,
                    Email = User.PersonalEmail,
                    FullName = User.FullName,
                    Address = User.Address,
                    WorkEmail = User.WorkEmail,
                    PersonalEmail = User.PersonalEmail,
                    WorkPhone = User.WorkPhone,
                    PersonalPhone = User.PersonalPhone
                };

                byte[] bytes = new byte[2];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(bytes);
                }

                int pin = BitConverter.ToUInt16(bytes, 0) % 10000;

                var result = await _userManager.CreateAsync(user, pin.ToString("D4"));

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Client");

                    var accounts = new List<BankAccount>
                    {
                        new BankAccount
                        {
                            Type = BankAccount.AccountType.Checking,
                            Balance = 0,
                            UserId = user.Id
                        },
                        new BankAccount
                        {
                            Type = BankAccount.AccountType.Savings,
                            Balance = 0,
                            UserId = user.Id
                        },
                        new BankAccount
                        {
                            Type = BankAccount.AccountType.TrustFund,
                            Balance = 0,
                            UserId = user.Id
                        }
                    };

                    _context.BankAccounts.AddRange(accounts);
                    await _context.SaveChangesAsync();

                    TempData["NotificationMessage"] = "Client and bank accounts created successfully!";

                    TempData["Username"] = user.UserName;
                    TempData["PIN"] = pin.ToString("D4");

                    return RedirectToAction("Details", new { id = user.Id });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ModelState.AddModelError(string.Empty, "Please fill in all required fields.");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserVM User)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByIdAsync(User.Id);

                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.UserName = User.FullName.Replace(" ", "");
                existingUser.FullName = User.FullName;
                existingUser.Address = User.Address;
                existingUser.WorkEmail = User.WorkEmail;
                existingUser.PersonalEmail = User.PersonalEmail;
                existingUser.WorkPhone = User.WorkPhone;
                existingUser.PersonalPhone = User.PersonalPhone;

                var result = await _userManager.UpdateAsync(existingUser);

                if (result.Succeeded)
                {
                    TempData["NotificationMessage"] = "Client information edited successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            ModelState.AddModelError(string.Empty, "Please fill in all required fields.");
            return View(User);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string Id)
        {
            var user = await _context.Users
                .Include(u => u.BankAccounts)
                    .ThenInclude(a => a.Transactions)
                .FirstOrDefaultAsync(u => u.Id == Id);

            if (user == null)
                return NotFound();

            // Delete transactions first
            foreach (var account in user.BankAccounts)
            {
                _context.Transactions.RemoveRange(account.Transactions);
            }

            // Then delete the bank accounts
            _context.BankAccounts.RemoveRange(user.BankAccounts);

            // Finally, delete the user
            await _userManager.DeleteAsync(user);

            // Save all changes
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateNewPassword(string Id)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(Id);

            if (user == null)
                return NotFound();

            byte[] bytes = new byte[2];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            int pin = BitConverter.ToUInt16(bytes, 0) % 10000;
            string newPassword = pin.ToString("D4");

            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            IdentityResult result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            TempData["Username"] = user.UserName;
            TempData["PIN"] = newPassword;

            TempData["NotificationMessage"] = "New PIN generated successfully!";

            return RedirectToAction("Details", new { id = user.Id });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBalance(string id)
        {
            return View();
        }
    }
}
