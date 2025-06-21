using BankingAPI.Models;
using BankingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BankingAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        // 🔹 POST: /api/auth/signup
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Aadhaar) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new SignupResponse
                {
                    Success = false,
                    Message = "All fields are required."
                });
            }

            if (request.Aadhaar.Length != 12 || !long.TryParse(request.Aadhaar, out _))
            {
                return BadRequest(new SignupResponse
                {
                    Success = false,
                    Message = "Invalid Aadhaar number."
                });
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var checkAadhaarCmd = new SqlCommand(
                    "SELECT UserName, UserPassword FROM Customer WHERE AdharNumber = @Aadhaar", conn);
                checkAadhaarCmd.Parameters.AddWithValue("@Aadhaar", request.Aadhaar);

                using var reader = await checkAadhaarCmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return BadRequest(new SignupResponse
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
                    return BadRequest(new SignupResponse
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
                    return BadRequest(new SignupResponse
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
                    return Ok(new SignupResponse
                    {
                        Success = true,
                        Message = "Signup successful. You can now login."
                    });
                }
                else
                {
                    return Problem("Signup failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                return Problem($"Internal error: {ex.Message}");
            }
        }

        // 🔹 POST: /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Username or password cannot be empty."
                });
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var cmd = new SqlCommand("SELECT UserPassword, CustomerId FROM Customer WHERE UserName = @username", conn);
                cmd.Parameters.AddWithValue("@username", request.Username);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "No user found with this username."
                    });
                }

                var storedHash = reader["UserPassword"].ToString();
                var customerId = Convert.ToInt32(reader["CustomerId"]);
                var inputHash = SecurityHelper.HashPassword(request.Password);

                bool isValid = storedHash == inputHash;

                return Ok(new LoginResponse
                {
                    Success = isValid,
                    Message = isValid ? "Login successful" : "Invalid password",
                    CustomerId = isValid ? customerId : 0
                });
            }
            catch (Exception ex)
            {
                return Problem($"Error: {ex.Message}");
            }
        }

        // 🔹 POST: /api/auth/forgotpassword
        [HttpPost("forgotpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Aadhaar) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Aadhaar and new password are required."
                });
            }

            if (request.Aadhaar.Length != 12 || !long.TryParse(request.Aadhaar, out _))
            {
                return BadRequest(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Invalid Aadhaar number."
                });
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Customer WHERE AdharNumber = @Aadhaar", conn);
                checkCmd.Parameters.AddWithValue("@Aadhaar", request.Aadhaar);

                int count = (int)await checkCmd.ExecuteScalarAsync();
                if (count == 0)
                {
                    return Ok(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "No account found with this Aadhaar number."
                    });
                }

                string hashedPassword = SecurityHelper.HashPassword(request.Password);

                var updateCmd = new SqlCommand(
                    "UPDATE Customer SET UserPassword = @Password WHERE AdharNumber = @Aadhaar", conn);
                updateCmd.Parameters.AddWithValue("@Password", hashedPassword);
                updateCmd.Parameters.AddWithValue("@Aadhaar", request.Aadhaar);

                int rows = await updateCmd.ExecuteNonQueryAsync();

                if (rows > 0)
                {
                    return Ok(new ForgotPasswordResponse
                    {
                        Success = true,
                        Message = "Password reset successfully."
                    });
                }
                else
                {
                    return Ok(new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Failed to reset password."
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem($"Internal server error: {ex.Message}");
            }
        }
    }
}
