# TalTec Password Manager – Chrome / Brave

Manifest V3 extension for the local TalTec web app. It uses the web app's
existing HTTP-only session, does not persist credentials or session tokens,
and fills credentials only after you click **Fill**. It does not automatically
submit login forms.

## Requirements

1. From the repository directory, start the app with `docker compose up --build -d`.
2. In Chrome or Brave, open `https://localhost:8443` and trust the self-signed
   certificate for this local development environment.
3. Sign in and complete MFA setup or verification. The vault must be unlocked.

## Installation (Developer mode)

1. Open `chrome://extensions` in Chrome or `brave://extensions` in Brave.
2. Enable **Developer mode**.
3. Select **Load unpacked** and choose this repository's `browser-extension`
   folder.
4. Open an HTTPS login page, click the TalTec extension icon, then click
   **Fill** on the matching entry.

The extension displays only entries whose saved website origin exactly matches
the active tab (scheme, host, and port). Passwords are not displayed in the
popup. It uses `activeTab` and `scripting` to fill fields after an explicit
click, and accesses `https://localhost:8443/*` for the API.

## Notes and limitations

- This version is configured for `https://localhost:8443`. Before deploying to
  another domain, update the API base URL and `host_permissions` specifically;
  do not use broad wildcards.
- The local certificate must be trusted by the browser. Do not bypass
  certificate warnings for third-party or production systems.
- The extension detects basic visible HTML username and password fields.
  Multi-step login pages, certain embedded frames, and some JavaScript forms
  may require manual input.
- The extension does not modify vault entries; manage them in the web app.
