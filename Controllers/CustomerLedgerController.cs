using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using BankingAPI.Models; 

namespace BankingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerLedgerController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CustomerLedgerController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("account-ledger")]
        public async Task<IActionResult> GetCustomerAccountLedger(
        [FromQuery] int customerId,
        [FromQuery] DateTime transactionDate,
        [FromQuery] bool isClosed = true)
        {
            try
            {
                var result = new List<CustomerAccountLedgerModel>();

                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                using var cmd = new SqlCommand("RPT_CustomerAccountLedger", conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 300
                };

                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@TransactionDate", transactionDate);
                cmd.Parameters.AddWithValue("@IsClosed", isClosed);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = "No ledger data found for the given customer and date.",
                        Data = result
                    });
                }

                while (await reader.ReadAsync())
                {
                    var model = new CustomerAccountLedgerModel
                    {
                        TransactionDate = reader.HasColumn("TransactionDate") && !reader.IsDBNull("TransactionDate")
                            ? reader.GetDateTime(reader.GetOrdinal("TransactionDate")) : DateTime.MinValue,

                        SubSchemeName = reader.HasColumn("SubSchemeName") ? reader["SubSchemeName"]?.ToString() : null,

                        PrimaryId = reader.HasColumn("PrimaryId") && !reader.IsDBNull("PrimaryId")
                            ? reader.GetInt32(reader.GetOrdinal("PrimaryId")) : 0,

                        Id = reader.HasColumn("Id") && !reader.IsDBNull("Id") ? reader.GetInt32(reader.GetOrdinal("Id")) : 0,
                        SubSchemeId = reader.HasColumn("SubSchemeId") && !reader.IsDBNull("SubSchemeId") ? reader.GetInt32("SubSchemeId") : 0,
                        PigmyAgentId = reader.HasColumn("PigmyAgentId") && !reader.IsDBNull("PigmyAgentId") ? reader.GetInt32("PigmyAgentId") : 0,

                        AccountNumber = reader.HasColumn("AccountNumber") ? reader["AccountNumber"]?.ToString() : null,
                        OldAccountNumber = reader.HasColumn("OldAccountNumber") ? reader["OldAccountNumber"]?.ToString() : null,
                        CustomerId = reader.HasColumn("CustomerId") && !reader.IsDBNull("CustomerId") ? reader.GetInt32("CustomerId") : 0,

                        OpeningDate = reader.HasColumn("OpeningDate") && !reader.IsDBNull("OpeningDate") ? reader.GetDateTime("OpeningDate") : DateTime.MinValue,
                        AsOnDate = reader.HasColumn("AsOnDate") && !reader.IsDBNull("AsOnDate") ? reader.GetDateTime("AsOnDate") : DateTime.MinValue,
                        ReceiptNo = reader.HasColumn("ReceiptNo") ? reader["ReceiptNo"]?.ToString() : null,

                        PeriodInDay = reader.HasColumn("PeriodInDay") && !reader.IsDBNull("PeriodInDay") ? reader.GetInt32("PeriodInDay") : 0,
                        PeriodInMonth = reader.HasColumn("PeriodInMonth") && !reader.IsDBNull("PeriodInMonth") ? reader.GetInt32("PeriodInMonth") : 0,
                        PeriodInYear = reader.HasColumn("PeriodInYear") && !reader.IsDBNull("PeriodInYear") ? reader.GetInt32("PeriodInYear") : 0,

                        ExpiryDate = reader.HasColumn("ExpiryDate") && !reader.IsDBNull("ExpiryDate") ? reader.GetDateTime("ExpiryDate") : null,
                        RateOfInterestId = reader.HasColumn("RateOfInterestId") && !reader.IsDBNull("RateOfInterestId") ? reader.GetInt32("RateOfInterestId") : 0,
                        IAmount = reader.HasColumn("IAmount") && !reader.IsDBNull("IAmount") ? reader.GetDecimal("IAmount") : 0,
                        MaturityAmount = reader.HasColumn("MaturityAmount") && !reader.IsDBNull("MaturityAmount") ? reader.GetDecimal("MaturityAmount") : 0,
                        Installment = reader.HasColumn("Installment") && !reader.IsDBNull("Installment") ? reader.GetDecimal("Installment") : 0,

                        Closed = reader.HasColumn("Closed") && !reader.IsDBNull("Closed") && reader.GetBoolean("Closed"),
                        ClosedDate = reader.HasColumn("ClosedDate") && !reader.IsDBNull("ClosedDate") ? reader.GetDateTime("ClosedDate") : null,
                        IsApplyInterest = reader.HasColumn("IsApplyInterest") && !reader.IsDBNull("IsApplyInterest") && reader.GetBoolean("IsApplyInterest"),
                        LastInterestDate = reader.HasColumn("LastInterestDate") && !reader.IsDBNull("LastInterestDate") ? reader.GetDateTime("LastInterestDate") : null,

                        ODLoanAccountNo = reader.HasColumn("ODLoanAccountNo") ? reader["ODLoanAccountNo"]?.ToString() : null,
                        IsOverdueAccount = reader.HasColumn("IsOverdueAccount") && !reader.IsDBNull("IsOverdueAccount") && reader.GetBoolean("IsOverdueAccount"),
                        DirectorName = reader.HasColumn("DirectorName") ? reader["DirectorName"]?.ToString() : null,
                        Balance = reader.HasColumn("Balance") && !reader.IsDBNull("Balance") ? reader.GetDecimal("Balance") : 0,

                        Name = reader.HasColumn("Name") ? reader["Name"]?.ToString() : null,
                        PermanentAddress = reader.HasColumn("PermanentAddress") ? reader["PermanentAddress"]?.ToString() : null,
                        CellPhone = reader.HasColumn("CellPhone") ? reader["CellPhone"]?.ToString() : null,
                        AreaName = reader.HasColumn("AreaName") ? reader["AreaName"]?.ToString() : null
                    };

                    result.Add(model);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                 return Problem("Failed to fetch account details: " + ex.Message);
            }
        }



    }
}
