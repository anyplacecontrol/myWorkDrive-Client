# myworkdrive-ui

## Setup and Running

This project consists of the web frontend of the MyWorkDrive app with a mock background API that allows testing the UI against the REST API of the MyWorkDrive server.

### Prerequisites

Before setting up the project, ensure you have the following installed:

- **Node.js** (version 20 or later) - [Download from nodejs.org](https://nodejs.org/en/download)
- **.NET 9.0 SDK** - [Download from dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Git** - [Download from git-scm.com](https://git-scm.com/)

### Getting Started

1. **Clone the repository:**
   ```bash
   git clone https://github.com/anyplacecontrol/myWorkDrive-Client
   cd myworkdrive-ui
   ```

2. **Install frontend dependencies:**
   ```bash
   npm install
   ```

3. **Extract the backend server:**
   
   Extract the contents of `backend/mock-server-net9.0.zip` into the `backend/net9.0/` directory:
   
   **Windows (PowerShell):**
   ```powershell
   Expand-Archive -Path backend\mock-server-net9.0.zip -DestinationPath backend\net9.0
   ```
   
   **macOS/Linux:**
   ```bash
   unzip backend/mock-server-net9.0.zip -d backend/net9.0
   ```
   
   **Or manually:** Extract the zip file using your preferred tool and place the contents in `backend/net9.0/`

### Backend Server

The backend server is a pre-built .NET 9.0 executable (`MWDMockServer.exe`) that runs on:
- **Port:** 8357
- **Base Path:** `C:\temp\mock` (Windows)
- **API URL:** `http://localhost:8357/api/v3/`

The server starts automatically when you run `npm run start:backend` or `npm run start:dev`.

### Running the Application

#### Option 1: Run Both Frontend and Backend Together (Recommended)
```bash
npm run start:dev
```
This command starts both the backend API server and the frontend development server. The backend will wait until it's ready before starting the frontend.

#### Option 2: Run Components Separately

**Start the backend server:**
```bash
npm run start:backend
```

The backend runs in detached mode (background). To see console output:
```bash
npm run start:backend -- --attach
```

**Start the frontend (in a new terminal):**
```bash
npm start
```

**View backend logs:**
- Logs are saved to `backend/net9.0/backend.log`
- **Windows (PowerShell):** `Get-Content backend\net9.0\backend.log -Wait -Tail 100`
- **macOS/Linux:** `tail -f backend/net9.0/backend.log`

### Building for Production

**Build the frontend:**
```bash
npm run build
```

**Build for production with mocking enabled:**
```bash
npm run build-prod
```

**Create a production release:**
```bash
npm run release-prod
```

**Preview the built application:**
```bash
npm run preview
```

### Available Scripts

- **`npm run start:dev`** - Start backend and frontend together (recommended)
- **`npm run start:backend`** - Start backend server only
- **`npm start`** - Start frontend only (requires backend running separately)
- **`npm run build`** - Build frontend for production
- **`npm run build-prod`** - Build with mocking enabled
- **`npm run preview`** - Preview production build locally
- **`npm run release-ci`** - Build and create distribution zip (CI)
- **`npm run release-prod`** - Build production release and create zip
 
## Additional resources

- **Old web-client feature list:** A feature spreadsheet from the previous web-client is available here:

   https://wanpath.sharepoint.com/:x:/g/IQC5alJyUWDVQYTh0mz_a8V3ATe3ufPenapA3prbYHRJSUE?rtime=9OkxPF1Y3kg

- **API specification for the mock server:** The repository includes `uniform-client-server-api.yaml` which documents the mock server API used by the frontend. Use this file as the source of truth for available endpoints and request/response shapes.

