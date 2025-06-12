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
                [FromQuery] string deviceId = "d7620a1b553407a7",
                [FromQuery] int closed = 0) =>
            {
                try
                {
                    var accounts = new List<AccountModel>();

                    var accountTypeMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "SHARE", 1 },
            { "SAVING", 2 },
            { "FIXED", 3 },
            { "LOAN", 4 },
            { "RECCURING", 5 },
            { "PIGMYAGENT", 6 },
            { "PIGMY", 7 }
        };

                    if (!accountTypeMap.TryGetValue(accountType.Trim().ToUpper(), out int acType))
                        return Results.BadRequest($"Invalid account type: '{accountType}'");

                    using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
                    await conn.OpenAsync();

                    using var cmd = new SqlCommand("App_GetCustomerAccount", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@CustomerId", customerId);
                    cmd.Parameters.AddWithValue("@AcType", acType);
                    cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                    cmd.Parameters.AddWithValue("@Closed", closed);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var account = new AccountModel
                        {
                            PrimaryId = reader["PrimaryId"] != DBNull.Value ? Convert.ToInt32(reader["PrimaryId"]) : -1,
                            SubSchemeId = reader["SubSchemeId"] != DBNull.Value ? Convert.ToInt32(reader["SubSchemeId"]) : 0,
                            PigmyAgentId = reader["PigmyAgentId"] != DBNull.Value ? Convert.ToInt32(reader["PigmyAgentId"]) : 0,
                            AccountNumber = reader["AccountNumber"]?.ToString(),
                            OldAccountNumber = reader["OldAccountNumber"]?.ToString(),
                            CustomerId = reader["CustomerId"] != DBNull.Value ? Convert.ToInt32(reader["CustomerId"]) : 0,
                            OpeningDate = reader["OpeningDate"] != DBNull.Value ? Convert.ToDateTime(reader["OpeningDate"]) : null,
                            Balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0,
                            Closed = reader["Closed"] != DBNull.Value && Convert.ToBoolean(reader["Closed"]),
                            ClosedDate = reader["ClosedDate"] != DBNull.Value ? Convert.ToDateTime(reader["ClosedDate"]) : null,
                            IsApplyInterest = reader["IsApplyInterest"] != DBNull.Value && Convert.ToBoolean(reader["IsApplyInterest"]),
                            ODLoanAccountNo = reader["ODLoanAccountNo"]?.ToString()
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
