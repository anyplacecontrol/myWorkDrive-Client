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

2. **Install frontend dependencies:**
   ```bash
   npm install
   ```

3. **Restore backend dependencies:**
   ```bash
   cd backend/ClientAPIServer
   dotnet restore
   cd ../..
   ```

### Server Configuration

The backend API server can be configured through platform-specific `appsettings.json` files located in the `backend/ClientAPIServer` directory. The server automatically selects the appropriate configuration based on the runtime platform.

#### Configuration Files
- `appsettings.json` - Base configuration (no BasePath set)
- `appsettings.Linux.json` - Linux-specific settings (BasePath: `/tmp/mwd`)
- `appsettings.macOS.json` - macOS-specific settings (BasePath: `~/mwd`)
- `appsettings.Windows.json` - Windows-specific settings (BasePath: `C:\temp\mock`)

#### New Windows mock server (net9.0)

- A newer Windows-only mock server release is available as `backend/mock-server-net9.0.zip`.
- To use it, unzip the archive into `backend/net9.0` so the folder contains the `MWDMockServer.exe` and related files.
- Run the backend only with: `npm run start:backend-windows`.
- Run both backend (Windows exe) and frontend with: `npm run start:dev-windows`.

This option starts the published .NET 9.0 mock server executable instead of building/running the project source.

#### Server Configuration Parameters

**ServerConfiguration Section:**
- **ApiPort** (default: `8357`): The port number on which the API server listens
- **BasePath**: The root directory path for file operations and storage
  - Linux: `/tmp/mwd`
  - macOS: `~/mwd` (user's home directory)
  - Windows: `C:\tmp\mwd`
  - Base config: Empty string (uses current directory)
- **UseHttps** (default: `false`): Whether to enable HTTPS for the API server
- **DefaultShare** (default: `"Documents"`): The default share name for file operations
- **PathFormat** (default: `"scheme"`): The path format scheme used for file paths

**Logging Configuration:**
- **Default** log level: `Information`
- **Microsoft** framework logs: `Warning` level
- **Microsoft.Hosting.Lifetime**: `Information` level

#### Customizing Configuration

To modify server settings:
1. Navigate to `backend/ClientAPIServer/`
2. Edit the appropriate platform-specific appsettings file
3. Restart the backend server to apply changes

**Example - Changing the API port:**
```json
{
  "ServerConfiguration": {
    "ApiPort": 8080,
    // ... other settings
  }
}
```

### Running the Application

#### Option 1: Run Both Frontend and Backend Together (Recommended)
```bash
npm run start:dev
```
This command starts both the frontend development server and the backend API server concurrently.

#### Option 2: Run Components Separately

**Start the backend server:**
```bash
npm run start:backend
```
or
```bash
cd backend/ClientAPIServer
dotnet run
```

**Start the frontend (in a new terminal):**
```bash
npm start
```

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

 
## Additional resources

- **Old web-client feature list:** A feature spreadsheet from the previous web-client is available here:

   https://wanpath.sharepoint.com/:x:/g/IQC5alJyUWDVQYTh0mz_a8V3ATe3ufPenapA3prbYHRJSUE?rtime=9OkxPF1Y3kg

- **API specification for the mock server:** The repository includes `uniform-client-server-api.yaml` which documents the mock server API used by the frontend. Use this file as the source of truth for available endpoints and request/response shapes.

- **Figma prototype for design:** available here:

   https://www.figma.com/design/J8YIyDa2y2rM01WsDe6FQI/Browser-client--Old-version-?node-id=388-13551&p=f&t=mWLCgF4TpMYLOM27-0


- **Old prototype of web-client:** available here:

   https://github.com/xmlui-org/myworkdrive-ui
