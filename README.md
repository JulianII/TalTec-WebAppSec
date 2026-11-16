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
- Vault entry creation, retrieval, updates, and deletion through API and web UI
- Cryptographically secure password generation in the web UI
- TOTP enrollment and verification for full authorization
- Trusted-device registration and revocation; trusted sessions retain reduced
  access and still require MFA to view or manage existing vault entries
- Session validation and revocation
- HTTP-only, Secure cookies
- CSRF token validation on state-changing API routes
- Docker-based PostgreSQL integration
- Chrome/Brave Manifest V3 extension for local development with explicit autofill
- In-memory, per-client-IP rate limits on authentication, MFA, and vault operations

### Remaining Work
- Validate the database migration and new flows in the local Docker instance.
- Run and document security tests, including SQL injection, XSS, and
  cross-user vault access checks; the test plan is in
  [Documents/Checkpoint-3-Security-Testing.md](./Documents/Checkpoint-3-Security-Testing.md).
- Draft the Checkpoint 3 report sections for Implementation, Security Analysis,
  and Conclusion.

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
| GET | `/api/auth/trusted-devices` | List active trusted devices (Full authorization required) |
| POST | `/api/auth/trusted-devices` | Trust this device (Full authorization required) |
| DELETE | `/api/auth/trusted-devices/{deviceId}` | Revoke a trusted device (Full authorization required) |
| GET | `/api/auth/csrf-token` | Obtain an anti-forgery request token |

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
- Docker Desktop (or Docker Engine with the Compose plugin)
- Git

### Setup
1. Clone the repository and navigate to the project directory:
   ```bash
   git clone https://github.com/JulianII/TalTec-WebAppSec.git
   cd TalTec-WebAppSec
   ```

2. Create the Compose environment file:
   ```bash
   cp .env.example .env
   ```
   On Windows PowerShell, use `Copy-Item .env.example .env`. For a non-local
   environment, replace the example database password in `.env`.

3. Build and start the application:
   ```bash
   docker compose up --build -d
   ```
   Compose waits for PostgreSQL to become healthy, applies the EF Core
   migrations, and then starts the web application. No host-installed .NET SDK
   or separate `dotnet run` command is needed.

4. Confirm the services are running:
   ```bash
   docker compose ps
   ```

### Access
- Web UI: `https://localhost:8443`
- API: `https://localhost:8443/api/...`

The Docker development setup uses a self-signed certificate. Your browser will
warn that it is not trusted; accept the warning only for this local development
instance. Requests made over HTTP on port 8080 are redirected to HTTPS.

### Browser Extension (Chrome / Brave)
The Manifest V3 extension uses the existing web-app session and can fill
matching vault entries into login forms after an explicit click. It is currently
configured for the local development URL `https://localhost:8443`. Start the
app, open that URL once to accept its development certificate, sign in and
complete MFA, then load the `browser-extension` folder as an unpacked extension
from `chrome://extensions` or `brave://extensions` with Developer mode enabled.
See [browser-extension/README.md](./browser-extension/README.md) for details.

### Stop
```bash
docker compose down
```
The database volume is retained when stopping the application. To remove the
local database and all stored accounts and vault entries as well, run
`docker compose down --volumes`.

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
- Trusted-device tokens are stored as hashes, expire after one year, and can be
  revoked; at most 10 active devices are allowed per account
- State-changing API calls require a server-validated anti-forgery token
- Per-client-IP fixed-window limits: login/MFA (5 per minute), registration
  (5 per hour), MFA setup (3 per hour), session/token reads (120 per minute),
  vault reads (60 per minute), vault writes (30 per minute), and trusted-device
  changes (10 per day). Rejected requests return HTTP 429 with a generic
  response and `Retry-After`.
- Unhandled server errors return a generic response with a trace ID; detailed
  exceptions are logged server-side for diagnosis. Application log messages
  do not intentionally include passwords, usernames, or session credential hashes.

Rate-limit counters are in memory and apply per server instance. When deploying
behind a reverse proxy, configure ASP.NET Core forwarded headers to trust only
the known proxy addresses so limits use the actual client IP. These limits are
not a substitute for distributed denial-of-service protection.

---
## Project Timeline

| Milestone | Deadline |
|-----------|----------|
| Checkpoint 1 | Tuesday, 22 September 2026, 12:00 AM |
| Checkpoint 2 | Friday, 30 October 2026, 12:00 AM |
| Checkpoint 3 | Monday, 16 November 2026, 12:00 AM |
| Final report and code submission | Sunday, 29 November 2026, 12:00 AM |