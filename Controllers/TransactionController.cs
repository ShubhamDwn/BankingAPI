using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BankingAPI.Models;

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

                using var cmd = new SqlCommand("UTIL_NeftTransactionPOST", conn)
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

                var message = "NEFT Transaction executed.";
                if (reader.HasRows && await reader.ReadAsync())
                {
                    message = reader[0]?.ToString() ?? message;
                }

                return Ok(new
                {
                    Success = true,
                    Message = message
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = "Database error: " + ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = "Unexpected error: " + ex.Message
                });
            }
        }
    }
}
