// ===== Configuration =====
const API = {
    register: '/api/auth/register',
    login: '/api/auth/login',
    logout: '/api/auth/logout',
    me: '/api/auth/me',
    upgrade: '/api/auth/upgrade',
    totpSetup: '/api/auth/totp/setup',
    totpEnable: '/api/auth/totp/enable',
    vault: '/api/vault',
    vaultGet: (id) => `/api/vault/${id}`,
    vaultPut: (id) => `/api/vault/${id}`,
    vaultDelete: (id) => `/api/vault/${id}`,
    generatePassword: '/api/crypt/generateCred'
};

// ===== State =====
const state = {
    user: null,
    session: null,
    entries: [],
    currentView: null,
    currentEntryId: null,
    isLoading: false
};

// ===== DOM Elements =====
const elements = {
    // Views
    viewLogin: document.getElementById('view-login'),
    viewRegister: document.getElementById('view-register'),
    viewMfaSetup: document.getElementById('view-mfa-setup'),
    viewMfaUpgrade: document.getElementById('view-mfa-upgrade'),
    viewDashboard: document.getElementById('view-dashboard'),
    viewVault: document.getElementById('view-vault'),
    viewGenerate: document.getElementById('view-generate'),
    viewSettings: document.getElementById('view-settings'),
    viewProfile: document.getElementById('view-profile'),
    appLayout: document.getElementById('app-layout'),
    
    // Forms
    loginForm: document.getElementById('login-form'),
    registerForm: document.getElementById('register-form'),
    mfaVerifyForm: document.getElementById('mfa-verify-form'),
    mfaUpgradeForm: document.getElementById('mfa-upgrade-form'),
    createEntryForm: document.getElementById('create-entry-form'),
    editEntryForm: document.getElementById('edit-entry-form'),
    
    // Modals
    modalCreateEntry: document.getElementById('modal-create-entry'),
    modalViewEntry: document.getElementById('modal-view-entry'),
    modalEditEntry: document.getElementById('modal-edit-entry'),
    modalDeleteConfirm: document.getElementById('modal-delete-confirm'),
    
    // Toast
    toast: document.getElementById('toast')
};

// ===== API Helper =====
async function apiRequest(url, method = 'GET', data = null, expectJson = true) {
    const options = {
        method,
        credentials: 'same-origin',
        headers: {}
    };
    
    if (data !== null) {
        options.headers['Content-Type'] = 'application/json';
        options.body = JSON.stringify(data);
    }
    
    try {
        const response = await fetch(url, options);
        
        if (expectJson) {
            try {
                const json = await response.json();
                return { ok: response.ok, status: response.status, data: json };
            } catch {
                const text = await response.text();
                return { ok: response.ok, status: response.status, data: text };
            }
        } else {
            const text = await response.text();
            return { ok: response.ok, status: response.status, data: text };
        }
    } catch (error) {
        console.error('API Request Error:', error);
        return { ok: false, status: 0, data: error.message };
    }
}

