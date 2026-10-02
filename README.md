# WorkTracker

A web application for managing products, purchase orders, sourcing channels, and tasks. It includes a dashboard and tracks product costs and profit on sales.

## Features

- Manage products, images, status, and condition.
- Record purchase orders and sales.
- Manage sourcing channels and their images.
- View product and sales indicators on a dashboard.
- Manage tasks on a Kanban board with configurable statuses.
- Register, authenticate, and manage user profiles.

## Technology stack

- Frontend: Angular 22, TypeScript, and SCSS.
- Backend: ASP.NET Core and .NET 10.
- Database: SQL Server, with schema managed through Entity Framework Core migrations.

## Requirements

- .NET 10 SDK.
- Node.js and npm versions compatible with Angular 22.
- SQL Server. On Windows, the included development configuration uses SQL Server LocalDB.

## Run in development

### 1. Configure the API

From the repository root, set the database connection string and a unique JWT secret in the process environment. The secret should be random and at least 32 characters long. Do not use the example secret in `appsettings.Development.json` in a shared or public environment.

PowerShell example:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=WorkTrackerDb;Trusted_Connection=True;TrustServerCertificate=True"
$env:Jwt__Secret = "replace-with-a-random-secret-at-least-32-characters-long"
```

Install the EF Core tool once if it is not already installed:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
```

Apply the database migrations and start the API:

```powershell
dotnet ef database update --project backend/WorkeTracker/Infrastructure --startup-project backend/WorkeTracker/WorkeTracker
dotnet run --project backend/WorkeTracker/WorkeTracker --launch-profile https
```

The API uses the HTTPS profile defined in `launchSettings.json` and is available at `https://localhost:7176`. If needed, trust the local development certificate with `dotnet dev-certs https --trust`.

### 2. Start the frontend

In a separate terminal, run:

```powershell
cd frontend
npm ci
npm start
```

Open `http://localhost:4200`. The development server forwards `/api` requests to the local API at `https://localhost:7176`, as configured in `frontend/proxy.conf.json`.

### 3. Configure Google sign-in

Create a **Web application** OAuth client in Google Cloud Console. Add `http://localhost:4200` as an authorized JavaScript origin. The app uses Google Identity Services to return an ID token to the API, so this flow does not need an OAuth client secret or an authorized redirect URI.

Set the same OAuth client ID in both places:

- Frontend: `frontend/src/environments/environment.development.ts` (`googleClientId`). A client ID is public and can be included in frontend code; never put a client secret there.
- Backend: set `Google__ClientId` in the API process environment. ASP.NET Core maps this to the `Google:ClientId` setting and uses it to validate the ID token audience.

PowerShell example:

```powershell
$env:Google__ClientId = "your-client-id.apps.googleusercontent.com"
```

For production, set the same client ID in the frontend's production environment configuration and in the backend environment. Add the deployed frontend origin to the OAuth client's authorized JavaScript origins.

## Configuration for other environments

Set `ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `Cors__AllowedOrigins`, and `Google__ClientId` through environment variables or your platform's secret manager. `Cors__AllowedOrigins` must contain the frontend's exact origin, including the protocol. Do not commit secrets to the repository.

Migrations are located in `backend/WorkeTracker/Infrastructure/Migrations`. Uploaded images are stored locally in `backend/WorkeTracker/WorkeTracker/wwwroot/uploads`; deployments with ephemeral storage or multiple instances should use shared persistent storage.

## Project structure

```text
backend/WorkeTracker/
  WorkeTracker/       ASP.NET Core API
  Application/        Services and use cases
  Domain/             Entities and domain rules
  Infrastructure/     SQL Server, repositories, and migrations
frontend/             Angular application
database/             SQL and DBML models
```

## License

This project has no license. Publishing the repository does not grant permission to reuse, modify, or redistribute the code.
