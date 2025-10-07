using TechChallengePayments.Api.Configurations;
using TechChallengePayments.Api.Middlewares;
using TechChallengePayments.Application;
using TechChallengePayments.Data;
using TechChallengePayments.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();

builder.Services.AddSqlContext(builder.Configuration);
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddServices();

builder.AddSerilog();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();