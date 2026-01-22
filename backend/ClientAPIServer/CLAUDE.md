# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **MWD Mock Server** - a C# .NET 9.0 ASP.NET Core application that implements a mock/test server for the MyWorkDrive (MWD) client-server API. It serves as a testing harness for the uniform client-server REST API defined in the OpenAPI specification.

## Architecture

The codebase follows a layered architecture:

- **Program.cs** - Main entry point and server configuration (runs on port 5001)
- **ClientAPIHandler.cs** - Core API request handler implementing the MWD client-server API endpoints
- **APIHandler.cs** - Base API handling infrastructure and HTTP context management
- **uniform-client-server-api.yaml** - OpenAPI 3.1.0 specification defining the complete REST API
- **real_tests/** - Integration test suite

### Key Components

1. **Mock Server Mode**: Uses `#if MOCK_SERVER` preprocessor directives to enable file system-based mock implementations
2. **API Endpoints**: Implements ~30+ REST endpoints including file operations, folder management, uploads, authentication, and sharing
3. **Test Infrastructure**: Built-in test methods in Program.cs plus formal test classes in real_tests/

### File System Operations

The mock server uses `C:\temp\mwd` as the base path for all file system operations. Core operations include:
- File/folder CRUD (create, read, update, delete)
- File copying, moving, and uploading (including chunked uploads)
- Directory listing and metadata management
- File locking and versioning simulation

## Development Commands

### Build and Run
```bash
dotnet build
dotnet run
```

### Testing
```bash
# Run xUnit tests
dotnet test
```

The project includes both:
- Built-in integration tests in Program.cs (`RunTests()` method) - currently commented out
- Formal test classes in `real_tests/ClientAPITests.cs` using MSTest framework

### Project Dependencies
- Microsoft.AspNetCore.Mvc.Testing
- Microsoft.AspNetCore.OpenApi  
- Newtonsoft.Json
- xunit + xunit.runner.visualstudio
- log4net

## API Specification

The server implements the uniform client-server API v3.0 as defined in `uniform-client-server-api.yaml`. Key endpoint categories:

- **Session Management**: CheckSession, authentication
- **File Operations**: CreateFile, ReadFile, WriteFile, DeleteFile, GetFileInfo, SetFileInfo
- **Folder Operations**: ListFolder, CreateFile (folders), CopyFolder, MoveFolder, DeleteFolder
- **Upload Management**: StartFileUpload, WriteFileBlock, CompleteUpload, CancelUpload
- **Advanced Features**: File locking, versioning, public links, bookmarks, search

Authentication uses SessionID scheme in Authorization header format: `SessionID <session-id>`

## Configuration

- **API Port**: 5001 (configured in Program.cs)
- **Base API URL**: `http://localhost:5001/api/v3/`
- **File Storage**: `C:\temp\mwd` directory
- **Target Framework**: .NET 9.0

## Testing Approach

The codebase includes comprehensive test coverage through multiple approaches:
- Unit tests using xUnit framework
- Integration tests that verify end-to-end API functionality
- File system verification to ensure operations persist correctly

Tests cover all major API operations including edge cases like conflict resolution, file locking, and upload session management.