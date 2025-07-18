using GuardianCapitalLLC.Data;
using GuardianCapitalLLC.Migrations;
using GuardianCapitalLLC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GuardianCapitalLLC.Controllers
{
    public class UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment env) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            IList<ApplicationUser> usersInRole = await _userManager.GetUsersInRoleAsync("Client");

            foreach (var user in usersInRole)
            {
                _context.Entry(user).Collection(u => u.BankAccounts!).Load();
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
            ApplicationUser? user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            List<UserFile> userFiles = await _context.UserFiles
                .Where(f => f.UserId == id)
                .ToListAsync();

            EditUserVM editUser = new EditUserVM
            {
                Id = user.Id,
                UserName = user.UserName!,
                FullName = user.FullName!,
                Address = user.Address!,
                WorkEmail = user.WorkEmail!,
                PersonalEmail = user.PersonalEmail!,
                WorkPhone = user.WorkPhone!,
                PersonalPhone = user.PersonalPhone!,
                ExistingFiles = userFiles,
            };

            return View(editUser);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(string id)
        {
            ApplicationUser? user = await _userManager.Users
                .Include(u => u.BankAccounts!)
                .ThenInclude(a => a.Transactions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

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

            List<UserFile> files = await _context.UserFiles
                .Where(f => f.UserId == user.Id)
                .ToListAsync();

            UserViewVM viewModel = new UserViewVM
            {
                Id = user.Id,
                FullName = user.FullName!,
                Address = user.Address!,
                WorkEmail = user.WorkEmail!,
                PersonalEmail = user.PersonalEmail!,
                WorkPhone = user.WorkPhone!,
                PersonalPhone = user.PersonalPhone!,
                BankAccounts = user.BankAccounts!,
                Transactions = allTransactions,
                Files = files,
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Download(int id)
        {
            var file = _context.UserFiles.FirstOrDefault(f => f.Id == id);
            if (file == null)
                return NotFound();

            var filePath = Path.Combine(_env.ContentRootPath, file.FilePath);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(filePath, contentType, Path.GetFileName(file.FileName));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult DeleteFile(int Id, string UserId)
        {
            var file = _context.UserFiles.FirstOrDefault(f => f.Id == Id);
            if (file == null)
                return NotFound();

            var filePath = Path.Combine(_env.ContentRootPath, file.FilePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.UserFiles.Remove(file);
            _context.SaveChanges();

            return RedirectToAction("Details", new { id = UserId });
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
                    UserName = User.UserName,
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

                IdentityResult result = await _userManager.CreateAsync(user, pin.ToString("D4"));

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Client");

                    List<BankAccount> accounts = new List<BankAccount>
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

                foreach (IdentityError error in result.Errors)
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
                ApplicationUser? existingUser = await _userManager.FindByIdAsync(User.Id);

                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.UserName = User.UserName.Replace(" ", "");
                existingUser.FullName = User.FullName;
                existingUser.Address = User.Address;
                existingUser.WorkEmail = User.WorkEmail;
                existingUser.PersonalEmail = User.PersonalEmail;
                existingUser.WorkPhone = User.WorkPhone;
                existingUser.PersonalPhone = User.PersonalPhone;

                if (User.Files != null && User.Files.Any())
                {
                    var uploadsPath = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads");
                    Directory.CreateDirectory(uploadsPath);

                    foreach (var file in User.Files)
                    {
                        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var userFile = new UserFile
                        {
                            FileName = file.FileName,
                            FilePath = "App_Data/Uploads/" + fileName,
                            ContentType = file.ContentType,
                            UserId = User.Id
                        };

                        _context.UserFiles.Add(userFile);
                    }

                    await _context.SaveChangesAsync();
                }

                IdentityResult result = await _userManager.UpdateAsync(existingUser);

                if (result.Succeeded)
                {
                    TempData["NotificationMessage"] = "Client information edited successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            ModelState.AddModelError(string.Empty, "Please fill in all required fields.");
            return View(User);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string Id)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(Id);

            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string Id)
        {
            ApplicationUser? user = await _context.Users
                .Include(u => u.BankAccounts!)
                .ThenInclude(a => a.Transactions)
                .FirstOrDefaultAsync(u => u.Id == Id);

            if (user == null)
                return NotFound();

            // Delete transactions first
            foreach (BankAccount account in user.BankAccounts!)
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
        public async Task<IActionResult> GenerateNewPassword(string id, string? customPassword)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            string newPassword;

            if (!string.IsNullOrWhiteSpace(customPassword))
            {
                newPassword = customPassword;
            }
            else
            {
                // Generate a secure 10-character alphanumeric password
                const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$";
                using var rng = RandomNumberGenerator.Create();
                var buffer = new byte[10];
                rng.GetBytes(buffer);
                newPassword = new string(buffer.Select(b => chars[b % chars.Length]).ToArray());
            }

            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            IdentityResult result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            TempData["Username"] = user.UserName;
            TempData["PIN"] = newPassword; // You may want to rename this key
            TempData["NotificationMessage"] = "New password set successfully!";

            return RedirectToAction("Details", new { id = user.Id });
        }



        [Authorize(Roles = "Admin")]
        public IActionResult UpdateBalance(string id)
        {
            return View();
        }
    }
}
