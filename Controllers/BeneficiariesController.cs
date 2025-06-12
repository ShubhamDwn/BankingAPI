using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using BankingAPI.Models;

namespace BankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BeneficiariesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public BeneficiariesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection GetConnection() =>
            new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        // GET: api/Beneficiaries?customerId=1&accountNumber=123 (accountNumber optional)
        [HttpGet]
        public async Task<IActionResult> GetBeneficiaries(int customerId, int? accountNumber = null)
        {
            try
            {
                List<object> beneficiaries = new();
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = @"
                    SELECT BeneficiaryName, BankName, IFSC, AccountNumber, BranchName, BeneficiaryNickName, CustomerId
                    FROM BeneficiaryDetail
                    WHERE CustomerId = @CustomerId AND IsRegister = 1 AND Status = 1";

                if (accountNumber != null)
                    query += " AND AccountNumber = @AccountNumber";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                if (accountNumber != null)
                    cmd.Parameters.AddWithValue("@AccountNumber", accountNumber);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    beneficiaries.Add(new
                    {
                        BeneficiaryName = reader["BeneficiaryName"].ToString(),
                        BankName = reader["BankName"].ToString(),
                        IFSCCode = reader["IFSC"].ToString(),
                        AccountNumber = Convert.ToInt32(reader["AccountNumber"]),
                        BranchName = reader["BranchName"].ToString(),
                        BeneficiaryNickName = reader["BeneficiaryNickName"]?.ToString(),
                        CustomerId = Convert.ToInt32(reader["CustomerId"])
                    });
                }

                return Ok(new { success = true, data = beneficiaries });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error fetching beneficiaries", error = ex.Message });
            }
        }

        // POST: api/Beneficiaries
        [HttpPost]
        public async Task<IActionResult> AddBeneficiary([FromBody] Beneficiary model)
        {
            if (model.AccountNumber != model.ConfirmAccountNumber)
                return BadRequest(new { success = false, message = "Account numbers do not match." });

            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                string checkQuery = @"
                    SELECT COUNT(*) FROM BeneficiaryDetail 
                    WHERE CustomerId = @CustomerId AND AccountNumber = @AccountNumber";
                using var checkCmd = new SqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
                checkCmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber);
                int exists = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
                if (exists > 0)
                    return BadRequest(new { success = false, message = "This beneficiary already exists." });

                string insertQuery = @"
                    INSERT INTO BeneficiaryDetail
                    (CustomerId, BenificiaryCode, BeneficiaryName, BeneficiaryNickName, AccountNumber, IFSC, MobileNo, Email, BankName, BranchName, IsRegister, RegistrationDate, RegistrationStatus, Status, SysDate)
                    VALUES
                    (@CustomerId, @BeneficiaryCode, @BeneficiaryName, @BeneficiaryNickName, @AccountNumber, @IFSC, @MobileNo, @Email, @BankName, @BranchName, 1, GETDATE(), 'Registered', 1, GETDATE())";

                using var insertCmd = new SqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
                insertCmd.Parameters.AddWithValue("@BeneficiaryCode", model.AccountNumber + model.IFSC);
                insertCmd.Parameters.AddWithValue("@BeneficiaryName", model.BeneficiaryName);
                insertCmd.Parameters.AddWithValue("@BeneficiaryNickName", model.BeneficiaryNickName ?? "");
                insertCmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber);
                insertCmd.Parameters.AddWithValue("@IFSC", model.IFSC);
                insertCmd.Parameters.AddWithValue("@MobileNo", model.MobileNo ?? "");
                insertCmd.Parameters.AddWithValue("@Email", model.Email ?? "");
                insertCmd.Parameters.AddWithValue("@BankName", model.BankName ?? "");
                insertCmd.Parameters.AddWithValue("@BranchName", model.BranchName ?? "");

                await insertCmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, message = "Beneficiary added successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Failed to add beneficiary", error = ex.Message });
            }
        }

        // DELETE: api/Beneficiaries?customerId=1&accountNumber=2
        [HttpDelete]
        public async Task<IActionResult> DeleteBeneficiary(int customerId, int accountNumber)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = @"
                    DELETE FROM BeneficiaryDetail 
                    WHERE CustomerId = @CustomerId AND AccountNumber = @AccountNumber";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@AccountNumber", accountNumber);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                    return Ok(new { success = true, message = "Beneficiary deleted successfully." });

                return NotFound(new { success = false, message = "No beneficiary found to delete." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error deleting beneficiary", error = ex.Message });
            }
        }
    }
}
