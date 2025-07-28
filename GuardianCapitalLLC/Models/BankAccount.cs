namespace GuardianCapitalLLC.Models
{
    public class BankAccount
    {
        public int Id { get; set; }

        public AccountType Type { get; set; }

        public decimal Balance { get; set; }

        // Foreign key
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Transactions for this account
        public virtual ICollection<Transaction> Transactions { get; set; }
        public string AccountNumber { get; set; }
        public enum AccountType
        {
            Checking,
            Savings,
            TrustFund,
        }
    }

}
