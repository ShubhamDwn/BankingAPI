using BankingAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Register modular endpoints
app.MapLoginEndpoints();
app.MapSignupEndpoints();
app.MapForgotPasswordEndpoints();  
app.MapHomeEndpoints(builder.Configuration);
app.MapStatementEndpoints(app.Configuration);



app.Run();
