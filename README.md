# Printer Service Visit Management API

> ASP.NET Core 10 Web API · SQL Server · JWT Authentication

---

## Project Structure

```
PrinterServiceAPI/
├── Controllers/
│   ├── AuthController.cs          # Login, refresh, logout, password management
│   ├── SiteVisitController.cs     # Create visits, list, export PDF/Excel
│   └── OtherControllers.cs        # Dashboard, Technicians, Machines, Categories
├── Data/
│   └── AppDbContext.cs            # EF Core DbContext + seed data
├── Database/
│   └── schema.sql                 # Raw SQL schema (alternative to EF migrations)
├── DTOs/
│   └── Dtos.cs                    # All request/response records
├── Helpers/
│   └── JwtHelper.cs               # JWT generation & validation
├── Middleware/
│   └── ExceptionMiddleware.cs     # Global error handler
├── Models/
│   └── Entities.cs                # EF Core entity classes
├── Services/
│   ├── AuthService.cs
│   ├── SiteVisitService.cs
│   ├── DashboardService.cs
│   ├── TechnicianMachineService.cs
│   └── ReportService.cs           # PDF (iText7) + Excel (ClosedXML)
├── appsettings.json
├── Program.cs
└── PrinterServiceAPI.csproj
```

---

## Database Schema

```
Roles               (RoleId, RoleName)
Users               (UserId, TechnicianCode [unique], FullName, Email,
                     PasswordHash, RoleId, IsActive, CreatedAt, UpdatedAt)
Machines            (MachineId, MachineRefNumber [unique], ModelName,
                     SerialNumber, CustomerName, CustomerPhone,
                     CustomerEmail, CustomerAddress, InstalledDate)
SolutionCategories  (CategoryId, CategoryName [unique], SortOrder, IsActive)
SiteVisits          (VisitId, TechnicianId, TechnicianCode, TechnicianName,
                     MachineRefNumber, CategoryId, Note, MeterReadingValue,
                     Latitude, Longitude, LocationAddress, VisitDate,
                     VisitTime, CreatedAt)
RefreshTokens       (TokenId, UserId, Token, ExpiresAt, CreatedAt, IsRevoked)
PasswordResetTokens (ResetId, UserId, Token, ExpiresAt, IsUsed, CreatedAt)
```

---

## Setup

### 1. Prerequisites
- .NET 10 SDK
- SQL Server 2019+ (or LocalDB for dev)

### 2. Configure Connection String

Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=PrinterServiceDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Configure JWT

```json
"JwtSettings": {
  "SecretKey": "REPLACE_WITH_32+_CHARACTER_SECRET_KEY",
  "Issuer": "PrinterServiceAPI",
  "Audience": "PrinterServiceClient",
  "AccessTokenExpiryMinutes": 60,
  "RefreshTokenExpiryDays": 7
}
```

### 4. Option A — EF Core Migrations (recommended)

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Option B — Raw SQL

Run `Database/schema.sql` directly on your SQL Server instance.

### 5. Run

```bash
dotnet run
# Swagger UI → https://localhost:5001/swagger
```

---

## Default Credentials (after seed)

| Role       | Code    | Password   |
|------------|---------|------------|
| Manager    | MGR001  | Admin@123  |
| Technician | TECH001 | Admin@123  |
| Technician | TECH002 | Admin@123  |
| Technician | TECH003 | Admin@123  |

> ⚠️ Change all passwords immediately in production.

---

## API Endpoints

### Authentication — `/api/auth`

| Method | Endpoint                   | Auth     | Description                        |
|--------|----------------------------|----------|------------------------------------|
| POST   | `/login`                   | Public   | Login → returns JWT + refresh token |
| POST   | `/refresh`                 | Public   | Rotate refresh token                |
| POST   | `/logout`                  | Any role | Revoke refresh token                |
| GET    | `/me`                      | Any role | Current user info                   |
| POST   | `/change-password`         | Any role | Change own password                 |
| POST   | `/forgot-password`         | Public   | Generate reset token                |
| POST   | `/reset-password`          | Public   | Reset password with token           |

