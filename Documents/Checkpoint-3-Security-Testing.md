# Checkpoint 3 Security Test Record

## Test results

The project owner reports that these tests were run on the local application
using synthetic data and passed. Supporting screenshots or logs were not
provided with this record.

| Test | Attack or check | Result | Evidence |
|---|---|---|---|
| SQL injection | Login payload: `test' OR '1'='1' --` | PASS* | Attach sanitized response |
| XSS | Vault title: `<b>CP3-XSS-TEST</b>` | PASS* | Attach screenshot showing literal text |
| Access control | User B requests User A's entry ID | PASS* | Attach sanitized status and retest as User A |
| Error handling | Submit malformed request; inspect response and logs | PASS* | Attach redacted response/log excerpt |
| Session/logout | Try vault access after logout and session expiry | PASS* | Attach sanitized status |
| MFA | Try vault access before and after TOTP | PASS* | Attach sanitized statuses; no MFA secrets |
| Rate limiting | Repeat invalid login requests | PASS* | Attach HTTP status and `Retry-After` |
| Encryption at rest | Inspect a synthetic vault record | PASS* | Attach redacted proof that values are ciphertext |
| CSRF | Send state-changing request without token | PASS* | Attach sanitized response |
| Trusted devices | Trust, re-login, revoke, and retry | PASS* | Attach sanitized status or screenshot |

`*` PASS is based on the project owner's confirmation. Attach the listed
sanitized evidence before submission; do not include passwords, tokens,
cookies, MFA codes, or vault contents.

## Static review notes

- Entity Framework Core queries and user-scoped vault lookups were observed.
- Vault credentials are encrypted with AES-256-GCM; password verifiers use
  Argon2id.
- State-changing endpoints use antiforgery validation, and UI vault fields use
  text rendering.


