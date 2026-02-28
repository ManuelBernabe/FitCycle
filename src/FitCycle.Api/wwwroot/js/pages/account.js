// FitCycle Account Page — profile, language, logout, user management (Superuser)

import { t, availableLanguages, languageDisplayName, currentLanguage, setLanguage } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';

let allUsers = [];

export function render() {
  const username = auth.getUsername() || '?';
  const email = auth.getEmail() || '';
  const role = auth.getRole() || '';
  const initial = username.charAt(0).toUpperCase();

  const langOptions = availableLanguages
    .map(l => `<option value="${l}" ${l === currentLanguage() ? 'selected' : ''}>${languageDisplayName(l)}</option>`)
    .join('');

  return `
    <div class="page no-tabs">
      <div class="page-content">
        <div class="flex items-center gap-8 mb-16">
          <button id="account-back" class="btn btn-ghost">${t('BackToRoutinesBtn')}</button>
        </div>

        <!-- User info card -->
        <div class="account-header">
          <div class="avatar avatar-lg" style="margin:0 auto 8px">${initial}</div>
          <div class="section-title" id="account-username-display">${username}</div>
          <div class="status-text" id="account-email-display">${email}</div>
          <div class="tag mt-8">${role}</div>
        </div>

        <div class="divider"></div>

        <!-- Edit Profile -->
        <div class="account-section" id="account-profile">
          <div class="account-section-title">${t('EditProfile')}</div>
          <div class="card">
            <div class="form-group">
              <label class="form-label">${t('Username')}</label>
              <input id="profile-username" class="form-input" type="text" value="${username}">
            </div>
            <div class="form-group">
              <label class="form-label">${t('Email')}</label>
              <input id="profile-email" class="form-input" type="email" value="${email}">
            </div>
            <button id="profile-save" class="btn btn-primary btn-block">${t('Save')}</button>
          </div>
        </div>

        <div class="divider"></div>

        <!-- Language selector -->
        <div class="account-section">
          <div class="flex items-center gap-8">
            <label style="font-size:15px;font-weight:bold;">${t('Language')}</label>
            <select id="account-lang-select" class="form-input" style="width:150px;font-size:14px;">
              ${langOptions}
            </select>
          </div>
        </div>

        <div class="divider"></div>

        <!-- Logout -->
        <div class="account-section">
          <button id="account-logout" class="btn btn-danger btn-block">${t('Logout')}</button>
        </div>

        <!-- Admin section (Superuser only) -->
        ${auth.isSuperuser() ? `
        <div class="divider"></div>
        <div class="account-section" id="user-management">
          <div class="account-section-title">${t('UserManagement')}</div>
          <button id="create-user-btn" class="btn btn-outline btn-block mb-8">${t('CreateUser')}</button>
          <div id="users-list">
            <div class="loading-page"><div class="spinner"></div></div>
          </div>
        </div>
        ` : ''}

        <div id="account-status" class="status-text mt-8"></div>
      </div>
    </div>
  `;
}

export async function mount() {
  // Back button
  document.getElementById('account-back')?.addEventListener('click', () => {
    location.hash = '#routines';
  });

  // Logout
  document.getElementById('account-logout')?.addEventListener('click', () => {
    auth.clear();
    location.hash = '#login';
  });

  // Edit profile save
  document.getElementById('profile-save')?.addEventListener('click', handleProfileSave);

  // Language change
  document.getElementById('account-lang-select')?.addEventListener('change', (e) => {
    const newLang = e.target.value;
    if (newLang === currentLanguage()) return;
    setLanguage(newLang);
    // Rerender the whole app to apply new language
    window.dispatchEvent(new Event('app-rerender'));
  });

  // Superuser: create user
  document.getElementById('create-user-btn')?.addEventListener('click', showCreateUserModal);

  // Load current user info from API
  try {
    const me = await api.get('/auth/me');
    if (me) {
      const usernameDisplay = document.getElementById('account-username-display');
      const emailDisplay = document.getElementById('account-email-display');
      const profileUsername = document.getElementById('profile-username');
      const profileEmail = document.getElementById('profile-email');

      if (usernameDisplay) usernameDisplay.textContent = me.username || me.Username || '';
      if (emailDisplay) emailDisplay.textContent = me.email || me.Email || '';
      if (profileUsername) profileUsername.value = me.username || me.Username || '';
      if (profileEmail) profileEmail.value = me.email || me.Email || '';
    }
  } catch (e) {
    /* Use cached data from auth */
  }

  // Load users if superuser
  if (auth.isSuperuser()) {
    await loadUsers();
  }
}

export function destroy() {}

// ── Edit Profile ──