### Site Visits — `/api/visits`

| Method | Endpoint              | Auth        | Description                         |
|--------|-----------------------|-------------|-------------------------------------|
| POST   | `/`                   | Technician  | Create a site visit (captures GPS)  |
| GET    | `/my`                 | Technician  | Own visit history (paged, filtered) |
| GET    | `/`                   | Any role    | All visits (manager) / own (tech)   |
| GET    | `/{id}`               | Any role    | Visit details                       |
| GET    | `/export/excel`       | Any role    | Download Excel report               |
| GET    | `/export/pdf`         | Any role    | Download PDF report                 |

### Dashboard — `/api/dashboard`

| Method | Endpoint  | Auth    | Description                         |
|--------|-----------|---------|-------------------------------------|
| GET    | `/stats`  | Manager | Totals, charts, top technicians     |

### Technicians — `/api/technicians`

| Method | Endpoint        | Auth    | Description              |
|--------|-----------------|---------|--------------------------|
| GET    | `/`             | Manager | List all technicians     |
| GET    | `/{techCode}`   | Manager | Technician profile       |
| POST   | `/`             | Manager | Create technician        |
| PUT    | `/{techCode}`   | Manager | Update technician        |
| DELETE | `/{techCode}`   | Manager | Soft-delete technician   |

### Machines — `/api/machines`

| Method | Endpoint            | Auth    | Description                |
|--------|---------------------|---------|----------------------------|
| GET    | `/{refNumber}`      | Any     | Machine + customer details |
| GET    | `/search?q=xxx`     | Any     | Search machines            |
| POST   | `/`                 | Manager | Add machine record         |

### Solution Categories — `/api/categories`

| Method | Endpoint | Auth | Description             |
|--------|----------|------|-------------------------|
| GET    | `/`      | Any  | All active categories   |

---

## Request/Response Examples

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "technicianCode": "TECH001",
  "password": "Admin@123"
}
```

```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "accessToken": "eyJhbGci...",
    "refreshToken": "base64string...",
    "technicianCode": "TECH001",
    "fullName": "Kamal Perera",
    "role": "Technician",
    "expiresAt": "2025-01-15T11:00:00Z"
  }
}
```

### Create Site Visit
```http
POST /api/visits
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "machineRefNumber": "MCH-0001",
  "categoryId": 1,
  "note": "Toner level low, replacement advised",
  "meterReadingValue": 125430,
  "latitude": 6.9271,
  "longitude": 79.8612,
  "locationAddress": "No 5, Galle Road, Colombo 03"
}
```

### Export Report
```http
GET /api/visits/export/excel?fromDate=2025-01-01&toDate=2025-01-31
Authorization: Bearer {accessToken}
```

---

## Authentication Flow

```
1. Client  → POST /api/auth/login         → { accessToken, refreshToken }
2. Client  → API requests with header:     Authorization: Bearer {accessToken}
3. Token expires (60 min default)
4. Client  → POST /api/auth/refresh       → { new accessToken, new refreshToken }
5. Client  → POST /api/auth/logout        → revokes refreshToken
```

---

## Security Notes

- Passwords are hashed with **BCrypt** (work factor 12)
- Refresh tokens are **rotated** on every use (one-time use)
- All refresh tokens are **revoked on password change**
- Role-based access via **JWT claims** + `[Authorize(Roles = "...")]`
- Input is trimmed and validated before persistence
- Technicians **cannot** access other technicians' data

---

## Deployment (IIS)

1. `dotnet publish -c Release -o ./publish`
2. Create IIS site pointing to `./publish`
3. Install ASP.NET Core Hosting Bundle
4. Set `ASPNETCORE_ENVIRONMENT=Production` in IIS environment variables
5. Update `appsettings.json` connection string and JWT secret