// ===== Navigation =====
function navigateTo(viewName, entryId = null) {
    // Hide all views
    Object.values(elements).forEach(el => {
        if (el && el.id && el.id.startsWith('view-')) {
            el.classList.add('hidden');
        }
    });
    
    // Show requested view
    const viewElement = document.getElementById(`view-${viewName}`);
    if (viewElement) {
        viewElement.classList.remove('hidden');
    }
    
    // Handle auth vs app views
    if (['login', 'register', 'mfa-setup', 'mfa-upgrade'].includes(viewName)) {
        elements.appLayout.classList.add('hidden');
        document.body.style.background = '';
    } else {
        elements.appLayout.classList.remove('hidden');
        document.body.style.background = '#f9fafb';
    }
    
    // Update state
    state.currentView = viewName;
    state.currentEntryId = entryId;
    
    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

// ===== Toast Notifications =====
function showToast(message, type = 'success') {
    const toast = elements.toast;
    const toastMessage = toast.querySelector('.toast-message');
    const toastIcon = toast.querySelector('.toast-icon');
    
    toastMessage.textContent = message;
    
    // Set icon based on type
    toastIcon.textContent = {
        success: '✓',
        error: '✗',
        warning: '!',
        info: 'i'
    }[type] || 'i';
    
    // Set color based on type
    toastIcon.style.color = {
        success: '#10b981',
        error: '#ef4444',
        warning: '#f59e0b',
        info: '#3b82f6'
    }[type] || '#3b82f6';
    
    toast.classList.remove('hidden');
    
    // Auto-hide after 3 seconds
    setTimeout(() => {
        toast.classList.add('hidden');
    }, 3000);
}

// ===== Loading State =====
function setLoading(elementOrSelector, loading = true) {
    let element;
    if (typeof elementOrSelector === 'string') {
        element = document.querySelector(elementOrSelector);
    } else {
        element = elementOrSelector;
    }
    
    if (!element) return;
    
    const spinner = element.querySelector('.loading-spinner');
    const span = element.querySelector('span:not(.loading-spinner)');
    
    if (loading) {
        element.classList.add('loading');
        if (span) span.style.visibility = 'hidden';
        if (spinner) spinner.classList.remove('hidden');
        element.disabled = true;
    } else {
        element.classList.remove('loading');
        if (span) span.style.visibility = '';
        if (spinner) spinner.classList.add('hidden');
        element.disabled = false;
    }
}

// ===== Error Handling =====
function showError(elementId, message) {
    const errorElement = document.getElementById(elementId);
    const errorMessageElement = document.getElementById(`${elementId}-message`);
    
    if (errorElement && errorMessageElement) {
        errorMessageElement.textContent = message;
        errorElement.classList.remove('hidden');
    }
}

function hideError(elementId) {
    const errorElement = document.getElementById(elementId);
    if (errorElement) {
        errorElement.classList.add('hidden');
    }
}

// ===== Session Management =====
async function checkSession() {
    const result = await apiRequest(API.me);
    
    if (result.ok && result.data) {
        state.user = result.data;
        state.session = result.data;
        
        // Check if session needs MFA upgrade
        if (result.data.authenticationLevel === 0) { // Reduced
            // Check if user has TOTP enabled
            if (result.data.totpEnabled) {
                navigateTo('mfa-upgrade');
            } else {
                // TOTP not enabled, go to MFA setup
                navigateTo('mfa-setup');
                await setupMFA();
            }
        } else {
            // Full authentication, go to dashboard
            navigateTo('dashboard');
            loadDashboard();
        }
    } else {
        // No valid session, go to login
        state.user = null;
        state.session = null;
        console.error('Session check failed:', result);
        navigateTo('login');
    }
}

async function logout() {
    await apiRequest(API.logout, 'POST');
    state.user = null;
    state.session = null;
    state.entries = [];
    navigateTo('login');
    showToast('Successfully signed out', 'success');
}

// ===== Password Strength =====
function checkPasswordStrength(password) {
    const requirements = {
        length: password.length >= 12,
        uppercase: /[A-Z]/.test(password),
        lowercase: /[a-z]/.test(password),
        number: /[0-9]/.test(password),
        special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
    };
    
    const score = Object.values(requirements).filter(Boolean).length;
    
    // Update UI
    const bar = document.getElementById('password-strength-bar');
    const reqLength = document.getElementById('req-length');
    const reqUppercase = document.getElementById('req-uppercase');
    const reqLowercase = document.getElementById('req-lowercase');
    const reqNumber = document.getElementById('req-number');
    const reqSpecial = document.getElementById('req-special');
    
    if (bar) {
        bar.className = `password-strength-bar password-strength-${score}`;
    }
    
    if (reqLength) reqLength.classList.toggle('valid', requirements.length);
    if (reqUppercase) reqUppercase.classList.toggle('valid', requirements.uppercase);
    if (reqLowercase) reqLowercase.classList.toggle('valid', requirements.lowercase);
    if (reqNumber) reqNumber.classList.toggle('valid', requirements.number);
    if (reqSpecial) reqSpecial.classList.toggle('valid', requirements.special);
    
    return score >= 3; // At least 3 out of 5 requirements
}

// ===== Authentication =====
async function handleLogin(e) {
    e.preventDefault();
    
    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;
    
    if (!username || !password) {
        showError('login-error', 'Please enter both username and password');
        return;
    }
    
    hideError('login-error');
    setLoading(e.target.querySelector('button'), true);
    
    try {
        const result = await apiRequest(API.login, 'POST', {
            username,
            password
        });
        
        if (result.ok) {
            // Use the user data from the response if available
            if (result.data && result.data.userID) {
                state.user = result.data;
                state.session = result.data;
            }
            // Check session to ensure it's valid
            await checkSession();
            showToast('Welcome back!', 'success');
        } else {
            // Improved error handling - check for specific error messages
            let errorMessage = 'Invalid credentials';
            if (result.data && typeof result.data === 'string') {
                errorMessage = result.data;
            } else if (result.data?.message) {
                errorMessage = result.data.message;
            } else if (result.status === 401) {
                errorMessage = 'Invalid username or password';
            }
            showError('login-error', errorMessage);
            console.error('Login failed:', result);
        }
    } catch (error) {
        showError('login-error', 'Login failed. Please try again.');
    } finally {
        setLoading(e.target.querySelector('button'), false);
    }
}

async function handleRegister(e) {
    e.preventDefault();
    
    const username = document.getElementById('register-username').value;
    const password = document.getElementById('register-password').value;
    const verifyPassword = document.getElementById('register-verify-password').value;
    
    if (!username || !password || !verifyPassword) {
        showError('register-error', 'Please fill in all fields');
        return;
    }
    
    if (password !== verifyPassword) {
        showError('register-error', 'Passwords do not match');
        return;
    }
    
    if (!checkPasswordStrength(password)) {
        showError('register-error', 'Password does not meet strength requirements');
        return;
    }
    
    hideError('register-error');
    setLoading(e.target.querySelector('button'), true);
    
    try {
        const result = await apiRequest(API.register, 'POST', {
            username,
            password,
            verifyPassword
        });
        
        if (result.ok) {
            // Use the user data from the response if available
            if (result.data && result.data.userID) {
                state.user = result.data;
                state.session = result.data;
            }
            // Check session to ensure it's valid
            await checkSession();
            showToast('Account created successfully!', 'success');
        } else {
            showError('register-error', result.data?.message || 'Registration failed');
        }
    } catch (error) {
        showError('register-error', 'Registration failed. Please try again.');
    } finally {
        setLoading(e.target.querySelector('button'), false);
    }
}

// ===== MFA Functions =====
let currentMfaSecret = '';

async function setupMFA() {
    navigateTo('mfa-setup');
    
    // Clear previous secret
    currentMfaSecret = '';
    
    // Call backend to generate a new TOTP secret
    setLoading('#mfa-setup-container', true);
    
    try {
        const result = await apiRequest(API.totpSetup, 'GET');
        
        if (result.ok && result.data) {
            currentMfaSecret = result.data.secret;
            
            // Create QR code URL from the response
            const qrCodeUrl = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(result.data.qrCodeUrl)}`;
            
            const qrCodeElement = document.getElementById('mfa-qr-code');
            if (qrCodeElement) {
                qrCodeElement.innerHTML = `<img src="${qrCodeUrl}" alt="QR Code" style="width: 200px; height: 200px;">`;
            }
            
            const secretElement = document.getElementById('mfa-secret');
            if (secretElement) {
                secretElement.textContent = currentMfaSecret;
            }
        } else {
            showError('mfa-error', result.data?.message || 'Failed to initialize TOTP setup');
            navigateTo('dashboard');
        }
    } catch (error) {
        showError('mfa-error', 'Failed to initialize TOTP setup. Please try again.');
        navigateTo('dashboard');
    } finally {
        setLoading('#mfa-setup-container', false);
    }
}

async function handleMFAVerify(e) {
    e.preventDefault();
    
    const code = document.getElementById('mfa-code').value;
    
    if (!code || code.length !== 6) {
        showError('mfa-error', 'Please enter a valid 6-digit code');
        return;
    }
    
    if (!currentMfaSecret) {
        showError('mfa-error', 'TOTP secret not loaded. Please try again.');
        return;
    }
    
    hideError('mfa-error');
    setLoading(e.target.querySelector('button'), true);
    
    try {
        // Call backend to enable TOTP with the code and secret
        const result = await apiRequest(API.totpEnable, 'POST', {
            code,
            secret: currentMfaSecret
        });
        
        if (result.ok) {
            showToast('2FA enabled successfully!', 'success');
            // Reload session info
            await checkSession();
        } else {
            showError('mfa-error', result.data?.message || 'Invalid code. Please try again.');
        }
    } catch (error) {
        showError('mfa-error', 'Verification failed. Please try again.');
    } finally {
        setLoading(e.target.querySelector('button'), false);
    }
}

async function handleMFAUpgrade(e) {
    e.preventDefault();
    
    const code = document.getElementById('mfa-upgrade-code').value;
    
    if (!code || code.length !== 6) {
        showError('mfa-upgrade-error', 'Please enter a valid 6-digit code');
        return;
    }
    
    hideError('mfa-upgrade-error');
    setLoading(e.target.querySelector('button'), true);
    
    try {
        const result = await apiRequest(API.upgrade, 'POST', {
            code
        });
        
        if (result.ok) {
            showToast('Session upgraded!', 'success');
            await checkSession();
        } else {
            showError('mfa-upgrade-error', 'Invalid code. Please try again.');
        }
    } catch (error) {
        showError('mfa-upgrade-error', 'Upgrade failed. Please try again.');
    } finally {
        setLoading(e.target.querySelector('button'), false);
    }
}

// ===== Vault Functions =====
async function loadVaultEntries() {
    // In a real implementation, we would have an endpoint to list all entries
    // For now, we'll use a placeholder
    
    // Actually, looking at the backend, there's no GET /api/vault endpoint
    // Only GET /api/vault/{id} for individual entries
    // So we need to track entries differently
    
    // For now, we'll just show the empty state or any entries we've created
    renderVaultEntries();
}

function renderVaultEntries(entries = state.entries) {
    const container = document.getElementById('vault-entries-container');
    
    if (!container) return;
    
    if (entries.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-state-icon">!</div>
                <h3 class="empty-state-title">No entries yet</h3>
                <p>Start by adding your first password entry</p>
                <button class="btn btn-primary margin-top-2" data-action="open-create-entry-modal">
                    Add Your First Entry
                </button>
            </div>
        `;
    } else {
        container.innerHTML = `
            <div class="table-responsive">
                <table class="table">
                    <thead>
                        <tr>
                            <th>Title</th>
                            <th>Website</th>
                            <th>Username</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody id="vault-entries-table-body">
                    </tbody>
                </table>
            </div>
        `;
        
        const tbody = document.getElementById('vault-entries-table-body');
        tbody.innerHTML = entries.map(entry => `
            <tr data-entry-id="${entry.id}">
                <td>${escapeHtml(entry.title || 'Untitled')}</td>
                <td>${escapeHtml(entry.website || '')}</td>
                <td>${escapeHtml(entry.externalUsername || '')}</td>
                <td>
                    <div class="table-actions">
                        <button class="btn btn-sm btn-secondary" data-action="view-entry" data-id="${entry.id}">
                            View
                        </button>
                        <button class="btn btn-sm btn-secondary" data-action="edit-entry" data-id="${entry.id}">
                            Edit
                        </button>
                        <button class="btn btn-sm btn-danger" data-action="delete-entry" data-id="${entry.id}">
                            Delete
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }
}

async function createVaultEntry(data) {
    setLoading('#create-entry-submit', true);
    hideError('create-entry-error');
    
    try {
        const result = await apiRequest(API.vault, 'POST', {
            title: data.title,
            website: data.website,
            externalUsername: data.username,
            generatedPassword: data.password
        });
        
        if (result.ok) {
            showToast('Entry created successfully!', 'success');
            // Close modal
            closeModal('modal-create-entry');
            // Reset form
            elements.createEntryForm.reset();
            // Reload entries
            await loadVaultEntries();
        } else {
            showError('create-entry-error', result.data?.message || 'Failed to create entry');
        }
    } catch (error) {
        showError('create-entry-error', 'Failed to create entry. Please try again.');
    } finally {
        setLoading('#create-entry-submit', false);
    }
}

async function viewVaultEntry(id) {
    const result = await apiRequest(API.vaultGet(id));
    
    if (result.ok && result.data) {
        const entry = result.data.message; // The data is wrapped in { message: entry }
        
        // Populate modal
        document.getElementById('view-entry-title').textContent = entry.title || 'Entry';
        document.getElementById('view-entry-title-display').textContent = entry.title || 'Untitled';
        document.getElementById('view-entry-website').textContent = entry.website || 'No website';
        document.getElementById('view-entry-username').textContent = entry.externalUsername || 'No username';
        document.getElementById('view-entry-password').textContent = entry.encryptedPassword || '';
        
        // Show modal
        openModal('modal-view-entry');
        
        // Store entry ID for edit/delete
        state.currentEntryId = id;
    } else {
        showToast('Failed to load entry', 'error');
    }
}

async function editVaultEntry() {
    const id = state.currentEntryId;
    if (!id) return;
    
    const result = await apiRequest(API.vaultGet(id));
    
    if (result.ok && result.data) {
        const entry = result.data.message; // The data is wrapped in { message: entry }
        
        // Populate form
        document.getElementById('edit-entry-id').value = id;
        document.getElementById('edit-entry-title').value = entry.title || '';
        document.getElementById('edit-entry-website').value = entry.website || '';
        document.getElementById('edit-entry-username').value = entry.externalUsername || '';
        document.getElementById('edit-entry-password').value = entry.encryptedPassword || '';
        
        // Show modal
        openModal('modal-edit-entry');
    }
}

async function updateVaultEntry(e) {
    e.preventDefault();
    
    const id = document.getElementById('edit-entry-id').value;
    const title = document.getElementById('edit-entry-title').value;
    const website = document.getElementById('edit-entry-website').value;
    const username = document.getElementById('edit-entry-username').value;
    const password = document.getElementById('edit-entry-password').value;
    
    if (!title) {
        showError('edit-entry-error', 'Title is required');
        return;
    }
    
    setLoading('#edit-entry-submit', true);
    hideError('edit-entry-error');
    
    try {
        const result = await apiRequest(API.vaultPut(id), 'PUT', {
            title,
            website,
            externalUsername: username,
            encryptedPassword: password
        });
        
        if (result.ok) {
            showToast('Entry updated successfully!', 'success');
            closeModal('modal-edit-entry');
            await loadVaultEntries();
        } else {
            showError('edit-entry-error', result.data?.message || 'Failed to update entry');
        }
    } catch (error) {
        showError('edit-entry-error', 'Failed to update entry. Please try again.');
    } finally {
        setLoading('#edit-entry-submit', false);
    }
}

async function deleteVaultEntry(id) {
    if (!id) return;
    
    // Show confirmation modal
    document.getElementById('delete-confirm-title').textContent = 
        state.entries.find(e => e.id === id)?.title || `entry #${id}`;
    state.currentEntryId = id;
    openModal('modal-delete-confirm');
}

async function confirmDelete() {
    const id = state.currentEntryId;
    if (!id) return;
    
    setLoading('#confirm-delete-btn', true);
    
    try {
        const result = await apiRequest(API.vaultDelete(id), 'DELETE');
        
        if (result.ok) {
            showToast('Entry deleted successfully', 'success');
            closeModal('modal-delete-confirm');
            state.currentEntryId = null;
            await loadVaultEntries();
        } else {
            showToast('Failed to delete entry', 'error');
        }
    } catch (error) {
        showToast('Failed to delete entry. Please try again.', 'error');
    } finally {
        setLoading('#confirm-delete-btn', false);
    }
}

// ===== Password Generation =====
async function generatePassword() {
    const length = parseInt(document.getElementById('password-length').value) || 32;
    const strength = document.getElementById('password-strength-select').value;
    const excludeSimilar = document.getElementById('exclude-similar').checked;
    
    setLoading('#generate-password-btn', true);
    
    try {
        const result = await apiRequest(API.generatePassword, 'POST', null, false);
        
        if (result.ok) {
            document.getElementById('generated-password-display').textContent = result.data;
            showToast('Password generated!', 'success');
        } else {
            showToast('Failed to generate password', 'error');
        }
    } catch (error) {
        showToast('Failed to generate password', 'error');
    } finally {
        setLoading('#generate-password-btn', false);
    }
}

// ===== Dashboard =====
async function loadDashboard() {
    // Load user info
    const userInfo = await apiRequest(API.me);
    if (userInfo.ok && userInfo.data) {
        document.getElementById('user-display-name').textContent = 
            userInfo.data.username || 'User';
        
        // Update session info
        updateSessionInfo();
    }
    
    // Load entries count
    // For now, just set to 0 since we don't have a list endpoint
    document.getElementById('dashboard-total-entries').textContent = state.entries.length;
}

function updateSessionInfo() {
    const userInfo = state.user;
    if (!userInfo) return;
    
    // Update auth level badge
    const authLevelElement = document.getElementById('session-auth-level');
    if (authLevelElement) {
        const level = userInfo.authenticationLevel === 1 ? 'Full' : 'Reduced';
        const badgeClass = userInfo.authenticationLevel === 1 ? 'badge-success' : 'badge-warning';
        authLevelElement.innerHTML = `<span class="badge ${badgeClass}">${level}</span>`;
    }
    
    // Update TOTP status
    // This would need to come from the user's profile data
    // For now, we'll just show a placeholder
    const totpStatusElement = document.getElementById('totp-status');
    if (totpStatusElement) {
        // In a real implementation, this would check if user has TOTP enabled
        totpStatusElement.innerHTML = '<span class="badge badge-success">Enabled</span>';
    }
}

// ===== Modals =====
function openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.remove('hidden');
    }
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.add('hidden');
    }
    
    // Clear form errors
    const error = modal.querySelector('.alert-danger');
    if (error) error.classList.add('hidden');
}

