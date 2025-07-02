


System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;


var builder = WebApplication.CreateBuilder(args);

// Register services before building
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers(); // Important: must be before app.Build()

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization(); // Needed only if using [Authorize] attributes

// Map controllers
app.MapControllers(); // Needed for MVC/Web API controllers

// Modular minimal API endpoints (still in use)
//app.MapLoginEndpoints();
//app.MapSignupEndpoints();
//app.MapForgotPasswordEndpoints();
// Commented out HomeEndpoint as it's migrated to controller
// app.MapHomeEndpoints(builder.Configuration);
//app.MapStatementEndpoints(app.Configuration);
//app.MapBeneficiaryEndpoints();

app.Run();
