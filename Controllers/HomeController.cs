using BankingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IConfiguration _config;

        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCustomerHomeData(int customerId)
        {
            var response = new HomeResponse();

            try
            {
                var connectionString = _config.GetConnectionString("DefaultConnection");
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // Get customer name
                var cmd = new SqlCommand(@"
                    SELECT FirstName, MiddleName, SurName 
                    FROM Customer 
                    WHERE CustomerId = @CustomerId", conn);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var fullName = string.Join(" ",
                        reader["FirstName"].ToString(),
                        reader["MiddleName"].ToString(),
                        reader["SurName"].ToString()
                    );
                    response.CustomerName = fullName.Trim();
                }
                reader.Close();

                // Get savings balance (dummy for now)
                var balanceObj = 50000.00M;

                response.SavingsBalance = Convert.ToDecimal(balanceObj);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem("Failed to load customer data: " + ex.Message);
            }
        }
    }
}
