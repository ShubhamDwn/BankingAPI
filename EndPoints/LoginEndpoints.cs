using BankingAPI.Models;
using BankingAPI.Services;
using Microsoft.Data.SqlClient;

namespace BankingAPI.Endpoints
{
    
    public static class LoginEndpoints
    {
        public static void MapLoginEndpoints(this WebApplication app)
        {
            app.MapPost("/api/auth/login", async (LoginRequest request, IConfiguration config) =>
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Results.BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Username or password cannot be empty."
                    });
                }

                try
                {
                    using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
                    await conn.OpenAsync();

                    var cmd = new SqlCommand("SELECT UserPassword, CustomerId FROM Customer WHERE UserName = @username", conn);
                    cmd.Parameters.AddWithValue("@username", request.Username);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                    {
                        return Results.Ok(new LoginResponse
                        {
                            Success = false,
                            Message = "No user found with this username."
                        });
                    }

                    var storedHash = reader["UserPassword"].ToString();
                    var customerId = Convert.ToInt32(reader["CustomerId"]);
                    var inputHash = SecurityHelper.HashPassword(request.Password);

                    bool isValid = storedHash == inputHash;

                    return Results.Ok(new LoginResponse
                    {
                        Success = isValid,
                        Message = isValid ? "Login successful" : "Invalid password",
                        CustomerId = isValid ? customerId : 0
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error: {ex.Message}");
                }
            });
        }
    }
}
