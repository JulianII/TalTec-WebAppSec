# **Introduction and overview**

This Project includes the designing of a web-based software system. In this case the project is a password manager. The password manager will consist of four major systems.

First, an **API** which handles authentication, authorization and data flow. Secondly, a **web interface**, where the user can interact with the API through predetermined commands (add, delete and edit). Third, a **browser extension**, which will allow for convenient logins, password generation, and saving of credentials. Finally, a **database** that stores user accounts and encrypted vault data.

This project was developed during the autumn semester of 2026 as part of the Web Application Security course by Julian Lorenz, an Erasmus exchange student from Germany. 

This design document will include the system architecture diagram, a threat model mapped to the OWASP top 10, the cryptographic design, planned authentication and session model.

# System architecture Diagram

![][image1]  
created using draw.io

# **Threat model**

## Assets

**User Credentials**  
Stored usernames, passwords, URLs and other sensitive information managed by the password manager. These must be protected against unauthorized access, modification and disclosure.

**User's Master Password**  
The user's primary authentication secret. It is never stored by the system and must be protected during transmission and processing.

**Authenticated Session**  
Represents the user's current authorization state. A compromised session could allow an attacker to perform unwanted operations on behalf of the user.

**Unlocked Vault State**  
The decrypted vault and PEK temporarily held by the API while credentials are being accessed. Compromise of this state can expose the user's stored credentials.

**Trusted Device Token**  
A long-lived authentication token used to identify a trusted device and reduce the need for repeated MFA. Compromise of the token could allow unauthorized access to the associated account.

## Threat actors

**External Attacker**  
An unauthenticated attacker attempting to exploit the system, the web application, API or browser extension.

**Malicious User**  
A legitimate user attempting to access or modify resources belonging to another user, for example by exploiting broken access control.

**Database Attacker**  
An attacker who obtains unauthorized access to the database and attempts to recover sensitive information from stored data.

## Attack surfaces

| Web Application | Login and authentication Credential management Session handling User input |
| :---- | :---- |
| API | Authentication and authorization endpoints Credential CRUD operations Session management Vault encryption/decryption Input handling |
|  Browser Extension  | Communication with the API Detection of login forms Handling of credentials Storage of authentication tokens |
|  Database  | User account data Password verifiers Encrypted PEKs Encrypted vault data TOTP-related data |
|  Network Communication | Client ↔ API API ↔ Database |

Protected using HTTPS/TLS where applicable.

| Threat | Example | Mitigation |
| ----- | ----- | ----- |
| **SQL Injection** | Attacker manipulates database queries through user input | Parameterized queries / ORM, input validation |
| **Broken Access Control** | User accesses another user's credential by modifying an ID | Server-side authorization checks for every resource |
| **Cross-Site Scripting (XSS)** | Attacker injects JavaScript into the web application | Output encoding, input validation, Content Security Policy |
| **CSRF** | Attacker causes a logged-in user to perform an unwanted action | SameSite cookies, CSRF tokens where applicable |
| **Session Hijacking** | Attacker obtains a valid session token | Secure, HttpOnly, SameSite cookies; TLS; session expiration |
| **Credential Theft** | Attacker attempts to obtain stored passwords from the database | AES-256-GCM encryption with per-user PEK |
| **Database Compromise** | Attacker gains read access to the database | Encrypted vault data, encrypted PEK, Argon2id password hashing |
| **Brute-force / Password Guessing** | Attacker repeatedly guesses the master password | Argon2id, rate limiting, MFA |
| **Token Theft** | Trusted device token is stolen | High-entropy random tokens, hashed storage, revocation |
| **Privilege Escalation** | Reduced session attempts full-session operations | Explicit authorization level checks on sensitive operations |
| **Credential Exposure in Transit** | Attacker intercepts credentials between client and API | HTTPS/TLS |
| **Malicious Input** | Unexpected input causes application errors or injection | Input validation and secure API design |

## Example attack scenarios

| Scenario | Description | Impact | Mitigations |
| :---- | :---- | :---- | :---- |
| **Scenario 1: Database Compromise** | The attacking-force has a copy of the database | Password verifier can be subject to offline password guessing. Encrypted PEKs and vault data are obtained. | Master passwords are not processed in plaintext. PEKs are random and unique for each user. PEKs are stored encrypted. Vault credentials are encrypted. No credentials are stored in plaintext form. |
| **Scenario 2: Broken Access Control** | The attacker has an authenticated session and modifies a credential identifier inside of a request.  | The attacker may be able to disturb the confidentiality, availability and integrity of other users' credentials. | The API performs server-side authorization and ownership checks for every operation. |
| **Scenario 3: Trusted Device Token Theft** |  The attacker obtains a trusted device token of another user. | The attacker is able to establish an reduced authenticated session with another users permission | Tokens are only transmitted over HTTPS/TLS. Only hashes of tokens are stored in the database. Tokens can be revoked. Token possession does not automatically allow for vault operations. |

## Security objectives

**Confidentiality**  
Prevent unauthorized access to user credentials, master passwords, vault data and authentication secrets.

**Integrity**  
Prevent unauthorized modification of credentials, account data and security-sensitive operations.

**Availability**  
Ensure that legitimate users can access their credentials and core password-management functionality under normal operating conditions.

**Authentication**  
Ensure that users and trusted devices are properly authenticated before accessing protected functionality.

**Authorization**  
Ensure that users can only access resources and perform operations they are authorized to perform.

**Session Security**  
Protect authenticated sessions against theft, fixation, unauthorized privilege escalation and misuse.

**Secure Credential Storage**  
Ensure that sensitive vault data is not stored in plaintext and remains protected in the event of a database compromise.

