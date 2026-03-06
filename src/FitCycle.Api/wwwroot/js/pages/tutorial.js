// FitCycle Tutorial / User Guide Page — Complete feature guide

import { t } from '../l10n.js';
import { auth } from '../auth.js';

export function render() {
  const isAdmin = auth.isAdmin();

  return `
    <div class="page no-tabs">
      <div class="page-content" style="padding-top:16px;padding-bottom:32px;">

        <div style="text-align:center;margin-bottom:24px;">
          <div style="font-size:48px;">📖</div>
          <h1 style="font-size:22px;font-weight:700;color:#333;margin:8px 0 4px;">${t('TutorialTitle')}</h1>
          <p style="color:#666;font-size:14px;margin:0;">${t('TutorialSubtitle')}</p>
        </div>

        <!-- TOC -->
        <div style="background:#f3f0fc;border-radius:12px;padding:14px 16px;margin-bottom:20px;">
          <div style="font-weight:700;font-size:13px;color:#512BD4;margin-bottom:8px;">${t('TutTOC')}</div>
          <div style="display:flex;flex-direction:column;gap:4px;font-size:12px;">
            ${tocItem('1', t('TutWelcomeTitle'))}
            ${tocItem('2', t('TutRegisterTitle'))}
            ${tocItem('3', t('TutHomeTitle'))}
            ${tocItem('4', t('TutRoutinesTitle'))}
            ${tocItem('5', t('TutExercisesTitle'))}
            ${tocItem('6', t('TutWorkoutTitle'))}
            ${tocItem('7', t('TutSummaryTitle'))}
            ${tocItem('8', t('TutStatsTitle'))}
            ${tocItem('9', t('TutMeasurementsTitle'))}
            ${tocItem('10', t('TutAccountTitle'))}
            ${tocItem('11', t('TutOfflineTitle'))}
            ${isAdmin ? tocItem('12', t('TutAdminTitle')) : ''}
          </div>
        </div>

        <!-- 1. Bienvenida -->
        ${section('1', '💪', t('TutWelcomeTitle'), t('TutWelcomeDesc'), `
          <div style="background:#512BD4;border-radius:12px;padding:20px;text-align:center;color:#fff;">
            <div style="font-size:32px;font-weight:bold;letter-spacing:2px;">FC</div>
            <div style="font-size:16px;margin-top:4px;">FitCycle</div>
            <div style="margin-top:12px;font-size:13px;opacity:0.8;">${t('TutWelcomeMockup')}</div>
          </div>
          <div style="margin-top:10px;display:flex;flex-wrap:wrap;gap:6px;">
            ${featureTag('📅', t('TutFeatureRoutines'))}
            ${featureTag('🏋️', t('TutFeatureWorkouts'))}
            ${featureTag('📊', t('TutFeatureStats'))}
            ${featureTag('📏', t('TutFeatureMeasurements'))}
            ${featureTag('🌐', t('TutFeatureMultilang'))}
            ${featureTag('📴', t('TutFeatureOffline'))}
          </div>
        `)}

        <!-- 2. Registro -->
        ${section('2', '📧', t('TutRegisterTitle'), t('TutRegisterDesc'), `
          <div style="background:#fff;border-radius:10px;padding:16px;border:1px solid #eee;">
            <div style="background:#f5f5f5;border-radius:8px;padding:10px 12px;margin-bottom:8px;color:#999;font-size:13px;">${t('Username')}</div>
            <div style="background:#f5f5f5;border-radius:8px;padding:10px 12px;margin-bottom:8px;color:#999;font-size:13px;">${t('Email')}</div>
            <div style="background:#f5f5f5;border-radius:8px;padding:10px 12px;margin-bottom:8px;color:#999;font-size:13px;">********</div>
            <div style="font-size:11px;padding:6px 10px;background:#f9f9f9;border-radius:8px;margin-bottom:12px;">
              <div style="color:#28a745;">● ${t('PwdMinLength')}</div>
              <div style="color:#28a745;">● ${t('PwdUppercase')}</div>
              <div style="color:#999;">○ ${t('PwdDigit')}</div>
              <div style="color:#999;">○ ${t('PwdSpecial')}</div>
            </div>
            <div style="background:#512BD4;color:#fff;border-radius:8px;padding:10px;text-align:center;font-weight:600;font-size:14px;">${t('Register')}</div>
          </div>
          ${tip(t('TutRegisterTip'))}
          <div style="margin-top:8px;background:#fff;border-radius:10px;padding:16px;border:1px solid #eee;text-align:center;">
            <div style="font-size:32px;">✉️</div>
            <div style="font-weight:600;font-size:14px;color:#333;margin-top:4px;">${t('CheckYourEmail')}</div>
            <div style="font-size:12px;color:#888;margin-top:4px;">${t('TutActivationFlow')}</div>
          </div>
        `)}

        <!-- 3. Home -->
        ${section('3', '🏠', t('TutHomeTitle'), t('TutHomeDesc'), `
          <div style="background:#fff;border-radius:10px;padding:16px;border:1px solid #eee;">
            <div style="display:flex;gap:8px;margin-bottom:12px;">
              ${mockStat('💪', '12', t('Workouts'))}
              ${mockStat('🔥', '5', t('Streak'))}
              ${mockStat('📅', t('Today'), t('LastWorkout'))}
            </div>
            <div style="background:#512BD4;color:#fff;border-radius:8px;padding:10px;text-align:center;font-weight:600;font-size:14px;">${t('GoToRoutines')}</div>
            <div style="margin-top:6px;border:1px solid #ddd;color:#666;border-radius:8px;padding:8px;text-align:center;font-size:13px;">${t('ViewStats')}</div>
            <div style="margin-top:6px;border:1px solid #512BD4;color:#512BD4;border-radius:8px;padding:8px;text-align:center;font-size:13px;">📖 ${t('UserGuide')}</div>
          </div>
        `)}

        <!-- 4. Rutina semanal -->
        ${section('4', '📅', t('TutRoutinesTitle'), t('TutRoutinesDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            ${mockDayCard(t('Monday'), ['🏋️ ' + t('TutChest'), '💪 ' + t('TutTriceps')])}
            ${mockDayCard(t('Tuesday'), ['🏋️ ' + t('TutBack'), '💪 ' + t('TutBiceps')])}
            ${mockDayCard(t('Wednesday'), ['🦵 ' + t('TutLegs'), '🍑 ' + t('TutGlutes')])}
            ${mockDayCard(t('Thursday'), [])}
            <div style="padding:8px 0;text-align:center;">
              <span style="font-size:11px;color:#aaa;">...</span>
            </div>
          </div>
          ${tip(t('TutRoutinesTip'))}
          ${tip(t('TutRoutinesTip2'), '#e3f2fd', '#1565c0')}
        `)}

        <!-- 5. Configurar ejercicios -->
        ${section('5', '⚙️', t('TutExercisesTitle'), t('TutExercisesDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="display:flex;align-items:center;gap:8px;margin-bottom:10px;">
              <div style="width:40px;height:40px;background:#f0f0f0;border-radius:6px;display:flex;align-items:center;justify-content:center;font-size:18px;">🏋️</div>
              <div style="font-weight:600;font-size:14px;color:#333;">Press banca</div>
            </div>
            <div style="display:flex;gap:6px;margin-bottom:8px;">
              ${mockSet('S1', '12', '40kg')}
              ${mockSet('S2', '10', '50kg')}
              ${mockSet('S3', '8', '60kg')}
              <div style="flex:1;border:1px dashed #ccc;border-radius:6px;padding:6px;text-align:center;font-size:18px;color:#aaa;cursor:pointer;">+</div>
            </div>
            <!-- Tempo & Grip detail -->
            <div style="background:#f3f0fc;border-radius:8px;padding:10px;margin-bottom:8px;">
              <div style="font-size:12px;font-weight:600;color:#512BD4;margin-bottom:6px;">⏱ ${t('Tempo')} — ${t('TutTempoExplain')}</div>
              <div style="display:flex;gap:8px;margin-bottom:6px;">
                <div style="flex:1;background:#fff;border-radius:6px;padding:6px;text-align:center;border:1px solid #DFD8F7;">
                  <div style="font-size:10px;color:#512BD4;font-weight:600;">↑ ${t('TempoAsc')}</div>
                  <div style="font-size:16px;font-weight:700;color:#333;">2s</div>
                  <div style="font-size:9px;color:#888;">${t('TempoAscFull')}</div>
                </div>
                <div style="flex:1;background:#fff;border-radius:6px;padding:6px;text-align:center;border:1px solid #DFD8F7;">
                  <div style="font-size:10px;color:#512BD4;font-weight:600;">↓ ${t('TempoDesc')}</div>
                  <div style="font-size:16px;font-weight:700;color:#333;">3s</div>
                  <div style="font-size:9px;color:#888;">${t('TempoDescFull')}</div>
                </div>
              </div>
              <div style="font-size:12px;font-weight:600;color:#e67e22;margin-bottom:4px;">✊ ${t('Grip')} — ${t('TutGripExplain')}</div>
              <div style="display:flex;gap:6px;">
                <span style="background:#fff;border:1px solid #e67e22;color:#e67e22;padding:3px 8px;border-radius:6px;font-size:11px;">${t('GripProne')}</span>
                <span style="background:#fff;border:1px solid #ddd;color:#666;padding:3px 8px;border-radius:6px;font-size:11px;">${t('GripSupine')}</span>
                <span style="background:#fff;border:1px solid #ddd;color:#666;padding:3px 8px;border-radius:6px;font-size:11px;">${t('GripNeutral')}</span>
              </div>
            </div>
            <div style="display:flex;gap:6px;flex-wrap:wrap;margin-bottom:8px;">
              ${featureTag('🔗', 'Superset')}
              ${featureTag('📝', t('Notes'))}
            </div>
            <div style="background:#f9f9f9;border-radius:6px;padding:8px;font-size:12px;color:#666;font-style:italic;">
              "${t('TutExampleNote')}"
            </div>
          </div>
          ${tip(t('TutExercisesTip'))}
          ${tip(t('TutExercisesTip2'), '#fff3e0', '#e65100')}
          ${tip(t('TutExercisesTip3'), '#e3f2fd', '#1565c0')}
          ${tip(t('TutExercisesTip4'), '#f3f0fc', '#512BD4')}
        `)}

        <!-- 6. Entrenar -->
        ${section('6', '🏋️', t('TutWorkoutTitle'), t('TutWorkoutDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <!-- Exercise progress -->
            <div style="display:flex;gap:4px;margin-bottom:10px;">
              <div style="flex:1;height:4px;background:#512BD4;border-radius:2px;"></div>
              <div style="flex:1;height:4px;background:#512BD4;border-radius:2px;"></div>
              <div style="flex:1;height:4px;background:#DFD8F7;border-radius:2px;"></div>
              <div style="flex:1;height:4px;background:#DFD8F7;border-radius:2px;"></div>
            </div>
            <div style="text-align:center;margin-bottom:10px;">
              <div style="font-size:12px;color:#888;">${t('TutCurrentExercise')} 2/4</div>
              <div style="font-size:18px;font-weight:700;color:#333;">Press banca</div>
              <div style="font-size:12px;color:#512BD4;margin-top:2px;">Serie 2 / 3</div>
              <!-- Set dots -->
              <div style="display:flex;justify-content:center;gap:6px;margin-top:6px;">
                <div style="width:10px;height:10px;border-radius:50%;background:#28a745;"></div>
                <div style="width:10px;height:10px;border-radius:50%;background:#512BD4;border:2px solid #512BD4;"></div>
                <div style="width:10px;height:10px;border-radius:50%;background:#ddd;"></div>
              </div>
            </div>
            <div style="display:flex;gap:8px;margin-bottom:10px;">
              <div style="flex:1;background:#f5f5f5;border-radius:8px;padding:10px;text-align:center;">
                <div style="font-size:11px;color:#888;">Reps</div>
                <div style="font-size:20px;font-weight:700;color:#333;">10</div>
              </div>
              <div style="flex:1;background:#f5f5f5;border-radius:8px;padding:10px;text-align:center;">
                <div style="font-size:11px;color:#888;">Peso</div>
                <div style="font-size:20px;font-weight:700;color:#333;">50kg</div>
              </div>
            </div>
            <div style="display:flex;gap:6px;margin-bottom:10px;flex-wrap:wrap;justify-content:center;">
              <span style="background:#f3f0fc;color:#512BD4;padding:3px 8px;border-radius:6px;font-size:11px;">↑ 2s ${t('TempoAsc')}</span>
              <span style="background:#f3f0fc;color:#512BD4;padding:3px 8px;border-radius:6px;font-size:11px;">↓ 3s ${t('TempoDesc')}</span>
              <span style="background:#fff3e0;color:#e67e22;padding:3px 8px;border-radius:6px;font-size:11px;">✊ ${t('Grip')}: ${t('GripProne')}</span>
            </div>
            <div style="background:#512BD4;color:#fff;border-radius:8px;padding:10px;text-align:center;font-weight:600;font-size:14px;">✓ ${t('TutLogSet')}</div>
            <div style="margin-top:8px;text-align:center;padding:10px;background:#f3f0fc;border-radius:8px;">
              <div style="font-size:11px;color:#888;">${t('TutRestTimer')}</div>
              <div style="font-size:28px;font-weight:700;color:#512BD4;">1:30</div>
              <div style="display:flex;justify-content:center;gap:8px;margin-top:6px;">
                <span style="background:#512BD4;color:#fff;padding:4px 12px;border-radius:6px;font-size:11px;">${t('Pause')}</span>
                <span style="background:#ddd;color:#333;padding:4px 12px;border-radius:6px;font-size:11px;">${t('Reset')}</span>
              </div>
            </div>
          </div>
          ${tip(t('TutWorkoutTip'))}
          ${tip(t('TutWorkoutTip2'), '#e3f2fd', '#1565c0')}
          ${tip(t('TutWorkoutTip3'), '#fff3e0', '#e65100')}
        `)}

        <!-- 7. Resumen -->
        ${section('7', '🎉', t('TutSummaryTitle'), t('TutSummaryDesc'), `
          <div style="background:#fff;border-radius:10px;padding:16px;border:1px solid #eee;text-align:center;">
            <div style="font-size:36px;margin-bottom:4px;">✅</div>
            <div style="font-size:16px;font-weight:700;color:#333;margin-bottom:12px;">${t('TutWorkoutComplete')}</div>
            <div style="display:flex;gap:8px;margin-bottom:12px;">
              ${mockStat('⏱', '45:20', t('Duration'))}
              ${mockStat('🏋️', '6', t('Exercises'))}
              ${mockStat('📋', '18', t('Sets'))}
            </div>
            <div style="background:#fff8e1;border-radius:8px;padding:8px;font-size:13px;color:#f57f17;margin-bottom:10px;">
              🏆 ${t('NewPR')} Press banca: 60 kg
            </div>
            <div style="text-align:left;font-size:12px;color:#666;border-top:1px solid #eee;padding-top:10px;">
              <div style="font-weight:600;color:#333;margin-bottom:4px;">${t('TutExerciseBreakdown')}</div>
              <div style="padding:4px 0;border-bottom:1px solid #f5f5f5;">Press banca — S1: 12×40kg · S2: 10×50kg · S3: 8×<span style="color:#512BD4;font-weight:600;">60kg</span></div>
              <div style="padding:4px 0;">Curl bíceps — S1: 12×15kg · S2: 10×17kg</div>
            </div>
          </div>
        `)}

        <!-- 8. Estadísticas -->
        ${section('8', '📈', t('TutStatsTitle'), t('TutStatsDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="display:flex;gap:6px;margin-bottom:12px;">
              ${mockStat('💪', '48', t('Workouts'))}
              ${mockStat('📋', '864', t('Sets'))}
              ${mockStat('🔄', '10.4k', t('Reps'))}
              ${mockStat('🔥', '12', t('Streak'))}
            </div>
            <div style="font-size:12px;color:#888;margin-bottom:4px;">${t('TutWeeklyChart')}</div>
            <div style="display:flex;align-items:flex-end;gap:4px;height:50px;">
              ${mockBar(60)}${mockBar(80)}${mockBar(40)}${mockBar(100)}${mockBar(70)}${mockBar(90)}${mockBar(50)}
            </div>
            <div style="display:flex;justify-content:space-between;font-size:10px;color:#aaa;margin-top:2px;margin-bottom:10px;">
              <span>L</span><span>M</span><span>X</span><span>J</span><span>V</span><span>S</span><span>D</span>
            </div>
            <div style="font-size:12px;color:#888;margin-bottom:4px;">${t('TutTopExercises')}</div>
            <div style="margin-bottom:4px;">
              ${mockHBar('Press banca', 90)}
              ${mockHBar('Sentadilla', 70)}
              ${mockHBar('Curl bíceps', 55)}
            </div>
            ${tip(t('TutStatsTip'))}
          </div>
          <div style="margin-top:8px;background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="font-size:12px;color:#888;margin-bottom:6px;">${t('TutWeightProgression')}</div>
            <svg viewBox="0 0 200 60" style="width:100%;height:60px;">
              <polyline points="10,50 50,40 90,35 130,28 170,20" fill="none" stroke="#512BD4" stroke-width="2"/>
              <circle cx="10" cy="50" r="3" fill="#512BD4"/><circle cx="50" cy="40" r="3" fill="#512BD4"/>
              <circle cx="90" cy="35" r="3" fill="#512BD4"/><circle cx="130" cy="28" r="3" fill="#512BD4"/>
              <circle cx="170" cy="20" r="3" fill="#512BD4"/>
            </svg>
            <div style="display:flex;justify-content:space-between;font-size:10px;color:#aaa;">
              <span>40kg</span><span>50kg</span><span>55kg</span><span>60kg</span><span>65kg</span>
            </div>
          </div>
          ${tip(t('TutStatsTip2'), '#e3f2fd', '#1565c0')}
        `)}

        <!-- 9. Medidas corporales -->
        ${section('9', '📏', t('TutMeasurementsTitle'), t('TutMeasurementsDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:6px;margin-bottom:10px;">
              ${mockMeasure('⚖️', t('MeasWeight'), '75.5 kg')}
              ${mockMeasure('📏', t('MeasHeight'), '178 cm')}
              ${mockMeasure('📐', 'IMC', '23.8')}
            </div>
            <div style="background:#e8f5e9;border-radius:6px;padding:6px 10px;text-align:center;margin-bottom:10px;">
              <span style="font-size:12px;color:#2e7d32;font-weight:600;">✓ ${t('TutBMINormal')}</span>
            </div>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:6px;margin-bottom:10px;">
              ${mockMeasure('💪', t('MeasBiceps'), '36 cm')}
              ${mockMeasure('📐', t('MeasChest'), '98 cm')}
              ${mockMeasure('📐', t('MeasWaist'), '82 cm')}
              ${mockMeasure('🦵', t('MeasThighs'), '58 cm')}
            </div>
            <div style="font-size:12px;color:#888;margin-bottom:4px;">${t('MeasTrend')}: ${t('MeasWeight')}</div>
            <svg viewBox="0 0 200 50" style="width:100%;height:50px;">
              <polyline points="10,40 50,35 90,30 130,28 170,25" fill="none" stroke="#28a745" stroke-width="2"/>
              <circle cx="10" cy="40" r="3" fill="#28a745"/><circle cx="50" cy="35" r="3" fill="#28a745"/>
              <circle cx="90" cy="30" r="3" fill="#28a745"/><circle cx="130" cy="28" r="3" fill="#28a745"/>
              <circle cx="170" cy="25" r="3" fill="#28a745"/>
            </svg>
          </div>
          ${tip(t('TutMeasurementsTip'))}
          ${tip(t('TutMeasurementsTip2'), '#e3f2fd', '#1565c0')}
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
              ${mockMenuItem('✏️', t('Edit') + ' ' + t('Profile'), '')}
              ${mockMenuItem('🔒', t('TutChangePassword'), '')}
              ${mockMenuItem('🌐', t('Language'), 'Español / English / Français')}
              ${mockMenuItem('🚪', t('Logout'), '', true)}
            </div>
          </div>
        `)}

        <!-- 11. Offline / PWA -->
        ${section('11', '📴', t('TutOfflineTitle'), t('TutOfflineDesc'), `
          <div style="background:#fff;border-radius:10px;padding:16px;border:1px solid #eee;text-align:center;">
            <div style="font-size:36px;margin-bottom:8px;">📱</div>
            <div style="font-size:14px;font-weight:600;color:#333;margin-bottom:8px;">${t('TutPWA')}</div>
            <div style="display:flex;flex-direction:column;gap:6px;text-align:left;font-size:12px;color:#555;">
              <div>✓ ${t('TutOffline1')}</div>
              <div>✓ ${t('TutOffline2')}</div>
              <div>✓ ${t('TutOffline3')}</div>
              <div>✓ ${t('TutOffline4')}</div>
            </div>
          </div>
          ${tip(t('TutOfflineTip'))}
        `)}

        <!-- 12. Admin (solo si es admin) -->
        ${isAdmin ? section('12', '🛠️', t('TutAdminTitle'), t('TutAdminDesc'), `
          <div style="background:#fff;border-radius:10px;padding:12px;border:1px solid #eee;">
            <div style="font-weight:600;font-size:13px;color:#333;margin-bottom:8px;">${t('TutAdminFeatures')}</div>
            <div style="display:flex;flex-direction:column;gap:6px;">
              ${mockMenuItem('📋', t('TutCopyRoutines'), t('TutCopyRoutinesDesc'))}
              ${mockMenuItem('📄', t('TutImportPDF'), t('TutImportPDFDesc'))}
              ${mockMenuItem('📦', t('TutTemplates'), t('TutTemplatesDesc'))}
              ${mockMenuItem('👥', t('TutUserMgmt'), t('TutUserMgmtDesc'))}
              ${mockMenuItem('🗄️', t('TutSQLConsole'), t('TutSQLConsoleDesc'))}
              ${mockMenuItem('💾', t('TutBackups'), t('TutBackupsDesc'))}
              ${mockMenuItem('⬇️', t('TutDownloadDB'), t('TutDownloadDBDesc'))}
              ${mockMenuItem('🎭', t('TutImpersonate'), t('TutImpersonateDesc'))}
            </div>
          </div>
        `) : ''}

        <div style="margin-top:24px;">
          <button id="tutorial-back-home" class="btn btn-primary btn-block btn-lg">${t('BackToHome')}</button>
        </div>
      </div>
    </div>
  `;
}

function tocItem(num, title) {
  return `<a href="#tut-${num}" style="color:#333;text-decoration:none;padding:2px 0;display:block;" onclick="document.getElementById('tut-${num}')?.scrollIntoView({behavior:'smooth'});return false;"><span style="color:#512BD4;font-weight:600;margin-right:6px;">${num}.</span>${title}</a>`;
}

function section(num, icon, title, description, mockupHtml) {
  return `
    <div id="tut-${num}" style="background:#fff;border-radius:12px;border-left:4px solid #512BD4;padding:16px;margin-bottom:16px;box-shadow:0 1px 3px rgba(0,0,0,0.06);">
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

function tip(text, bg = '#e8f5e9', color = '#2e7d32') {
  return `<div style="margin-top:8px;padding:8px 12px;background:${bg};border-radius:8px;font-size:12px;color:${color};">💡 ${text}</div>`;
}

function featureTag(icon, text) {
  return `<span style="background:#f3f0fc;color:#512BD4;padding:3px 8px;border-radius:12px;font-size:11px;">${icon} ${text}</span>`;
}

function mockStat(icon, value, label) {
  return `<div style="flex:1;background:#f3f0fc;border-radius:8px;padding:8px;text-align:center;">
    <div style="font-size:16px;">${icon}</div>
    <div style="font-weight:700;font-size:15px;color:#512BD4;">${value}</div>
    <div style="font-size:10px;color:#888;">${label}</div>
  </div>`;
}

function mockDayCard(day, muscles) {
  const content = muscles.length > 0 ? muscles.join(' · ') : `<span style="color:#ccc;font-style:italic;">${t('TutRestDay')}</span>`;
  return `
    <div style="display:flex;align-items:center;gap:10px;padding:8px 0;border-bottom:1px solid #f0f0f0;">
      <div style="font-weight:600;font-size:13px;color:#333;width:70px;">${day}</div>
      <div style="font-size:12px;color:#666;">${content}</div>
    </div>
  `;
}

function mockSet(label, reps, weight) {
  return `<div style="flex:1;background:#f5f5f5;border-radius:6px;padding:6px;text-align:center;font-size:11px;">
    <div style="color:#888;font-size:10px;">${label}</div>
    <div style="color:#333;font-weight:600;">${reps}×${weight}</div>
  </div>`;
}

function mockBar(pct) {
  return `<div style="flex:1;background:#DFD8F7;border-radius:3px 3px 0 0;height:${pct}%;"><div style="background:#512BD4;border-radius:3px 3px 0 0;height:100%;"></div></div>`;
}

function mockHBar(label, pct) {
  return `<div style="margin-bottom:4px;">
    <div style="font-size:11px;color:#666;margin-bottom:2px;">${label}</div>
    <div style="background:#f0f0f0;border-radius:4px;height:8px;"><div style="background:#512BD4;border-radius:4px;height:100%;width:${pct}%;"></div></div>
  </div>`;
}

function mockMeasure(icon, label, value) {
  return `<div style="background:#f5f5f5;border-radius:8px;padding:8px;text-align:center;">
    <div style="font-size:14px;">${icon}</div>
    <div style="font-size:10px;color:#888;">${label}</div>
    <div style="font-weight:700;color:#333;font-size:13px;">${value}</div>
  </div>`;
}

function mockMenuItem(icon, title, subtitle, danger = false) {
  const bg = danger ? '#fff5f5' : '#f5f5f5';
  const color = danger ? '#dc3545' : '#333';
  return `<div style="display:flex;justify-content:space-between;align-items:center;padding:8px 10px;background:${bg};border-radius:6px;font-size:13px;color:${color};">
    <div><span>${icon}</span> ${title}${subtitle ? `<div style="font-size:11px;color:#888;font-weight:normal;margin-top:2px;">${subtitle}</div>` : ''}</div>
    <span style="color:#aaa;">→</span>
  </div>`;
}

export function mount() {
  document.getElementById('tutorial-back-home')?.addEventListener('click', () => {
    location.hash = '#home';
  });
}

export function destroy() {}
