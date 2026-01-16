using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Parking.API.BackgroundServices;
using Parking.API.Controllers;
using Parking.Core.Configuration;
using Parking.Core.Interfaces;
using Parking.Infrastructure.External;
using Parking.Infrastructure.Templates;
using Parking.Infrastructure.Repositories;
using Parking.Core.Factories; // Add this
using Parking.Services.Services;

var builder = WebApplication.CreateBuilder(args);

var plateRecognitionSettings = builder.Configuration
	.GetSection(PlateRecognitionOptions.SectionName)
	.Get<PlateRecognitionOptions>() ?? new PlateRecognitionOptions();

builder.Services.Configure<TicketTemplateOptions>(builder.Configuration.GetSection(TicketTemplateOptions.SectionName));

// Add services to the container.
builder.Services.AddControllers(options =>
{
	options.Conventions.Add(new PlateRecognitionVisibilityConvention(plateRecognitionSettings.Enabled));
});
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


// JWT Authentication Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "VERY_SECRET_KEY_FOR_PARKING_SYSTEM_123456");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ParkingSystem",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ParkingFrontend",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// --- [OOP - DEPENDENCY INJECTION CONFIGURATION] ---

// 1. Repositories (Data Layer)
builder.Services.AddScoped<IParkingSessionRepository, ParkingSessionRepository>();
builder.Services.AddScoped<IParkingZoneRepository, ParkingZoneRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>(); // Bạn cần tạo file này rỗng kế thừa BaseJsonRepository nếu chưa có
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IMonthlyTicketRepository, MonthlyTicketRepository>();
builder.Services.AddScoped<IMembershipPolicyRepository, MembershipPolicyRepository>();
builder.Services.AddScoped<IPricePolicyRepository, PricePolicyRepository>();
builder.Services.AddScoped<IMembershipHistoryRepository, MembershipHistoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// 2. Services (Logic Layer)
builder.Services.AddSingleton<IVehicleFactory, VehicleFactory>(); // Singleton is fine for a stateless Factory
builder.Services.AddScoped<IParkingService, ParkingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddSingleton<ITicketTemplateService, TicketTemplateService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();

// 3. Infrastructure / External (Device, Payment)
builder.Services.AddSingleton<IGateDevice, MockGateDevice>(); // Singleton vì thiết bị là duy nhất
builder.Services.AddSingleton<IPaymentGateway, MockPaymentGatewayAdapter>();

// 3.1 External HTTP Clients
builder.Services.Configure<PlateRecognitionOptions>(builder.Configuration.GetSection(PlateRecognitionOptions.SectionName));

if (plateRecognitionSettings.Enabled)
{
	var provider = (plateRecognitionSettings.Provider ?? string.Empty).Trim().ToLowerInvariant();
	if (provider == "viettel")
	{
		builder.Services.AddHttpClient<IPlateRecognitionClient, ViettelAlprClient>((sp, client) =>
		{
			var options = sp.GetRequiredService<IOptions<PlateRecognitionOptions>>().Value;
			client.BaseAddress = options.GetBaseUri();
			var timeout = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30;
			client.Timeout = TimeSpan.FromSeconds(timeout);
		});
	}
	else
	{
		builder.Services.AddHttpClient<IPlateRecognitionClient, LicensePlateRecognitionClient>((sp, client) =>
		{
			var options = sp.GetRequiredService<IOptions<PlateRecognitionOptions>>().Value;
			client.BaseAddress = options.GetBaseUri();
			var timeout = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30;
			client.Timeout = TimeSpan.FromSeconds(timeout);
		});
	}
}
else
{
	builder.Services.AddSingleton<IPlateRecognitionClient, DisabledPlateRecognitionClient>();
}

// 4. Background Services (Scheduler)
builder.Services.AddHostedService<SystemScheduler>();

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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

sealed class PlateRecognitionVisibilityConvention : IControllerModelConvention
{
	private readonly bool _enabled;

	public PlateRecognitionVisibilityConvention(bool enabled)
	{
		_enabled = enabled;
	}

	public void Apply(ControllerModel controller)
	{
		if (_enabled)
		{
			return;
		}

		if (controller.ControllerType != typeof(PlateRecognitionController))
		{
			return;
		}

		controller.ApiExplorer.IsVisible = false;
		controller.Actions.Clear();
		controller.Selectors.Clear();
	}
}