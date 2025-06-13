using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BankingAPI.Models
{
    // Models/AccountModel.cs
    public class AccountModel
    {
        public int PrimaryId { get; set; }
        public int Id { get; set; }
        public int SubSchemeId { get; set; }
        public string SubSchemeName { get; set; }
        public int PigmyAgentId { get; set; }
        public string AccountNumber { get; set; }
        public string OldAccountNumber { get; set; }
        public int CustomerId { get; set; }
        public DateTime? OpeningDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? RateOfInterestId { get; set; }
        public decimal? IAmount { get; set; }
        public decimal? MaturityAmount { get; set; }
        public decimal? Installment { get; set; }
        public bool Closed { get; set; }
        public DateTime? ClosedDate { get; set; }
        public bool IsApplyInterest { get; set; }
        public string ODLoanAccountNo { get; set; }
        public bool? IsOverdueAccount { get; set; }
        public string DirectorName { get; set; }
        public decimal Balance { get; set; }
    }



    // Models/TransactionModel.cs
    public class TransactionModel
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
    }
    public static class SqlDataReaderExtensions
    {
        public static bool HasColumn(this SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }




}
