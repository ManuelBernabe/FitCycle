// FitCycle Account Page — profile, language, logout, user management (Superuser)

import { t, availableLanguages, languageDisplayName, currentLanguage, setLanguage } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';
import { showAlert, showConfirm } from '../utils.js';

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
        <button id="account-back" class="floating-back-btn">${t('BackToRoutinesBtn')}</button>

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

        <!-- Change Password -->
        <div class="account-section" id="account-password">
          <div class="account-section-title">${t('ChangePassword')}</div>
          <div class="card">
            <div class="form-group">
              <label class="form-label">${t('CurrentPassword')}</label>
              <input id="current-password" class="form-input" type="password">
            </div>
            <div class="form-group">
              <label class="form-label">${t('NewPassword')}</label>
              <input id="new-password-self" class="form-input" type="password">
            </div>
            <button id="password-change-btn" class="btn btn-primary btn-block">${t('ChangePassword')}</button>
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

        <!-- Admin tools (all Superusers) -->
        ${auth.isSuperuser() ? `
        <div class="divider"></div>
        <div class="account-section">
          <div class="account-section-title">${t('AdminPanel')}</div>
          <div style="display:flex;gap:6px;margin-bottom:8px;flex-wrap:wrap;">
            <button id="admin-panel-btn" class="btn btn-outline btn-sm" style="flex:1;min-width:0;font-size:12px;padding:6px 8px;color:#512BD4;border-color:#512BD4;">${t('AdminPanel')}</button>
            <button id="download-db-btn" class="btn btn-outline btn-sm" style="flex:1;min-width:0;font-size:12px;padding:6px 8px;color:#ff8c00;border-color:#ff8c00;">${t('DownloadDb')}</button>
          </div>
        </div>
        ` : ''}

        <!-- User Management (SuperUserMaster only) -->
        ${auth.isSuperUserMaster() ? `
        <div class="divider"></div>
        <div class="account-section" id="user-management">
          <div class="account-section-title">${t('UserManagement')}</div>
          <div style="margin-bottom:8px;">
            <button id="create-user-btn" class="btn btn-outline btn-sm" style="font-size:12px;padding:6px 12px;">${t('CreateUser')}</button>
          </div>
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
    location.hash = '#home';
  });

  // Logout
  document.getElementById('account-logout')?.addEventListener('click', () => {
    auth.clear();
    location.hash = '#login';
  });

  // Edit profile save
  document.getElementById('profile-save')?.addEventListener('click', handleProfileSave);

  // Change password
  document.getElementById('password-change-btn')?.addEventListener('click', handlePasswordChange);

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

  // Superuser: admin panel
  document.getElementById('admin-panel-btn')?.addEventListener('click', () => {
    location.hash = '#admin';
  });

  // Superuser: download database
  document.getElementById('download-db-btn')?.addEventListener('click', async () => {
    const btn = document.getElementById('download-db-btn');
    if (btn) { btn.disabled = true; btn.textContent = t('Downloading'); }
    try {
      await api.downloadBlob('/admin/download-db', 'fitcycle.db');
    } catch (err) {
      await showAlert(t('ErrorFmt', err.message));
    } finally {
      if (btn) { btn.disabled = false; btn.textContent = t('DownloadDb'); }
    }
  });

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

  // Load users if SuperUserMaster
  if (auth.isSuperUserMaster()) {
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
    const result = await api.put('/me/profile', { username, email });

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

// ── Change Password ──

async function handlePasswordChange() {
  const btn = document.getElementById('password-change-btn');
  const statusEl = document.getElementById('account-status');
  const currentPassword = document.getElementById('current-password')?.value;
  const newPassword = document.getElementById('new-password-self')?.value;

  if (!currentPassword || !newPassword) return;

  if (btn) { btn.disabled = true; btn.textContent = t('Updating'); }

  try {
    await api.put('/me/password', { currentPassword, newPassword });

    // Clear inputs
    const curPwEl = document.getElementById('current-password');
    const newPwEl = document.getElementById('new-password-self');
    if (curPwEl) curPwEl.value = '';
    if (newPwEl) newPwEl.value = '';

    if (statusEl) statusEl.textContent = t('PasswordChanged');
    if (btn) { btn.textContent = t('PasswordChanged'); }
    setTimeout(() => {
      if (btn) { btn.textContent = t('ChangePassword'); btn.disabled = false; }
      if (statusEl) statusEl.textContent = '';
    }, 2000);
  } catch (err) {
    if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
    if (btn) { btn.textContent = t('ChangePassword'); btn.disabled = false; }
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

    const uActive = u.isActive ?? u.IsActive ?? true;
    const statusDot = `<span style="display:inline-block;width:8px;height:8px;border-radius:50%;background:${uActive ? '#28a745' : '#ff8c00'};margin-right:4px;vertical-align:middle;" title="${uActive ? t('Active') : t('Inactive')}"></span>`;

    return `
      <div class="user-row">
        <div class="avatar avatar-sm">${initial}</div>
        <div class="user-row-info">
          <div class="user-row-name">${statusDot}${uName} <span class="tag tag-sm">${uRole}</span></div>
          <div class="user-row-email">${uEmail}</div>
        </div>
        <div class="user-row-actions" style="flex-wrap:wrap;gap:2px;">
          <button class="btn btn-sm btn-ghost" style="color:#512BD4;font-size:11px;padding:4px 6px;" data-login-as="${uId}" data-username="${uName}">${t('LoginAs')}</button>
          <button class="btn btn-sm btn-ghost" style="font-size:11px;padding:4px 6px;" data-edit-user="${uId}">${t('Edit')}</button>
          <button class="btn btn-sm btn-ghost" style="color:var(--danger,#dc3545);font-size:11px;padding:4px 6px;" data-delete-user="${uId}" data-username="${uName}">${t('DeleteUserBtn')}</button>
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

  // Delete user
  container.querySelectorAll('[data-delete-user]').forEach(btn => {
    btn.addEventListener('click', async (e) => {
      const userId = parseInt(e.currentTarget.dataset.deleteUser);
      const username = e.currentTarget.dataset.username;
      if (!await showConfirm(t('ConfirmDeleteUser', username))) return;

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

  // Login as user (impersonate)
  container.querySelectorAll('[data-login-as]').forEach(btn => {
    btn.addEventListener('click', async (e) => {
      const userId = parseInt(e.currentTarget.dataset.loginAs);
      const username = e.currentTarget.dataset.username;
      if (!await showConfirm(t('ConfirmLoginAs', username))) return;

      try {
        const result = await api.post(`/auth/impersonate/${userId}`);
        auth.store(result);
        location.hash = '#home';
        location.reload();
      } catch (err) {
        await showAlert(t('ErrorFmt', err.message));
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
          <option value="SuperUserMaster">SuperUserMaster</option>
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
  const uActive = user.isActive ?? user.IsActive ?? true;

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
          <option value="SuperUserMaster" ${uRole === 'SuperUserMaster' ? 'selected' : ''}>SuperUserMaster</option>
        </select>
      </div>
      <div class="form-group" style="margin-top:12px;">
        <label style="display:flex;align-items:center;gap:8px;cursor:pointer;">
          <input id="edit-active" type="checkbox" ${uActive ? 'checked' : ''}>
          <span class="form-label" style="margin:0;">${t('Active')}</span>
        </label>
      </div>
      <div class="divider" style="margin:12px 0;"></div>
      <div class="form-group">
        <label class="form-label">${t('ChangePassword')}</label>
        <input id="edit-password" class="form-input" type="password" placeholder="${t('PasswordMinLength')}">
        <div class="form-hint" style="font-size:11px;margin-top:4px;">${t('LeaveEmptyNoChange')}</div>
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
    const isActive = overlay.querySelector('#edit-active')?.checked;
    const newPw = overlay.querySelector('#edit-password')?.value;
    const statusEl = document.getElementById('account-status');

    if (!username) return;

    try {
      if (statusEl) statusEl.textContent = t('UpdatingUser');
      await api.put(`/users/${uId}`, {
        username: username !== uName ? username : undefined,
        email: email !== uEmail ? email : undefined,
        role: role !== uRole ? role : undefined,
        isActive: isActive !== uActive ? isActive : undefined,
      });

      // Change password if provided
      if (newPw && newPw.length > 0) {
        await api.put(`/users/${uId}/password`, { newPassword: newPw });
      }

      overlay.remove();
      if (statusEl) statusEl.textContent = t('UserUpdated');
      await loadUsers();
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
