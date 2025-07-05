using BankingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace BankingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly IConfiguration _config;

        public TransactionController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("neft-transaction")]
        public async Task<IActionResult> PostNeftTransaction([FromBody] NeftTransactionRequest model)
        {
            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                using var cmd = new SqlCommand("App_PostNEFTTransaction", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@SubSchemeId", model.SubSchemeId);
                cmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber);
                cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
                cmd.Parameters.AddWithValue("@PaymentTypeId", model.PaymentTypeId);
                cmd.Parameters.AddWithValue("@BranchId", model.BranchId);
                cmd.Parameters.AddWithValue("@TransactionDate", model.TransactionDate);
                cmd.Parameters.AddWithValue("@NeftAmount", model.NeftAmount);
                cmd.Parameters.AddWithValue("@NeftCharges", model.NeftCharges);
                cmd.Parameters.AddWithValue("@BenificiaryRemark", model.BenificiaryRemark ?? string.Empty);
                cmd.Parameters.AddWithValue("@BenificiaryIFSCCode", model.BenificiaryIFSCCode ?? string.Empty);
                cmd.Parameters.AddWithValue("@BenificiaryAccountNumber", model.BenificiaryAccountNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@BenificiaryAccountHolderName", model.BenificiaryAccountHolderName ?? string.Empty);
                cmd.Parameters.AddWithValue("@BenificiaryBranchName", model.BenificiaryBranchName ?? string.Empty);
                cmd.Parameters.AddWithValue("@BenificiaryBankName", model.BenificiaryBankName ?? string.Empty);
                cmd.Parameters.AddWithValue("@IsFileGenerate", model.IsFileGenerate);
                cmd.Parameters.AddWithValue("@UserName", model.UserName ?? string.Empty);
                cmd.Parameters.AddWithValue("@BenificiaryMobileNo", model.BenificiaryMobileNo ?? string.Empty);
                cmd.Parameters.AddWithValue("@BenificiaryEmail", model.BenificiaryEmail ?? string.Empty);

                using var reader = await cmd.ExecuteReaderAsync();

                string message = "NEFT Transaction executed.";
                int transactionId = 0;

                if (reader.HasRows && await reader.ReadAsync())
                {
                    message = reader[0]?.ToString() ?? message;

                    if (reader.FieldCount > 1 && int.TryParse(reader[1]?.ToString(), out int id))
                    {
                        transactionId = id;
                    }
                }

                var response = new TransactionResponse
                {
                    Success = true,
                    Message = message,
                    TransactionId = transactionId
                };

                return Ok(response);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new TransactionResponse
                {
                    Success = false,
                    Message = "Database error: " + ex.Message,
                    TransactionId = 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TransactionResponse
                {
                    Success = false,
                    Message = "Unexpected error: " + ex.Message,
                    TransactionId = 0
                });
            }
        }

        [HttpGet("neft-history")]
        public async Task<IActionResult> GetNeftTransactionHistory(
            [FromQuery] int customerId,
            [FromQuery] int accountNumber)
        {
            try
            {
                var result = new List<NeftTransactionModel>();

                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var query = @"
                SELECT 
                    TransactionDate,
                    ScrollNumber,
                    SubSchemeId,
                    BatchNumber,
                    AccountNumber,
                    CustomerId,
                    BenificiaryAccountHolderName,
                    Amount,
                    BenificiaryCode,
                    BenificiaryAccountNumber,
                    BenificiaryBankName,
                    BenificiaryIFSCCode,
                    BenificiaryRemark,
                    UniqueTransactionId,
                    UserName,
                    BranchId,
                    PaymentTypeId
                FROM NeftTransaction
                WHERE CustomerId = @CustomerId AND AccountNumber = @AccountNumber
                ORDER BY TransactionDate DESC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@AccountNumber", accountNumber);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new NeftTransactionModel
                    {
                        TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                        ScrollNumber = reader.GetInt32(reader.GetOrdinal("ScrollNumber")),
                        SubSchemeId = reader.GetInt32(reader.GetOrdinal("SubSchemeId")),
                        BatchNumber = reader.GetInt32(reader.GetOrdinal("BatchNumber")),
                        AccountNumber = reader.GetInt32(reader.GetOrdinal("AccountNumber")),
                        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        BenificiaryAccountHolderName = reader["BenificiaryAccountHolderName"]?.ToString(),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        BeneficiaryCode = reader["BenificiaryCode"]?.ToString(),
                        BenificiaryAccountNumber = reader["BenificiaryAccountNumber"]?.ToString(),
                        BenificiaryBankName = reader["BenificiaryBankName"]?.ToString(),
                        BenificiaryIFSCCode = reader["BenificiaryIFSCCode"]?.ToString(),
                        BenificiaryRemark = reader["BenificiaryRemark"]?.ToString(),
                        UniqueTransactionId = reader["UniqueTransactionId"]?.ToString(),
                        UserName = reader["UserName"]?.ToString(),
                        BranchId = reader.GetInt32(reader.GetOrdinal("BranchId")),
                        PaymentTypeId = reader.GetInt32(reader.GetOrdinal("PaymentTypeId"))
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = "Failed to fetch NEFT transactions: " + ex.Message
                });
            }
        }
    }
}
