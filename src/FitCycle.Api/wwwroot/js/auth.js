// FitCycle Auth helper — manages tokens and user info in localStorage

const AUTH_KEYS = {
  accessToken:  'auth_access_token',
  refreshToken: 'auth_refresh_token',
  username:     'auth_username',
  email:        'auth_email',
  role:         'auth_role',
  userId:       'auth_user_id',
};

const auth = {
  isAuthenticated() {
    return !!localStorage.getItem(AUTH_KEYS.accessToken);
  },

  getAccessToken() {
    return localStorage.getItem(AUTH_KEYS.accessToken);
  },

  getRefreshToken() {
    return localStorage.getItem(AUTH_KEYS.refreshToken);
  },

  getUsername() {
    return localStorage.getItem(AUTH_KEYS.username);
  },

  getEmail() {
    return localStorage.getItem(AUTH_KEYS.email);
  },

  getRole() {
    return localStorage.getItem(AUTH_KEYS.role);
  },

  getUserId() {
    return localStorage.getItem(AUTH_KEYS.userId);
  },

  /**
   * Store token response from /auth/login or /auth/register.
   * API shape: { accessToken, refreshToken, user: { id, username, email, role } }
   */
  store(tokenResponse) {
    if (tokenResponse.accessToken)  localStorage.setItem(AUTH_KEYS.accessToken,  tokenResponse.accessToken);
    if (tokenResponse.refreshToken) localStorage.setItem(AUTH_KEYS.refreshToken, tokenResponse.refreshToken);
    const u = tokenResponse.user;
    if (u) {
      if (u.username) localStorage.setItem(AUTH_KEYS.username, u.username);
      if (u.email)    localStorage.setItem(AUTH_KEYS.email,    u.email);
      if (u.role)     localStorage.setItem(AUTH_KEYS.role,     u.role);
      if (u.id != null) localStorage.setItem(AUTH_KEYS.userId, String(u.id));
    }
  },

  /**
   * Clear all auth data (logout).
   */
  clear() {
    Object.values(AUTH_KEYS).forEach(k => localStorage.removeItem(k));
  },

  /**
   * Check if the current user has a specific role.
   */
  hasRole(...roles) {
    const r = this.getRole();
    return r && roles.includes(r);
  },

  isSuperuser() {
    return this.hasRole('Superuser');
  },

  isAdmin() {
    return this.hasRole('Admin', 'Superuser');
  }
};

export { auth };
