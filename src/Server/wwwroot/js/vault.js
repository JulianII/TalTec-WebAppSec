const entriesElement = document.getElementById('vault-page-entries');
const statusElement = document.getElementById('vault-page-status');
const errorElement = document.getElementById('vault-page-error');

async function request(url, method = 'GET') {
    const response = await fetch(url, {
        method,
        credentials: 'include'
    });
    const text = await response.text();
    let data = null;

    if (text) {
        try {
            data = JSON.parse(text);
        } catch {
            data = { message: text };
        }
    }

    return { ok: response.ok, status: response.status, data };
}

function showError(message) {
    errorElement.textContent = message;
    errorElement.classList.remove('hidden');
}

function renderEntries(entries) {
    entriesElement.replaceChildren();

    if (entries.length === 0) {
        statusElement.textContent = 'Your vault is empty. Add an entry from the main page.';
        return;
    }

    statusElement.textContent = `${entries.length} ${entries.length === 1 ? 'entry' : 'entries'}`;

    for (const entry of entries) {
        const card = document.createElement('article');
        card.className = 'vault-entry-card';

        const title = document.createElement('h2');
        title.textContent = entry.title;

        const website = document.createElement('p');
        website.className = 'vault-entry-website';
        website.textContent = entry.website;

        const username = document.createElement('p');
        username.textContent = `Username: ${entry.username}`;

        const password = document.createElement('p');
        password.textContent = 'Password: ';
        const passwordValue = document.createElement('span');
        passwordValue.textContent = '••••••••';
        passwordValue.dataset.password = entry.password;
        password.append(passwordValue);

        const togglePassword = document.createElement('button');
        togglePassword.type = 'button';
        togglePassword.className = 'password-toggle';
        togglePassword.textContent = 'Show password';
        togglePassword.setAttribute('aria-pressed', 'false');
        togglePassword.addEventListener('click', () => {
            const isVisible = togglePassword.getAttribute('aria-pressed') === 'true';
            passwordValue.textContent = isVisible ? '••••••••' : passwordValue.dataset.password;
            togglePassword.textContent = isVisible ? 'Show password' : 'Hide password';
            togglePassword.setAttribute('aria-pressed', String(!isVisible));
        });

        card.append(title, website, username, password, togglePassword);
        entriesElement.append(card);
    }
}

async function loadVault() {
    errorElement.classList.add('hidden');
    statusElement.textContent = 'Loading your vault...';

    try {
        const [session, vault] = await Promise.all([
            request('/api/auth/me'),
            request('/api/vault')
        ]);

        if (session.status === 401) {
            statusElement.textContent = '';
            showError('Your session is missing or expired. Go back to the login page and sign in again.');
            return;
        }

        if (!session.ok) {
            statusElement.textContent = '';
            showError(session.data?.message || 'Could not verify your session. Please try again.');
            return;
        }

        if (session.data) {
            document.getElementById('vault-user-info').textContent =
                `Signed in as ${session.data.username}`;
        }

        if (vault.status === 401) {
            statusElement.textContent = '';
            showError('Your session is valid, but its vault key is unavailable. Sign out and sign in again to unlock the vault.');
            return;
        }

        if (vault.status === 403) {
            statusElement.textContent = '';
            showError('Complete 2FA verification on the main page to unlock your vault.');
            return;
        }

        if (!vault.ok || !Array.isArray(vault.data)) {
            statusElement.textContent = '';
            showError(vault.data?.message || 'Could not load vault entries. Please try again.');
            return;
        }

        renderEntries(vault.data);
    } catch (error) {
        statusElement.textContent = '';
        showError(`Could not load vault entries: ${error.message}`);
    }
}

document.getElementById('vault-refresh-btn').addEventListener('click', loadVault);
document.getElementById('vault-page-logout-btn').addEventListener('click', async () => {
    try {
        const result = await request('/api/auth/logout', 'POST');
        if (!result.ok) {
            showError('Could not end the session. Please try again.');
            return;
        }

        window.location.assign('/');
    } catch (error) {
        showError(`Could not end the session: ${error.message}`);
    }
});

loadVault();
