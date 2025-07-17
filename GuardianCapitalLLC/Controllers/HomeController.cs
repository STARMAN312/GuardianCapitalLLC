using GuardianCapitalLLC.Data;
using GuardianCapitalLLC.Models;
using GuardianCapitalLLC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
namespace GuardianCapitalLLC.Controllers;

public class HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, MarketDataService marketDataService) : Controller
{

    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly MarketDataService _marketDataService = marketDataService;

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> IndexAsync()
    {

        ApplicationUser? user = await _userManager.GetUserAsync(User);

        if (user == null)
            return RedirectToAction("Login", "Account");

        string fullName = user.FullName!;

        // Fetch dashboard data
        DateTime now = DateTime.UtcNow;
        DateTime last24h = now.AddDays(-1);
        DateTime last7d = now.AddDays(-7);
        DateTime today = now.Date;

        IList<ApplicationUser> clientUsers = await _userManager.GetUsersInRoleAsync("Client");
        int totalUsers = clientUsers.Count;

        decimal totalBalance = await _context.BankAccounts.SumAsync(a => a.Balance);

        int transactionsLast24h = await _context.Transactions
            .CountAsync(t => t.Date >= last24h);

        int transactionsLast7d = await _context.Transactions
            .CountAsync(t => t.Date >= last7d);

        int transactionsAllTime = await _context.Transactions.CountAsync();

        int transfersToday = await _context.Transactions
            .CountAsync(t => t.Date >= today && t.Type == TransactionType.Transfer);

        int failedLoginsLast24h = await _context.FailedLoginLog
            .CountAsync(f => f.AttemptedAt >= last24h);

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

        AdminDashboardVM dashboardVM = new AdminDashboardVM
        {
            Username = user.UserName!,
            TotalUsers = totalUsers,
            TotalBalance = totalBalance,
            TransactionsLast24Hours = transactionsLast24h,
            TransactionsLast7Days = transactionsLast7d,
            TransactionsAllTime = transactionsAllTime,
            TransfersToday = transfersToday,
            FailedLoginsLast24Hours = failedLoginsLast24h,
            ConvertedBalances = convertedBalances,
            MarketData = marketData,
        };

        return View(dashboardVM);
    }

    [Route("/NoAccess")]
    public ActionResult NoAccess()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
