using BankingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BankingAPI.Endpoints
{
    public static class StatementEndpoints
    {
        public static void MapStatementEndpoints(this IEndpointRouteBuilder app, IConfiguration config)
        {
            // 🔹 Get Account Types
            app.MapGet("/api/statement/account-types/{customerId:int}", async (int customerId) =>
            {
                var results = new List<string>();

                try
                {
                    using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
                    await conn.OpenAsync();

                    using var cmd = new SqlCommand("App_AccountList", conn)
                    {
                        CommandType = CommandType.StoredProcedure,
                        CommandTimeout=200
                    };
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                            results.Add(reader.GetString(0));
                    }

                    return Results.Ok(results);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to load account types: " + ex.Message);
                }
            });

            // 🔹 Get Customer Accounts by Type
            app.MapGet("/api/statement/accounts", async (
                IConfiguration config,
                [FromQuery] int customerId,
                [FromQuery] string accountType,
                [FromQuery] string deviceId = "083ea3911295b82d",
                [FromQuery] int closed = 0) =>
            {
                try
                {
                    var accounts = new List<AccountModel>();

                    // 🔧 Convert accountType string to corresponding AcType integer
                    int acType = accountType.ToUpper() switch
                    {
                        "SHARE" => 1,
                        "SAVING" => 2,
                        "FIXED" => 3,
                        "LOAN" => 4,
                        "RECCURING" => 5,
                        "PIGMYAGENT" => 6,
                        "PIGMY" => 7,
                        _ => throw new ArgumentException($"Invalid accountType: {accountType}")
                    };

                    using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
                    await conn.OpenAsync();

                    using var cmd = new SqlCommand("App_GetCustomerAccount", conn)
                    {
                        CommandType = CommandType.StoredProcedure,
                        CommandTimeout = 300
                    };

                    cmd.Parameters.AddWithValue("@AcType", acType);
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                    cmd.Parameters.AddWithValue("@Closed", closed);

                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var account = new AccountModel
                        {
                            PrimaryId = reader.GetInt32(reader.GetOrdinal("PrimaryId")),
                            Id = reader.HasColumn("Id") && !reader.IsDBNull("Id") ? reader.GetInt32(reader.GetOrdinal("Id")) : 0,
                            SubSchemeId = reader.GetInt32(reader.GetOrdinal("SubSchemeId")),
                            PigmyAgentId = reader.HasColumn("PigmyAgentId") ? reader.GetInt32(reader.GetOrdinal("PigmyAgentId")) : 0,
                            AccountNumber = reader["AccountNumber"]?.ToString(),
                            SubSchemeName = reader["SubSchemeName"]?.ToString(),
                            OldAccountNumber = reader.HasColumn("OldAccountNumber") ? reader["OldAccountNumber"]?.ToString() : null,
                            CustomerId = reader.HasColumn("CustomerId") ? reader.GetInt32(reader.GetOrdinal("CustomerId")) : customerId,
                            OpeningDate = reader.HasColumn("OpeningDate") && !reader.IsDBNull("OpeningDate") ? reader.GetDateTime(reader.GetOrdinal("OpeningDate")) : null,
                            ExpiryDate = reader.HasColumn("ExpiryDate") && !reader.IsDBNull("ExpiryDate") ? reader.GetDateTime(reader.GetOrdinal("ExpiryDate")) : null,
                            RateOfInterestId = reader.HasColumn("RateOfInterestId") && !reader.IsDBNull("RateOfInterestId") ? reader.GetInt32(reader.GetOrdinal("RateOfInterestId")) : null,
                            IAmount = reader.HasColumn("IAmount") && !reader.IsDBNull("IAmount") ? reader.GetDecimal(reader.GetOrdinal("IAmount")) : null,
                            MaturityAmount = reader.HasColumn("MaturityAmount") && !reader.IsDBNull("MaturityAmount") ? reader.GetDecimal(reader.GetOrdinal("MaturityAmount")) : null,
                            Installment = reader.HasColumn("Installment") && !reader.IsDBNull("Installment") ? reader.GetDecimal(reader.GetOrdinal("Installment")) : null,
                            Closed = reader.HasColumn("Closed") && !reader.IsDBNull("Closed") ? reader.GetBoolean(reader.GetOrdinal("Closed")) : false,
                            ClosedDate = reader.HasColumn("ClosedDate") && !reader.IsDBNull("ClosedDate") ? reader.GetDateTime(reader.GetOrdinal("ClosedDate")) : null,
                            IsApplyInterest = reader.HasColumn("IsApplyInterest") && !reader.IsDBNull("IsApplyInterest") ? reader.GetBoolean(reader.GetOrdinal("IsApplyInterest")) : false,
                            ODLoanAccountNo = reader.HasColumn("ODLoanAccountNo") ? reader["ODLoanAccountNo"]?.ToString() : null,
                            IsOverdueAccount = reader.HasColumn("IsOverdueAccount") && !reader.IsDBNull("IsOverdueAccount") ? reader.GetBoolean(reader.GetOrdinal("IsOverdueAccount")) : null,
                            DirectorName = reader.HasColumn("DirectorName") ? reader["DirectorName"]?.ToString() : null,
                            Balance = reader.HasColumn("Balance") && !reader.IsDBNull("Balance") ? Convert.ToDecimal(reader["Balance"]) : 0
                        };

                        accounts.Add(account);
                    }

                    return Results.Ok(accounts);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch account details: " + ex.Message);
                }
            });


            // 🔹 Get Transaction Statement
            app.MapGet("/api/statement/transactions", async (
                [FromQuery] int customerId,
                [FromQuery] string accountType,
                [FromQuery] DateTime fromDate,
                [FromQuery] DateTime toDate,
                IConfiguration config) =>
            {
                var transactions = new List<TransactionModel>();

                try
                {
                    using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
                    await conn.OpenAsync();

                    using var cmd = new SqlCommand("App_GetNewStatement", conn)
                    {
                        CommandType = CommandType.StoredProcedure,
                        CommandTimeout = 200
                    };

                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    cmd.Parameters.AddWithValue("@AccountType", accountType);
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        transactions.Add(new TransactionModel
                        {
                            Description = reader["Description"]?.ToString(),
                            Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                            Date = reader["Date"] != DBNull.Value ? Convert.ToDateTime(reader["Date"]) : DateTime.MinValue
                        });
                    }

                    return Results.Ok(transactions);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch transaction statement: " + ex.Message);
                }
            });
        }
    }
}
