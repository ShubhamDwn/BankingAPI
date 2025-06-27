using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using BankingAPI.Models;

namespace BankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BeneficiariesController : ControllerBase
    {
        private readonly IConfiguration _config;

        public BeneficiariesController(IConfiguration config)
        {
            _config = config;
        }

        // 🔹 GET Beneficiaries (All or One)
        [HttpGet]
        public async Task<IActionResult> GetBeneficiaries([FromQuery] int customerId, [FromQuery] string? accountNumber)
        {
            var beneficiaries = new List<object>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var query = @"
    SELECT BeneficiaryName, BankName, IFSC, AccountNumber, BranchName, 
           BeneficiaryNickName, CustomerId, MobileNo, Email
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
                    IFSC = reader["IFSC"].ToString(),
                    AccountNumber = reader["AccountNumber"].ToString(),
                    BranchName = reader["BranchName"].ToString(),
                    BeneficiaryNickName = reader["BeneficiaryNickName"].ToString(),
                    MobileNo = reader["MobileNo"].ToString(),
                    Email = reader["Email"].ToString(),
                    CustomerId = Convert.ToInt64(reader["CustomerId"])
                });
            }

            return Ok(beneficiaries);
        }

        // 🔹 POST Add Beneficiary
        [HttpPost]
        public async Task<IActionResult> AddBeneficiary([FromBody] AddBeneficiaryRequest model)
        {
            if (model.AccountNumber != model.ConfirmAccountNumber)
                return BadRequest(new { success = false, message = "Account numbers do not match." });

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            string checkQuery = @"SELECT COUNT(*) FROM BeneficiaryDetail WHERE CustomerId = @CustomerId AND AccountNumber = @AccountNumber";
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
                (@CustomerId, @BenificiaryCode, @BeneficiaryName, @BeneficiaryNickName, @AccountNumber, @IFSC, @MobileNo, @Email, @BankName, @BranchName, 1, GETDATE(), 'Registered', 1, GETDATE())";

            using var insertCmd = new SqlCommand(insertQuery, conn);
            insertCmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
            insertCmd.Parameters.AddWithValue("@BenificiaryCode", model.AccountNumber + model.IFSC);
            insertCmd.Parameters.AddWithValue("@BeneficiaryName", model.BeneficiaryName);
            insertCmd.Parameters.AddWithValue("@BeneficiaryNickName", model.BeneficiaryNickName ?? "");
            insertCmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber);
            insertCmd.Parameters.AddWithValue("@IFSC", model.IFSC);
            insertCmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
            insertCmd.Parameters.AddWithValue("@Email", model.Email);
            insertCmd.Parameters.AddWithValue("@BankName", model.BankName);
            insertCmd.Parameters.AddWithValue("@BranchName", model.BranchName);

            await insertCmd.ExecuteNonQueryAsync();

            return Ok(new { success = true, message = "Beneficiary added successfully!" });
        }

        // 🔹 DELETE Beneficiary
        [HttpDelete]
        public async Task<IActionResult> DeleteBeneficiary([FromQuery] int customerId, [FromQuery] int accountNumber)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            string query = @"DELETE FROM BeneficiaryDetail WHERE CustomerId = @CustomerId AND AccountNumber = @AccountNumber";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.Parameters.AddWithValue("@AccountNumber", accountNumber);

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
                return Ok(new { success = true, message = "Beneficiary deleted." });

            return NotFound(new { success = false, message = "Beneficiary not found." });
        }
    }
}