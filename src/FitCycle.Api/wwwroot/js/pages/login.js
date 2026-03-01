// FitCycle Login Page

import { t, availableLanguages, languageDisplayName, currentLanguage, setLanguage } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';

export function render() {
  const isRegister = (sessionStorage.getItem('login_mode') === 'register');

  const langOptions = availableLanguages
    .map(l => `<option value="${l}" ${l === currentLanguage() ? 'selected' : ''}>${languageDisplayName(l)}</option>`)
    .join('');

  return `
    <div class="login-page" style="padding-top:0;">
      <div class="login-lang">
        <select id="login-lang-select">${langOptions}</select>
      </div>
      <div class="login-logo">FC</div>
      <div class="login-app-name">${t('AppName')}</div>
      <div class="login-card">
        <h2 class="login-section-title">${isRegister ? t('CreateAccount') : t('SignIn')}</h2>
        <div id="login-error" class="login-error hidden"></div>
        <input id="login-username" class="form-input" type="text" placeholder="${t('Username')}" autocomplete="username">
        ${isRegister ? `<input id="login-email" class="form-input" type="email" placeholder="${t('Email')}" autocomplete="email">` : ''}
        <input id="login-password" class="form-input" type="password" placeholder="${t('Password')}" autocomplete="${isRegister ? 'new-password' : 'current-password'}">
        ${isRegister ? `
        <div id="pwd-requirements" style="font-size:12px;margin:-4px 0 8px;padding:6px 10px;background:#f9f9f9;border-radius:8px;">
          <div id="pwd-len" style="color:#999;">&#9675; ${t('PwdMinLength')}</div>
          <div id="pwd-upper" style="color:#999;">&#9675; ${t('PwdUppercase')}</div>
          <div id="pwd-lower" style="color:#999;">&#9675; ${t('PwdLowercase')}</div>
          <div id="pwd-digit" style="color:#999;">&#9675; ${t('PwdDigit')}</div>
          <div id="pwd-special" style="color:#999;">&#9675; ${t('PwdSpecial')}</div>
        </div>
        ` : ''}
        <button id="login-submit" class="btn btn-primary btn-block btn-lg">${isRegister ? t('Register') : t('SignIn')}</button>
        <div id="login-loading" class="login-loading hidden">
          <div class="spinner"></div>
        </div>
      </div>
      <div class="login-toggle" id="login-toggle">
        ${isRegister ? t('HaveAccountSignIn') : t('NoAccountSignUp')}
      </div>
    </div>
  `;
}

export function mount() {
  const form = document.getElementById('login-submit');
  const toggle = document.getElementById('login-toggle');
  const langSelect = document.getElementById('login-lang-select');

  form?.addEventListener('click', handleSubmit);

  // Allow Enter key to submit
  document.querySelectorAll('.login-card .form-input').forEach(input => {
    input.addEventListener('keydown', e => {
      if (e.key === 'Enter') handleSubmit();
    });
  });

  toggle?.addEventListener('click', () => {
    const isRegister = (sessionStorage.getItem('login_mode') === 'register');
    sessionStorage.setItem('login_mode', isRegister ? 'login' : 'register');
    window.dispatchEvent(new Event('app-rerender'));
  });

  langSelect?.addEventListener('change', (e) => {
    setLanguage(e.target.value);
    window.dispatchEvent(new Event('app-rerender'));
  });

  // Live password strength feedback during registration
  const pwdInput = document.getElementById('login-password');
  if (pwdInput && sessionStorage.getItem('login_mode') === 'register') {
    pwdInput.addEventListener('input', () => {
      const v = pwdInput.value;
      const check = (id, ok) => {
        const el = document.getElementById(id);
        if (el) { el.style.color = ok ? '#28a745' : '#999'; el.innerHTML = (ok ? '&#9679; ' : '&#9675; ') + el.textContent.replace(/^[●○]\s*/, ''); }
      };
      check('pwd-len', v.length >= 8);
      check('pwd-upper', /[A-Z]/.test(v));
      check('pwd-lower', /[a-z]/.test(v));
      check('pwd-digit', /\d/.test(v));
      check('pwd-special', /[^a-zA-Z0-9]/.test(v));
    });
  }
}

async function handleSubmit() {
  const errorEl = document.getElementById('login-error');
  const submitBtn = document.getElementById('login-submit');
  const loadingEl = document.getElementById('login-loading');
  const username = document.getElementById('login-username')?.value?.trim();
  const password = document.getElementById('login-password')?.value;
  const email = document.getElementById('login-email')?.value?.trim();
  const isRegister = (sessionStorage.getItem('login_mode') === 'register');

  if (!username || !password) return;

  if (isRegister) {
    if (password.length < 8 || !/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/\d/.test(password) || !/[^a-zA-Z0-9]/.test(password)) {
      errorEl.textContent = t('PwdMinLength') + ', ' + t('PwdUppercase').toLowerCase() + ', ' + t('PwdLowercase').toLowerCase() + ', ' + t('PwdDigit').toLowerCase() + ', ' + t('PwdSpecial').toLowerCase();
      errorEl.classList.remove('hidden');
      return;
    }
  }

  errorEl?.classList.add('hidden');
  submitBtn.disabled = true;
  submitBtn.textContent = t('Loading');
  if (loadingEl) loadingEl.classList.remove('hidden');

  try {
    let result;
    if (isRegister) {
      result = await api.post('/auth/register', { username, email, password });
    } else {
      result = await api.post('/auth/login', { username, password });
    }
    auth.store(result);
    location.hash = '#routines';
  } catch (err) {
    errorEl.textContent = err.message || t('UnknownError');
    errorEl.classList.remove('hidden');
    submitBtn.disabled = false;
    submitBtn.textContent = isRegister ? t('Register') : t('SignIn');
    if (loadingEl) loadingEl.classList.add('hidden');
  }
}

export function destroy() {}
