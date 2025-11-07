# MinIO CRUD Backend + Folder Management

![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity_Framework_Core-6.0-512BD4?style=for-the-badge&logo=dotnet)
![Postgres](https://img.shields.io/badge/postgres-%23316192.svg?style=for-the-badge&logo=postgresql&logoColor=white)
![MinIO](https://img.shields.io/badge/MinIO-900C3F?style=for-the-badge&logo=minio&logoColor=white)
![xUnit](https://img.shields.io/badge/Tests-xUnit-5A29E4?style=for-the-badge)
![Swagger](https://img.shields.io/badge/API-OpenAPI_Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)

---

## Table of contents

- [About the project](#about-the-project)
- [Features](#features)
- [File lifecycle and statuses](#file-lifecycle-and-statuses)
- [Getting started](#getting-started)
  - [Requirements](#requirements)
  - [Clone & run](#clone--run)
    - [Run with dotnet run](#run-with-dotnet-run)
    - [Run with docker](#run-with-docker)
- [Docker Compose Configuration](#docker-compose-configuration)
- [Environment Configuration](#environment-configuration)
- [Standard API Response Format](#standard-api-response-format)
- [Base API Controller](#base-api-controller)
- [Usage / API endpoints](#usage--api-endpoints)
- [Services and important implementation notes](#services-and-important-implementation-notes)
- [File cleanup service](#file-cleanup-service)
- [Testing](#testing)
- [Troubleshooting & recovery notes](#troubleshooting--recovery-notes)
- [Tech stack](#tech-stack)
- [Recent cleanup & improvements](#recent-cleanup--improvements)
- [License](#license)

---

## About the project

This repository is a **backend service** to manage folders (through the DB) and files stored in MinIO (S3-compatible object storage).  
It was developed as part of a **larger media & file-management project** and focuses on reliable storage operations, presigned uploads, and safe cleanup of stale records and objects.

Key goals:
- Clear separation between DB records and object storage
- Safe presigned uploads for direct client-to-MinIO transfer
- Defensive background cleanup with dry-run mode
- Testable services using `IMinioService` abstractions

---

## Features

- Folder and file CRUD backed by EF Core
- Uploads to MinIO (via direct server upload or presigned URL)
- Presigned GET and PUT URLs
- File metadata tracking (size, content-type, status)
- Safe background cleanup service with dry-run and configurable thresholds
- Cancellation-token support for long-running operations
- Centralized exception handling middleware
- Unit tests using xUnit + Moq + EF Core InMemory provider
- Swagger/OpenAPI documentation

---

## File lifecycle and statuses

- **Pending** a record created when the server issues a presigned upload URL. The client hasn't completed the upload yet.
- **Uploaded** the object exists in MinIO and the DB record was updated upon confirmation.
- **Failed** confirmation attempted (e.g., `StatObjectAsync`) and failed (object missing, network error). These are candidates for cleanup after `FailedExpiryDays`.
- **IsDeleted = true** - soft-deleted records; purged after `DeletedExpiryDays`.

> Cleanup rules are conservative by default (dry-run ON). See `FileCleanupService` section for details.

---

## Getting started

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MinIO Server](https://min.io/download) or any S3-compatible endpoint
- [PostgreSQL](https://www.postgresql.org/download/)
- (Optional) Docker for local MinIO and Postgres setup


### Clone & run

To clone the project, use the following commands:
```bash
git clone https://github.com/echitey/MinIOCRUD.git
cd MinIOCRUD
```

#### Run with dotnet run

Make sure postgres and MinIO are running locally.
In you appsettings.json, configure your MinIO and Postgres connection strings:
Postgres Host: localhost
MinIO Endpoint: localhost:9000

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=filecrud;Username=postgres;Password=postgres"
},
"Minio": {
  "Endpoint": "localhost:9000", // http debug
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "PublicEndpoint": "localhost:9000",
  "Secure": false,
  "Bucket": "files",
  "URLExpiryMinutes": 15
}
```

To run the project, use the following commands:
```bash
dotnet restore
dotnet ef database update
dotnet run
```
To access the Swagger UI, navigate to: `http://localhost:5121/swagger` (or the port you have configured).

To access the MinIO Console, navigate to: `http://localhost:9001` (default credentials: minioadmin:minioadmin).


#### Run with docker

You can also run the entire stack using Docker and Docker Compose.
In you appsettings.json, configure your MinIO and Postgres connection strings:
Postgres Host: postgres (this is the service name in docker-compose)
MinIO Endpoint: minio:9000 (this is the service name in docker-compose)

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=postgres;Port=5432;Database=filecrud;Username=postgres;Password=postgres" // docker-compose debug
},
"Minio": {
  "Endpoint": "minio:9000", // docker-compose debug: minio is the service name
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "PublicEndpoint": "localhost:9000",
  "Secure": false,
  "Bucket": "files",
  "URLExpiryMinutes": 15
}
```
1. Build and run containers

```bash
docker-compose up --build
```

2. Access services

- API (Swagger UI): `http://localhost:5000/swagger`

- MinIO Console: `http://localhost:9001` (default credentials: minioadmin:minioadmin)

3. Stop containers

```bash
docker-compose down
```

4. (Optional) Remove all volumes and rebuild from scratch

```bash
docker-compose down -v
docker-compose up --build
```


## Docker Compose Configuration
```yaml
services:
  miniocrud:
    image: ${DOCKER_REGISTRY-}miniocrud
    build:
      context: .
      dockerfile: MinIOCRUD/Dockerfile
    container_name: minio_crud_api
    ports:
      - "5000:8080"
    networks:
      - filecrud-network

  minio:
    image: minio/minio:RELEASE.2025-04-22T22-12-26Z
    container_name: miniocrud-minio
    restart: always
    command: server /data --console-address ":9001"
    ports:
      - "9000:9000"
      - "9001:9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    volumes:
      - minio_data:/data
    networks:
      - filecrud-network

  postgres:
    image: postgres:15
    container_name: miniocrud-postgres
    restart: always
    environment:
      POSTGRES_DB: filecrud
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    networks:
      - filecrud-network

volumes:
  minio_data:
  pgdata:

networks:
  filecrud-network:
    driver: bridge

```


## Environment Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=filecrud;Username=postgres;Password=postgres"
    //"DefaultConnection": "Host=postgres;Port=5432;Database=filecrud;Username=postgres;Password=postgres" // docker-compose debug
  },
  "Minio": {
    "Endpoint": "localhost:9000", // http debug
    //"Endpoint": "minio:9000", // docker-compose debug: minio is the service name
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "PublicEndpoint": "localhost:9000",
    "Secure": false,
    "Bucket": "files",
    "URLExpiryMinutes": 15
  },
  "FileCleanup": {
    "PendingExpiryMinutes": 60, // Time limit before uncompleted uploads are considered expired.
    "IntervalMinutes": 10, // How often the cleanup job runs.
    "FailedExpiryDays": 1, // Time limit before failed uploads are automatically cleaned up
    "DeletedExpiryDays": 30, // Retention period before soft-deleted files are permanently purged.
    "FileCleanup:DryRun": true //When true, performs a simulation (logging only, no deletions).
  }
}

```

##  Standard API Response Format

All endpoints return data wrapped in a standardized JSON structure to ensure consistency and better error handling across the API.

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public int StatusCode { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 200
        };
    }

    public static ApiResponse<T> Created(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Resource created successfully",
            StatusCode = 201
        };
    }

    public static ApiResponse<T> Fail(string message, int statusCode = 400, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            Errors = errors
        };
    }

    public static ApiResponse<T> FromException(Exception ex, int statusCode = 500)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = ex.Message,
            StatusCode = statusCode,
            Errors = new List<string> { ex.InnerException?.Message ?? ex.Message }
        };
    }
}
```

### Success Response

**Example**
```json
{
  "success": true,
  "message": "File uploaded successfully.",
  "data": {
    "id": "e6d8f3b0-9a22-4b5e-8c73-8f4fa1f14c87",
    "fileName": "document.pdf",
    "bucket": "files",
    "size": 152340,
    "status": "Uploaded",
    "createdAt": "2025-11-07T14:12:05Z"
  }
}
```

### Error Response

**Example**
```json
{
  "success": false,
  "message": "The requested file could not be found.",
  "error": {
    "type": "NotFoundException",
    "details": "No file found with ID 1234abcd-5678-efgh-9012-ijklmnopqrst."
  },
  "statusCode": 404
}
```

### Notes
- `success` Indicates whether the operation was successful.
- `message` A human-readable message describing the result.
- `data` Contains the returned entity, list, or result payload.
- `error` Present only in error responses; provides detailed error info.
- `statusCode` Mirrors the HTTP response status code for easier debugging.



##  Base API Controller

All controllers in this project inherit from a shared `BaseApiController`, which provides a unified way to return consistent API responses.

This base class ensures that all endpoints:
- whether they succeed, create resources
- encounter errors, return data using the standardized response format defined in `ApiResponse<T>`.

```csharp
[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult OkResponse<T>(T data, string? message = null)
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }

    protected IActionResult CreatedResponse<T>(T data, string? message = null)
    {
        return StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Created(data, message));
    }

    protected IActionResult ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)
    {
        return StatusCode(statusCode, ApiResponse<object>.Fail(message, statusCode, errors));
    }
}
```

### How It Works

| Method                                                                             | Description                                                                           | Typical Usage                                                            |
| ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| `OkResponse<T>(T data, string? message = null)`                                    | Returns a successful **200 OK** response with the provided data and optional message. | For standard GET or POST operations that succeed.                        |
| `CreatedResponse<T>(T data, string? message = null)`                               | Returns a **201 Created** response, typically after resource creation.                | When a new file or folder is created successfully.                       |
| `ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)` | Returns an error response with a given status code and optional validation errors.    | For validation errors, not found exceptions, or business logic failures. |


### Benefits

- Consistent structure: All endpoints respond using the same JSON schema.

- Simplified controllers: No need to manually format responses in each action.

- Improved maintainability: Changes to response behavior can be made centrally.

### Example usage of the base controller

```csharp
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class FoldersController : BaseApiController
{
    private readonly AppDbContext _db;
    private readonly IFolderService _folderService;

    public FoldersController(AppDbContext db, IFolderService folderService)
    {
        _db = db;
        _folderService = folderService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetFolder(Guid id, CancellationToken cancellationToken = default)
    {
        var folder = await _folderService.GetFolderDtoWithBreadcrumbsAsync(id, cancellationToken);

        return folder == null
            ? ErrorResponse("Folder not found", 404)
            : OkResponse(folder);
    }
}
```
Note that the controller class extends `BaseApiController` and returns `ErrorResponse` and `OkResponse` implemented there.



##  Usage / API endpoints

All endpoints are exposed under /api.

**Folders**

| route | description |
|--------|--------------|
| <kbd>POST /api/folders?name={name}&parentId={parentId?}</kbd> | Create a new folder. |
| <kbd>GET /api/folders/\{id}</kbd> | Retrieve folder details with breadcrumb path. |
| <kbd>GET /api/folders/root</kbd> | List all root-level folders and files. |
| <kbd>DELETE /api/folders/\{id}</kbd> | Delete a folder recursively. |

### POST /api/folders?name=NewFolder

**RESPONSE**
```json
{
  "success": true,
  "message": "Resource created successfully",
  "data": {
    "id": "bdd92ef3-218a-4789-9e2a-ffa37047feaf",
    "name": "newfolder",
    "parent": null,
    "path": "",
    "breadcrumb": [],
    "subFolders": [],
    "files": []
  },
  "errors": null,
  "statusCode": 201
}
```

### GET /api/folders/bdd92ef3-218a-4789-9e2a-ffa37047feaf

**RESPONSE**
```json
{
  "success": true,
  "message": null,
  "data": {
    "id": "bdd92ef3-218a-4789-9e2a-ffa37047feaf",
    "name": "newfolder",
    "parent": null,
    "path": "newfolder",
    "breadcrumb": [
      {
        "id": "bdd92ef3-218a-4789-9e2a-ffa37047feaf",
        "name": "newfolder",
        "url": "/folders/bdd92ef3-218a-4789-9e2a-ffa37047feaf"
      }
    ],
    "subFolders": [],
    "files": []
  },
  "errors": null,
  "statusCode": 200
}
```

### GET /api/folders/root

**RESPONSE**
```json
{
  "success": true,
  "message": null,
  "data": {
    "folders": [
      {
        "id": "bdd92ef3-218a-4789-9e2a-ffa37047feaf",
        "name": "newfolder",
        "parent": null,
        "path": "",
        "breadcrumb": [],
        "subFolders": [],
        "files": []
      }
    ],
    "files": []
  },
  "errors": null,
  "statusCode": 200
}
```

### DELETE /api/folders/bdd92ef3-218a-4789-9e2a-ffa37047feaf

**RESPONSE**
```json
204	No Content
```

**Files**

| route | method | description |
|--------|---------|--------------|
| <kbd>/api/files</kbd> | **POST** | Upload file via server (`multipart/form-data`). |
| <kbd>/api/files/presign-upload</kbd> | **POST** | Generate a presigned PUT URL for direct client-side uploads to MinIO. |
| <kbd>/api/files/\{id}/confirm</kbd> | **POST** | Confirm that an uploaded file exists in MinIO and update its status. |
| <kbd>/api/files</kbd> | **GET** | Retrieve a paginated list of stored files. |
| <kbd>/api/files/\{id}</kbd> | **GET** | Get metadata and details for a specific file. |
| <kbd>/api/files/\{id}/download</kbd> | **GET** | Generate a presigned GET URL to securely download a file. |
| <kbd>/api/files/\{id}</kbd> | **DELETE** | Soft delete a file (mark as deleted without removing MinIO object). |
| <kbd>/api/files/\{id}/hard</kbd> | **DELETE** | Permanently delete a file and its MinIO object. |
| <kbd>/api/files/hard-delete</kbd> | **POST** | Bulk hard delete multiple files (requires `"force": true` flag). |

Here are some request/response examples


### GET /api/Files?page=1&pageSize=20

**RESPONSE**
```json
{
  "success": true,
  "message": "Files List",
  "data": [
    {
      "id": "c9ebe873-88b9-4da5-b6b5-2dd95f4cdd2e",
      "fileName": "frame_54792.png",
      "contentType": "image/png",
      "safeContentType": "image/png",
      "friendlyType": "Image",
      "size": 648,
      "parent": null,
      "createdAt": "2025-11-07T22:00:51.325987+00:00",
      "metadata": "",
      "status": "Uploaded"
    }
  ],
  "errors": null,
  "statusCode": 200
}
```

### POST /api/Files/hard-delete

**REQUEST**
```json
{
  "ids": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  ],
  "force": true
}
```

**RESPONSE**
```json
204	No Content
```

### POST /api/files/presign-upload

**REQUEST**
```json
{
  "fileName": "string",
  "contentType": "string",
  "size": 0
}
```

**RESPONSE**
```json
{
  "success": true,
  "message": null,
  "data": {
    "fileId": "6e2b9af0-9006-40cb-b095-de563e0235b7",
    "uploadUrl": "http://localhost:9000/files/20251107/6e2b9af0-9006-40cb-b095-de563e0235b7_string?X-Amz----483",
    "objectKey": "20251107/6e2b9af0-9006-40cb-b095-de563e0235b7_string",
    "bucket": "files"
  },
  "errors": null,
  "statusCode": 200
}
```



## Services and important implementation notes

### MinioService
- Wraps MinIO SDK client creation.
- Centralizes endpoint config and public/internal endpoint replacement for presigned URLs.

Sample public signatures:
```csharp
Task PutObjectAsync(string bucket, string objectKey, Stream data, string contentType, CancellationToken cancellationToken);

Task<Uri> GetPresignedPutObjectUrlAsync(string bucket, string objectKey, TimeSpan expiry);

Task<Uri> GetPresignedGetObjectUrlAsync(string bucket, string objectKey, TimeSpan expiry);

Task DeleteObjectAsync(string bucket, string objectKey, CancellationToken cancellationToken);

Task<(long Size, string ContentType)> StatObjectAsync(string bucket, string objectKey, CancellationToken cancellationToken);
```

### FolderService
- Handles hierarchical folder structures, breadcrumbs, and recursive deletion.
- Now accepts `ILogger<FolderService>` and uses CancellationToken on public actions that may be long-running.
- Recursive deletion collects deletes and commits once per transaction for integrity.

Sample public signatures:
```csharp
Task<Folder> CreateFolderAsync(Folder folder);

Task DeleteFolderAsync(Guid folderId, CancellationToken cancellationToken = default);

Task<Folder?> GetFolderByIdAsync(Guid id);

Task<FolderDto?> GetFolderDtoWithBreadcrumbsAsync(Guid id, CancellationToken cancellationToken);

Task<(List<FolderDto>, List<FileRecordDto>)> GetRootContentsAsync();
```

### FileService
- Manages DB records and MinIO interactions (uploads via server stream or presigned).
- Exposes cancellation tokens for uploads, deletes, and long operations.
- Uses FileStatus (or string constants) to track Pending, Uploaded, Failed.

## File cleanup service

Key behaviors:

- Dry-run default (`FileCleanup:DryRun = true`): logs candidates but does not delete. Change to false to enable true deletion.

- Safer predicates: only deletes `Pending` and `Failed` records that are older than thresholds and excludes `Uploaded` records.

- Cancellation aware: MinIO and EF Core calls accept passed `CancellationToken`.

- Logs what it will remove when in dry-run, improving safety.

## Testing
Run all tests:
```bash
dotnet test
```

Testing notes:

- Tests use an InMemory EF Core provider for isolation.
- Mocks (Moq) are used for `IMinioService`, `IConfiguration`, and `ILogger<T>`.
- Tests follow Arrange-Act-Assert pattern and assert both DB state and MinIO interactions.

Key test adjustments made during cleanup: 
- `FolderService` tests now mock `GetValue` / indexer for configuration and supply a `Mock<ILogger<FolderService>>`.
- `DeleteObjectAsync` mock includes `CancellationToken` parameter to match updated signatures.

Example test expectations:

- `DeleteFolderAsync` removes folder and files, calls `DeleteObjectAsync("files", "file.txt", It.IsAny<CancellationToken>())` once.
- `GetFolderDtoWithBreadcrumbsAsync` preserves original breadcrumb assertions.


## Tech stack

- .NET 8
- C#
- Entity Framework Core (InMemory + relational providers)
- MinIO (.NET SDK)
- Swagger / OpenAPI
- xUnit for tests
- Moq for mocking
- Docker + Docker Compose
- PostgreSQL


## Recent cleanup & improvements

This repository was refactored and hardened as part of the wider project. Highlights:

- Centralized configuration and validation in `MinioService`.
- DRY and small helper extractions (e.g., endpoint replacement).
- Added `ILogger<T>` to services (e.g., `FolderService`) for logging.
- Introduced `CancellationToken` in public service methods where meaningful (upload/delete/cleanup).
- `FileCleanupService` rewritten to be safe-by-default (dry-run, safer predicates).
- Tests updated to match new signatures (logger mock, cancellation token in `DeleteObjectAsync`).
- Exception handling middleware enhanced (`HasStarted` check and response clearing).
- Controllers updated to accept `CancellationToken` for long-running endpoints (upload/download).
- Added XML summaries and `#region` in service files for clarity.


## License

This project is licensed under the MIT License