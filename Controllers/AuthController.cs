using Azure;
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
            if (string.IsNullOrWhiteSpace(request.CustomerId) ||
                string.IsNullOrWhiteSpace(request.Pin) ||
                string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return BadRequest(new SignupResponse
                {
                    Success = false,
                    Message = "CustomerId, PIN, and DeviceId are required."
                });
            }

            if (request.Pin.Length != 4 || !int.TryParse(request.Pin, out _))
            {
                return BadRequest(new SignupResponse
                {
                    Success = false,
                    Message = "PIN must be a 4-digit number."
                });
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var checkCmd = new SqlCommand("SELECT UserPassword, IMEINo FROM Customer WHERE CustomerId = @CustomerId", conn);
                checkCmd.Parameters.AddWithValue("@CustomerId", request.CustomerId);

                using var reader = await checkCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return BadRequest(new SignupResponse
                    {
                        Success = false,
                        Message = "Customer ID not found."
                    });
                }

                var existingPassword = reader["UserPassword"]?.ToString();
                var existingDevice = reader["IMEINo"]?.ToString();
                reader.Close();

                if (!string.IsNullOrWhiteSpace(existingPassword) && !string.IsNullOrWhiteSpace(existingDevice))
                {
                    if (existingDevice != request.DeviceId)
                    {
                        if (!request.ForceOverride)
                        {
                            return BadRequest(new SignupResponse
                            {
                                Success = false,
                                Message = "Account already registered on another device.",
                                DeviceGuid = existingDevice
                            });
                        }
                        // else: allow override
                    }
                    else
                    {
                        return BadRequest(new SignupResponse
                        {
                            Success = false,
                            Message = "Account already registered. Please login."
                        });
                    }
                }

                string hashedPin = SecurityHelper.HashPassword(request.Pin);

                var updateCmd = new SqlCommand(@"
            UPDATE Customer 
            SET UserPassword = @UserPassword, IMEINo = @IMEINo 
            WHERE CustomerId = @CustomerId", conn);

                updateCmd.Parameters.AddWithValue("@UserPassword", hashedPin);
                updateCmd.Parameters.AddWithValue("@IMEINo", request.DeviceId);
                updateCmd.Parameters.AddWithValue("@CustomerId", request.CustomerId);

                int rows = await updateCmd.ExecuteNonQueryAsync();

                if (rows > 0)
                {
                    return Ok(new SignupResponse
                    {
                        Success = true,
                        Message = "Signup successful. You can now login.",
                        DeviceGuid = request.DeviceId
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


        [HttpPost("check-device")]
        public async Task<IActionResult> CheckDevice([FromBody] DeviceCheckRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return BadRequest(new { Success = false, Message = "DeviceId is required." });
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
            SELECT CustomerId, FirstName, MiddleName, SurName 
            FROM Customer 
            WHERE IMEINo = @DeviceId", conn);
                cmd.Parameters.AddWithValue("@DeviceId", request.DeviceId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Ok(new DeviceCheckResponse
                    {
                        Success = false,
                        Message = "No account found. Please sign up."
                    });
                }

                var fullName = string.Join(" ",
                    reader["FirstName"].ToString(),
                    reader["MiddleName"].ToString(),
                    reader["SurName"].ToString()).Trim();

                return Ok(new DeviceCheckResponse
                {
                    Success = true,
                    CustomerId = Convert.ToInt32(reader["CustomerId"]),
                    FullName = fullName,
                    Message = "Device recognized."
                });
            }
            catch (Exception ex)
            {
                return Problem($"Error: {ex.Message}");
            }
        }



        // 🔹 POST: /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.Pin))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "DeviceId and PIN are required."
                });
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                // Get customer record by device
                var cmd = new SqlCommand("SELECT CustomerId, UserPassword FROM Customer WHERE IMEINo = @DeviceId", conn);
                cmd.Parameters.AddWithValue("@DeviceId", request.DeviceId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "No customer found for this device. Please sign up."
                    });
                }

                var storedHash = reader["UserPassword"]?.ToString();
                var customerId = Convert.ToInt32(reader["CustomerId"]);

                if (string.IsNullOrWhiteSpace(storedHash))
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "PIN not set. Please complete signup."
                    });
                }

                // Compare hashed PIN
                string inputHash = SecurityHelper.HashPassword(request.Pin);
                bool isValid = storedHash == inputHash;

                return Ok(new LoginResponse
                {
                    Success = isValid,
                    Message = isValid ? "Login successful." : "Invalid PIN.",
                    CustomerId = isValid ? customerId : 0
                });
            }
            catch (Exception ex)
            {
                return Problem($"Login error: {ex.Message}");
            }
        }

        // 🔹 POST: /api/auth/logout-all
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAllDevices([FromBody] LogoutAllRequest request)
        {
            if (request.CustomerId <= 0)
            {
                return BadRequest(new LogoutAllResponse
                {
                    Success = false,
                    Message = "Invalid Customer ID."
                });
            }

            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var updateCmd = new SqlCommand("UPDATE Customer SET IMEINo = NULL WHERE CustomerId = @CustomerId", conn);
                updateCmd.Parameters.AddWithValue("@CustomerId", request.CustomerId);

                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    return Ok(new LogoutAllResponse
                    {
                        Success = true,
                        Message = "Logged out from all devices successfully."
                    });
                }
                else
                {
                    return Ok(new LogoutAllResponse
                    {
                        Success = false,
                        Message = "No records updated. Customer may not exist."
                    });
                }
            }
            catch (Exception ex)
            {
                return Problem($"Internal Server Error: {ex.Message}");
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
