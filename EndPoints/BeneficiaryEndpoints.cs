using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using BankingAPI.Models;

namespace BankingAPI.Endpoints
{
    public static class BeneficiaryEndpoints
    {
        public static void MapBeneficiaryEndpoints(this WebApplication app)
        {
            // 🔹 GET Beneficiaries (All or One)
            app.MapGet("/api/beneficiaries", async ([FromQuery] int customerId, [FromQuery] string? accountNumber, IConfiguration config) =>
            {
                var beneficiaries = new List<object>();
                using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var query = @"
                    SELECT BeneficiaryName, BankName, IFSC, AccountNumber, BranchName, BeneficiaryNickName, CustomerId
                    FROM BeneficiaryDetail
                    WHERE CustomerId = @CustomerId AND IsRegister = 1 AND Status = 1";
                if (accountNumber!= null)
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
                        AccountNumber = reader["AccountNumber"].ToString(),
                        BranchName = reader["BranchName"].ToString(),
                        BeneficiaryNickName = reader["BeneficiaryNickName"].ToString(),
                        CustomerId = Convert.ToInt64(reader["CustomerId"])
                    });
                }

                return Results.Ok(beneficiaries);
            });

            // 🔹 POST Add Beneficiary
            app.MapPost("/api/beneficiaries", async ([FromBody] AddBeneficiaryRequest model, IConfiguration config) =>
            {
                if (model.AccountNumber != model.ConfirmAccountNumber)
                    return Results.BadRequest(new { success = false, message = "Account numbers do not match." });

                using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                string checkQuery = @"SELECT COUNT(*) FROM BeneficiaryDetail WHERE CustomerId = @CustomerId AND AccountNumber = @AccountNumber";
                using var checkCmd = new SqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
                checkCmd.Parameters.AddWithValue("@AccountNumber", model.AccountNumber);

                int exists = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
                if (exists > 0)
                    return Results.BadRequest(new { success = false, message = "This beneficiary already exists." });

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

                return Results.Ok(new { success = true, message = "Beneficiary added successfully!" });
            });

            // 🔹 DELETE Beneficiary
            app.MapDelete("/api/beneficiaries", async ([FromQuery] int customerId, [FromQuery] int accountNumber, IConfiguration config) =>
            {
                using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                string query = @"DELETE FROM BeneficiaryDetail WHERE CustomerId = @CustomerId AND AccountNumber = @AccountNumber";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@AccountNumber", accountNumber);

                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows > 0)
                    return Results.Ok(new { success = true, message = "Beneficiary deleted." });

                return Results.NotFound(new { success = false, message = "Beneficiary not found." });
            });
        }
    }
}
