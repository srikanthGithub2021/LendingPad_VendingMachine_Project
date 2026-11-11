using VendingMachineApi.Data;
using VendingMachineApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Register In-Memory Store as Singleton to persist state across requests
builder.Services.AddSingleton<InMemoryVendingStore>();
builder.Services.AddSingleton<IHardwareController, MockHardwareController>();
builder.Services.AddScoped<VendingService>();


// Register Third-Party Payment Client with Mock HTTP Handler for testing
builder.Services.AddTransient<MockPaymentGatewayHandler>();
builder.Services.AddHttpClient<IThirdPartyPaymentClient, ThirdPartyPaymentClient>()
    .ConfigurePrimaryHttpMessageHandler<MockPaymentGatewayHandler>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