# 

# 

# **Cryptographic design**

The password manager uses client-side encryption to protect stored credentials from unauthorized access to the database. The cryptographic design is based on a master password-derived key, a separate Password Encryption Key (PEK), authenticated encryption, and additional secrets for multi-factor authentication and trusted devices.

The master password is never stored by the application. Instead, it is used to derive cryptographic material through a password-based key derivation function.

## Key Hierarchy

Each unique user is provided with a single randomly generated PEK. This PEK gets encrypted using the KEK which gets derived from the user's Master Password.  

This PEK sits encrypted on the Database, it is used to encrypt and decrypt the external credentials.

The users Master Password is not used to encrypt but rather a derived hash from that password.

## Master Password and Key Derivation

The master password is never stored by the application.

The Server will use Argon2id, a key derivation function. A unique random salt is generated for each user and stored alongside the user's account data.

The master password is also used to derive the user's KEK:

KEK \= Argon2id(  
    master\_password,  
    user\_salt,  
    parameters  
)

The KEK is not stored in the database. It is derived when required from the user's master password.

The master password is transmitted to the API only over an encrypted HTTPS/TLS connection and is never stored in plaintext.

## Password Encryption Key (PEK)

A unique PEK is generated for each user using a cryptographically secure random number generator.

The PEK is a high-entropy key and is used for encryption and decryption of the user's vault data.

The PEK is not stored in plaintext in the database. Instead, it is encrypted using the KEK derived from the user's master password.

The encrypted PEK can therefore be stored in the database without exposing the plaintext PEK.

After successful authentication, the API derives the user's KEK and uses it to decrypt the PEK. The plaintext PEK only exists in the API's memory while it is required for vault encryption or decryption.

## Vault Encryption

Sensitive vault information, including stored usernames and passwords, is encrypted before being stored in the database.

The PEK is used with AES-256-GCM to provide both confidentiality and integrity.

Each encryption operation uses a unique, randomly generated nonce.

The nonce and authentication tag do not need to be kept secret and can be stored alongside the ciphertext.

AES-GCM also allows the application to detect unauthorized modification of encrypted vault data.

The database therefore contains encrypted vault data rather than plaintext credentials.

## Per-User Key Isolation

Each user has a separate PEK and therefore a separate encryption context.

The compromise of one user's PEK does not provide the encryption key for another user's vault.

This limits the potential impact of a compromise compared to using one global encryption key for all users.

## **TOTP Multi-Factor Authentication**

The application uses Time-based One-Time Password (TOTP) as an additional authentication factor.

During MFA enrollment, a randomly generated TOTP secret is associated with the user's account. The secret is shared with the user's authenticator application.

The authenticator generates time-dependent one-time codes based on the shared secret and the current time.

The API verifies the submitted TOTP code before granting full authorization.

The TOTP secret is separate from the vault encryption keys and is used exclusively for authentication.

## Trusted Device Token

A trusted device token is used to identify a device that the user has previously chosen to trust.

The token is generated using a cryptographically secure random number generator and is unique to the device.

The token is not used to encrypt vault data. Instead, it acts as an additional authentication and device-trust mechanism.

The server does not store the token in plaintext. Instead, a cryptographic hash of the token is stored and compared when the device presents the token.A trusted device token is bound to the corresponding user account and can be revoked.

Possession of a valid trusted device token does not provide direct access to the user's vault. It is only used to satisfy the trusted-device requirement for operations that do not require full authorization.

# **Planned authentication model**

The application will use two authentication levels, **reduced authentication** and **full authentication**.

In reduced authentication, the user authenticates using their username and master password. This provides limited access to their password manager. In full authentication, the user additionally provides the MFA TOTP.

A full authentication will always be required upon initial account creation. However after the user created his account he will be able to “trust their current device” this will create a trusted-device token and bind it to the users account. With this token the user will no longer be required to enter their MFA TOTP when adding new external credentials. 

# Planned Session Model

The application differentiates between three session levels: unauthenticated session, reduced authorized session, and full authorized session.

**Unauthenticated Session**

A user is in this state when no valid authorized session exists. The user has not yet successfully authenticated against the API and cannot perform any protected operations.

An unauthenticated session can transition into either a reduced or full authorized session after successfully authenticating using an available user account.

**Reduced Authorized Session**

A reduced authorized session is established when the user successfully authenticates using their username and master password and provides a valid trusted-device token.

If the user cannot provide a valid trusted-device token, they will be required to provide an MFA TOTP code. 

A reduced authorized session provides limited access to the APIs capabilities. The user can perform operations that do not require access to existing sensitive credentials, such as adding new credentials through the browser extension, for example.

Operations that expose or modify existing credentials require full authorization.

**Full Authorized Session**

A full authorized session is established when the user successfully authenticates using their username, master password, and MFA TOTP.

This session provides access to operations involving existing user credentials, such as viewing, editing, and deleting vault credentials. Before executing such operations, the application will additionally verify that the requested credential belongs to the authenticated user.

**Session Level Rationale**

Although the authentication process may use the exact same credentials in some cases, the application deliberately distinguishes between reduced and full authorized sessions based on their granted permissions.

A reduced authorized session is intended for more frequent, lower-risk operations and provides only limited access to the API. A full authorized session provides access to existing sensitive credentials and therefore represents a significantly higher privilege level.

This separation limits the impact of a compromised reduced session. An attacker who obtains a reduced session cannot automatically gain the privileges of a full session. The session's authorization level therefore remains fixed until the session expires or is terminated.

[image1]: <data:image/png;base64,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>
