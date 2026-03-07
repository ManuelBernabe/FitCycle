// FitCycle Login Page

import { t, availableLanguages, languageDisplayName, currentLanguage, setLanguage } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';
import { showPrompt } from '../utils.js';

let registeredEmail = null;
let tempToken2FA = null;

export function render() {
  const isRegister = (sessionStorage.getItem('login_mode') === 'register');
  const showActivation = sessionStorage.getItem('login_show_activation') === 'true';
  const show2FA = sessionStorage.getItem('login_show_2fa') === 'true';

  const langOptions = availableLanguages
    .map(l => `<option value="${l}" ${l === currentLanguage() ? 'selected' : ''}>${languageDisplayName(l)}</option>`)
    .join('');

  if (show2FA) {
    return `
      <div class="login-page" style="padding-top:0;">
        <div class="login-lang">
          <select id="login-lang-select">${langOptions}</select>
        </div>
        <div class="login-logo">FC</div>
        <div class="login-app-name">${t('AppName')}</div>
        <div class="login-card" style="text-align:center;">
          <div style="font-size:36px;margin-bottom:8px;">&#128272;</div>
          <h2 class="login-section-title">${t('TwoFAVerification')}</h2>
          <p style="color:#555;font-size:14px;line-height:1.6;margin:8px 0 16px;">${t('EnterCodeFromApp')}</p>
          <div id="login-error" class="login-error hidden"></div>
          <input id="twofa-code" class="form-input" type="text" inputmode="numeric" pattern="[0-9]*" maxlength="10" placeholder="${t('VerificationCode')}" autocomplete="one-time-code" style="text-align:center;font-size:20px;letter-spacing:4px;">
          <button id="twofa-submit" class="btn btn-primary btn-block btn-lg" style="margin-top:12px;">${t('Verify')}</button>
          <div id="login-loading" class="login-loading hidden">
            <div class="spinner"></div>
          </div>
          <button id="twofa-recovery" class="btn btn-outline btn-block" style="margin-top:8px;font-size:13px;">${t('UseRecoveryCode')}</button>
          <button id="twofa-back" class="btn btn-outline btn-block" style="margin-top:4px;font-size:13px;">${t('BackToLogin')}</button>
        </div>
      </div>
    `;
  }

  if (showActivation) {
    return `
      <div class="login-page" style="padding-top:0;">
        <div class="login-lang">
          <select id="login-lang-select">${langOptions}</select>
        </div>
        <div class="login-logo">FC</div>
        <div class="login-app-name">${t('AppName')}</div>
        <div class="login-card" style="text-align:center;">
          <div style="font-size:48px;margin-bottom:12px;">&#9993;</div>
          <h2 class="login-section-title">${t('CheckYourEmail')}</h2>
          <p style="color:#555;font-size:14px;line-height:1.6;margin:12px 0;">${t('ActivationEmailSent')}</p>
          <div id="login-error" class="login-error hidden"></div>
          <button id="back-to-login" class="btn btn-primary btn-block btn-lg" style="margin-top:16px;">${t('BackToLogin')}</button>
          <button id="resend-activation" class="btn btn-outline btn-block" style="margin-top:8px;color:#512BD4;border-color:#512BD4;">${t('ResendActivation')}</button>
        </div>
      </div>
    `;
  }

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
  const backToLogin = document.getElementById('back-to-login');
  const resendBtn = document.getElementById('resend-activation');

  // 2FA verification handlers
  const twofaSubmit = document.getElementById('twofa-submit');
  const twofaCode = document.getElementById('twofa-code');
  const twofaRecovery = document.getElementById('twofa-recovery');
  const twofaBack = document.getElementById('twofa-back');

  if (twofaSubmit) {
    twofaSubmit.addEventListener('click', handle2FASubmit);
    twofaCode?.addEventListener('keydown', e => { if (e.key === 'Enter') handle2FASubmit(); });
    twofaCode?.focus();
    twofaRecovery?.addEventListener('click', () => {
      if (twofaCode) {
        twofaCode.maxLength = 10;
        twofaCode.placeholder = t('RecoveryCodes');
        twofaCode.inputMode = 'text';
        twofaCode.style.letterSpacing = '2px';
        twofaCode.value = '';
        twofaCode.focus();
      }
    });
    twofaBack?.addEventListener('click', () => {
      tempToken2FA = null;
      sessionStorage.removeItem('login_show_2fa');
      window.dispatchEvent(new Event('app-rerender'));
    });
  }

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
    sessionStorage.removeItem('login_show_activation');
    window.dispatchEvent(new Event('app-rerender'));
  });

  langSelect?.addEventListener('change', (e) => {
    setLanguage(e.target.value);
    window.dispatchEvent(new Event('app-rerender'));
  });

  backToLogin?.addEventListener('click', () => {
    sessionStorage.setItem('login_mode', 'login');
    sessionStorage.removeItem('login_show_activation');
    registeredEmail = null;
    window.dispatchEvent(new Event('app-rerender'));
  });

  resendBtn?.addEventListener('click', async () => {
    const errorEl = document.getElementById('login-error');
    const email = registeredEmail || sessionStorage.getItem('login_activation_email');
    if (!email) return;

    resendBtn.disabled = true;
    resendBtn.textContent = t('Loading');
    try {
      await api.post('/auth/resend-activation', { email });
      if (errorEl) {
        errorEl.textContent = t('ActivationResent');
        errorEl.classList.remove('hidden');
        errorEl.style.color = '#28a745';
        errorEl.style.background = '#f0fff0';
      }
    } catch (err) {
      if (errorEl) {
        errorEl.textContent = err.message || t('UnknownError');
        errorEl.classList.remove('hidden');
        errorEl.style.color = '';
        errorEl.style.background = '';
      }
    } finally {
      resendBtn.disabled = false;
      resendBtn.textContent = t('ResendActivation');
    }
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
    if (isRegister) {
      await api.post('/auth/register', { username, email, password });
      // Registration successful — show activation screen
      registeredEmail = email;
      sessionStorage.setItem('login_activation_email', email);
      sessionStorage.setItem('login_show_activation', 'true');
      window.dispatchEvent(new Event('app-rerender'));
    } else {
      const result = await api.post('/auth/login', { username, password });
      if (result.requires2FA) {
        tempToken2FA = result.tempToken;
        sessionStorage.setItem('login_show_2fa', 'true');
        window.dispatchEvent(new Event('app-rerender'));
        return;
      }
      auth.store(result);
      location.hash = '#home';
    }
  } catch (err) {
    const msg = err.message || t('UnknownError');
    errorEl.textContent = msg;
    errorEl.classList.remove('hidden');
    submitBtn.disabled = false;
    submitBtn.textContent = isRegister ? t('Register') : t('SignIn');
    if (loadingEl) loadingEl.classList.add('hidden');

    // If account not activated, show resend option
    if (!isRegister && msg.includes('no está activada')) {
      errorEl.innerHTML = msg + `<br><button id="login-resend-inline" style="margin-top:8px;background:none;border:1px solid #512BD4;color:#512BD4;padding:6px 16px;border-radius:6px;cursor:pointer;font-size:13px;">${t('ResendActivation')}</button>`;
      const inlineResend = document.getElementById('login-resend-inline');
      inlineResend?.addEventListener('click', async () => {
        const resendEmail = await showPrompt(t('Email') + ':');
        if (!resendEmail) return;
        try {
          await api.post('/auth/resend-activation', { email: resendEmail });
          inlineResend.textContent = t('ActivationResent');
          inlineResend.disabled = true;
        } catch (e) {
          inlineResend.textContent = t('UnknownError');
        }
      });
    }
  }
}

async function handle2FASubmit() {
  const errorEl = document.getElementById('login-error');
  const submitBtn = document.getElementById('twofa-submit');
  const loadingEl = document.getElementById('login-loading');
  const code = document.getElementById('twofa-code')?.value?.trim();

  if (!code || !tempToken2FA) return;

  errorEl?.classList.add('hidden');
  submitBtn.disabled = true;
  submitBtn.textContent = t('Loading');
  if (loadingEl) loadingEl.classList.remove('hidden');

  try {
    const result = await api.post('/auth/verify-2fa', { tempToken: tempToken2FA, code });
    auth.store(result);
    tempToken2FA = null;
    sessionStorage.removeItem('login_show_2fa');
    location.hash = '#home';
  } catch (err) {
    errorEl.textContent = err.message || t('InvalidCode');
    errorEl.classList.remove('hidden');
    submitBtn.disabled = false;
    submitBtn.textContent = t('Verify');
    if (loadingEl) loadingEl.classList.add('hidden');
  }
}

export function destroy() {
  tempToken2FA = null;
  sessionStorage.removeItem('login_show_2fa');
}
