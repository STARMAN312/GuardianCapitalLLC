using GuardianCapitalLLC.Models;

public class Transaction
{
    public int Id { get; set; }

    public decimal Amount { get; set; } // Always positive
    public TransactionType Type { get; set; }
    public string? Description { get; set; }
    public string? Recipient { get; set; }
    public PurposeType? Purpose { get; set; }
    public string? ToAccountNumber { get; set; } // For transfers, wire transfers, etc.
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // Foreign key to the account
    public int BankAccountId { get; set; }
    public virtual BankAccount BankAccount { get; set; }

    // Optional FK for auditing
    public string UserId { get; set; }
    public virtual ApplicationUser User { get; set; }
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    ExternalTransfer,
    WireTransfer,
    ServiceFee,
    Interest,
}

public enum PurposeType
{
    PersonalTransfer,
    BillPayment,
    LoanPayment,
    Investment,
    Gift,
    BusinessPayment,
    Other,
}