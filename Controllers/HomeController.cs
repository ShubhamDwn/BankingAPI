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
        [HttpGet("{Id}")]
        public async Task<IActionResult> GetCustomerHomeData(int Id)
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
                    WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", Id);

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

                // Get savings account with highest balance
                var getCustomerIdCmd = new SqlCommand("SELECT CustomerId FROM Customer WHERE Id = @Id", conn);
                getCustomerIdCmd.Parameters.AddWithValue("@Id", Id);
                var customerId = (int?)await getCustomerIdCmd.ExecuteScalarAsync();

                if (customerId == null)
                    return NotFound("Customer ID not found for this internal ID.");
                var balanceCmd = new SqlCommand(@"
                    SELECT TOP 1 Mast.AccountNumber, Mast.Balance
                    FROM [INDO_vwMaster_CombineMaster](NULL, @CustomerId, 0) AS Mast
                    INNER JOIN SubScheme ON Mast.SubSchemeId = SubScheme.Id
                    WHERE SubScheme.ShortName = 'SAVING'
                      AND Mast.Closed = 0
                      AND Mast.CustomerId = @CustomerId
                    ORDER BY Mast.Balance DESC", conn);

                balanceCmd.Parameters.AddWithValue("@CustomerId", customerId);

                using var balanceReader = await balanceCmd.ExecuteReaderAsync();
                if (await balanceReader.ReadAsync())
                {
                    response.SavingsAccountNumber = balanceReader["AccountNumber"].ToString();
                    response.SavingsBalance = Convert.ToDecimal(balanceReader["Balance"]);
                }
                else
                {
                    response.SavingsAccountNumber = null;
                    response.SavingsBalance = 0;
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem("Failed to load customer data: " + ex.Message);
            }
        }

    }
}