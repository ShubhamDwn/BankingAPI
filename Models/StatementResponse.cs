namespace BankingAPI.Models
{
    // Models/AccountModel.cs
    public class AccountModel
    {
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }
        public decimal Balance { get; set; }
        public DateTime OpeningDate { get; set; }
        public string BranchName { get; set; }
        public string Status { get; set; }

    }

    // Models/TransactionModel.cs
    public class TransactionModel
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
    }

}
