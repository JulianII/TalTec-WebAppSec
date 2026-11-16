const entriesElement = document.getElementById('vault-page-entries');
const statusElement = document.getElementById('vault-page-status');
const errorElement = document.getElementById('vault-page-error');
const request = window.secureApiRequest;
const editForm = document.getElementById('vault-edit-form');
let editingEntryId = null;

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

        const editButton = document.createElement('button');
        editButton.type = 'button';
        editButton.textContent = 'Edit';
        editButton.addEventListener('click', () => {
            editingEntryId = entry.entryId;
            document.getElementById('edit-title').value = entry.title;
            document.getElementById('edit-website').value = entry.website;
            document.getElementById('edit-username').value = entry.username;
            document.getElementById('edit-password').value = entry.password;
            editForm.classList.remove('hidden');
            editForm.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });

        const deleteButton = document.createElement('button');
        deleteButton.type = 'button';
        deleteButton.className = 'secondary-button';
        deleteButton.textContent = 'Delete';
        deleteButton.addEventListener('click', async () => {
            if (!window.confirm(`Delete "${entry.title}" from your vault?`)) {
                return;
            }

            const result = await request(
                `/api/vault/${encodeURIComponent(entry.entryId)}`,
                'DELETE');
            if (!result.ok) {
                showError('Could not delete this entry. Please refresh and try again.');
                return;
            }
            await loadVault();
        });

        card.append(title, website, username, password, togglePassword, editButton, deleteButton);
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
    } catch {
        statusElement.textContent = '';
        showError('Could not load vault entries. Please try again.');
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
    } catch {
        showError('Could not end the session. Please try again.');
    }
});

loadVault();

editForm.addEventListener('submit', async event => {
    event.preventDefault();
    if (editingEntryId === null) {
        return;
    }

    errorElement.classList.add('hidden');
    const result = await request(`/api/vault/${encodeURIComponent(editingEntryId)}`, 'PUT', {
        title: document.getElementById('edit-title').value,
        website: document.getElementById('edit-website').value,
        externalUsername: document.getElementById('edit-username').value,
        generatedPassword: document.getElementById('edit-password').value
    });

    if (!result.ok) {
        showError('Could not update this entry. Please check the fields and try again.');
        return;
    }

    editForm.reset();
    editForm.classList.add('hidden');
    editingEntryId = null;
    await loadVault();
});

document.getElementById('vault-edit-cancel').addEventListener('click', () => {
    editForm.reset();
    editForm.classList.add('hidden');
    editingEntryId = null;
});
