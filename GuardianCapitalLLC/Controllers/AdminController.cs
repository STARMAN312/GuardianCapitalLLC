using GuardianCapitalLLC.Data;
using GuardianCapitalLLC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GuardianCapitalLLC.Controllers
{
    public class AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ApplicationDbContext _context = context;

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            IList<ApplicationUser> usersInRole = await _userManager.GetUsersInRoleAsync("Admin");

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
            {
                return NotFound();
            }

            EditAdminVM editUser = new EditAdminVM
            {
                Id = user.Id,
                FullName = user.Email ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            };

            return View(editUser);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(string id)
        {
            ApplicationUser? user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(); // Handle the case where the user is null
            }

            AdminViewVM viewModel = new AdminViewVM
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty, // Use null-coalescing operator to handle potential null values
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAdminVM User)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = new ApplicationUser
                {
                    UserName = User.FullName.Replace(" ", ""),
                    FullName = User.FullName,
                    PhoneNumber = User.PhoneNumber,
                    Email = User.Email,
                };

                byte[] bytes = new byte[2];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(bytes);
                }

                int pin = BitConverter.ToUInt16(bytes, 0) % 10000;

                string password = Generate();

                IdentityResult result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");

                    await _context.SaveChangesAsync();

                    TempData["NotificationMessage"] = "Admin created succesfully!";

                    TempData["Username"] = user.UserName;
                    TempData["Password"] = password;

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
        public async Task<IActionResult> Edit(EditAdminVM User)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser? existingUser = await _userManager.FindByIdAsync(User.Id);

                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.FullName = User.FullName;
                existingUser.PhoneNumber = User.PhoneNumber;
                existingUser.Email = User.Email;

                IdentityResult result = await _userManager.UpdateAsync(existingUser);

                if (result.Succeeded)
                {
                    TempData["NotificationMessage"] = "Admin information edited successfully!";
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateNewPassword(string Id)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(Id);

            if (user == null)
                return NotFound();

            string newPassword = Generate();

            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            IdentityResult result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            TempData["Username"] = user.UserName;
            TempData["Password"] = newPassword;

            TempData["NotificationMessage"] = "New password generated successfully!";

            return RedirectToAction("Details", new { id = user.Id });
        }

        public static string Generate(int minLength = 8, int maxLength = 12, bool includeSpecialChars = true)
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()-_=+[]{};:,.<>?";

            string allChars = upper + lower + digits + (includeSpecialChars ? special : "");

            // Ensure at least one of each type (optional but better security)
            var requiredChars = upper[RandomNumber(0, upper.Length)].ToString()
                              + lower[RandomNumber(0, lower.Length)]
                              + digits[RandomNumber(0, digits.Length)];

            if (includeSpecialChars)
                requiredChars += special[RandomNumber(0, special.Length)];

            int length = RandomNumber(minLength, maxLength + 1);
            int remaining = length - requiredChars.Length;

            var password = new StringBuilder(requiredChars);
            for (int i = 0; i < remaining; i++)
            {
                password.Append(allChars[RandomNumber(0, allChars.Length)]);
            }

            // Shuffle the result
            return new string(password.ToString().OrderBy(_ => RandomNumber(0, int.MaxValue)).ToArray());
        }

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
        private static int RandomNumber(int min, int max)
        {
            return RandomNumberGenerator.GetInt32(min, max);
        }
    }
}
