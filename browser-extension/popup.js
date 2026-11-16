const API_BASE = 'https://localhost:8443';

const statusElement = document.getElementById('status');
const siteElement = document.getElementById('site');
const entriesElement = document.getElementById('entries');
const openAppButton = document.getElementById('open-app-button');

let activeTab = null;

async function request(path) {
    const response = await fetch(`${API_BASE}${path}`, {
        credentials: 'include',
        cache: 'no-store'
    });

    let data = null;
    const text = await response.text();
    if (text) {
        try {
            data = JSON.parse(text);
        } catch {
            data = { message: text };
        }
    }

    return { ok: response.ok, status: response.status, data };
}

function showAppButton() {
    openAppButton.classList.remove('hidden');
}

function getActivePageOrigin(url) {
    try {
        const page = new URL(url);
        if (page.protocol !== 'https:' &&
            !(page.protocol === 'http:' &&
                (page.hostname === 'localhost' || page.hostname === '127.0.0.1'))) {
            return null;
        }

        return page.origin;
    } catch {
        return null;
    }
}

function getEntryOrigin(website) {
    if (typeof website !== 'string' || website.trim() === '') {
        return null;
    }

    try {
        const value = website.trim();
        const url = new URL(/^[a-z][a-z\d+.-]*:\/\//i.test(value) ? value : `https://${value}`);
        if (url.protocol !== 'https:' &&
            !(url.protocol === 'http:' &&
                (url.hostname === 'localhost' || url.hostname === '127.0.0.1'))) {
            return null;
        }

        return url.origin;
    } catch {
        return null;
    }
}

function renderEntries(entries, pageOrigin) {
    entriesElement.replaceChildren();
    const matches = entries.filter(entry => getEntryOrigin(entry.website) === pageOrigin);

    if (matches.length === 0) {
        statusElement.textContent = 'No vault entries found for this exact website.';
        return;
    }

    statusElement.textContent = `${matches.length} matching ${matches.length === 1 ? 'login' : 'logins'}`;

    for (const entry of matches) {
        const card = document.createElement('article');
        card.className = 'entry';

        const details = document.createElement('div');
        const title = document.createElement('h2');
        title.className = 'entry-title';
        title.textContent = entry.title || 'Untitled';

        const username = document.createElement('p');
        username.className = 'entry-username';
        username.textContent = entry.username;
        details.append(title, username);

        const fillButton = document.createElement('button');
        fillButton.className = 'fill-button';
        fillButton.type = 'button';
        fillButton.textContent = 'Fill';
        fillButton.addEventListener('click', () => fillCredentials(entry, pageOrigin));

        card.append(details, fillButton);
        entriesElement.append(card);
    }
}

async function fillCredentials(entry, expectedOrigin) {
    try {
        const [result] = await chrome.scripting.executeScript({
            target: { tabId: activeTab.id },
            func: (username, password, origin) => {
                if (window.location.origin !== origin) {
                    return { ok: false, message: 'The website has changed. Please check it again.' };
                }

                if (window.location.protocol !== 'https:' &&
                    window.location.hostname !== 'localhost' &&
                    window.location.hostname !== '127.0.0.1') {
                    return { ok: false, message: 'For your security, autofill is only available on HTTPS websites.' };
                }

                const isUsable = input =>
                    input instanceof HTMLInputElement &&
                    !input.disabled &&
                    !input.readOnly &&
                    input.getClientRects().length > 0;
                const passwordInput = Array.from(document.querySelectorAll('input[type="password"]'))
                    .find(isUsable);

                if (!passwordInput) {
                    return { ok: false, message: 'No visible password field found on this page.' };
                }

                const usernameInput = passwordInput.form
                    ? [
                        ...document.querySelectorAll('input[autocomplete="username"]'),
                        ...document.querySelectorAll('input[type="email"]'),
                        ...document.querySelectorAll('input[type="text"]')
                    ].find(input =>
                        isUsable(input) &&
                        input.type !== 'password' &&
                        input.form === passwordInput.form)
                    : null;

                const setValue = (input, value) => {
                    const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')?.set;
                    if (setter) {
                        setter.call(input, value);
                    } else {
                        input.value = value;
                    }
                    input.dispatchEvent(new Event('input', { bubbles: true }));
                    input.dispatchEvent(new Event('change', { bubbles: true }));
                };

                if (usernameInput) {
                    setValue(usernameInput, username);
                }
                setValue(passwordInput, password);
                return { ok: true, usernameFilled: Boolean(usernameInput) };
            },
            args: [entry.username, entry.password, expectedOrigin]
        });

        if (!result?.result?.ok) {
            statusElement.textContent = result?.result?.message || 'Could not fill credentials.';
            return;
        }

        statusElement.textContent = result.result.usernameFilled
            ? 'Credentials filled. The login form was not submitted.'
            : 'Password filled. No username field found; the login form was not submitted.';
    } catch {
        statusElement.textContent = 'Could not fill credentials. Make sure the page is open in a regular tab.';
    }
}

async function loadVault() {
    statusElement.textContent = 'Checking connection …';
    siteElement.classList.add('hidden');
    entriesElement.replaceChildren();
    openAppButton.classList.add('hidden');

    try {
        [activeTab] = await chrome.tabs.query({ active: true, currentWindow: true });
        const pageOrigin = getActivePageOrigin(activeTab?.url);
        if (!pageOrigin) {
            statusElement.textContent = 'Open an HTTPS website to find matching credentials.';
            return;
        }

        siteElement.textContent = `Website: ${new URL(pageOrigin).host}`;
        siteElement.classList.remove('hidden');

        const session = await request('/api/auth/me');
        if (session.status === 401) {
            statusElement.textContent = 'Not signed in or session expired.';
            showAppButton();
            return;
        }
        if (!session.ok) {
            statusElement.textContent = 'Could not verify the session.';
            return;
        }

        if (session.data?.authenticationLevel !== 1) {
            statusElement.textContent = 'Sign in to the web app and complete MFA verification.';
            showAppButton();
            return;
        }

        const vault = await request('/api/vault');
        if (vault.status === 401 || vault.status === 403) {
            statusElement.textContent = 'Vault locked. Sign in again and complete MFA verification.';
            showAppButton();
            return;
        }
        if (!vault.ok || !Array.isArray(vault.data)) {
            statusElement.textContent = 'Could not load the vault.';
            return;
        }

        renderEntries(vault.data, pageOrigin);
    } catch {
        statusElement.textContent =
            'Could not connect to the web app. Start it, then open https://localhost:8443 in your browser once to trust the local certificate.';
    }
}

document.getElementById('refresh-button').addEventListener('click', loadVault);
openAppButton.addEventListener('click', () => {
    chrome.tabs.create({ url: `${API_BASE}/` });
});

loadVault();
