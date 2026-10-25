// API Endpoints
const API = {
    register: '/api/auth/register',
    login: '/api/auth/login',
    logout: '/api/auth/logout',
    me: '/api/auth/me',
    upgrade: '/api/auth/upgrade',
    totpSetup: '/api/auth/totp/setup',
    totpEnable: '/api/auth/totp/enable',
    vault: '/api/vault'
};

// State
let user = null;
let currentMfaSecret = '';

// API Request Helper
async function apiRequest(url, method = 'GET', data = null) {
    const options = {
        method,
        credentials: 'include',
        headers: {}
    };
    
    if (data) {
        options.headers['Content-Type'] = 'application/json';
        options.body = JSON.stringify(data);
    }
    
    try {
        const response = await fetch(url, options);
        const responseText = await response.text();
        let responseData = null;

        if (responseText) {
            try {
                responseData = JSON.parse(responseText);
            } catch {
                responseData = { message: responseText };
            }
        }

        return { ok: response.ok, status: response.status, data: responseData };
    } catch (error) {
        console.error('API Error:', error);
        return { ok: false, status: 0, data: { message: error.message } };
    }
}

// Show Error
function showError(elementId, message) {
    const el = document.getElementById(elementId);
    if (el) {
        el.textContent = message;
        el.classList.remove('hidden');
    }
}

// Hide Error
function hideError(elementId) {
    const el = document.getElementById(elementId);
    if (el) el.classList.add('hidden');
}

// Navigate between views
function showView(viewId) {
    // Hide all views
    document.querySelectorAll('.view').forEach(v => v.classList.add('hidden'));
    // Show requested view
    const view = document.getElementById(viewId);
    if (view) view.classList.remove('hidden');
}

// Check Session
async function checkSession() {
    const result = await apiRequest(API.me);
    
    if (result.ok && result.data) {
        user = result.data;
        
        // Full auth -> vault
        if (result.data.authenticationLevel === 1) {
            showView('view-vault');
            updateUserInfo();
            loadEntries();
            return;
        }
        
        // Reduced auth with TOTP enabled -> upgrade
        if (result.data.totpEnabled) {
            showView('view-totp-upgrade');
            return;
        }
        
        // Reduced auth without TOTP -> setup
        showView('view-totp-setup');
        await setupTOTP();
    } else {
        // No session -> login
        user = null;
        showView('view-login');
    }
}

// Update user info display
function updateUserInfo() {
    const el = document.getElementById('user-info');
    if (el && user) {
        el.textContent = `Logged in as: ${user.username} | Auth: ${user.authenticationLevel === 1 ? 'Full' : 'Reduced'}`;
    }
}

// ===== LOGIN =====
document.getElementById('login-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError('login-error');
    
    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;
    
    const result = await apiRequest(API.login, 'POST', { username, password });
    
    if (result.ok) {
        await checkSession();
    } else {
        showError('login-error', 'Invalid username or password');
    }
});

// ===== REGISTER =====
document.getElementById('register-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError('register-error');
    
    const username = document.getElementById('register-username').value;
    const password = document.getElementById('register-password').value;
    const verifyPassword = document.getElementById('register-verify-password').value;
    
    if (password !== verifyPassword) {
        showError('register-error', 'Passwords do not match');
        return;
    }
    
    const result = await apiRequest(API.register, 'POST', { username, password, verifyPassword });
    
    if (result.ok) {
        await checkSession();
    } else {
        showError('register-error', result.data?.message || 'Registration failed');
    }
});

// ===== TOTP SETUP =====
async function setupTOTP() {
    const result = await apiRequest(API.totpSetup, 'GET');
    
    if (result.ok && result.data) {
        currentMfaSecret = result.data.secret;
        
        // Show QR code
        const qrEl = document.getElementById('totp-qr-code');
        if (qrEl) {
            qrEl.innerHTML = `<img src="https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(result.data.qrCodeUrl)}" alt="QR Code">`;
        }
        
        // Show secret
        const secretEl = document.getElementById('totp-secret');
        if (secretEl) {
            secretEl.textContent = currentMfaSecret;
        }
    } else {
        showError('totp-error', 'Failed to setup 2FA');
    }
}

