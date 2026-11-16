# Checkpoint 3 Draft Report

## Introduction

TalTec is a web-based password manager. It stores users' website credentials
and protects them from common web attacks.

## Implementation

The application uses ASP.NET Core, PostgreSQL, and a browser interface. It
hashes master passwords with Argon2id and encrypts vault usernames and
passwords with AES-256-GCM. Sessions expire after five minutes. TOTP provides
full vault access, while trusted devices retain reduced access. The API also
uses per-user ownership checks, CSRF tokens, and rate limits.

## Security Analysis

The code uses Entity Framework Core for database queries, checks vault-entry
ownership, and renders user-controlled vault fields as text. These controls
help defend against SQL injection, unauthorized access, and XSS.

The project owner reports that the security tests passed. The test record is
in [Checkpoint-3-Security-Testing.md](./Checkpoint-3-Security-Testing.md);
sanitized supporting evidence still needs to be attached.

The static review also identified improvements: add CSP and other security
headers, set explicit input length limits, and use shared protected session
context storage before any multi-instance deployment. The session cookie uses
`SameSite=None` to support the browser extension and is protected by CSRF
validation.

## Conclusion

The near-final application includes the main password storage,
authentication, and access-control features required for Checkpoint 3.
Security testing is reported as passed; attach sanitized evidence to complete
the checkpoint materials.