function closeAllModals() {
    document.querySelectorAll('.modal-overlay').forEach(modal => {
        modal.classList.add('hidden');
    });
}

// ===== Copy to Clipboard =====
function copyToClipboard(text, successMessage = 'Copied to clipboard!') {
    navigator.clipboard.writeText(text).then(() => {
        showToast(successMessage, 'success');
    }).catch(() => {
        showToast('Failed to copy', 'error');
    });
}

// ===== Utility Functions =====
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ===== Password Visibility Toggle =====
function togglePasswordVisibility(inputId, button) {
    const input = document.getElementById(inputId);
    if (!input) return;
    
    if (input.type === 'password') {
        input.type = 'text';
        button.textContent = 'Hide';
    } else {
        input.type = 'password';
        button.textContent = 'Show';
    }
}

// ===== Event Listeners =====
function initEventListeners() {
    // Authentication
    if (elements.loginForm) {
        elements.loginForm.addEventListener('submit', handleLogin);
    }
    
    if (elements.registerForm) {
        elements.registerForm.addEventListener('submit', handleRegister);
        document.getElementById('register-password').addEventListener('input', (e) => {
            checkPasswordStrength(e.target.value);
        });
    }
    
    if (elements.mfaVerifyForm) {
        elements.mfaVerifyForm.addEventListener('submit', handleMFAVerify);
    }
    
    if (elements.mfaUpgradeForm) {
        elements.mfaUpgradeForm.addEventListener('submit', handleMFAUpgrade);
    }
    
    // Copy secret
    document.getElementById('copy-secret')?.addEventListener('click', () => {
        copyToClipboard(currentMfaSecret, 'Secret copied to clipboard!');
    });
    
    // Generate password
    document.getElementById('generate-password-btn')?.addEventListener('click', generatePassword);
    
    // Copy generated password
    document.getElementById('copy-generated-password')?.addEventListener('click', () => {
        const password = document.getElementById('generated-password-display').textContent;
        copyToClipboard(password);
    });
    
    // Toggle password visibility
    document.getElementById('toggle-password-visibility')?.addEventListener('click', () => {
        togglePasswordVisibility('entry-password', document.getElementById('toggle-password-visibility'));
    });
    
    // Create entry form
    if (elements.createEntryForm) {
        elements.createEntryForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const title = document.getElementById('entry-title').value;
            const website = document.getElementById('entry-website').value;
            const username = document.getElementById('entry-username').value;
            const password = document.getElementById('entry-password').value;
            const generatePassword = document.getElementById('entry-generate-password').checked;
            
            if (!title) {
                showError('create-entry-error', 'Title is required');
                return;
            }
            
            let finalPassword = password;
            if (generatePassword) {
                // Generate a password
                const result = await apiRequest(API.generatePassword, 'POST', null, false);
                if (result.ok) {
                    finalPassword = result.data;
                }
            }
            
            await createVaultEntry({
                title,
                website,
                username,
                password: finalPassword
            });
        });
        
        // Generate password checkbox
        document.getElementById('entry-generate-password')?.addEventListener('change', function() {
            const passwordInput = document.getElementById('entry-password');
            if (this.checked) {
                passwordInput.disabled = true;
                passwordInput.placeholder = 'A password will be generated';
                passwordInput.value = '';
            } else {
                passwordInput.disabled = false;
                passwordInput.placeholder = '••••••••';
            }
        });
    }
    
    // Edit entry form
    if (elements.editEntryForm) {
        elements.editEntryForm.addEventListener('submit', updateVaultEntry);
    }
    
    // Confirm delete
    document.getElementById('confirm-delete-btn')?.addEventListener('click', confirmDelete);
    
    // Close modals on overlay click
    document.querySelectorAll('.modal-overlay').forEach(modal => {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                closeModal(modal.id);
            }
        });
    });
    
    // Close modals on close button click
    document.querySelectorAll('.modal-close').forEach(btn => {
        btn.addEventListener('click', () => {
            closeModal(btn.closest('.modal-overlay').id);
        });
    });
    
    // Navigation actions
    document.querySelectorAll('[data-action]').forEach(el => {
        el.addEventListener('click', async (e) => {
            e.preventDefault();
            const action = el.dataset.action;
            
            switch (action) {
                case 'navigate-to-login':
                    navigateTo('login');
                    break;
                case 'navigate-to-register':
                    navigateTo('register');
                    break;
                case 'navigate-to-dashboard':
                    navigateTo('dashboard');
                    loadDashboard();
                    break;
                case 'navigate-to-vault':
                    navigateTo('vault');
                    loadVaultEntries();
                    break;
                case 'navigate-to-generate':
                    navigateTo('generate');
                    break;
                case 'navigate-to-settings':
                    navigateTo('settings');
                    updateSessionInfo();
                    break;
                case 'navigate-to-profile':
                    navigateTo('profile');
                    break;
                case 'navigate-to-mfa-setup':
                    navigateTo('mfa-setup');
                    setupMFA();
                    break;
                case 'navigate-to-mfa-upgrade':
                    navigateTo('mfa-upgrade');
                    break;
                case 'skip-mfa':
                    // In a real app, this would skip MFA setup
                    // For now, just go to dashboard
                    navigateTo('dashboard');
                    break;
                case 'logout':
                    await logout();
                    break;
                case 'open-create-entry-modal':
                    openModal('modal-create-entry');
                    break;
                case 'close-modal':
                    closeAllModals();
                    break;
                case 'view-entry':
                    const id = el.dataset.id;
                    if (id) await viewVaultEntry(parseInt(id));
                    break;
                case 'edit-entry':
                    const editId = el.dataset.id;
                    if (editId) {
                        state.currentEntryId = parseInt(editId);
                        await editVaultEntry();
                    }
                    break;
                case 'delete-entry':
                    const deleteId = el.dataset.id;
                    if (deleteId) await deleteVaultEntry(parseInt(deleteId));
                    break;
                case 'copy-username':
                    const username = document.getElementById('view-entry-username').textContent;
                    copyToClipboard(username, 'Username copied!');
                    break;
                case 'copy-password':
                    const password = document.getElementById('view-entry-password').textContent;
                    copyToClipboard(password, 'Password copied!');
                    break;
                case 'toggle-password-visibility-modal':
                    const passwordElement = document.getElementById('view-entry-password');
                    const button = e.currentTarget;
                    togglePasswordVisibility('view-entry-password', button);
                    break;
            }
        });
    });
    
    // Enable TOTP button
    document.getElementById('enable-totp-btn')?.addEventListener('click', () => {
        navigateTo('mfa-setup');
        setupMFA();
    });
    
    // Upgrade session button
    document.getElementById('upgrade-session-btn')?.addEventListener('click', () => {
        navigateTo('mfa-upgrade');
    });
}

// ===== Initialization =====
async function init() {
    initEventListeners();
    await checkSession();
}

// Start the app when DOM is ready
document.addEventListener('DOMContentLoaded', init);
