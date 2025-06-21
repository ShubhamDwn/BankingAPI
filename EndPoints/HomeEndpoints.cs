using BankingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BankingAPI.Endpoints
{
    //public static class HomeEndpoints
    //{
    //    public static void MapHomeEndpoints(this IEndpointRouteBuilder app, IConfiguration config)
    //    {
    //        app.MapGet("/api/home/{customerId:int}", async (int customerId) =>
    //        {
    //            var response = new HomeResponse();

    //            try
    //            {
    //                var connectionString = config.GetConnectionString("DefaultConnection");
    //                using var conn = new SqlConnection(connectionString);
    //                await conn.OpenAsync();

    //                var cmd = new SqlCommand(@"
    //                    SELECT FirstName, MiddleName, SurName 
    //                    FROM Customer 
    //                    WHERE CustomerId = @CustomerId", conn);

    //                cmd.Parameters.AddWithValue("@CustomerId", customerId);

    //                using var reader = await cmd.ExecuteReaderAsync();
    //                if (await reader.ReadAsync())
    //                {
    //                    var fullName = string.Join(" ", reader["FirstName"], reader["MiddleName"], reader["SurName"]);
    //                    response.CustomerName = fullName;
    //                }

    //                reader.Close();

    //                // Optionally get savings balance
    //                var balanceCmd = new SqlCommand(@"
    //                    SELECT Balance 
    //                    FROM Account 
    //                    WHERE CustomerId = @CustomerId AND AccountType = 'Savings'", conn);

    //                //balanceCmd.Parameters.AddWithValue("@CustomerId", customerId);

    //                //var balanceObj = await balanceCmd.ExecuteScalarAsync();
    //                var balanceObj = 50000.00M;
    //                if (balanceObj != null)
    //                {
    //                    response.SavingsBalance = Convert.ToDecimal(balanceObj);
    //                }
    //                else
    //                {
    //                    response.SavingsBalance = 50000.00M;
    //                }

    //                    return Results.Ok(response);
    //            }
    //            catch (Exception ex)
    //            {
    //                return Results.Problem("Failed to load customer data: " + ex.Message);
    //            }
    //        });
    //    }
    //}
}