async function handleProfileSave() {
  const btn = document.getElementById('profile-save');
  const statusEl = document.getElementById('account-status');
  const username = document.getElementById('profile-username')?.value?.trim();
  const email = document.getElementById('profile-email')?.value?.trim();

  if (!username) return;

  if (btn) { btn.disabled = true; btn.textContent = t('Updating'); }

  try {
    const userId = auth.getUserId();
    const result = await api.put(`/users/${userId}`, { username, email, role: auth.getRole() });

    // Update local storage
    localStorage.setItem('auth_username', username);
    localStorage.setItem('auth_email', email || '');

    // Update display
    const usernameDisplay = document.getElementById('account-username-display');
    if (usernameDisplay) usernameDisplay.textContent = username;
    const emailDisplay = document.getElementById('account-email-display');
    if (emailDisplay) emailDisplay.textContent = email || '';

    if (statusEl) statusEl.textContent = t('ProfileUpdated');
    if (btn) { btn.textContent = t('ProfileUpdated'); }
    setTimeout(() => {
      if (btn) { btn.textContent = t('Save'); btn.disabled = false; }
      if (statusEl) statusEl.textContent = '';
    }, 2000);
  } catch (err) {
    if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
    if (btn) { btn.textContent = t('Save'); btn.disabled = false; }
  }
}

// ── User Management ──

async function loadUsers() {
  const container = document.getElementById('users-list');
  if (!container) return;

  try {
    allUsers = await api.get('/users');
    renderUsers(container);
  } catch (err) {
    container.innerHTML = `<div class="status-text">${t('ErrorFmt', err.message)}</div>`;
  }
}

function renderUsers(container) {
  if (!allUsers || allUsers.length === 0) {
    container.innerHTML = '';
    return;
  }

  container.innerHTML = allUsers.map(u => {
    const uName = u.username || u.Username || '?';
    const uEmail = u.email || u.Email || '';
    const uRole = u.role || u.Role || '';
    const uId = u.id || u.Id;
    const initial = uName.charAt(0).toUpperCase();

    return `
      <div class="user-row">
        <div class="avatar avatar-sm">${initial}</div>
        <div class="user-row-info">
          <div class="user-row-name">${uName} <span class="tag tag-sm">${uRole}</span></div>
          <div class="user-row-email">${uEmail}</div>
        </div>
        <div class="user-row-actions">
          <button class="btn btn-sm btn-ghost" data-edit-user="${uId}">${t('Edit')}</button>
          <button class="btn btn-sm btn-ghost" style="color:#ff8c00;" data-change-pw="${uId}" data-username="${uName}">${t('PasswordKey')}</button>
          <button class="btn btn-sm btn-ghost" style="color:var(--danger,#dc3545);" data-delete-user="${uId}" data-username="${uName}">${t('DeleteUserBtn')}</button>
        </div>
      </div>
    `;
  }).join('');

  // Edit user
  container.querySelectorAll('[data-edit-user]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      const userId = parseInt(e.currentTarget.dataset.editUser);
      const user = allUsers.find(u => (u.id || u.Id) === userId);
      if (user) showEditUserModal(user);
    });
  });

  // Change password
  container.querySelectorAll('[data-change-pw]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      const userId = parseInt(e.currentTarget.dataset.changePw);
      const username = e.currentTarget.dataset.username;
      showChangePasswordModal(userId, username);
    });
  });

  // Delete user
  container.querySelectorAll('[data-delete-user]').forEach(btn => {
    btn.addEventListener('click', async (e) => {
      const userId = parseInt(e.currentTarget.dataset.deleteUser);
      const username = e.currentTarget.dataset.username;
      if (!confirm(t('ConfirmDeleteUser', username))) return;

      const statusEl = document.getElementById('account-status');
      try {
        if (statusEl) statusEl.textContent = t('DeletingUser');
        await api.del(`/users/${userId}`);
        if (statusEl) statusEl.textContent = t('UserDeleted');
        await loadUsers();
      } catch (err) {
        if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
      }
    });
  });
}

// ── Create User Modal ──

