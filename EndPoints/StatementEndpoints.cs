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

                using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                using var cmd = new SqlCommand("App_GetCustomerAccount", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@CustomerId", (object?)customerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MasterType", (object?)accountType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Closed", (object?)closed ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var account = new AccountModel
                    {
                        PrimaryId = reader.GetInt32(reader.GetOrdinal("PrimaryId")),
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        SubSchemeId = reader.GetInt32(reader.GetOrdinal("SubSchemeId")),
                        PigmyAgentId = reader.GetInt32(reader.GetOrdinal("PigmyAgentId")),
                        AccountNumber = reader.GetString(reader.GetOrdinal("AccountNumber")),
                        OldAccountNumber = reader["OldAccountNumber"] as string,
                        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                        OpeningDate = reader["OpeningDate"] as DateTime?,
                        ExpiryDate = reader["ExpiryDate"] as DateTime?,
                        RateOfInterestId = reader["RateOfInterestId"] as int?,
                        IAmount = reader["IAmount"] as decimal?,
                        MaturityAmount = reader["MaturityAmount"] as decimal?,
                        Installment = reader["Installment"] as decimal?,
                        Closed = reader.GetBoolean(reader.GetOrdinal("Closed")),
                        ClosedDate = reader["ClosedDate"] as DateTime?,
                        IsApplyInterest = reader.GetBoolean(reader.GetOrdinal("IsApplyInterest")),
                        ODLoanAccountNo = reader["ODLoanAccountNo"] as string,
                        IsOverdueAccount = reader["IsOverdueAccount"] as bool?,
                        DirectorName = reader["DirectorName"] as string,
                        Balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0
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
