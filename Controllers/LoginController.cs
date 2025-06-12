using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using BankingAPI.Models;
using BankingAPI.Services;

namespace BankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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
                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
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
                return StatusCode(500, new { Success = false, Message = $"Error: {ex.Message}" });
            }
        }
    }
}
