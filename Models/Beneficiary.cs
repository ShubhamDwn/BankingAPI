﻿namespace BankingAPI.Models
{
    public class Beneficiary
    {
        public string BeneficiaryName { get; set; }
        public string BankName { get; set; }
        public string IFSC { get; set; }
        public string MobileNo { get; set; }
        public string Email { get; set; }
        public long AccountNumber { get; set; }
        public long ConfirmAccountNumber { get; set; }
        public string BranchName { get; set; }
        public string BeneficiaryNickName { get; set; }
        public long CustomerId { get; set; }

        // Add these for filtering if your DB/API includes them
        public bool IsRegister { get; set; }
        public bool Status { get; set; }
    }
    public class BeneficiaryDeleteRequest
    {
        public long CustomerId { get; set; }
        public long AccountNumber { get; set; }
        public long BeneficiaryAccountNumber { get; set; }
    }
    public class AddBeneficiaryRequest
    {
        public long CustomerId { get; set; }
        public string BeneficiaryName { get; set; }
        public string BeneficiaryNickName { get; set; }
        public long AccountNumber { get; set; }
        public long ConfirmAccountNumber { get; set; }
        public string IFSC { get; set; }
        public long MobileNo { get; set; }
        public string Email { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
    }
    public class BeneficiaryDetailResponse
    {

        public string Message { get; set; }
        public bool Success { get; set; }
    }
    public class BeneficiaryStatusResponse
    {

        public string Message { get; set; }
        public bool IsSuccess { get; set; }
    }
}
