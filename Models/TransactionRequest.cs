namespace BankingAPI.Models
{
    public class NeftTransactionRequest
    {
        public int SubSchemeId { get; set; }
        public int AccountNumber { get; set; }
        public int CustomerId { get; set; }
        public int PaymentTypeId { get; set; }
        public int BranchId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal NeftAmount { get; set; }
        public decimal NeftCharges { get; set; }
        public string BenificiaryRemark { get; set; }
        public string BenificiaryIFSCCode { get; set; }
        public string BenificiaryAccountNumber { get; set; }
        public string BenificiaryAccountHolderName { get; set; }
        public string BenificiaryBranchName { get; set; }
        public string BenificiaryBankName { get; set; }
        public bool IsFileGenerate { get; set; }
        public string UserName { get; set; }
        public string BenificiaryMobileNo { get; set; }
        public string BenificiaryEmail { get; set; }
    }

}
