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

        // 🔹 POST: Add Beneficiary using Stored Procedure
        [HttpPost("add")]
        public async Task<IActionResult> AddBeneficiary([FromBody] AddBeneficiaryRequest model)
        {
            if (model.AccountNumber != model.ConfirmAccountNumber)
                return BadRequest(new { success = false, message = "Account numbers do not match." });

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                using var cmd = new SqlCommand("App_PostBeneficiaryDetails", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@DeviceId", model.DeviceId ?? "111");
                cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
                cmd.Parameters.AddWithValue("@beneficiarycode", model.AccountNumber + model.IFSC);
                cmd.Parameters.AddWithValue("@BeneficiaryName", model.BeneficiaryName ?? "");
                cmd.Parameters.AddWithValue("@BeneficiaryNickName", model.BeneficiaryNickName ?? "");
                cmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber ?? "");
                cmd.Parameters.AddWithValue("@IFSC", model.IFSC ?? "");
                cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo ?? "");
                cmd.Parameters.AddWithValue("@Email", model.Email ?? "");
                cmd.Parameters.AddWithValue("@BankName", model.BankName ?? "");
                cmd.Parameters.AddWithValue("@BranchName", model.BranchName ?? "");
                cmd.Parameters.AddWithValue("@RegFrom", model.RegFrom ?? "Phone");

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true, message = "Beneficiary added successfully!" });
            }
            catch (Exception ex)
            {
                return Problem("Failed to add beneficiary: " + ex.Message);
            }
        }

        // 🔹 POST: List All Active Beneficiaries for a Customer
        [HttpPost("list")]
        public async Task<IActionResult> GetBeneficiaries([FromBody] BeneficiaryListRequest model)
        {
            var beneficiaries = new List<object>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var query = @"
                SELECT Id, BenificiaryCode, BeneficiaryName, BankName, IFSC, AccountNumber, BranchName, 
                       BeneficiaryNickName, MobileNo, Email
                FROM BeneficiaryDetail
                WHERE CustomerId = @CustomerId AND Status = 1";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                beneficiaries.Add(new
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    BeneficiaryCode = reader["BenificiaryCode"]?.ToString(),
                    BeneficiaryName = reader["BeneficiaryName"]?.ToString(),
                    BankName = reader["BankName"]?.ToString(),
                    IFSC = reader["IFSC"]?.ToString(),
                    AccountNumber = reader["AccountNumber"]?.ToString(),
                    BranchName = reader["BranchName"]?.ToString(),
                    BeneficiaryNickName = reader["BeneficiaryNickName"]?.ToString(),
                    MobileNo = reader["MobileNo"]?.ToString(),
                    Email = reader["Email"]?.ToString()
                });
            }

            return Ok(beneficiaries);
        }

        // 🔹 POST: Soft Delete Beneficiary
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteBeneficiary([FromBody] BeneficiaryDeleteRequest model)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            string query = @"UPDATE BeneficiaryDetail 
                             SET Status = 0 
                             WHERE CustomerId = @CustomerId AND AccountNumber = @BeneficiaryAccountNumber";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
            cmd.Parameters.AddWithValue("@BeneficiaryAccountNumber", model.BeneficiaryAccountNumber);

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
                return Ok(new { success = true, message = "Beneficiary deleted (marked inactive)." });

            return NotFound(new { success = false, message = "Beneficiary not found." });
        }
    }
}
