// FitCycle Tutorial / User Guide Page

import { t } from '../l10n.js';

export function render() {
  return `
    <div class="page no-tabs">
      <div class="page-content" style="padding-top:16px;padding-bottom:32px;">

        <div style="text-align:center;margin-bottom:24px;">
          <div style="font-size:48px;">📖</div>
          <h1 style="font-size:22px;font-weight:700;color:#333;margin:8px 0 4px;">${t('TutorialTitle')}</h1>
          <p style="color:#666;font-size:14px;margin:0;">${t('TutorialSubtitle')}</p>
        </div>

        <!-- 1. Bienvenida -->
        ${section('1', '💪', t('TutWelcomeTitle'), t('TutWelcomeDesc'), `
          <div style="background:#512BD4;border-radius:12px;padding:20px;text-align:center;color:#fff;">
            <div style="font-size:32px;font-weight:bold;letter-spacing:2px;">FC</div>
            <div style="font-size:16px;margin-top:4px;">FitCycle</div>
            <div style="margin-top:12px;font-size:13px;opacity:0.8;">${t('TutWelcomeMockup')}</div>
          </div>
        `)}

        <!-- 2. Registro -->
        ${section('2', '📧', t('TutRegisterTitle'), t('TutRegisterDesc'), `
          <div style="background:#fff;border-radius:10px;padding:16px;border:1px solid #eee;">
            <div style="background:#f5f5f5;border-radius:8px;padding:10px 12px;margin-bottom:8px;color:#999;font-size:13px;">${t('Username')}</div>
            <div style="background:#f5f5f5;border-radius:8px;padding:10px 12px;margin-bottom:8px;color:#999;font-size:13px;">${t('Email')}</div>
            <div style="background:#f5f5f5;border-radius:8px;padding:10px 12px;margin-bottom:12px;color:#999;font-size:13px;">********</div>
            <div style="background:#512BD4;color:#fff;border-radius:8px;padding:10px;text-align:center;font-weight:600;font-size:14px;">${t('Register')}</div>
          </div>
          <div style="margin-top:8px;padding:8px 12px;background:#e8f5e9;border-radius:8px;font-size:12px;color:#2e7d32;">
            💡 ${t('TutRegisterTip')}
          </div>
        `)}

        <!-- 3. Home -->
        ${section('3', '🏠', t('TutHomeTitle'), t('TutHomeDesc'), `
          <div style="background:#fff;border-radius:10px;padding:16px;border:1px solid #eee;">
            <div style="display:flex;gap:8px;margin-bottom:12px;">
              <div style="flex:1;background:#f3f0fc;border-radius:8px;padding:10px;text-align:center;">
                <div style="font-size:20px;">💪</div>
                <div style="font-weight:700;font-size:16px;color:#512BD4;">12</div>
                <div style="font-size:11px;color:#888;">${t('Workouts')}</div>
              </div>
              <div style="flex:1;background:#f3f0fc;border-radius:8px;padding:10px;text-align:center;">
                <div style="font-size:20px;">🔥</div>
                <div style="font-weight:700;font-size:16px;color:#512BD4;">5</div>
                <div style="font-size:11px;color:#888;">${t('Streak')}</div>
              </div>
              <div style="flex:1;background:#f3f0fc;border-radius:8px;padding:10px;text-align:center;">
                <div style="font-size:20px;">📅</div>
                <div style="font-weight:700;font-size:16px;color:#512BD4;">${t('Today')}</div>
                <div style="font-size:11px;color:#888;">${t('LastWorkout')}</div>
              </div>
            </div>
            <div style="background:#512BD4;color:#fff;border-radius:8px;padding:10px;text-align:center;font-weight:600;font-size:14px;">${t('GoToRoutines')}</div>
          </div>
        `)}

        <!-- 4. Rutina semanal -->
        ${section('4', '📅', t('TutRoutinesTitle'), t('TutRoutinesDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            ${mockDayCard(t('Monday'), ['🏋️ ' + t('TutChest'), '💪 ' + t('TutTriceps')])}
            ${mockDayCard(t('Tuesday'), ['🏋️ ' + t('TutBack'), '💪 ' + t('TutBiceps')])}
            ${mockDayCard(t('Wednesday'), ['🦵 ' + t('TutLegs'), '🍑 ' + t('TutGlutes')])}
          </div>
          <div style="margin-top:8px;padding:8px 12px;background:#e8f5e9;border-radius:8px;font-size:12px;color:#2e7d32;">
            💡 ${t('TutRoutinesTip')}
          </div>
        `)}

        <!-- 5. Configurar ejercicios -->
        ${section('5', '⚙️', t('TutExercisesTitle'), t('TutExercisesDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="font-weight:600;font-size:14px;color:#333;margin-bottom:8px;">Press banca</div>
            <div style="display:flex;gap:6px;margin-bottom:6px;">
              <div style="flex:1;background:#f5f5f5;border-radius:6px;padding:6px 8px;text-align:center;font-size:12px;">
                <div style="color:#888;font-size:10px;">Serie 1</div>
                <div style="color:#333;font-weight:600;">12 × 40kg</div>
              </div>
              <div style="flex:1;background:#f5f5f5;border-radius:6px;padding:6px 8px;text-align:center;font-size:12px;">
                <div style="color:#888;font-size:10px;">Serie 2</div>
                <div style="color:#333;font-weight:600;">10 × 50kg</div>
              </div>
              <div style="flex:1;background:#f5f5f5;border-radius:6px;padding:6px 8px;text-align:center;font-size:12px;">
                <div style="color:#888;font-size:10px;">Serie 3</div>
                <div style="color:#333;font-weight:600;">8 × 60kg</div>
              </div>
            </div>
            <div style="display:flex;gap:6px;flex-wrap:wrap;">
              <span style="background:#f3f0fc;color:#512BD4;padding:3px 8px;border-radius:12px;font-size:11px;">⏱ Tempo: 3-1-2</span>
              <span style="background:#fff3e0;color:#e65100;padding:3px 8px;border-radius:12px;font-size:11px;">🔗 Superset</span>
              <span style="background:#e8f5e9;color:#2e7d32;padding:3px 8px;border-radius:12px;font-size:11px;">📝 ${t('Notes')}</span>
            </div>
          </div>
          <div style="margin-top:8px;padding:8px 12px;background:#e8f5e9;border-radius:8px;font-size:12px;color:#2e7d32;">
            💡 ${t('TutExercisesTip')}
          </div>
        `)}

        <!-- 6. Entrenar -->
        ${section('6', '🏋️', t('TutWorkoutTitle'), t('TutWorkoutDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="text-align:center;margin-bottom:12px;">
              <div style="font-size:13px;color:#888;">${t('TutCurrentExercise')}</div>
              <div style="font-size:18px;font-weight:700;color:#333;">Press banca</div>
              <div style="font-size:13px;color:#512BD4;">Serie 2 / 3</div>
            </div>
            <div style="display:flex;gap:8px;margin-bottom:12px;">
              <div style="flex:1;background:#f5f5f5;border-radius:8px;padding:10px;text-align:center;">
                <div style="font-size:11px;color:#888;">Reps</div>
                <div style="font-size:20px;font-weight:700;color:#333;">10</div>
              </div>
              <div style="flex:1;background:#f5f5f5;border-radius:8px;padding:10px;text-align:center;">
                <div style="font-size:11px;color:#888;">Peso</div>
                <div style="font-size:20px;font-weight:700;color:#333;">50kg</div>
              </div>
            </div>
            <div style="background:#512BD4;color:#fff;border-radius:8px;padding:10px;text-align:center;font-weight:600;font-size:14px;">✓ ${t('TutLogSet')}</div>
            <div style="margin-top:8px;text-align:center;padding:8px;background:#f3f0fc;border-radius:8px;">
              <div style="font-size:11px;color:#888;">${t('TutRestTimer')}</div>
              <div style="font-size:24px;font-weight:700;color:#512BD4;">1:30</div>
            </div>
          </div>
        `)}

        <!-- 7. Resumen -->
        ${section('7', '📊', t('TutSummaryTitle'), t('TutSummaryDesc'), `
          <div style="background:#fff;border-radius:10px;padding:16px;border:1px solid #eee;text-align:center;">
            <div style="font-size:36px;margin-bottom:4px;">🎉</div>
            <div style="font-size:16px;font-weight:700;color:#333;margin-bottom:12px;">${t('TutWorkoutComplete')}</div>
            <div style="display:flex;gap:8px;margin-bottom:12px;">
              <div style="flex:1;background:#f3f0fc;border-radius:8px;padding:10px;">
                <div style="font-weight:700;color:#512BD4;font-size:18px;">45:20</div>
                <div style="font-size:11px;color:#888;">${t('Duration')}</div>
              </div>
              <div style="flex:1;background:#f3f0fc;border-radius:8px;padding:10px;">
                <div style="font-weight:700;color:#512BD4;font-size:18px;">6</div>
                <div style="font-size:11px;color:#888;">${t('Exercises')}</div>
              </div>
              <div style="flex:1;background:#f3f0fc;border-radius:8px;padding:10px;">
                <div style="font-weight:700;color:#512BD4;font-size:18px;">18</div>
                <div style="font-size:11px;color:#888;">${t('Sets')}</div>
              </div>
            </div>
            <div style="background:#fff8e1;border-radius:8px;padding:8px;font-size:13px;color:#f57f17;">
              🏆 ${t('NewPR')} Press banca: 60 kg
            </div>
          </div>
        `)}

        <!-- 8. Estadísticas -->
        ${section('8', '📈', t('TutStatsTitle'), t('TutStatsDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="display:flex;gap:8px;margin-bottom:12px;">
              <div style="flex:1;background:#f3f0fc;border-radius:8px;padding:10px;text-align:center;">
                <div style="font-weight:700;color:#512BD4;font-size:18px;">48</div>
                <div style="font-size:11px;color:#888;">${t('Workouts')}</div>
              </div>
              <div style="flex:1;background:#f3f0fc;border-radius:8px;padding:10px;text-align:center;">
                <div style="font-weight:700;color:#512BD4;font-size:18px;">864</div>
                <div style="font-size:11px;color:#888;">${t('Sets')}</div>
              </div>
            </div>
            <div style="font-size:12px;color:#888;margin-bottom:4px;">${t('TutWeeklyChart')}</div>
            <div style="display:flex;align-items:flex-end;gap:4px;height:50px;">
              ${mockBar(60)}${mockBar(80)}${mockBar(40)}${mockBar(100)}${mockBar(70)}${mockBar(90)}${mockBar(50)}
            </div>
            <div style="display:flex;justify-content:space-between;font-size:10px;color:#aaa;margin-top:2px;">
              <span>L</span><span>M</span><span>X</span><span>J</span><span>V</span><span>S</span><span>D</span>
            </div>
          </div>
        `)}

        <!-- 9. Medidas corporales -->
        ${section('9', '📏', t('TutMeasurementsTitle'), t('TutMeasurementsDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:6px;">
              ${mockMeasure('⚖️', t('MeasWeight'), '75.5 kg')}
              ${mockMeasure('📏', t('MeasHeight'), '178 cm')}
              ${mockMeasure('📐', 'IMC/BMI', '23.8')}
              ${mockMeasure('💪', t('MeasBiceps'), '36 cm')}
            </div>
            <div style="margin-top:8px;padding:6px 10px;background:#e3f2fd;border-radius:6px;font-size:11px;color:#1565c0;text-align:center;">
              📊 ${t('TutMeasurementsTip')}
            </div>
          </div>
        `)}

        <!-- 10. Cuenta -->
        ${section('10', '👤', t('TutAccountTitle'), t('TutAccountDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="display:flex;align-items:center;gap:12px;margin-bottom:12px;padding-bottom:12px;border-bottom:1px solid #eee;">
              <div style="width:40px;height:40px;background:#512BD4;border-radius:50%;display:flex;align-items:center;justify-content:center;color:#fff;font-weight:700;font-size:18px;">J</div>
              <div>
                <div style="font-weight:600;font-size:14px;color:#333;">Juan</div>
                <div style="font-size:12px;color:#888;">juan@email.com</div>
              </div>
            </div>
            <div style="display:flex;flex-direction:column;gap:6px;">
              <div style="display:flex;justify-content:space-between;padding:8px 10px;background:#f5f5f5;border-radius:6px;font-size:13px;">
                <span>🌐 ${t('Language')}</span>
                <span style="color:#512BD4;">Español</span>
              </div>
              <div style="display:flex;justify-content:space-between;padding:8px 10px;background:#f5f5f5;border-radius:6px;font-size:13px;">
                <span>✏️ ${t('Edit')} ${t('Profile')}</span>
                <span style="color:#aaa;">→</span>
              </div>
              <div style="display:flex;justify-content:space-between;padding:8px 10px;background:#fff5f5;border-radius:6px;font-size:13px;color:#dc3545;">
                <span>🚪 ${t('Logout')}</span>
                <span>→</span>
              </div>
            </div>
          </div>
        `)}

        <div style="margin-top:24px;">
          <button id="tutorial-back-home" class="btn btn-primary btn-block btn-lg">${t('BackToHome')}</button>
        </div>
      </div>
    </div>
  `;
}

function section(num, icon, title, description, mockupHtml) {
  return `
    <div style="background:#fff;border-radius:12px;border-left:4px solid #512BD4;padding:16px;margin-bottom:16px;box-shadow:0 1px 3px rgba(0,0,0,0.06);">
      <div style="display:flex;align-items:center;gap:10px;margin-bottom:10px;">
        <div style="background:#f3f0fc;width:36px;height:36px;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:18px;flex-shrink:0;">${icon}</div>
        <div>
          <div style="font-size:11px;color:#512BD4;font-weight:600;">${t('TutStep')} ${num}</div>
          <div style="font-size:16px;font-weight:700;color:#333;">${title}</div>
        </div>
      </div>
      <p style="color:#555;font-size:13px;line-height:1.6;margin:0 0 12px;">${description}</p>
      ${mockupHtml}
    </div>
  `;
}

function mockDayCard(day, muscles) {
  return `
    <div style="display:flex;align-items:center;gap:10px;padding:8px 0;border-bottom:1px solid #f0f0f0;">
      <div style="font-weight:600;font-size:13px;color:#333;width:70px;">${day}</div>
      <div style="font-size:12px;color:#666;">${muscles.join(' · ')}</div>
    </div>
  `;
}

function mockBar(pct) {
  return `<div style="flex:1;background:#DFD8F7;border-radius:3px 3px 0 0;height:${pct}%;"><div style="background:#512BD4;border-radius:3px 3px 0 0;height:100%;"></div></div>`;
}

function mockMeasure(icon, label, value) {
  return `
    <div style="background:#f5f5f5;border-radius:8px;padding:8px;text-align:center;">
      <div style="font-size:16px;">${icon}</div>
      <div style="font-size:11px;color:#888;">${label}</div>
      <div style="font-weight:700;color:#333;font-size:14px;">${value}</div>
    </div>
  `;
}

export function mount() {
  document.getElementById('tutorial-back-home')?.addEventListener('click', () => {
    location.hash = '#home';
  });
}

export function destroy() {}
