// FitCycle Home Page — professional landing with quick stats

import { t } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';
import { calculateStreak, envTag, checkStreakMilestone } from '../utils.js';
import { APP_VERSION } from '../version.js';

export function render() {
  const username = auth.getUsername() || '?';

  return `
    <div class="page no-tabs">
      <div class="home-hero">
        <div class="home-logo">FC</div>
        <div class="home-app-name">FitCycle${envTag()} <span class="home-version">v${APP_VERSION}</span></div>
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
        <button id="home-go-calendar" class="btn btn-outline btn-block home-secondary-btn" style="margin-top:10px;">
          📅 ${t('CalendarTitle')}
        </button>
        <button id="home-go-tutorial" class="btn btn-outline btn-block home-secondary-btn" style="margin-top:10px;color:#512BD4;border-color:#512BD4;">
          📖 ${t('UserGuide')}
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
  document.getElementById('home-go-calendar')?.addEventListener('click', () => {
    location.hash = '#calendar';
  });
  document.getElementById('home-go-tutorial')?.addEventListener('click', () => {
    location.hash = '#tutorial';
  });

  // Load stats async
  try {
    const [stats, workouts] = await Promise.all([
      api.get('/workouts/stats'),
      api.get('/workouts'),
    ]);

    const totalEl = document.getElementById('home-total-workouts');
    if (totalEl && stats) totalEl.textContent = stats.totalWorkouts || 0;

    // Calculate streak (and celebrate milestones)
    const streakEl = document.getElementById('home-streak');
    if (streakEl && workouts && workouts.length > 0) {
      const streak = calculateStreak(workouts);
      streakEl.textContent = streak;
      // Show celebration modal once per milestone crossed
      checkStreakMilestone(streak);
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
