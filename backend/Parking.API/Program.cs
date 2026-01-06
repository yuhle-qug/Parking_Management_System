using Parking.Core.Interfaces;
using Parking.Infrastructure.External;
using Parking.Infrastructure.Repositories;
using Parking.Services.Services;
using Parking.API.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS cho frontend Vite (http://localhost:5173)
builder.Services.AddCors(options =>
{
	options.AddPolicy("frontend", policy =>
	{
		if (builder.Environment.IsDevelopment())
		{
			policy
				.SetIsOriginAllowed(origin =>
				{
					if (string.IsNullOrWhiteSpace(origin)) return false;
					if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

					// Allow local dev (localhost/127.0.0.1) + external dev tunnels.
					if (uri.IsLoopback) return true;
					if (uri.Host.EndsWith(".loca.lt", StringComparison.OrdinalIgnoreCase)) return true;
					if (uri.Host.EndsWith(".ngrok-free.dev", StringComparison.OrdinalIgnoreCase)) return true;

					return false;
				})
				.AllowAnyHeader()
				.AllowAnyMethod();
		}
		else
		{
			policy
				.WithOrigins("http://localhost:5173")
				.AllowAnyHeader()
				.AllowAnyMethod();
		}
	});
});

// --- [OOP - DEPENDENCY INJECTION CONFIGURATION] ---

// 1. Repositories (Data Layer)
builder.Services.AddScoped<IParkingSessionRepository, ParkingSessionRepository>();
builder.Services.AddScoped<IParkingZoneRepository, ParkingZoneRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>(); // Bạn cần tạo file này rỗng kế thừa BaseJsonRepository nếu chưa có
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IMonthlyTicketRepository, MonthlyTicketRepository>();
builder.Services.AddScoped<IMembershipPolicyRepository, MembershipPolicyRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();

// 2. Services (Logic Layer)
builder.Services.AddScoped<IParkingService, ParkingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();

// 3. Infrastructure / External (Device, Payment)
builder.Services.AddSingleton<IGateDevice, MockGateDevice>(); // Singleton vì thiết bị là duy nhất
builder.Services.AddSingleton<IPaymentGateway, MockPaymentGatewayAdapter>();

// 4. Background Services (Scheduler)
builder.Services.AddHostedService<SystemScheduler>();

// --------------------------------------------------

var app = builder.Build();

// Force-create zones.json at startup (seed data) by resolving the repository once.
using (var scope = app.Services.CreateScope())
{
	// Ensure zones.json is seeded at startup (instead of waiting for the first CheckIn request).
	scope.ServiceProvider.GetRequiredService<IParkingZoneRepository>();
	// Ensure membership_policies.json is seeded for pricing policies.
	scope.ServiceProvider.GetRequiredService<IMembershipPolicyRepository>();
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");
app.UseAuthorization();
app.MapControllers();

app.Run();