// Verify TOTP and enable
document.getElementById('totp-verify-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError('totp-error');
    
    const code = document.getElementById('totp-code').value;
    
    if (!code || code.length !== 6) {
        showError('totp-error', 'Please enter a valid 6-digit code');
        return;
    }
    
    const result = await apiRequest(API.totpEnable, 'POST', { code, secret: currentMfaSecret });
    
    if (result.ok) {
        // Reload session to check auth level
        await checkSession();
    } else {
        showError('totp-error', result.data?.message || 'Invalid code');
    }
});

// ===== TOTP UPGRADE =====
document.getElementById('totp-upgrade-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideError('totp-upgrade-error');
    
    const code = document.getElementById('totp-upgrade-code').value;
    
    if (!code || code.length !== 6) {
        showError('totp-upgrade-error', 'Please enter a valid 6-digit code');
        return;
    }
    
    const result = await apiRequest(API.upgrade, 'POST', { code });
    
    if (result.ok) {
        await checkSession();
    } else {
        showError('totp-upgrade-error', result.data?.message || 'Invalid code');
    }
});

// ===== VAULT =====
async function loadEntries() {
    // For now, just show add button
    // TODO: Implement actual entry loading when backend endpoint exists
    const entriesEl = document.getElementById('vault-entries');
    if (entriesEl) {
        entriesEl.innerHTML = '<p>No vault entries endpoint yet. Use backend directly.</p>';
    }
}

// Add entry form handling
let isAdding = false;
document.getElementById('add-entry-btn')?.addEventListener('click', () => {
    isAdding = true;
    document.getElementById('vault-form').classList.remove('hidden');
    document.getElementById('vault-title').value = '';
    document.getElementById('vault-website').value = '';
    document.getElementById('vault-username').value = '';
    document.getElementById('vault-password').value = '';
});

document.getElementById('vault-cancel')?.addEventListener('click', () => {
    isAdding = false;
    document.getElementById('vault-form').classList.add('hidden');
});

document.getElementById('vault-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const title = document.getElementById('vault-title').value;
    const website = document.getElementById('vault-website').value;
    const username = document.getElementById('vault-username').value;
    const password = document.getElementById('vault-password').value;
    
    const result = await apiRequest(API.vault, 'POST', {
        title,
        website,
        externalUsername: username,
        generatedPassword: password
    });
    
    if (result.ok) {
        isAdding = false;
        document.getElementById('vault-form').classList.add('hidden');
        loadEntries();
    } else {
        alert('Failed to save entry: ' + (result.data?.message || 'Error'));
    }
});

// ===== LOGOUT =====
async function logoutAndReturnToLogin(errorElementId = null) {
    const result = await apiRequest(API.logout, 'POST');
    if (!result.ok) {
        if (errorElementId) {
            showError(errorElementId, 'Could not end the session. Please try again.');
        } else {
            alert('Could not end the session. Please try again.');
        }
        return;
    }

    user = null;
    currentMfaSecret = '';
    document.getElementById('login-password').value = '';
    hideError('totp-upgrade-error');
    showView('view-login');
}

document.getElementById('logout-btn')?.addEventListener('click', logoutAndReturnToLogin);
document.getElementById('totp-upgrade-logout-btn')?.addEventListener('click', () =>
    logoutAndReturnToLogin('totp-upgrade-error'));

// ===== NAVIGATION =====
document.getElementById('goto-register')?.addEventListener('click', (e) => {
    e.preventDefault();
    showView('view-register');
});

document.getElementById('goto-login')?.addEventListener('click', (e) => {
    e.preventDefault();
    showView('view-login');
});

// Initialize
async function init() {
    await checkSession();
}

// Start when DOM is ready
document.addEventListener('DOMContentLoaded', init);