function showCreateUserModal() {
  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';
  overlay.innerHTML = `
    <div class="modal-content">
      <div class="modal-header">
        <div class="modal-title">${t('CreateUserTitle')}</div>
        <button class="modal-close" id="modal-close">&times;</button>
      </div>
      <div class="form-group">
        <label class="form-label">${t('Username')}</label>
        <input id="new-username" class="form-input" type="text">
      </div>
      <div class="form-group">
        <label class="form-label">${t('Email')}</label>
        <input id="new-email" class="form-input" type="email">
      </div>
      <div class="form-group">
        <label class="form-label">${t('Password')}</label>
        <input id="new-password" class="form-input" type="password">
      </div>
      <div class="form-group">
        <label class="form-label">${t('SelectRole')}</label>
        <select id="new-role" class="form-input">
          <option value="Standard">Standard</option>
          <option value="Admin">Admin</option>
          <option value="Superuser">Superuser</option>
        </select>
      </div>
      <button id="create-user-submit" class="btn btn-primary btn-block">${t('Create')}</button>
    </div>
  `;

  document.body.appendChild(overlay);
  bindModalClose(overlay);

  overlay.querySelector('#create-user-submit')?.addEventListener('click', async () => {
    const username = overlay.querySelector('#new-username')?.value?.trim();
    const email = overlay.querySelector('#new-email')?.value?.trim();
    const password = overlay.querySelector('#new-password')?.value;
    const role = overlay.querySelector('#new-role')?.value;
    const statusEl = document.getElementById('account-status');

    if (!username || !password) return;

    try {
      if (statusEl) statusEl.textContent = t('CreatingUser');
      await api.post('/users', { username, email, password, role });
      overlay.remove();
      if (statusEl) statusEl.textContent = t('UserCreated');
      await loadUsers();
    } catch (err) {
      if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
    }
  });
}

// ── Edit User Modal ──

function showEditUserModal(user) {
  const uName = user.username || user.Username || '';
  const uEmail = user.email || user.Email || '';
  const uRole = user.role || user.Role || '';
  const uId = user.id || user.Id;

  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';
  overlay.innerHTML = `
    <div class="modal-content">
      <div class="modal-header">
        <div class="modal-title">${t('EditUserTitle')}</div>
        <button class="modal-close" id="modal-close">&times;</button>
      </div>
      <div class="form-group">
        <label class="form-label">${t('Username')}</label>
        <input id="edit-username" class="form-input" type="text" value="${uName}">
      </div>
      <div class="form-group">
        <label class="form-label">${t('Email')}</label>
        <input id="edit-email" class="form-input" type="email" value="${uEmail}">
      </div>
      <div class="form-group">
        <label class="form-label">${t('SelectRole')}</label>
        <select id="edit-role" class="form-input">
          <option value="Standard" ${uRole === 'Standard' ? 'selected' : ''}>Standard</option>
          <option value="Admin" ${uRole === 'Admin' ? 'selected' : ''}>Admin</option>
          <option value="Superuser" ${uRole === 'Superuser' ? 'selected' : ''}>Superuser</option>
        </select>
      </div>
      <button id="edit-user-submit" class="btn btn-primary btn-block">${t('Save')}</button>
    </div>
  `;

  document.body.appendChild(overlay);
  bindModalClose(overlay);

  overlay.querySelector('#edit-user-submit')?.addEventListener('click', async () => {
    const username = overlay.querySelector('#edit-username')?.value?.trim();
    const email = overlay.querySelector('#edit-email')?.value?.trim();
    const role = overlay.querySelector('#edit-role')?.value;
    const statusEl = document.getElementById('account-status');

    if (!username) return;

    try {
      if (statusEl) statusEl.textContent = t('UpdatingUser');
      await api.put(`/users/${uId}`, {
        username: username !== uName ? username : undefined,
        email: email !== uEmail ? email : undefined,
        role: role !== uRole ? role : undefined,
      });
      overlay.remove();
      if (statusEl) statusEl.textContent = t('UserUpdated');
      await loadUsers();
    } catch (err) {
      if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
    }
  });
}

// ── Change Password Modal ──

function showChangePasswordModal(userId, username) {
  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';
  overlay.innerHTML = `
    <div class="modal-content">
      <div class="modal-header">
        <div class="modal-title">${t('ChangePassword')}</div>
        <button class="modal-close" id="modal-close">&times;</button>
      </div>
      <div class="form-group">
        <label class="form-label">${t('NewPasswordFor', username)}</label>
        <input id="new-pw" class="form-input" type="password">
      </div>
      <div class="form-hint">${t('PasswordMinLength')}</div>
      <button id="change-pw-submit" class="btn btn-primary btn-block mt-8">${t('Save')}</button>
    </div>
  `;

  document.body.appendChild(overlay);
  bindModalClose(overlay);

  overlay.querySelector('#change-pw-submit')?.addEventListener('click', async () => {
    const pw = overlay.querySelector('#new-pw')?.value;
    const statusEl = document.getElementById('account-status');

    if (!pw || pw.length < 6) {
      if (statusEl) statusEl.textContent = t('PasswordMinLength');
      return;
    }

    try {
      if (statusEl) statusEl.textContent = t('ChangingPassword');
      await api.put(`/users/${userId}/password`, { newPassword: pw });
      overlay.remove();
      if (statusEl) statusEl.textContent = t('PasswordUpdated');
    } catch (err) {
      if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
    }
  });
}

// ── Modal Helper ──

function bindModalClose(overlay) {
  overlay.querySelector('#modal-close')?.addEventListener('click', () => overlay.remove());
  overlay.addEventListener('click', (e) => {
    if (e.target === overlay) overlay.remove();
  });
}
