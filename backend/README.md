# Parking Management Backend

Modular ASP.NET Core backend that exposes REST APIs for parking check-in/out, membership management, reporting, and integration points such as gate hardware and payment adapters. License plates are entered manually by attendants by default, but you can optionally forward images to an external Python-based recognition service (fork of [PhucDaizz/LicensePlateRecognitionVNAPI](https://github.com/PhucDaizz/LicensePlateRecognitionVNAPI)) via the `/api/PlateRecognition` endpoint once the feature flag is enabled. The solution is split into API, Core domain models, Infrastructure (repositories and external drivers), and Services layers.

## Solution Layout

```
backend/
├── .gitignore                     # Ignore patterns for all backend projects
├── BUILD_ERRORS_FIX_GUIDE.md      # Steps to clear IDE cache/build artifacts when Roslyn gets stuck
├── CleanRebuild.bat               # Helper script to purge bin/obj/.vs and rebuild via dotnet CLI
├── ParkingSystem.sln              # Multi-project solution referencing API, Core, Infrastructure, Services
├── README.md                      # This file
├── Parking.API/                   # ASP.NET Core Web API host
│   ├── appsettings.json           # Default logging + hosting config
│   ├── appsettings.Development.json
│   ├── Parking.API.csproj
│   ├── Program.cs                 # WebApplication bootstrap, DI graph, CORS, hosted services
│   ├── BackgroundServices/
│   │   └── SystemScheduler.cs     # Hosted service that expires monthly tickets every 60s loop
│   ├── Controllers/               # REST controllers for check-in/out, membership, reports, etc.
│   │   ├── CheckInController.cs
│   │   ├── CheckOutController.cs
│   │   ├── IncidentController.cs
│   │   ├── MembershipController.cs
│   │   ├── PaymentController.cs
│   │   ├── PlateRecognitionController.cs
│   │   ├── PricePolicyController.cs
│   │   ├── ReportController.cs
│   │   ├── UserAccountController.cs
│   │   └── ZonesController.cs
│   ├── DataStore/                 # JSON files that act as persistence layer (seeded at runtime)
│   │   ├── customers.json
│   │   ├── incidents.json
│   │   ├── membership_policies.json
│   │   ├── monthly_tickets.json
│   │   ├── sessions.json
│   │   ├── tickets.json
│   │   ├── users.json
│   │   ├── zones.json
│   │   └── _backup/
│   ├── Properties/
│   │   └── launchSettings.json    # Local run profiles (http/https)
│   ├── bin/                       # Build output (gitignored)
│   └── obj/                       # Intermediate build files (gitignored)
├── Parking.Core/                  # Domain entities + interface contracts
│   ├── Parking.Core.csproj
│   ├── Entities/
│   │   ├── AuditLog.cs
│   │   ├── CoreEntities.cs
│   │   ├── Incident.cs
│   │   ├── MembershipDtos.cs
│   │   ├── MembershipEntities.cs
│   │   ├── MembershipPolicy.cs
│   │   ├── ParkingStructure.cs
│   │   ├── PaymentGatewayResult.cs
│   │   ├── PricePolicy.cs
│   │   ├── ReportEntities.cs
│   │   ├── TicketPrint.cs
│   │   ├── UserEntities.cs
│   │   ├── Vehicle.cs
│   │   └── PlateRecognitionResult.cs
│   ├── Configuration/
│   │   └── PlateRecognitionOptions.cs
│   ├── Interfaces/
│   │   ├── IIncidentRepository.cs
│   │   ├── IMembershipRepositories.cs
│   │   ├── IParkingRepositories.cs
│   │   ├── IRepository.cs
│   │   ├── IServices.cs
│   │   └── IUserRepository.cs
│   ├── bin/
│   └── obj/
├── Parking.Infrastructure/        # JSON repositories + external adapters (payment, hardware)
│   ├── Parking.Infrastructure.csproj
│   ├── Data/
│   │   └── JsonFileHelper.cs
│   ├── External/
│   │   ├── MockGateDevice.cs
│   │   ├── MockPaymentGatewayAdapter.cs
│   │   └── LicensePlateRecognitionClient.cs
│   ├── Repositories/
│   │   ├── AuditLogRepository.cs
│   │   ├── BaseJsonRepository.cs
│   │   ├── IncidentRepository.cs
│   │   ├── MembershipPolicyRepository.cs
│   │   ├── MembershipRepositories.cs
│   │   ├── ParkingSessionRepository.cs
│   │   ├── ParkingZoneRepository.cs
│   │   ├── PricePolicyRepository.cs
│   │   ├── TicketRepository.cs
│   │   └── UserRepository.cs
│   ├── Templates/
│   │   └── TicketTemplateService.cs
│   ├── bin/
│   └── obj/
└── Parking.Services/              # Application/business services coordinating repositories
	├── Parking.Services.csproj
	├── Services/
	│   ├── IncidentService.cs
	│   ├── MembershipService.cs
	│   ├── ParkingService.cs
	│   └── PaymentService.cs
	├── bin/
	└── obj/
```

## File Guide

### Root artifacts

| Path | Contents |
| ---- | -------- |
| .gitignore | Ignores bin/obj, IDE caches, node_modules, logs, temp editor files. |
| BUILD_ERRORS_FIX_GUIDE.md | Documents how to fully reset the IDE caches or use dotnet CLI when namespaces resolve incorrectly. |
| CleanRebuild.bat | Recursively removes `bin`, `obj`, `.vs`, then runs `dotnet clean/restore/build`. |
| ParkingSystem.sln | Solution referencing API, Core, Infrastructure, Services projects. |
| README.md | Detailed tree (this document). |

### Parking.API (presentation layer)

| File | Contents |
| ---- | -------- |
| appsettings*.json | Logging levels, allowed hosts, and other host configuration. |
| Program.cs | Configures controllers, Swagger, custom CORS, DI registrations for repositories/services/adapters, plus the hosted scheduler. |
| BackgroundServices/SystemScheduler.cs | `BackgroundService` loop that every 60 seconds fetches monthly tickets and marks overdue ones as `Expired`, logging progress. |
| Controllers/CheckInController.cs | Provides the manual plate entry check-in endpoint that defers to `IParkingService`. |
| Controllers/CheckOutController.cs | Resolves pending session by ticket or plate and returns fee preview via `IParkingService.CheckOutAsync`. |
| Controllers/IncidentController.cs | Wraps `IIncidentService` to report/resolve incidents and list history. |
| Controllers/MembershipController.cs | Creates vehicles/customers, issues monthly tickets, lists tickets/policies, deletes tickets. |
| Controllers/PaymentController.cs | Calls `IPaymentService` to finalize payments and open gates. |
| Controllers/PlateRecognitionController.cs | Accepts multipart image uploads and proxies them to the Python LPR service, returning normalized plate candidates. |
| Controllers/PricePolicyController.cs | CRUD for pricing policies and lost ticket penalty configuration. |
| Controllers/ReportController.cs | Produces active session list, revenue report, and traffic report from `IParkingSessionRepository`. |
| Controllers/UserAccountController.cs | Authenticates admin/attendant accounts, CRUD for users, toggles status with safety checks. |
| Controllers/ZonesController.cs | CRUD for parking zones (lanes) and capacity management. |
| DataStore/*.json | JSON persistence for customers, incidents, memberships, active sessions, tickets, user accounts, and zone definitions plus backups. |
| Properties/launchSettings.json | Local profile binding HTTP to `0.0.0.0:5166` and HTTPS `localhost:7275`. |

### Parking.Core (domain + contracts)

| File | Contents |
| ---- | -------- |
| Entities/AuditLog.cs | Entity recording system actions for security auditing. |
| Entities/CoreEntities.cs | Declares `Ticket`, `Payment`, and `ParkingSession` aggregate with helper methods to close sessions and attach payments. |
| Entities/Incident.cs | Incident aggregate with status, metadata, and resolution notes. |
| Entities/MembershipDtos.cs | DTOs for membership interactions (Request/Response models). |
| Entities/MembershipEntities.cs | `Customer` and `MonthlyTicket` objects plus helper to validate ticket status. |
| Entities/MembershipPolicy.cs | POCO describing monthly pricing plans per vehicle type. |
| Entities/ParkingStructure.cs | `ParkingZone` (capacity management, price policy) and `ParkingLot` helper for zone resolution. |
| Entities/PaymentGatewayResult.cs | DTO representing the outcome of a payment gateway transaction. |
| Entities/PricePolicy.cs | Abstract `PricePolicy` with `ParkingFeePolicy` (hourly calc) and `LostTicketFeePolicy` (flat penalty). |
| Entities/ReportEntities.cs | DTOs for revenue/traffic reports with metadata windows. |
| Entities/TicketPrint.cs | Model for generating printed ticket content. |
| Entities/UserEntities.cs | Polymorphic admin/attendant accounts with capability helpers plus JSON discriminator attributes. |
| Entities/Vehicle.cs | Abstract `Vehicle` and concrete types (car, motorbike, bicycle + electric variants) overriding fee factors. |
| Entities/PlateRecognitionResult.cs | Value object that wraps success flag, normalized plate list, and error metadata returned by the OCR bridge. |
| Interfaces/*.cs | Repository/service/contracts for parking sessions, zones, tickets, incidents, memberships, hardware abstractions, payment gateway, and generic CRUD base interface. |
| Configuration/PlateRecognitionOptions.cs | Strongly-typed binding for the `PlateRecognition` configuration section (base URL, endpoint, timeout). |

### Parking.Infrastructure (data + adapters)

| File | Contents |
| ---- | -------- |
| Data/JsonFileHelper.cs | Shared serializer helper with custom converters to persist polymorphic `Vehicle` and `PricePolicy` objects. |
| External/MockGateDevice.cs | Simulated gate hardware that logs gate open actions; also exposes demo `ReadPlateAsync`. |
| External/MockPaymentGatewayAdapter.cs | Mock adapter that logs and always approves payments after a delay. |
| External/LicensePlateRecognitionClient.cs | Typed `HttpClient` that posts image bytes to the Python OCR service and normalizes the response. |
| Repositories/AuditLogRepository.cs | Persists audit logs for system actions. |
| Repositories/BaseJsonRepository.cs | Generic JSON-backed CRUD implementation used by specific repositories. |
| Repositories/IncidentRepository.cs | Adds query for unresolved incidents. |
| Repositories/MembershipPolicyRepository.cs | Seeds default policies and resolves pricing by vehicle type. |
| Repositories/MembershipRepositories.cs | Customer + monthly ticket repositories with phone lookup, active ticket lookup, expired filter. |
| Repositories/ParkingSessionRepository.cs | Session persistence plus lookups by plate or ticket ID. |
| Repositories/ParkingZoneRepository.cs | Seeds default zones, enforces capacity, finds suitable zones based on vehicle type/electric flag. |
| Repositories/PricePolicyRepository.cs | Persistence for dynamic pricing policies. |
| Repositories/TicketRepository.cs | Simple JSON repository for ad-hoc tickets. |
| Repositories/UserRepository.cs | Provides username lookup for authentication. |
| Templates/TicketTemplateService.cs | Generates HTML content for thermal printers from ticket data. |

### Parking.Services (application orchestration)

| File | Contents |
| ---- | -------- |
| Services/AuditService.cs | Logic for recording critical system actions (login, check-in, etc.) to the audit log. |
| Services/IncidentService.cs | Implements `IIncidentService` to log, resolve, and list incidents via repository. |
| Services/MembershipService.cs | Registers/extents monthly tickets, ensures customers exist, enforces single active ticket per plate, surfaces pricing and ticket lists. |
| Services/ParkingService.cs | Coordinates zone selection, ticket issuance, session persistence, and gate control for check-in/out flows. |
| Services/PaymentService.cs | Calls payment gateway, creates receipts, updates sessions, and triggers gate opening after successful payment. |
| Services/JwtService.cs | Handles JWT token generation with secure key retrieval (Env / Config). |
| Services/BcryptPasswordHasher.cs | Secure password hashing using BCrypt. |

> **Note:** `bin/` and `obj/` folders within each project contain compiled artifacts and are regenerated on every build.

## Plate Recognition Service Bridge

### Configuration (`appsettings*.json`)

```
"PlateRecognition": {
	"Enabled": false,
	"BaseUrl": "http://localhost:5000",
	"RecognizeEndpoint": "/recognize",
	"TimeoutSeconds": 30
}
```

Set `Enabled` to `true` only when you want to expose `/api/PlateRecognition` (the controller is hidden from routing/Swagger while disabled, and a stub client is used so other services keep functioning). Adjust `BaseUrl` to wherever the Python service is hosted (container, VM, dev tunnel). `RecognizeEndpoint` normally stays `/recognize`, and the timeout can be tuned depending on average image size (still-image uploads finish comfortably under 30 seconds on CPU).

### Running the Python OCR service

1. Clone the upstream repo next to this solution (or reuse the existing `ocr-service` folder):
	 ```powershell
	 git clone https://github.com/PhucDaizz/LicensePlateRecognitionVNAPI.git
	 cd LicensePlateRecognitionVNAPI
	 python -m venv .venv
	 .venv\Scripts\activate
	 pip install -r requirements.txt
	 ```
2. Ensure `best.pt` lives in the repo root and that the first EasyOCR run can reach the internet to download its language packs.
3. Start the API:
	 ```powershell
	 uvicorn main_api:app --host 0.0.0.0 --port 5000 --reload
	 ```
4. Update `PlateRecognition:BaseUrl` if you expose the service via another host/port (e.g., Docker, ngrok, dev tunnel). Finally, switch `PlateRecognition:Enabled` to `true` and restart `Parking.API` to reveal the endpoint.

### Calling from the ASP.NET API

- Endpoint: `POST /api/PlateRecognition` (only available when `PlateRecognition:Enabled` is `true`)
- Body: `multipart/form-data` with one key named `file` that contains the captured image.
- Response:
	```json
	{
		"success": true,
		"plates": ["51A-123.45"],
		"error": null
	}
	```

Example curl:

```bash
curl -X POST https://localhost:5166/api/PlateRecognition ^
	-H "accept: application/json" ^
	-F "file=@sample.jpg"
```

If the feature flag is off, the endpoint is hidden and attendants should continue with manual entry. When enabled but the Python service is offline, the controller returns HTTP 502 with an explanatory error so the frontend can prompt the attendant to retry or fall back to manual entry.