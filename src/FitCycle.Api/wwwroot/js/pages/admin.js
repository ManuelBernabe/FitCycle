// FitCycle Admin Panel — SQL Console + Backup Management (Superuser only)

import { t } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';
import { escapeHtml, showAlert, showConfirm } from '../utils.js';

const QUICK_QUERIES = [
  { label: 'Usuarios', sql: 'SELECT Id, Username, Email, Role, CreatedAt FROM Users ORDER BY Id' },
  { label: 'Ejercicios', sql: 'SELECT e.Id, e.Name, mg.Name AS MuscleGroup FROM Exercises e JOIN MuscleGroups mg ON e.MuscleGroupId = mg.Id ORDER BY mg.Name, e.Name' },
  { label: 'Rutinas', sql: 'SELECT de.Id, u.Username, de.Day, e.Name AS Exercise, de.Sets, de.Reps, de.Weight FROM DayExercises de JOIN Users u ON de.UserId = u.Id JOIN Exercises e ON de.ExerciseId = e.Id ORDER BY u.Username, de.Day' },
  { label: 'Sesiones', sql: 'SELECT ws.Id, u.Username, ws.Day, ws.StartedAt, ws.CompletedAt FROM WorkoutSessions ws JOIN Users u ON ws.UserId = u.Id ORDER BY ws.CompletedAt DESC LIMIT 50' },
  { label: 'Tablas', sql: "SELECT name, type FROM sqlite_master WHERE type IN ('table','index') ORDER BY type, name" },
];

export function render() {
  const quickBtns = QUICK_QUERIES.map((q, i) =>
    `<button class="btn btn-sm btn-outline quick-query-btn" data-qi="${i}">${q.label}</button>`
  ).join('');

  return `
    <div class="page no-tabs">
      <div class="page-content">
        <button id="admin-back" class="floating-back-btn">${t('Back')}</button>

        <!-- SQL Console -->
        <div class="account-section">
          <div class="account-section-title">${t('SqlConsole')}</div>
          <div class="card">
            <textarea id="sql-input" class="form-input sql-textarea" placeholder="SELECT * FROM Users..." rows="4"></textarea>
            <div style="display:flex;gap:6px;margin-top:8px;flex-wrap:wrap;">
              <button id="sql-execute" class="btn btn-primary" style="flex:1;min-width:120px;">${t('ExecuteQuery')}</button>
              ${quickBtns}
            </div>
          </div>
          <div id="sql-status" class="status-text mt-8"></div>
          <div id="sql-results"></div>
        </div>

        <div class="divider"></div>

        <!-- Backups -->
        <div class="account-section">
          <div class="account-section-title">${t('Backups')}</div>
          <div style="display:flex;gap:8px;margin-bottom:8px;">
            <button id="create-backup-btn" class="btn btn-outline" style="flex:1;color:#28a745;border-color:#28a745;">${t('CreateBackup')}</button>
          </div>
          <div id="backups-list">
            <div class="loading-page"><div class="spinner"></div></div>
          </div>
        </div>

        <div id="admin-status" class="status-text mt-8"></div>
      </div>
    </div>
  `;
}

export async function mount() {
  if (!auth.isSuperuser()) {
    location.hash = '#routines';
    return;
  }

  document.getElementById('admin-back')?.addEventListener('click', () => {
    location.hash = '#account';
  });

  document.getElementById('sql-execute')?.addEventListener('click', executeQuery);

  // Ctrl+Enter to execute
  document.getElementById('sql-input')?.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault();
      executeQuery();
    }
  });

  // Quick query buttons
  document.querySelectorAll('.quick-query-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const idx = parseInt(btn.dataset.qi);
      const q = QUICK_QUERIES[idx];
      if (q) {
        document.getElementById('sql-input').value = q.sql;
        executeQuery();
      }
    });
  });

  document.getElementById('create-backup-btn')?.addEventListener('click', createBackup);

  await loadBackups();
}

export function destroy() {}

// ── SQL Console ──

