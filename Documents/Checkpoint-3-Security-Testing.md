# Checkpoint 3 Security Test Record

## Test results

The project owner reports that these tests were run on the local application
using synthetic data and passed. Supporting screenshots or logs were not
provided with this record.

| Test | Attack or check | Result | Evidence |
|---|---|---|---|
| SQL injection | Login payload: `test' OR '1'='1' --` | PASS* | <img width="518" height="354" alt="Screenshot 2026-11-16 225507" src="https://github.com/user-attachments/assets/cab06e16-07cc-4218-ad89-88795b8a068c" /> |
| XSS | Vault title: `<b>CP3-XSS-TEST</b>` | PASS* | <img width="340" height="196" alt="xss" src="https://github.com/user-attachments/assets/4505a340-da76-482a-ab22-0767b9f59042" /> |
| Access control | User B requests User A's entry ID | PASS* | <img width="1162" height="428" alt="expiredcookie" src="https://github.com/user-attachments/assets/6e48a527-8803-4c87-879a-cafe9fe6652a" /> |
| Error handling | Submit malformed request; inspect response and logs | PASS* | <img width="1213" height="218" alt="image" src="https://github.com/user-attachments/assets/4f83447f-7c66-468f-8c2e-14f02a2283f3" /> |
| Session/logout | Try vault access after logout and session expiry | PASS* | <img width="834" height="182" alt="image" src="https://github.com/user-attachments/assets/a639c665-6978-4768-a1a4-b3ab28a273f3" /> |
| MFA | Try vault access before and after TOTP | PASS* | <img width="760" height="274" alt="image" src="https://github.com/user-attachments/assets/09f0d84a-abd4-4704-a659-2c51211ea9c3" /> |
| Rate limiting | Repeat invalid login requests | PASS* | <img width="450" height="188" alt="ratelimiting" src="https://github.com/user-attachments/assets/8f73444d-ed64-481c-a355-f95ef33d6baf" /> |
| Encryption at rest | Inspect a synthetic vault record | PASS* | <img width="1264" height="240" alt="encryptedcreds" src="https://github.com/user-attachments/assets/a2b1a001-6e72-4d51-92e3-7098b96e8a17" /> |
| CSRF | Send state-changing request without token | PASS* | <img width="1158" height="333" alt="missingscrf" src="https://github.com/user-attachments/assets/bb2ddf1a-1b60-4f24-a4ab-9fbbf5864d86" />|
| Trusted devices | Trust, re-login, revoke, and retry | PASS* | <img width="1257" height="73" alt="tdrevokeddb" src="https://github.com/user-attachments/assets/83678408-6f84-4ff7-9ef4-e6410b16132c" /> <img width="329" height="46" alt="tdrevoked" src="https://github.com/user-attachments/assets/cc8932ff-4254-4c87-a88d-9fc71889496c" /> <img width="341" height="46" alt="tdcreated" src="https://github.com/user-attachments/assets/cef7f4e7-c13e-4f5b-aeb6-460389bcdfb8" /> |

## Static review notes

- Entity Framework Core queries and user-scoped vault lookups were observed.
- Vault credentials are encrypted with AES-256-GCM; password verifiers use
  Argon2id.
- State-changing endpoints use antiforgery validation, and UI vault fields use
  text rendering.


