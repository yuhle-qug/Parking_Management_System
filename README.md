# Parking Management System

Comprehensive parking facility platform that combines an ASP.NET Core backend, a React/Vite frontend dashboard, and supporting computer-vision tooling for license plate recognition.

## Repository Layout

```
ParkingManagementSystem/
├── backend/                  # ASP.NET Core Web API
│   ├── Parking.API/          # API Host, Controllers, Background Services
│   ├── Parking.Core/         # Domain Entities, Interfaces
│   ├── Parking.Infrastructure/ # Repositories, External Adapters (Payment/Hardware)
│   └── Parking.Services/     # Business Logic
├── frontend/                 # React 19 + Vite Dashboard
│   ├── src/config/           # API Config
│   ├── src/layouts/          # Layout Components
│   └── src/pages/            # Application Pages (Dashboard, Membership, etc.)
├── documentation/            # Analysis & test artifacts
├── licens/LicensePlateRecognitionVNAPI/ # Python OCR service
├── ngrok.yml                 # Tunnel configuration
├── SeedData.ps1              # Helper to seed JSON datastore
├── StartAll.(bat|ps1)        # Launch scripts
└── README.md                 # This file
```

## System Components

### 1. Backend (ASP.NET Core)
> **[Detailed Documentation](backend/README.md)**

A modular REST API built with **ASP.NET Core**.
- **Key Features**:
  - Manual & Automated Check-in/Check-out.
  - Membership & Monthly Ticket Management.
  - Revenue & Traffic Reporting.
  - Integration with Payment Gateways & Gate Hardware.
  - Background Scheduler for Ticket Expiration.
  - **Security**: JWT Authentication + BCrypt Password Hashing.

### 2. Frontend (React + Vite)
> **[Detailed Documentation](frontend/README.md)**
A modern, responsive dashboard built with **React 19** and **Vite**.
- **Tech Stack**:
  - **Styling**: TailwindCSS v4.
  - **Routing**: React Router DOM v7.
  - **State/HTTP**: Axios, React Hot Toast.
  - **Charts**: Recharts.
  - **Icons**: Lucide React.
- **Key Pages**:
  - `Dashboard.jsx`: Real-time parking status and occupancy.
  - `Membership.jsx`: Management of monthly tickets and customers.
  - `Report.jsx`: Financial and operational reports.
  - `Admin.jsx`: System administration and user management.

### 3. License Plate Recognition (Python)
Computer vision service for automated plate reading (ALPR).
- Uses **EasyOCR** / **YOLO** (configured via external service).
- Communicates with Backend via REST API.

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Python 3.10+ (for OCR service)

### Backend Setup
1. `cd backend`
2. `dotnet restore`
3. `dotnet run --project Parking.API`
   - API runs at `http://localhost:5166` (HTTP) or `https://localhost:7275` (HTTPS).
   - **Note**: Data is stored in `backend/Parking.API/DataStore/`.

### Frontend Setup
1. `cd frontend`
2. `npm install`
3. `npm run dev`
   - UI runs at `http://localhost:5173`.
   - Update `src/config/api.js` if Backend URL changes.

### Running Everything
Use the helper script to launch both:
- **Windows (PowerShell)**: `.\StartAll.ps1`
- **Windows (CMD)**: `StartAll.bat`

## Useful Scripts
- **SeedData.ps1**: Resets and populates JSON data stores for testing.
- **DevTunnel.ps1**: Exposes the frontend via localtunnel for testing on mobile devices.

## Contributing
1. Fork/branch from `main`.
2. Keep backend and frontend changes separated where possible.
3. Review `backend/README.md` for specific backend architecture guidelines.

## Báo cáo tổ chức mã nguồn
- Xem báo cáo chi tiết: [documentation/codebase_report.md](documentation/codebase_report.md)
