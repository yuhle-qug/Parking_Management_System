using Parking.Core.Interfaces;
using Parking.Infrastructure.External;
using Parking.Infrastructure.Repositories;
using Parking.Services.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS cho frontend Vite (http://localhost:5173)
builder.Services.AddCors(options =>
{
	options.AddPolicy("frontend", policy =>
		policy.WithOrigins("http://localhost:5173")
			  .AllowAnyHeader()
			  .AllowAnyMethod());
});

// --- [OOP - DEPENDENCY INJECTION CONFIGURATION] ---

// 1. Repositories (Data Layer)
builder.Services.AddScoped<IParkingSessionRepository, ParkingSessionRepository>();
builder.Services.AddScoped<IParkingZoneRepository, ParkingZoneRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>(); // Bạn cần tạo file này rỗng kế thừa BaseJsonRepository nếu chưa có

// 2. Services (Logic Layer)
builder.Services.AddScoped<IParkingService, ParkingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// 3. Infrastructure / External (Device, Payment)
builder.Services.AddSingleton<IGateDevice, MockGateDevice>(); // Singleton vì thiết bị là duy nhất
builder.Services.AddSingleton<IPaymentGateway, MockPaymentGatewayAdapter>();

// --------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");
app.UseAuthorization();
app.MapControllers();

app.Run();