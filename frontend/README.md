# Parking Management Frontend

Frontend dashboard built with React and Vite. It provides operational views for check-in/out, membership management, reports, and administration with role- and gate-based navigation.

## Tech Stack
- React 19 + Vite
- TailwindCSS
- React Router
- Axios
- Recharts
- Playwright (E2E)

## Folder Structure
```
frontend/
├── src/
│   ├── config/           # API base config
│   ├── layouts/          # Shared layout
│   ├── pages/            # Feature screens
│   ├── App.jsx           # Routes + auth guards
│   └── main.jsx          # App entry
├── public/               # Static assets
├── tests/                # Playwright E2E tests
└── documentation/        # UI screenshots
```

## Key Pages
- Dashboard: realtime occupancy
- Check-in: gate entry flow
- Check-out: payment/exit flow
- Membership: monthly tickets
- Report: revenue & traffic analytics (admin)
- Admin: user management (admin)

## Configuration
API base is configured in [src/config/api.js](src/config/api.js). Update this when the backend URL changes.

## Development
1. Install dependencies:
	```bash
	npm install
	```
2. Start dev server:
	```bash
	npm run dev
	```
3. App runs at `http://localhost:5173`.

## Testing
Run Playwright E2E tests:
```bash
npm run test:e2e
```
Reports are generated under [playwright-report](playwright-report).

Test plan and coverage details: [documentation/web_test_program.md](documentation/web_test_program.md).

## Notes
- Authentication is handled with JWT stored in localStorage; token is injected into Axios headers in [src/App.jsx](src/App.jsx).
- Menu visibility depends on role and gate type in [src/layouts/MainLayout.jsx](src/layouts/MainLayout.jsx).
