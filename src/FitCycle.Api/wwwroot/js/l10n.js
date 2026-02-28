// FitCycle Localization — ported from FitCycle.App/Services/L10n.cs

let _lang = 'es';

const Strings = {
  // === Days ===
  Monday:    { es: 'Lunes',     en: 'Monday',    fr: 'Lundi' },
  Tuesday:   { es: 'Martes',    en: 'Tuesday',   fr: 'Mardi' },
  Wednesday: { es: 'Miércoles', en: 'Wednesday', fr: 'Mercredi' },
  Thursday:  { es: 'Jueves',    en: 'Thursday',  fr: 'Jeudi' },
  Friday:    { es: 'Viernes',   en: 'Friday',    fr: 'Vendredi' },
  Saturday:  { es: 'Sábado',    en: 'Saturday',  fr: 'Samedi' },
  Sunday:    { es: 'Domingo',   en: 'Sunday',    fr: 'Dimanche' },

  // === Muscle Groups (key = MG_{spanish name}) ===
  MG_Pecho:        { en: 'Chest',     fr: 'Poitrine' },
  MG_Espalda:      { en: 'Back',      fr: 'Dos' },
  MG_Hombros:      { en: 'Shoulders', fr: 'Épaules' },
  'MG_Bíceps':     { en: 'Biceps',    fr: 'Biceps' },
  'MG_Tríceps':    { en: 'Triceps',   fr: 'Triceps' },
  MG_Piernas:      { en: 'Legs',      fr: 'Jambes' },
  MG_Abdominales:  { en: 'Abs',       fr: 'Abdominaux' },
  'MG_Glúteos':    { en: 'Glutes',    fr: 'Fessiers' },

  // === General ===
  Loading:            { es: 'Cargando...',             en: 'Loading...',           fr: 'Chargement...' },
  ServiceUnavailable: { es: 'Servicio no disponible',  en: 'Service unavailable',  fr: 'Service indisponible' },
  ErrorFmt:           { es: 'Error: {0}',              en: 'Error: {0}',           fr: 'Erreur : {0}' },
  Saving:             { es: 'Guardando...',             en: 'Saving...',            fr: 'Enregistrement...' },
  UnknownError:       { es: 'Error desconocido',        en: 'Unknown error',        fr: 'Erreur inconnue' },

  // === Buttons ===
  Save:     { es: 'Guardar',        en: 'Save',     fr: 'Enregistrer' },
  Cancel:   { es: 'Cancelar',       en: 'Cancel',   fr: 'Annuler' },
  Yes:      { es: 'Sí',             en: 'Yes',      fr: 'Oui' },
  No:       { es: 'No',             en: 'No',       fr: 'Non' },
  Confirm:  { es: 'Confirmar',      en: 'Confirm',  fr: 'Confirmer' },
  Next:     { es: 'Siguiente',      en: 'Next',     fr: 'Suivant' },
  Previous: { es: 'Anterior',       en: 'Previous', fr: 'Précédent' },
  Start:    { es: 'Iniciar',        en: 'Start',    fr: 'Démarrer' },
  Pause:    { es: 'Pausar',         en: 'Pause',    fr: 'Pause' },
  Reset:    { es: 'Reiniciar',      en: 'Reset',    fr: 'Réinitialiser' },
  Finish:   { es: 'Finalizar',      en: 'Finish',   fr: 'Terminer' },
  Edit:     { es: 'Editar',         en: 'Edit',     fr: 'Modifier' },
  Delete:   { es: 'Borrar',         en: 'Delete',   fr: 'Supprimer' },
  Create:   { es: 'Crear',          en: 'Create',   fr: 'Créer' },
  Add:      { es: 'Agregar',        en: 'Add',      fr: 'Ajouter' },
  Back:     { es: 'Volver',         en: 'Back',     fr: 'Retour' },
  Logout:   { es: 'Cerrar Sesión',  en: 'Log Out',  fr: 'Déconnexion' },

  // === Login ===
  AppName:           { es: 'FitCycle',                                 en: 'FitCycle',                              fr: 'FitCycle' },
  SignIn:            { es: 'Iniciar Sesión',                           en: 'Sign In',                               fr: 'Connexion' },
  CreateAccount:     { es: 'Crear Cuenta',                             en: 'Create Account',                        fr: 'Créer un compte' },
  Username:          { es: 'Nombre de usuario',                        en: 'Username',                              fr: "Nom d'utilisateur" },
  Email:             { es: 'Email',                                    en: 'Email',                                 fr: 'E-mail' },
  Password:          { es: 'Contraseña',                               en: 'Password',                              fr: 'Mot de passe' },
  Register:          { es: 'Registrarse',                              en: 'Register',                              fr: "S'inscrire" },
  NoAccountSignUp:   { es: '¿No tienes cuenta? Regístrate',            en: "Don't have an account? Sign up",        fr: 'Pas de compte ? Inscrivez-vous' },
  HaveAccountSignIn: { es: '¿Ya tienes cuenta? Inicia sesión',         en: 'Already have an account? Sign in',      fr: 'Déjà un compte ? Connectez-vous' },

  // === Routines ===
  MyWeeklyRoutine:   { es: 'Mi Rutina Semanal',                        en: 'My Weekly Routine',                     fr: 'Ma Routine Hebdomadaire' },
  ConfigureWeekly:   { es: 'Configura y entrena tu rutina semanal',     en: 'Configure and train your weekly routine', fr: 'Configurez et entraînez votre routine hebdomadaire' },
  NoGroupsAssigned:  { es: 'Sin grupos asignados',                      en: 'No groups assigned',                    fr: 'Aucun groupe assigné' },
  DeleteRoutineMsg:  { es: '¿Eliminar la rutina de este día?',          en: "Delete this day's routine?",            fr: 'Supprimer la routine de ce jour ?' },
  StartWorkout:      { es: 'Empezar',                                   en: 'Start',                                 fr: 'Commencer' },
  TabRoutines:       { es: 'Rutinas',                                   en: 'Routines',                              fr: 'Routines' },
  TabStats:          { es: 'Estadísticas',                              en: 'Statistics',                             fr: 'Statistiques' },

  // === EditDay ===
  EditDay:                { es: 'Editar Día',                                        en: 'Edit Day',                              fr: 'Modifier le jour' },
  SelectGroupsExercises:  { es: 'Selecciona grupos musculares y ejercicios:',         en: 'Select muscle groups and exercises:',   fr: 'Sélectionnez les groupes musculaires et exercices :' },
  Sets:                   { es: 'Series:',                                            en: 'Sets:',                                 fr: 'Séries :' },
  Reps:                   { es: 'Reps:',                                              en: 'Reps:',                                 fr: 'Reps :' },
  AddExercise:            { es: '+ Agregar ejercicio',                                en: '+ Add exercise',                        fr: '+ Ajouter un exercice' },
  CustomName:             { es: 'Escribir nombre personalizado...',                   en: 'Type custom name...',                   fr: 'Saisir un nom personnalisé...' },
  NewExercise:            { es: 'Nuevo ejercicio',                                    en: 'New exercise',                          fr: 'Nouvel exercice' },
  ExerciseNameFor:        { es: 'Nombre del ejercicio para {0}:',                     en: 'Exercise name for {0}:',                fr: "Nom de l'exercice pour {0} :" },
  AddExerciseTo:          { es: 'Agregar ejercicio a {0}',                            en: 'Add exercise to {0}',                   fr: 'Ajouter un exercice à {0}' },
  SelectExercise:         { es: 'Seleccionar ejercicio',                              en: 'Select exercise',                       fr: 'Sélectionner un exercice' },
  ConfirmDeleteExercise:  { es: "¿Eliminar '{0}' de este día?",                       en: "Remove '{0}' from this day?",           fr: "Supprimer '{0}' de ce jour ?" },
  Weight:                 { es: 'Peso',                                              en: 'Weight',                                fr: 'Poids' },
  WeightKg:               { es: 'kg',                                                en: 'kg',                                    fr: 'kg' },
  WeightProgress:         { es: 'Progreso de peso',                                  en: 'Weight Progress',                       fr: 'Progression du poids' },

  // === Workout ===
  Workout:           { es: 'Entrenamiento',                             en: 'Workout',                               fr: 'Entraînement' },
  Rest:              { es: 'DESCANSO',                                  en: 'REST',                                   fr: 'REPOS' },
  Min:               { es: 'Min:',                                      en: 'Min:',                                   fr: 'Min :' },
  Sec:               { es: 'Seg:',                                      en: 'Sec:',                                   fr: 'Sec :' },
  ExerciseProgress:  { es: 'Ejercicio {0} de {1}',                      en: 'Exercise {0} of {1}',                    fr: 'Exercice {0} sur {1}' },
  SetsRepsFormat:    { es: '{0} series x {1} repeticiones',             en: '{0} sets x {1} reps',                    fr: '{0} séries x {1} répétitions' },
  NoExercisesDay:    { es: 'No hay ejercicios configurados para este día.', en: 'No exercises configured for this day.', fr: 'Aucun exercice configuré pour ce jour.' },
  NoImage:           { es: 'Sin imagen',                                en: 'No image',                               fr: "Pas d'image" },
  SetN:              { es: 'Serie {0} de {1}',                          en: 'Set {0} of {1}',                          fr: 'Série {0} sur {1}' },
  AddSet:            { es: 'Añadir serie',                              en: 'Add set',                                 fr: 'Ajouter série' },
  PrevSet:           { es: 'Serie ant.',                                en: 'Prev set',                                fr: 'Série préc.' },
  NextSet:           { es: 'Sig. serie',                                en: 'Next set',                                fr: 'Série suiv.' },
  NextExercise:      { es: 'Sig. ejercicio',                            en: 'Next exercise',                           fr: 'Exercice suiv.' },

  // === Summary ===
  Summary:           { es: 'Resumen',                                   en: 'Summary',                                fr: 'Résumé' },
  WorkoutCompleted:  { es: 'Entrenamiento completado',                  en: 'Workout completed',                      fr: 'Entraînement terminé' },
  Duration:          { es: 'Duración',                                  en: 'Duration',                               fr: 'Durée' },
  Exercises:         { es: 'Ejercicios',                                en: 'Exercises',                               fr: 'Exercices' },
  TotalSets:         { es: 'Series totales',                            en: 'Total sets',                              fr: 'Séries totales' },
  WeeklyWorkouts:    { es: 'Entrenamientos esta semana',                en: 'Workouts this week',                      fr: 'Entraînements cette semaine' },
  ExercisesDone:     { es: 'Ejercicios realizados',                     en: 'Exercises done',                          fr: 'Exercices réalisés' },
  BackToRoutines:    { es: 'Volver a Rutinas',                          en: 'Back to Routines',                        fr: 'Retour aux routines' },

  // === Stats ===
  YourProgress:        { es: 'Tu Progreso',                              en: 'Your Progress',                          fr: 'Votre Progrès' },
  Workouts:            { es: 'Entrenamientos',                            en: 'Workouts',                               fr: 'Entraînements' },
  TotalReps:           { es: 'Reps totales',                              en: 'Total reps',                             fr: 'Reps totales' },
  WeeklyWorkoutsChart: { es: 'Entrenamientos por semana',                 en: 'Workouts per week',                      fr: 'Entraînements par semaine' },
  MostFrequent:        { es: 'Ejercicios más frecuentes',                 en: 'Most frequent exercises',                fr: 'Exercices les plus fréquents' },
  RecentHistory:       { es: 'Historial reciente',                        en: 'Recent history',                         fr: 'Historique récent' },
  NoWorkoutsYet:       { es: 'Aún no hay entrenamientos registrados',     en: 'No workouts recorded yet',               fr: 'Aucun entraînement enregistré' },
  ExercisesCount:      { es: '{0} ejercicios · {1}',                      en: '{0} exercises · {1}',                     fr: '{0} exercices · {1}' },
  MinSuffix:           { es: ' min',                                      en: ' min',                                    fr: ' min' },
  SecSuffix:           { es: 's',                                         en: 's',                                       fr: 's' },

  // === Account ===
  MyAccount:           { es: 'Mi Cuenta',                                 en: 'My Account',                              fr: 'Mon Compte' },
  EditProfile:         { es: 'Editar Perfil',                             en: 'Edit Profile',                            fr: 'Modifier le profil' },
  UserManagement:      { es: 'Gestión de Usuarios',                       en: 'User Management',                         fr: 'Gestion des utilisateurs' },
  CreateUser:          { es: '+ Crear Usuario',                           en: '+ Create User',                           fr: '+ Créer un utilisateur' },
  CreateUserTitle:     { es: 'Crear Usuario',                             en: 'Create User',                             fr: 'Créer un utilisateur' },
  EditUserTitle:       { es: 'Editar Usuario',                            en: 'Edit User',                               fr: "Modifier l'utilisateur" },
  SelectRole:          { es: 'Seleccionar Rol',                           en: 'Select Role',                             fr: 'Sélectionner le rôle' },
  CreatingUser:        { es: 'Creando usuario...',                        en: 'Creating user...',                        fr: "Création de l'utilisateur..." },
  UserCreated:         { es: 'Usuario creado correctamente',              en: 'User created successfully',               fr: 'Utilisateur créé avec succès' },
  ErrorCreatingUser:   { es: 'Error al crear usuario',                    en: 'Error creating user',                     fr: 'Erreur lors de la création' },
  UpdatingUser:        { es: 'Actualizando usuario...',                   en: 'Updating user...',                        fr: "Mise à jour de l'utilisateur..." },
  UserUpdated:         { es: 'Usuario actualizado',                       en: 'User updated',                            fr: 'Utilisateur mis à jour' },
  ErrorUpdatingUser:   { es: 'Error al actualizar usuario',               en: 'Error updating user',                     fr: 'Erreur lors de la mise à jour' },
  ChangePassword:      { es: 'Cambiar Contraseña',                        en: 'Change Password',                         fr: 'Changer le mot de passe' },
  NewPasswordFor:      { es: "Nueva contraseña para '{0}':",              en: "New password for '{0}':",                  fr: "Nouveau mot de passe pour '{0}' :" },
  PasswordMinLength:   { es: 'La contraseña debe tener al menos 6 caracteres', en: 'Password must be at least 6 characters', fr: 'Le mot de passe doit comporter au moins 6 caractères' },
  ChangingPassword:    { es: 'Cambiando contraseña...',                   en: 'Changing password...',                    fr: 'Changement du mot de passe...' },
  PasswordUpdated:     { es: 'Contraseña actualizada',                    en: 'Password updated',                        fr: 'Mot de passe mis à jour' },
  ErrorChangingPassword: { es: 'Error al cambiar contraseña',             en: 'Error changing password',                 fr: 'Erreur lors du changement' },
  ConfirmDeleteUser:   { es: "¿Eliminar al usuario '{0}'?",              en: "Delete user '{0}'?",                       fr: "Supprimer l'utilisateur '{0}' ?" },
  DeletingUser:        { es: 'Eliminando usuario...',                     en: 'Deleting user...',                        fr: 'Suppression...' },
  UserDeleted:         { es: 'Usuario eliminado',                         en: 'User deleted',                            fr: 'Utilisateur supprimé' },
  ErrorDeletingUser:   { es: 'Error al eliminar usuario',                 en: 'Error deleting user',                     fr: 'Erreur lors de la suppression' },
  Updating:            { es: 'Actualizando...',                           en: 'Updating...',                             fr: 'Mise à jour...' },
  ProfileUpdated:      { es: 'Perfil actualizado',                        en: 'Profile updated',                         fr: 'Profil mis à jour' },
  ErrorUpdatingProfile:{ es: 'Error al actualizar perfil',                en: 'Error updating profile',                  fr: 'Erreur lors de la mise à jour' },
  UserInfoError:       { es: 'No se pudo obtener la información del usuario', en: 'Could not get user information',      fr: "Impossible d'obtenir les informations" },
  PasswordKey:         { es: 'Clave',                                     en: 'Key',                                     fr: 'Clé' },
  DeleteUserBtn:       { es: 'Eliminar',                                  en: 'Delete',                                  fr: 'Supprimer' },
  BackToRoutinesBtn:   { es: '← Volver a Rutinas',                       en: '← Back to Routines',                     fr: '← Retour aux routines' },

  // === Auto-update ===
  AppUpdated:    { es: 'Aplicación actualizada',                                  en: 'App Updated',                                            fr: 'Application mise à jour' },
  AppUpdatedMsg: { es: 'Hay una nueva versión disponible. ¿Reiniciar ahora?',     en: 'A new version is available. Restart now?',                fr: 'Une nouvelle version est disponible. Redémarrer maintenant ?' },
  UpdateNow:     { es: 'Reiniciar',                                               en: 'Restart',                                                fr: 'Redémarrer' },
  Later:         { es: 'Más tarde',                                               en: 'Later',                                                  fr: 'Plus tard' },

  // === Language ===
  Language:       { es: 'Idioma',              en: 'Language',          fr: 'Langue' },
  SelectLanguage: { es: 'Seleccionar idioma',  en: 'Select language',  fr: 'Sélectionner la langue' },
};

