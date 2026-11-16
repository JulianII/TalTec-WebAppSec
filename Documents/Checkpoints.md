# Notes
PWM: Password manager
### collecting ideas / brainstorming
(?) Create Browser extension & API backend; Give backend access to simple user interface (See and delete Passwords here) rest through Extension
- Extension looks for login / register forms (input ... =password)
- Forward request to PWM API / Backend
- API Looks up relevant Password
- (?) Retrieve generated Session and send back to user (?)
- (?) Basically MITM, possible, too difficult? (?)
  - (PRO) Password never leaves Backend
  - (NEG) User authentication and validation
 
(?) C# or HTML, CSS, JS and PHP?


# Checkpoint 1 req.: 
### *Due: Tuesday, 22 September 2026, 12:00 AM*

Web-Based Secure Password Manager
Checkpoint 1, threat model and architecture (Week 4) 

Deliverables. A short design document plus an initialised repository. 

• System architecture diagram showing the browser, the application and the database, 
indicating where credentials are encrypted and where the trust boundaries are. 

• Threat model mapped to the OWASP Top 10 and the attack classes covered in 
Weeks 1–4. Each threat listed with its intended mitigation. 

• Cryptographic design: how the master password derives the key, what is encrypted 
server-side, and what the database stores. 

• Planned authentication and session model, including cookie flags, expiry, fixation 
prevention and optional multi-factor authentication. 

• Repository initialised with a README stating the scope, the planned routes and 
features, and how to run it locally. 

### Checklist:
| Tasks | Status |
| ------ | --- |
| Design Document | close to finish |
| System Diagram | DONE|
| Threat model | nearly done |
| Cryptographic design | nearly done |
| Auth and Session model | nearly done |
| Propper Readme | scope declared |

# Checkpoint 2 req.
### *Due: Friday, 30 October 2026, 12:00 AM*

Web-Based Secure Password Manager
Checkpoint 2, core requirements implemented (Week 9)

The checkpoint's reference to file encryption is interpreted for this project as
server-side encryption and decryption of password-vault credentials. File upload
or file management is not part of this password-manager requirement.

Deliverables: a running web application in the repository and a short progress
note.

| Requirement translated to the password manager | Status |
| ------ | --- |
| Basic web interface for registration, login, TOTP setup/verification, adding and viewing vault credentials, and logout | Partly implemented: core flows exist; edit/delete are not exposed in the UI |
| Server-side encryption/decryption of stored vault usernames and passwords | Implemented with AES-256-GCM |
| Password hashing and secure session management, including expiry and revocation | Implemented: Argon2id password verification; random session credentials with hashed storage, five-minute expiry, and revocation |
| Application served over TLS in development | Configured in Docker Compose using a self-signed development certificate |
| Clean-machine run instructions and incremental commit history | Docker Compose run instructions use the database health check and migration completion dependencies; incremental commits exist. End-to-end clean-machine setup still needs verification |
| Short progress note | Included in `checkpoint 2 (1).txt` |

### Progress note

The application has account registration and login, TOTP enrollment and
verification, server-side encryption of vault credentials, and API operations
for creating, viewing, updating, and deleting entries. The web interface
currently supports registration, login, TOTP, adding entries, and viewing them.
The development Docker setup configures HTTPS. Compose waits for the database
health check and successful migrations before starting the server. Remaining
CP2 work is to verify the clean-machine run instructions end to end and expose
vault editing/deletion in the UI if those operations are expected as part of
the core flows.
