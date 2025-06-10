using BankingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT UserPassword, CustomerId FROM Customer WHERE UserName = @username", conn);
            cmd.Parameters.AddWithValue("@username", request.Username);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return Unauthorized(new LoginResponse { Success = false, Message = "User not found" });

            string storedHashedPassword = reader["UserPassword"].ToString();
            int customerId = Convert.ToInt32(reader["CustomerId"]);

            string hashedInputPassword = HashPassword(request.Password);

            if (storedHashedPassword == hashedInputPassword)
            {
                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    CustomerId = customerId
                });
            }
            else
            {
                return Unauthorized(new LoginResponse { Success = false, Message = "Invalid password" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new LoginResponse { Success = false, Message = ex.Message });
        }
    }

    private string HashPassword(string password)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();

            foreach (var b in bytes)
                builder.Append(b.ToString("x2")); // convert byte to hex  

            return builder.ToString();
        }
    }
}
