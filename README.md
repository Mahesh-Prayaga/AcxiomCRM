# AcxiomCRM

A Customer Relationship Management (CRM) application built with ASP.NET Core 8 MVC and Web API.

## Features

- Authentication & role-based authorization
- Customer management
- Lead management
- Opportunity management
- Follow-ups & activities
- Dashboard & reports
- Audit logging
- REST API
- Server-side & client-side validation

## Tech Stack

- C#
- ASP.NET Core 8 MVC & Web API
- Entity Framework Core 8
- ASP.NET Core Identity
- SQL Server
- Razor
- Bootstrap 5
- JavaScript
- Chart.js

## Installation

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server / SQL Server Express
- Git

### Setup

Clone the repository:

```bash
git clone https://github.com/Mahesh-Prayaga/AcxiomCRM.git
```

Navigate to the project directory:

```bash
cd AcxiomCRM
```

Restore dependencies:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run the application:

```bash
dotnet run --launch-profile http
```

Update the SQL Server connection string in appsettings.json if required.
The application initializes the database and seed data on startup.

## Demo Accounts

| Role | Email | Password |
|---|---|---|
| Admin | `admin@acxiomcrm.com` | `Admin@123` |
| Manager | `manager@acxiomcrm.com` | `Manager@123` |
| Sales Executive | `sales1@acxiomcrm.com` | `Sales@123` |
| Sales Executive | `sales2@acxiomcrm.com` | `Sales@123` |

> These accounts are provided for local demonstration purposes.

## Project Structure

```text
AcxiomCRM/
├── Controllers/
├── DTOs/
├── Data/
├── Models/
├── Services/
├── ViewModels/
├── Views/
├── wwwroot/
├── Program.cs
└── AcxiomCRM.csproj
```

## License

Developed as an academic/project assignment.
