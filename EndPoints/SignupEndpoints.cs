using BankingAPI.Models;
using BankingAPI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

public static class SignupEndpoints
{
    public static void MapSignupEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/signup", async (SignupRequest request, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(request.Aadhaar) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new SignupResponse
                {
                    Success = false,
                    Message = "All fields are required."
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

                var checkAadhaarCmd = new SqlCommand(
                    "SELECT UserName, UserPassword FROM Customer WHERE AdharNumber = @Aadhaar", conn);
                checkAadhaarCmd.Parameters.AddWithValue("@Aadhaar", request.Aadhaar);

                using var reader = await checkAadhaarCmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Results.BadRequest(new SignupResponse
                    {
                        Success = false,
                        Message = "No account associated with this Aadhaar number."
                    });
                }

                string existingUsername = reader["UserName"]?.ToString();
                string existingPassword = reader["UserPassword"]?.ToString();
                reader.Close();

                if (!string.IsNullOrWhiteSpace(existingUsername) && !string.IsNullOrWhiteSpace(existingPassword))
                {
                    return Results.BadRequest(new SignupResponse
                    {
                        Success = false,
                        Message = "Account already exists. Please login."
                    });
                }

                var checkUsernameCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Customer WHERE UserName = @Username AND AdharNumber <> @Aadhaar", conn);
                checkUsernameCmd.Parameters.AddWithValue("@Username", request.Username);
                checkUsernameCmd.Parameters.AddWithValue("@Aadhaar", request.Aadhaar);

                int usernameCount = (int)await checkUsernameCmd.ExecuteScalarAsync();

                if (usernameCount > 0)
                {
                    return Results.BadRequest(new SignupResponse
                    {
                        Success = false,
                        Message = "Username already exists. Please choose another."
                    });
                }

                string hashedPassword = SecurityHelper.HashPassword(request.Password);

                var updateCmd = new SqlCommand(
                    "UPDATE Customer SET UserName = @Username, UserPassword = @UserPassword WHERE AdharNumber = @Aadhaar", conn);
                updateCmd.Parameters.AddWithValue("@Username", request.Username);
                updateCmd.Parameters.AddWithValue("@UserPassword", hashedPassword);
                updateCmd.Parameters.AddWithValue("@Aadhaar", request.Aadhaar);

                int rows = await updateCmd.ExecuteNonQueryAsync();

                if (rows > 0)
                {
                    return Results.Ok(new SignupResponse
                    {
                        Success = true,
                        Message = "Signup successful. You can now login."
                    });
                }
                else
                {
                    return Results.Problem("Signup failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal error: {ex.Message}");
            }
        });
    }
}
