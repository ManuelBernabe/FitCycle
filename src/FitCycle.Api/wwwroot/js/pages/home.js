// FitCycle Home Page — professional landing with quick stats

import { t } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';

export function render() {
  const username = auth.getUsername() || '?';

  return `
    <div class="page no-tabs">
      <div class="home-hero">
        <div class="home-logo">FC</div>
        <div class="home-app-name">FitCycle</div>
        <div class="home-greeting">${t('Welcome', username)}</div>
      </div>
      <div class="page-content" style="margin-top:-20px;position:relative;z-index:1;">
        <div id="home-stats" class="home-stats-grid" style="margin-bottom:16px;">
          <div class="home-stat-card">
            <div class="home-stat-icon">&#128170;</div>
            <div class="home-stat-value" id="home-total-workouts">-</div>
            <div class="home-stat-label">${t('Workouts')}</div>
          </div>
          <div class="home-stat-card">
            <div class="home-stat-icon">&#128293;</div>
            <div class="home-stat-value" id="home-streak">-</div>
            <div class="home-stat-label">${t('Streak')}</div>
          </div>
          <div class="home-stat-card">
            <div class="home-stat-icon">&#128197;</div>
            <div class="home-stat-value" id="home-last-workout">-</div>
            <div class="home-stat-label">${t('LastWorkout')}</div>
          </div>
        </div>

        <button id="home-go-routines" class="btn btn-primary btn-block btn-lg home-main-btn">
          ${t('GoToRoutines')}
        </button>
        <button id="home-go-stats" class="btn btn-outline btn-block home-secondary-btn" style="margin-top:10px;">
          ${t('ViewStats')}
        </button>
      </div>
    </div>
  `;
}

export async function mount() {
  document.getElementById('home-go-routines')?.addEventListener('click', () => {
    location.hash = '#routines';
  });
  document.getElementById('home-go-stats')?.addEventListener('click', () => {
    location.hash = '#stats';
  });

  // Load stats async
  try {
    const [stats, workouts] = await Promise.all([
      api.get('/workouts/stats'),
      api.get('/workouts'),
    ]);

    const totalEl = document.getElementById('home-total-workouts');
    if (totalEl && stats) totalEl.textContent = stats.totalWorkouts || 0;

    // Calculate streak
    const streakEl = document.getElementById('home-streak');
    if (streakEl && workouts && workouts.length > 0) {
      const streak = calculateStreak(workouts);
      streakEl.textContent = streak;
    } else if (streakEl) {
      streakEl.textContent = '0';
    }

    // Last workout date
    const lastEl = document.getElementById('home-last-workout');
    if (lastEl && workouts && workouts.length > 0) {
      const lastDate = new Date(workouts[0].completedAt || workouts[0].CompletedAt);
      const now = new Date();
      const diffDays = Math.floor((now - lastDate) / 86400000);
      if (diffDays === 0) lastEl.textContent = t('Today');
      else if (diffDays === 1) lastEl.textContent = t('Yesterday');
      else lastEl.textContent = `${diffDays}d`;
    } else if (lastEl) {
      lastEl.textContent = '-';
    }
  } catch (e) {
    // Stats are optional
  }
}

export function destroy() {}

function calculateStreak(workouts) {
  if (!workouts || workouts.length === 0) return 0;

  // Get unique workout dates (YYYY-MM-DD)
  const dates = new Set();
  workouts.forEach(w => {
    const d = new Date(w.completedAt || w.CompletedAt);
    dates.add(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`);
  });

  const sortedDates = [...dates].sort().reverse();
  if (sortedDates.length === 0) return 0;

  const today = new Date();
  const todayStr = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;

  // Start counting from today or yesterday
  let streak = 0;
  let checkDate = new Date(today);

  // If no workout today, start from yesterday
  if (!dates.has(todayStr)) {
    checkDate.setDate(checkDate.getDate() - 1);
    const yesterdayStr = `${checkDate.getFullYear()}-${String(checkDate.getMonth() + 1).padStart(2, '0')}-${String(checkDate.getDate()).padStart(2, '0')}`;
    if (!dates.has(yesterdayStr)) return 0;
  }

  // Count consecutive days (skipping weekends as rest days)
  for (let i = 0; i < 365; i++) {
    const dateStr = `${checkDate.getFullYear()}-${String(checkDate.getMonth() + 1).padStart(2, '0')}-${String(checkDate.getDate()).padStart(2, '0')}`;
    const dayOfWeek = checkDate.getDay(); // 0=Sun, 6=Sat

    if (dayOfWeek === 0 || dayOfWeek === 6) {
      // Weekend: skip (don't break streak)
      checkDate.setDate(checkDate.getDate() - 1);
      continue;
    }

    if (dates.has(dateStr)) {
      streak++;
      checkDate.setDate(checkDate.getDate() - 1);
    } else {
      break;
    }
  }

  return streak;
}
