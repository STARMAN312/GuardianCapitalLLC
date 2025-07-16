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
    }
}
