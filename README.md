# Graduation Project - Stock Portfolio Investment Platform

A modern stock portfolio management platform built with ASP.NET Core and React.

## Architecture

- **Backend**: ASP.NET Core 8 - Modular Monolith Architecture
- **Frontend**: React 18 with Vite
- **Database**: PostgreSQL 18
- **Cache**: Redis
- **Message Queue**: RabbitMQ with MassTransit
- **Authentication**: JWT Bearer Tokens

## Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- Docker & Docker Compose

### Running the Application

1. **Start Infrastructure Services** (PostgreSQL, Redis, RabbitMQ):
```bash
docker-compose up -d
```

2. **Apply Database Migrations**:
```bash
cd Backend/src/API/Project.Api
dotnet ef database update --context UsersDbContext
dotnet ef database update --context PortfolioDbContext
dotnet ef database update --context NotificationsDbContext
```

3. **Run Backend**:
```bash
cd Backend/src/API/Project.Api
dotnet run
```

4. **Run Frontend**:
```bash
cd frontend
npm install
npm run dev
```

## Testing Features

### Test Notifications

To test the notification system, log in to the application and open the browser console, then run:

```javascript
window.triggerTestNotification()
```

This will create a test notification and refresh the notification count in the UI.

## Project Structure

```
├── Backend/
│   └── src/
│       ├── API/              # API entry point
│       ├── Common/           # Shared infrastructure
│       └── Modules/
│           ├── Users/        # User management
│           ├── Portfolio/    # Portfolio & risk assessment
│           └── Notifications/# Notifications & emails
├── frontend/
│   └── src/
│       ├── components/
│       ├── context/
│       ├── pages/
│       └── services/
└── docker-compose.yml
```

## Documentation

- [System Architecture](SYSTEM_ARCHITECTURE.md)
- [Database ERD](DATABASE_ERD.md)
- [Use Case Diagrams](USE_CASE_DIAGRAM.md)
- [Sequence Diagrams](SEQUENCE_DIAGRAM.md)
- [Contributing Guide](CONTRIBUTING.md)