// DayOfWeek in .NET: Sunday=0 ... Saturday=6
const dayKeys = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

/**
 * Translate a key, with optional format args.
 * t('ErrorFmt', 'oops') => "Error: oops"
 */
export function t(key, ...args) {
  const entry = Strings[key];
  let val;
  if (entry) {
    val = entry[_lang] ?? entry['es'] ?? key;
  } else {
    val = key;
  }
  if (args.length > 0) {
    args.forEach((arg, i) => {
      val = val.replace(`{${i}}`, arg);
    });
  }
  return val;
}

/**
 * Get the translated day name.
 * @param {number} day — 0=Sunday .. 6=Saturday (matches .NET DayOfWeek)
 */
export function dayName(day) {
  const key = dayKeys[day];
  return key ? t(key) : String(day);
}

/**
 * Translate a muscle group given its Spanish name.
 */
export function muscleGroup(spanishName) {
  if (_lang === 'es') return spanishName;
  const key = `MG_${spanishName}`;
  const entry = Strings[key];
  if (entry && entry[_lang]) return entry[_lang];
  return spanishName;
}

/**
 * Set the active language and persist to localStorage.
 */
export function setLanguage(lang) {
  _lang = lang;
  localStorage.setItem('app_language', lang);
}

/**
 * Get the current language code.
 */
export function currentLanguage() {
  return _lang;
}

/**
 * Available language codes.
 */
export const availableLanguages = ['es', 'en', 'fr'];

/**
 * Display name for a language code.
 */
export function languageDisplayName(lang) {
  switch (lang) {
    case 'es': return 'Español';
    case 'en': return 'English';
    case 'fr': return 'Français';
    default: return lang;
  }
}

/**
 * Initialize: load language from localStorage.
 */
export function init() {
  _lang = localStorage.getItem('app_language') || 'es';
}
