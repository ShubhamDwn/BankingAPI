namespace BankingAPI.Models
{
    public class HomeResponse
    {
        public string CustomerName { get; set; }  = string.Empty;
        public string SavingsAccountNumber { get; set; }
        public decimal SavingsBalance { get; set; }
    }
}
