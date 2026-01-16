# Parking Management System

Comprehensive parking facility platform that combines an ASP.NET Core backend, a React/Vite frontend dashboard, and supporting computer-vision tooling for license plate recognition.

## Repository Layout

```
ParkingManagementSystem/
├── backend/
│   ├── Parking.API/              # ASP.NET Core Web API (controllers, background jobs, JSON datastore)
│   ├── Parking.Core/             # Domain entities and interfaces
│   ├── Parking.Infrastructure/   # Data access, repositories, external adapters
│   └── Parking.Services/         # Application services layer
├── frontend/                     # React + Vite dashboard (TailwindCSS)
│   ├── src/config/               # API configuration helpers
│   ├── src/layouts/              # Shared layout components
│   └── src/pages/                # Dashboard pages (Login, Membership, Reports, etc.)
├── ocr-service/                  # Python utilities for plate recognition & data labeling
├── licens/                       # Licensing-related assets
├── ngrok.yml                     # Tunnel configuration for secure exposure
├── SeedData.ps1                  # Helper to seed JSON datastore
├── StartAll.(bat|ps1)            # Convenience scripts to launch backend & frontend
└── README.md                     # (This file)
```

## Getting Started

### Backend (ASP.NET Core)
1. `cd backend`
2. Restore & build: `dotnet build ParkingSystem.sln`
3. Run API: `dotnet run --project Parking.API`
4. Configuration: adjust `backend/Parking.API/appsettings.*.json` as needed.

### Frontend (React/Vite)
1. `cd frontend`
2. Install dependencies: `npm install`
3. Start dev server: `npm run dev`
4. Update environment/API targets in `frontend/src/config/api.js`.

### License Plate OCR Tools
- Python scripts live in `ocr-service/` with requirements listed in `ocr-service/requirements.txt`.
- Create a virtual environment and install dependencies with `pip install -r requirements.txt`.
- Datasets live under `ocr-service/data_labeled/` with intermediate outputs in the `results_*` folders.

## Useful Scripts
- **SeedData.ps1**: Pre-populates JSON stores for quick demos.
- **StartAll.ps1 / StartAll.bat**: Launches backend API and frontend UI together.
- **DevTunnel.ps1** (frontend): Helps expose the local UI via an authenticated tunnel.

## Contributing
1. Fork/branch from `main`.
2. Keep backend/front-end changes in separate commits when possible.
3. Run tests or manual verification before opening a PR.
4. Follow project coding standards and document any new configuration flags in this README.
