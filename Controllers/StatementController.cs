using BankingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BankingAPI.Controllers
{
    [ApiController]
    [Route("api/statement")]
    public class StatementController : ControllerBase
    {
        private readonly IConfiguration _config;

        public StatementController(IConfiguration config)
        {
            _config = config;
        }

        // 🔹 Get Account Types
        [HttpGet("account-types/{customerId:int}")]
        public async Task<IActionResult> GetAccountTypes(int customerId)
        {
            var results = new List<string>();

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                using var cmd = new SqlCommand("App_AccountList", conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 200
                };
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                        results.Add(reader.GetString(0));
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return Problem("Failed to load account types: " + ex.Message);
            }
        }

        // 🔹 Get Customer Accounts by Type
        [HttpGet("accounts")]
        public async Task<IActionResult> GetAccounts(
            [FromQuery] int customerId,
            [FromQuery] string accountType,
            [FromQuery] string deviceId = "083ea3911295b82d",
            [FromQuery] int closed = 0)
        {
            try
            {
                var accounts = new List<AccountModel>();

                int acType = accountType.ToUpper() switch
                {
                    "SHARE" => 1,
                    "SAVING" => 2,
                    "FIXED" => 3,
                    "LOAN" => 4,
                    "RECCURING" => 5,
                    "PIGMYAGENT" => 6,
                    "PIGMY" => 7,
                    _ => throw new ArgumentException($"Invalid accountType: {accountType}")
                };

                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                using var cmd = new SqlCommand("App_GetCustomerAccount", conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 300
                };

                cmd.Parameters.AddWithValue("@AcType", acType);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                cmd.Parameters.AddWithValue("@Closed", closed);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var account = new AccountModel
                    {
                        PrimaryId = reader.GetInt32(reader.GetOrdinal("PrimaryId")),
                        Id = reader.HasColumn("Id") && !reader.IsDBNull("Id") ? reader.GetInt32(reader.GetOrdinal("Id")) : 0,
                        SubSchemeId = reader.GetInt32(reader.GetOrdinal("SubSchemeId")),
                        PigmyAgentId = reader.HasColumn("PigmyAgentId") ? reader.GetInt32(reader.GetOrdinal("PigmyAgentId")) : 0,
                        AccountNumber = reader["AccountNumber"]?.ToString(),
                        SubSchemeName = reader["SubSchemeName"]?.ToString(),
                        OldAccountNumber = reader.HasColumn("OldAccountNumber") ? reader["OldAccountNumber"]?.ToString() : null,
                        CustomerId = reader.HasColumn("CustomerId") ? reader.GetInt32(reader.GetOrdinal("CustomerId")) : customerId,
                        OpeningDate = reader.HasColumn("OpeningDate") && !reader.IsDBNull("OpeningDate") ? reader.GetDateTime(reader.GetOrdinal("OpeningDate")) : null,
                        ExpiryDate = reader.HasColumn("ExpiryDate") && !reader.IsDBNull("ExpiryDate") ? reader.GetDateTime(reader.GetOrdinal("ExpiryDate")) : null,
                        RateOfInterestId = reader.HasColumn("RateOfInterestId") && !reader.IsDBNull("RateOfInterestId") ? reader.GetInt32(reader.GetOrdinal("RateOfInterestId")) : null,
                        IAmount = reader.HasColumn("IAmount") && !reader.IsDBNull("IAmount") ? reader.GetDecimal(reader.GetOrdinal("IAmount")) : null,
                        MaturityAmount = reader.HasColumn("MaturityAmount") && !reader.IsDBNull("MaturityAmount") ? reader.GetDecimal(reader.GetOrdinal("MaturityAmount")) : null,
                        Installment = reader.HasColumn("Installment") && !reader.IsDBNull("Installment") ? reader.GetDecimal(reader.GetOrdinal("Installment")) : null,
                        Closed = reader.HasColumn("Closed") && !reader.IsDBNull("Closed") ? reader.GetBoolean(reader.GetOrdinal("Closed")) : false,
                        ClosedDate = reader.HasColumn("ClosedDate") && !reader.IsDBNull("ClosedDate") ? reader.GetDateTime(reader.GetOrdinal("ClosedDate")) : null,
                        IsApplyInterest = reader.HasColumn("IsApplyInterest") && !reader.IsDBNull("IsApplyInterest") ? reader.GetBoolean(reader.GetOrdinal("IsApplyInterest")) : false,
                        ODLoanAccountNo = reader.HasColumn("ODLoanAccountNo") ? reader["ODLoanAccountNo"]?.ToString() : null,
                        IsOverdueAccount = reader.HasColumn("IsOverdueAccount") && !reader.IsDBNull("IsOverdueAccount") ? reader.GetBoolean(reader.GetOrdinal("IsOverdueAccount")) : null,
                        DirectorName = reader.HasColumn("DirectorName") ? reader["DirectorName"]?.ToString() : null,
                        Balance = reader.HasColumn("Balance") && !reader.IsDBNull("Balance") ? Convert.ToDecimal(reader["Balance"]) : 0
                    };

                    accounts.Add(account);
                }

                return Ok(accounts);
            }
            catch (Exception ex)
            {
                return Problem("Failed to fetch account details: " + ex.Message);
            }
        }

        // 🔹 Get Transaction Statement
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int customerId,
            [FromQuery] int subSchemeId,
            [FromQuery] int accountNumber,
            [FromQuery] int pigmyAgentId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string deviceId = "083ea3911295b82d")
        {
            var transactions = new List<TransactionModel>();

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                using var cmd = new SqlCommand("App_GetNewStatement", conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 200
                };

                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@SubSchemeId", subSchemeId);
                cmd.Parameters.AddWithValue("@AccountNumber", accountNumber);
                cmd.Parameters.AddWithValue("@PigmyAgentId", pigmyAgentId);
                cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                cmd.Parameters.AddWithValue("@FromDate", fromDate);
                cmd.Parameters.AddWithValue("@ToDate", toDate);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    transactions.Add(new TransactionModel
                    {
                        PrimaryId = reader["PrimaryId"] != DBNull.Value ? Convert.ToInt32(reader["PrimaryId"]) : 0,
                        TransactionDate = reader["TransactionDate"] != DBNull.Value ? Convert.ToDateTime(reader["TransactionDate"]) : DateTime.MinValue,
                        SubSchemeId = reader["SubSchemeId"] != DBNull.Value ? Convert.ToInt32(reader["SubSchemeId"]) : 0,
                        AccountNumber = reader["AccountNumber"]?.ToString(),
                        ScrollNumber = reader["ScrollNumber"] != DBNull.Value ? Convert.ToInt32(reader["ScrollNumber"]) : 0,
                        Narration = reader["Narration"]?.ToString(),
                        TransactionType = reader["TransactionType"]?.ToString(),
                        Deposite = reader["Deposite"] != DBNull.Value ? Convert.ToDecimal(reader["Deposite"]) : 0,
                        Withdraw = reader["Withdraw"] != DBNull.Value ? Convert.ToDecimal(reader["Withdraw"]) : 0,
                        Plain = reader["Plain"] != DBNull.Value ? Convert.ToDecimal(reader["Plain"]) : 0,
                        PlainCr = reader["PlainCr"] != DBNull.Value ? Convert.ToDecimal(reader["PlainCr"]) : 0,
                        PlainDr = reader["PlainDr"] != DBNull.Value ? Convert.ToDecimal(reader["PlainDr"]) : 0,
                        Penalty = reader["Penalty"] != DBNull.Value ? Convert.ToDecimal(reader["Penalty"]) : 0,
                        PenaltyCr = reader["PenaltyCr"] != DBNull.Value ? Convert.ToDecimal(reader["PenaltyCr"]) : 0,
                        PenaltyDr = reader["PenaltyDr"] != DBNull.Value ? Convert.ToDecimal(reader["PenaltyDr"]) : 0,
                        Payable = reader["Payable"] != DBNull.Value ? Convert.ToDecimal(reader["Payable"]) : 0,
                        Receivable = reader["Receivable"] != DBNull.Value ? Convert.ToDecimal(reader["Receivable"]) : 0,
                        Dividend = reader["Dividend"] != DBNull.Value ? Convert.ToDecimal(reader["Dividend"]) : 0,
                        DrCr = reader["DrCr"]?.ToString(),
                        Balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0
                    });
                }

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return Problem("Failed to fetch transaction statement: " + ex.Message);
            }
        }
    }
}
