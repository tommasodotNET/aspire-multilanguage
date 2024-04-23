using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapPost("/subtract", ([FromBody]Operands operands) =>
{
    Console.WriteLine($"Subtracting {operands.OperandTwo} from {operands.OperandOne}"); 
    return Decimal.Parse(operands.OperandOne) - Decimal.Parse(operands.OperandTwo);
});

app.MapDefaultEndpoints();

app.Run();

record Operands(string OperandOne, string OperandTwo) {}