async function executeQuery() {
  const input = document.getElementById('sql-input');
  const statusEl = document.getElementById('sql-status');
  const resultsEl = document.getElementById('sql-results');
  const query = input?.value?.trim();

  if (!query) return;

  const btn = document.getElementById('sql-execute');
  if (btn) { btn.disabled = true; btn.textContent = t('Loading'); }
  if (statusEl) statusEl.textContent = '';
  if (resultsEl) resultsEl.innerHTML = '';

  try {
    const result = await api.post('/admin/query', { query });
    displayResults(result, resultsEl, statusEl);
  } catch (err) {
    if (statusEl) statusEl.textContent = err.message || 'Error';
    if (statusEl) statusEl.style.color = 'var(--danger)';
  } finally {
    if (btn) { btn.disabled = false; btn.textContent = t('ExecuteQuery'); }
  }
}

function displayResults(result, container, statusEl) {
  if (!result || !result.columns || result.columns.length === 0) {
    if (statusEl) { statusEl.textContent = t('NoResults'); statusEl.style.color = ''; }
    return;
  }

  const { columns, rows, rowCount, truncated } = result;
  let info = `${rowCount} ${t('Rows')}`;
  if (truncated) info += ` (${t('Truncated')})`;
  if (statusEl) { statusEl.textContent = info; statusEl.style.color = ''; }

  const headerCells = columns.map(c => `<th>${escapeHtml(c)}</th>`).join('');
  const bodyRows = rows.map(row =>
    `<tr>${row.map(cell => `<td>${cell === null ? '<span class="text-muted">NULL</span>' : escapeHtml(String(cell))}</td>`).join('')}</tr>`
  ).join('');

  container.innerHTML = `
    <div class="sql-results-wrapper mt-8">
      <table class="sql-table">
        <thead><tr>${headerCells}</tr></thead>
        <tbody>${bodyRows}</tbody>
      </table>
    </div>
  `;
}

// escapeHtml moved to utils.js

// ── Backups ──

async function loadBackups() {
  const container = document.getElementById('backups-list');
  if (!container) return;

  try {
    const result = await api.get('/admin/backups');
    const backups = result.backups || [];

    if (backups.length === 0) {
      container.innerHTML = `<div class="empty-state"><div class="empty-state-text">${t('NoBackups')}</div></div>`;
      return;
    }

    container.innerHTML = backups.map(b => {
      const sizeMB = (b.size / 1024 / 1024).toFixed(2);
      const date = new Date(b.date).toLocaleString();
      return `
        <div class="backup-row">
          <div class="backup-info">
            <div class="backup-name">${escapeHtml(b.name)}</div>
            <div class="backup-meta">${date} — ${sizeMB} MB</div>
          </div>
          <div class="backup-actions">
            <button class="btn btn-sm btn-ghost" style="color:#512BD4;" data-download-backup="${escapeHtml(b.name)}">${t('Download')}</button>
            <button class="btn btn-sm btn-ghost" style="color:#ff8c00;" data-restore-backup="${escapeHtml(b.name)}">${t('RestoreBackup')}</button>
          </div>
        </div>
      `;
    }).join('');

    // Download backup
    container.querySelectorAll('[data-download-backup]').forEach(btn => {
      btn.addEventListener('click', async () => {
        const name = btn.dataset.downloadBackup;
        try {
          await api.downloadBlob(`/admin/backup/download/${name}`, name);
        } catch (err) {
          await showAlert(t('ErrorFmt', err.message));
        }
      });
    });

    // Restore backup
    container.querySelectorAll('[data-restore-backup]').forEach(btn => {
      btn.addEventListener('click', async () => {
        const name = btn.dataset.restoreBackup;
        if (!await showConfirm(t('ConfirmRestore', name))) return;
        if (!await showConfirm(t('ConfirmRestoreDouble'))) return;

        const statusEl = document.getElementById('admin-status');
        try {
          if (statusEl) statusEl.textContent = t('Restoring');
          const result = await api.post(`/admin/restore/${name}`);
          if (statusEl) statusEl.textContent = result.message || t('BackupRestored');
        } catch (err) {
          if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
        }
      });
    });
  } catch (err) {
    container.innerHTML = `<div class="status-text">${t('ErrorFmt', err.message)}</div>`;
  }
}

async function createBackup() {
  const btn = document.getElementById('create-backup-btn');
  const statusEl = document.getElementById('admin-status');

  if (btn) { btn.disabled = true; btn.textContent = t('CreatingBackup'); }
  try {
    const result = await api.post('/admin/backup');
    if (statusEl) statusEl.textContent = result.message || t('BackupCreated');
    await loadBackups();
  } catch (err) {
    if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
  } finally {
    if (btn) { btn.disabled = false; btn.textContent = t('CreateBackup'); }
  }
}
