using GuardianCapitalLLC.Migrations;
using System.ComponentModel.DataAnnotations;

namespace GuardianCapitalLLC.Models
{
    public class LoginVM
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

    }
    public class CreateUserVM
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string WorkEmail { get; set; }
        [Required]
        public string PersonalEmail { get; set; }
        [Required]
        public string WorkPhone { get; set; }
        [Required]
        public string PersonalPhone { get; set; }
    }

    public class CreateAdminVM
    {
        [Required]
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class EditUserVM
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string WorkEmail { get; set; }
        [Required]
        public string PersonalEmail { get; set; }
        [Required]
        public string WorkPhone { get; set; }
        [Required]
        public string PersonalPhone { get; set; }
        public List<IFormFile>? Files { get; set; }
        public List<UserFile>? ExistingFiles { get; set; }
    }

    public class EditAdminVM
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
    }

    public class TransactionVM
    {
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }

        public string AccountName { get; set; }
    }

    public class UserViewVM
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string WorkEmail { get; set; }
        public string PersonalEmail { get; set; }
        public string WorkPhone { get; set; }
        public string PersonalPhone { get; set; }
        public virtual ICollection<BankAccount> BankAccounts { get; set; }
        public virtual ICollection<TransactionVM> Transactions { get; set; }
        public List<UserFile> Files { get; set; }
    }

    public class AdminViewVM
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class AccountViewVM
    {
        public string FullName { get; set; }
        public decimal TotalBalance { get; set; }
        public virtual ICollection<BankAccount> BankAccounts { get; set; }
        public virtual ICollection<TransactionVM> Transactions { get; set; }
        public Dictionary<string, decimal>? ConvertedBalances { get; set; }
        public Dictionary<string, List<MarketQuoteVM>>? MarketData { get; set; }
    }

    public class BankAccountsView
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public decimal TotalBalance { get; set; }
        public virtual ICollection<BankAccount> BankAccounts { get; set; }
    }

    public class ExternalTransferFundsVM
    {
        public Dictionary<string, decimal> ConvertedBalances { get; set; } = [];
        public string SelectedCurrency { get; set; } = "USD";
        public int AccountId { get; set; }

        [Required]
        public string TransferType { get; set; }

        [Required]
        public string ToAccountNumber { get; set; }

        [Required]
        public string RecipientName { get; set; }

        public decimal Amount { get; set; }

        [Required]
        public string Purpose { get; set; }

        public string? Description { get; set; }

        [Required]
        public string Pin { get; set; }

        public ICollection<BankAccount>? BankAccounts { get; set; }

        public BankAccount? FromAccount { get; set; }
    }

    public class InternalTransferFundsVM
    {
        public Dictionary<string, decimal>? ConvertedBalances { get; set; }
        public string SelectedCurrency { get; set; } = "USD";
        [Required]
        public int? FromAccountId { get; set; }

        [Required]
        public int? ToAccountId { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public ICollection<BankAccount>? BankAccounts { get; set; }
        [Required]
        public string Pin { get; set; }
        public BankAccount? FromAccount { get; set; }
        public BankAccount? ToAccount { get; set; }

    }

    public class DepositVM
    {
        public ICollection<BankAccount>? BankAccounts { get; set; }
        [Required]
        public int? AccountId { get; set; }
    }

    public class ConfirmDepositVM
    {
        public int OrderID { get; set; }
        public int AccountId { get; set; }
    }


    public class AdminDashboardVM
    {
        public string Username { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalBalance { get; set; }
        public int TransactionsLast24Hours { get; set; }
        public int TransactionsLast7Days { get; set; }
        public int TransactionsAllTime { get; set; }
        public int TransfersToday { get; set; }
        public int FailedLoginsLast24Hours { get; set; }
        public Dictionary<string, decimal>? ConvertedBalances { get; set; }
        public Dictionary<string, List<MarketQuoteVM>>? MarketData { get; set; }

    }

    public class ExchangeRatesResponse
    {
        public string Base { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }

    public class StockQuote
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
        public string LastTradingDay { get; set; }
    }

    public class MarketQuoteVM
    {
        public string Symbol { get; set; } = default!;
        public decimal? Current { get; set; }
        public decimal? High { get; set; }
        public decimal? Low { get; set; }
        public decimal? Open { get; set; }
        public decimal? PreviousClose { get; set; }
        public decimal? Change => Current.HasValue && PreviousClose.HasValue
            ? Current.Value - PreviousClose.Value
            : null;
        public decimal? ChangePercent => (Change.HasValue && PreviousClose.HasValue && PreviousClose != 0)
            ? Math.Round(Change.Value / PreviousClose.Value * 100, 2)
            : null;

        public string? CompanyName { get; set; }
        public string? LogoUrl { get; set; }

    }

}
