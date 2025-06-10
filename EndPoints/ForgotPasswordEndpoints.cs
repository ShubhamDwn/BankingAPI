using BankingAPI.Models;
using BankingAPI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

public static class ForgotPasswordEndpoints
{
    public static void MapForgotPasswordEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/forgotpassword", async (SignupRequest request, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(request.Aadhaar) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new SignupResponse
                {
                    Success = false,
                    Message = "Aadhaar and new password are required."
                });
            }

            if (request.Aadhaar.Length != 12 || !long.TryParse(request.Aadhaar, out _))
            {
                return Results.BadRequest(new SignupResponse
                {
                    Success = false,
                    Message = "Invalid Aadhaar number."
                });
            }

            try
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // Check Aadhaar exists
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Customer WHERE AdharNumber = @Aadhaar", conn);
                checkCmd.Parameters.AddWithValue("@Aadhaar", request.Aadhaar);

                int count = (int)await checkCmd.ExecuteScalarAsync();
                if (count == 0)
                {
                    return Results.Ok(new SignupResponse
                    {
                        Success = false,
                        Message = "No account found with this Aadhaar number."
                    });
                }

                // Hash new password
                string hashedPassword = SecurityHelper.HashPassword(request.Password);

                // Update password
                var updateCmd = new SqlCommand(
                    "UPDATE Customer SET UserPassword = @Password WHERE AdharNumber = @Aadhaar", conn);
                updateCmd.Parameters.AddWithValue("@Password", hashedPassword);
                updateCmd.Parameters.AddWithValue("@Aadhaar", request.Aadhaar);

                int rows = await updateCmd.ExecuteNonQueryAsync();

                if (rows > 0)
                {
                    return Results.Ok(new SignupResponse
                    {
                        Success = true,
                        Message = "Password reset successfully."
                    });
                }
                else
                {
                    return Results.Ok(new SignupResponse
                    {
                        Success = false,
                        Message = "Failed to reset password."
                    });
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal server error: {ex.Message}");
            }
        });
    }
}

