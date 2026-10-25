# TalTec-WebAppSec
## Web-Based Secure Password Manager

---

## Project Scope

### Deliverables
The project consists of the following components:

- **Web Application** - Browser-based user interface for password management
- **Browser Extension** - Provides password-manager functionality directly within the browser
- **Backend API** - Handles authentication, authorization, credential management, and security-related operations
- **Database** - Persistently stores user data, encrypted credentials, and related information

### Planned Features and Tasks
The project will implement the following functionality:

- User authentication through the backend API
- Creation, retrieval, modification, and deletion of credentials
- Secure password generation using a cryptographically secure random number generator
- Session management, including reduced and full authorization levels
- Encryption and decryption of stored credentials
- Server-side access control and authorization enforcement
- Protection against common web application vulnerabilities
- Multi-factor authentication using Time-based One-Time Passwords (TOTP)
- Monitoring and logging of relevant application and service events

### Exclusions
The following topics are outside the scope of this project:

- Distributed Denial-of-Service (DDoS) protection
- Load balancing
- High-availability infrastructure
- Development of a custom multi-factor authentication mechanism

---

## Current Implementation Status

### Completed Features
- User registration and login through backend API
- Session management with 5-minute expiry
- Argon2id password hashing (19 MiB memory, 2 iterations)
- AES-256-GCM credential encryption
- Vault entry creation, retrieval, and updates
- Session validation and revocation
- HTTP-only, Secure cookies
- Docker-based PostgreSQL integration

### In Development
- Vault entry deletion
- Full authorization level (currently only Reduced level implemented)

### Planned Features
- Multi-factor authentication using Time-based One-Time Passwords (TOTP)
- Trusted device management
- Browser extension
- Rate limiting
- CSRF protection

---

## Architecture

| Component | Technology |
|-----------|------------|
| Backend | ASP.NET Core 10.0 |
| Database | PostgreSQL 17 |
| ORM | Entity Framework Core |
| Cryptography | Argon2id (passwords), AES-256-GCM (credentials), SHA-256 (sessions) |
| Containerization | Docker / Docker Compose |

---

## API Endpoints

### Authentication
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | Create user account |
| POST | `/api/auth/login` | Authenticate user |
| GET | `/api/auth/me` | Get current session info |
| POST | `/api/auth/logout` | Terminate session |

### Credential Management
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/vault` | List the authenticated user's credentials (Full authorization required) |
| GET | `/api/vault/{id}` | Retrieve a single credential (Full authorization required) |
| POST | `/api/vault` | Create a credential |
| PUT | `/api/vault/{id}` | Update a credential (Full authorization required) |
| DELETE | `/api/vault/{id}` | Delete a credential (Full authorization required) |

Vault usernames and passwords are encrypted by the server before they are
stored. Vault entry ownership is checked on reads, updates, and deletes.

### Utilities
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/test` | API health check |
| POST | `/api/crypt/generateCred` | Generate random password |

---

## How to Run

### Prerequisites
- .NET SDK 10.0
- Docker Desktop
- Git

### Setup
1. Clone the repository and navigate to the project directory:
   ```bash
   git clone https://github.com/JulianII/TalTec-WebAppSec.git
   cd TalTec-WebAppSec
   ```

2. Configure environment variables:
   ```bash
   cp .env.example .env
   ```
   Edit `.env` with your PostgreSQL credentials.

3. Start the PostgreSQL database:
   ```bash
   docker compose up -d
   ```

4. Build and run the application:
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project src/Server
   ```

### Access
- Web UI: `https://localhost:8443`
- API: `https://localhost:8443/api/...`

The Docker development setup uses a self-signed certificate. Your browser will
warn that it is not trusted; accept the warning only for this local development
instance. Requests made over HTTP on port 8080 are redirected to HTTPS.

### Stop
```bash
docker compose down
```

---

## Authorization Levels

| Level | Capabilities |
|-------|--------------|
| Unauthenticated | No access to protected operations |
| Reduced | Create new credentials only |
| Full | Retrieve, create, edit, and delete credentials |

*Note: Currently all sessions are created as Reduced level.*

---

## Security Features
- Master password hashed with Argon2id (19 MiB memory, 2 iterations)
- Credentials encrypted with AES-256-GCM
- Session tokens are 256-bit random values, hashed with SHA-256
- VaultKey derived from master password + unique salt per user
- HTTP-only, Secure cookies with 5-minute expiry

---
## Project Timeline

| Milestone | Deadline |
|-----------|----------|
| Checkpoint 1 | Tuesday, 22 September 2026, 12:00 AM |
| Checkpoint 2 | Friday, 30 October 2026, 12:00 AM |
| Checkpoint 3 | Monday, 16 November 2026, 12:00 AM |
| Final report and code submission | Sunday, 29 November 2026, 12:00 AM |