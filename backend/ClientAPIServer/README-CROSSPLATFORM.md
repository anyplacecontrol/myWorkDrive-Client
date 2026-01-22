# Cross-Platform Configuration Guide

The MWD Mock Server now supports Windows, Linux, and macOS without recompilation.

## Configuration

The server uses `appsettings.json` for configuration. You can override settings using:

1. **Environment-specific files**: 
   - Copy the appropriate example file to `appsettings.json`:
     - Windows: `appsettings.Windows.json`
     - Linux: `appsettings.Linux.json`
     - macOS: `appsettings.macOS.json`

2. **Environment variables**:
   - `ServerConfiguration__ApiPort` - Override the API port
   - `ServerConfiguration__BasePath` - Override the base path
   - `ServerConfiguration__UseHttps` - Enable/disable HTTPS

3. **Command line arguments**:
   ```bash
   dotnet run --ServerConfiguration:ApiPort=6000 --ServerConfiguration:BasePath=/custom/path
   ```

## Default Paths

If no `BasePath` is configured, the server uses platform-specific defaults:
- **Windows**: `C:\temp\mwd`
- **Linux**: `/tmp/mwd`
- **macOS**: `~/mwd`

## Running on Different Platforms

### Windows
```powershell
copy appsettings.Windows.json appsettings.json
dotnet run
```

### Linux
```bash
cp appsettings.Linux.json appsettings.json
dotnet run
```

### macOS
```bash
cp appsettings.macOS.json appsettings.json
dotnet run
```

## Custom Configuration

Edit `appsettings.json` to customize:

```json
{
  "ServerConfiguration": {
    "ApiPort": 8357,
    "BasePath": "/your/custom/path",
    "UseHttps": false
  }
}
```

## Path Handling

The server automatically handles path separator differences:
- API always uses forward slashes (`/`)
- File system operations use the appropriate OS separator
- Path conversions are handled transparently

## Testing

Tests automatically use the configured base path. Ensure the path is writable for tests to pass.

```bash
dotnet test
```
