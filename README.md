# TalTec-WebAppSec
### Web-Based Secure Password Manager
# Planned Scope

## 1. Goal

Design and develop a secure web-based password manager that enables users to securely store and manage their credentials.

## 2. Deliverables

The project consists of the following components:

* **Web Application** – browser-based user interface for password management.
* **Browser Extension** – provides password-manager functionality directly within the browser.
* **Backend API** – handles authentication, authorization, credential management, and security-related operations.
* **Database** – persistently stores user data, encrypted credentials, and related information.

## 3. Planned Features and Tasks

The project will implement the following functionality:

* User authentication through the backend API.
* Creation, retrieval, modification, and deletion of credentials.
* Secure password generation using a cryptographically secure random number generator.
* Session management, including reduced and full authorization levels.
* Encryption and decryption of stored credentials.
* Server-side access control and authorization enforcement.
* Protection against common web application vulnerabilities.
* Multi-factor authentication using Time-based One-Time Passwords (TOTP).
* Monitoring and logging of relevant application and service events.

## 4. Exclusions

The following topics are outside the scope of this project:

* Distributed Denial-of-Service (DDoS) protection.
* Load balancing.
* High-availability infrastructure.
* Development of a custom multi-factor authentication mechanism.

## 5. Constraints and Milestones

The project is subject to the following submission deadlines:

| Milestone                        | Deadline                             |
| -------------------------------- | ------------------------------------ |
| Checkpoint 1                     | Tuesday, 22 September 2026, 12:00 AM |
| Checkpoint 2                     | Friday, 30 October 2026, 12:00 AM    |
| Checkpoint 3                     | Monday, 16 November 2026, 12:00 AM   |
| Final report and code submission | Sunday, 29 November 2026, 12:00 AM   |


# How to Run

## Prerequisites

The following software is required to run the Password Manager locally:

* **.NET SDK** – to build and run the ASP.NET Core application.
* **Docker Desktop** – to run PostgreSQL in a container.
* **Git** – to clone the project repository.
* **A modern web browser** – to access the web interface and test the browser extension.

## Setup and Execution

1. Clone the repository and navigate to the project directory:

   ```bash
   git clone <repository-url>
   cd <repository-directory>
   ```

2. Start the PostgreSQL database using Docker Compose:

   ```bash
   docker compose up -d
   ```

3. Configure the database connection string and other required application settings. Sensitive configuration values, such as database credentials, must be provided through environment variables or a local configuration file that is excluded from version control.

4. Restore the .NET dependencies and build the application:

   ```bash
   dotnet restore
   dotnet build
   ```

5. Apply the database migrations to create or update the database schema:

   ```bash
   dotnet ef database update
   ```

6. Start the ASP.NET Core server:

   ```bash
   dotnet run --project src/Server
   ```

7. Open the local application URL displayed in the terminal to access the web interface. The REST API is exposed by the same ASP.NET Core server.

## Stopping the Application

Stop the ASP.NET Core process using `Ctrl+C`. The PostgreSQL container can be stopped with:

```bash
docker compose down
```

The database uses a Docker volume to persist its data between container restarts. Removing the volume will delete the stored database data.

---

# Routes and Features

The application exposes a REST API through the ASP.NET Core server. The following routes describe the planned API structure; exact paths and request/response formats may be adjusted during implementation.

## Authentication and Account Management

| Method | Route                   | Feature                                              |
| ------ | ----------------------- | ---------------------------------------------------- |
| POST   | `/api/auth/register`    | Create a user account.                               |
| POST   | `/api/auth/login`       | Authenticate using the username and master password. |
| POST   | `/api/auth/totp/verify` | Verify a TOTP code during authentication.            |
| POST   | `/api/auth/logout`      | Terminate the current session.                       |
| GET    | `/api/users/me`         | Retrieve information about the authenticated user.   |

## Credential Management

| Method | Route                   | Feature                                                                                 |
| ------ | ----------------------- | --------------------------------------------------------------------------------------- |
| GET    | `/api/credentials`      | Retrieve the user's stored credentials.                    |
| GET    | `/api/credentials/{id}` | Retrieve a specific credential.  |
| POST   | `/api/credentials`      | Create a new credential.   |
| PUT    | `/api/credentials/{id}` | Update an existing credential.  |
| DELETE | `/api/credentials/{id}` | Delete an existing credential.   |

## Session and Trusted Device Management

| Method | Route                       | Feature                                                                     |
| ------ | --------------------------- | --------------------------------------------------------------------------- |
| GET    | `/api/sessions/current`     | Retrieve information about the current session and its authorization level. |
| POST   | `/api/trusted-devices`      | Register a trusted device.                                                  |
| GET    | `/api/trusted-devices`      | List the user's registered trusted devices.                                 |
| DELETE | `/api/trusted-devices/{id}` | Revoke a trusted device.                                                    |

## TOTP Management

| Method | Route               | Feature                                              |
| ------ | ------------------- | ---------------------------------------------------- |
| POST   | `/api/totp/setup`   | Generate the information required to configure TOTP. |
| POST   | `/api/totp/confirm` | Confirm TOTP setup by verifying a submitted code.    |
| DELETE | `/api/totp`         | Disable TOTP after the required verification.        |

## Additional Features

* **Password generation:** Generate strong random passwords using a cryptographically secure random number generator.
* **Encrypted credential storage:** Encrypt stored credentials using AES-256-GCM.
* **Master password key derivation:** Use Argon2id to derive the key-encryption key.
* **Browser extension:** Provide access to supported password-manager operations through the REST API.
* **Authorization enforcement:** Validate session status, authorization level, and resource ownership on the server for every protected operation.

## Authorization Rules

The application distinguishes between unauthenticated, reduced-authorized, and full-authorized sessions.

* **Unauthenticated:** No access to protected password-manager operations.
* **Reduced session:** May create new credentials but cannot retrieve, edit, or delete existing credentials.
* **Full session:** May retrieve, create, edit, and delete credentials belonging to the authenticated user.

A reduced session is not automatically upgraded to a full session by submitting a TOTP code. The server enforces these authorization rules independently of the web interface and browser extension.

