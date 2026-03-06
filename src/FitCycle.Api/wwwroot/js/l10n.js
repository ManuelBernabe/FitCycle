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

  // === Superset ===
  Superset:       { es: 'Superserie',                       en: 'Superset',                  fr: 'Superserie' },
  LinkSuperset:   { es: 'Vincular superserie',              en: 'Link superset',             fr: 'Lier superserie' },
  UnlinkSuperset: { es: 'Desvincular',                      en: 'Unlink',                    fr: 'Délier' },
  SelectPair:          { es: 'Selecciona el ejercicio pareja',   en: 'Select pair exercise',      fr: "Sélectionner l'exercice partenaire" },
  NoExercisesAvailable: { es: 'No hay ejercicios disponibles', en: 'No exercises available',    fr: "Aucun exercice disponible" },

  // === Tempo / Grip / Notes ===
  Tempo:           { es: 'Tempo',             en: 'Tempo',            fr: 'Tempo' },
  TempoPositive:   { es: 'Fase positiva',     en: 'Positive phase',   fr: 'Phase positive' },
  TempoNegative:   { es: 'Fase negativa',     en: 'Negative phase',   fr: 'Phase négative' },
  Grip:            { es: 'Agarre',            en: 'Grip',             fr: 'Prise' },
  GripProne:       { es: 'Prono',             en: 'Prone',            fr: 'Pronation' },
  GripSupine:      { es: 'Supino',            en: 'Supine',           fr: 'Supination' },
  GripNeutral:     { es: 'Neutro',            en: 'Neutral',          fr: 'Neutre' },
  ExerciseNotes:   { es: 'Notas',             en: 'Notes',            fr: 'Notes' },

  // === PDF Import ===
  ImportPdf:       { es: 'Importar PDF',                          en: 'Import PDF',                      fr: 'Importer PDF' },
  SelectUser:      { es: 'Seleccionar usuario',                   en: 'Select user',                     fr: 'Sélectionner utilisateur' },
  SelectPdfFile:   { es: 'Seleccionar archivo PDF',               en: 'Select PDF file',                 fr: 'Sélectionner fichier PDF' },
  Importing:       { es: 'Importando rutinas...',                 en: 'Importing routines...',            fr: 'Importation des routines...' },
  ImportSuccess:   { es: 'Rutinas importadas correctamente',      en: 'Routines imported successfully',   fr: 'Routines importées avec succès' },
  ImportError:     { es: 'Error al importar',                     en: 'Import error',                     fr: "Erreur d'importation" },

  // === Copy Routines ===
  CopyRoutines:        { es: 'Copiar Rutinas',                          en: 'Copy Routines',                    fr: 'Copier Routines' },
  SourceUser:          { es: 'Usuario origen',                          en: 'Source User',                      fr: 'Utilisateur source' },
  TargetUser:          { es: 'Usuario destino',                         en: 'Target User',                      fr: 'Utilisateur cible' },
  CopyingRoutines:     { es: 'Copiando rutinas...',                     en: 'Copying routines...',              fr: 'Copie des routines...' },
  CopySuccess:         { es: 'Rutinas copiadas correctamente',          en: 'Routines copied successfully',     fr: 'Routines copiées avec succès' },
  SameUserError:       { es: 'El usuario origen y destino deben ser diferentes', en: 'Source and target users must be different', fr: 'Les utilisateurs source et cible doivent être différents' },
  ConfirmCopyRoutines: { es: '¿Copiar todas las rutinas? Las rutinas actuales del usuario destino serán reemplazadas.', en: 'Copy all routines? The target user current routines will be replaced.', fr: "Copier toutes les routines ? Les routines actuelles de l'utilisateur cible seront remplacées." },

  // === Impersonate ===
  LoginAs:           { es: 'Login',                                      en: 'Login',                             fr: 'Connexion' },
  ConfirmLoginAs:    { es: '¿Iniciar sesión como {0}? Se cerrará tu sesión actual.', en: 'Log in as {0}? Your current session will end.', fr: 'Se connecter en tant que {0} ? Votre session actuelle sera fermée.' },
  LeaveEmptyNoChange: { es: 'Dejar vacío para no cambiar',              en: 'Leave empty to keep current',       fr: 'Laisser vide pour ne pas changer' },
  DownloadDb:        { es: 'Descargar BD',                               en: 'Download DB',                       fr: 'Télécharger BD' },
  Downloading:       { es: 'Descargando...',                             en: 'Downloading...',                    fr: 'Téléchargement...' },

  // === Templates ===
  TabTemplates:          { es: 'Plantillas',                                 en: 'Templates',                         fr: 'Modèles' },
  RoutineTemplates:      { es: 'Plantillas de Rutinas',                      en: 'Routine Templates',                 fr: 'Modèles de Routines' },
  SaveTemplate:          { es: 'Guardar Plantilla',                          en: 'Save Template',                     fr: 'Enregistrer Modèle' },
  TemplateName:          { es: 'Nombre de la plantilla',                     en: 'Template name',                     fr: 'Nom du modèle' },
  TemplateDescription:   { es: 'Descripción (opcional)',                     en: 'Description (optional)',             fr: 'Description (optionnel)' },
  ApplyTemplate:         { es: 'Aplicar',                                    en: 'Apply',                             fr: 'Appliquer' },
  DeleteTemplate:        { es: 'Eliminar',                                   en: 'Delete',                            fr: 'Supprimer' },
  ConfirmDeleteTemplate: { es: '¿Eliminar esta plantilla?',                  en: 'Delete this template?',             fr: 'Supprimer ce modèle ?' },
  ConfirmApplyTemplate:  { es: '¿Aplicar esta plantilla a {0}? Sus rutinas actuales serán reemplazadas.', en: 'Apply this template to {0}? Their current routines will be replaced.', fr: 'Appliquer ce modèle à {0} ? Ses routines actuelles seront remplacées.' },
  SavingTemplate:        { es: 'Guardando...',                               en: 'Saving...',                         fr: 'Enregistrement...' },
  TemplateSaved:         { es: 'Plantilla guardada',                         en: 'Template saved',                    fr: 'Modèle enregistré' },
  TemplateApplied:       { es: 'Plantilla aplicada correctamente',           en: 'Template applied successfully',     fr: 'Modèle appliqué avec succès' },
  TemplateDeleted:       { es: 'Plantilla eliminada',                        en: 'Template deleted',                  fr: 'Modèle supprimé' },
  NoTemplates:           { es: 'No hay plantillas guardadas',                en: 'No saved templates',                fr: 'Aucun modèle enregistré' },
  ViewDetails:           { es: 'Ver',                                        en: 'View',                              fr: 'Voir' },
  SelectSourceUser:      { es: 'Seleccionar usuario origen',                 en: 'Select source user',                fr: "Sélectionner l'utilisateur source" },
  Exercises:             { es: 'ejercicios',                                 en: 'exercises',                         fr: 'exercices' },

  // === Activation ===
  CheckYourEmail:       { es: 'Revisa tu email',                                en: 'Check your email',                   fr: 'Vérifiez votre e-mail' },
  ActivationEmailSent:  { es: 'Te hemos enviado un email con un enlace para activar tu cuenta. Revisa tu bandeja de entrada (y spam).', en: 'We sent you an email with a link to activate your account. Check your inbox (and spam).', fr: "Nous vous avons envoyé un e-mail avec un lien pour activer votre compte. Vérifiez votre boîte de réception (et spam)." },
  ResendActivation:     { es: 'Reenviar email de activación',                   en: 'Resend activation email',             fr: "Renvoyer l'e-mail d'activation" },
  ActivationResent:     { es: 'Email de activación reenviado',                  en: 'Activation email resent',             fr: "E-mail d'activation renvoyé" },
  BackToLogin:          { es: 'Volver al login',                                en: 'Back to login',                      fr: 'Retour à la connexion' },
  Active:               { es: 'Activo',                                         en: 'Active',                             fr: 'Actif' },
  Inactive:             { es: 'Inactivo',                                       en: 'Inactive',                           fr: 'Inactif' },
  AccountNotActivated:  { es: 'Cuenta no activada',                             en: 'Account not activated',              fr: 'Compte non activé' },

  // === Password ===
  PwdMinLength:  { es: 'Mínimo 8 caracteres',             en: 'At least 8 characters',           fr: 'Au moins 8 caractères' },
  PwdUppercase:  { es: 'Una letra mayúscula',              en: 'One uppercase letter',            fr: 'Une lettre majuscule' },
  PwdLowercase:  { es: 'Una letra minúscula',              en: 'One lowercase letter',            fr: 'Une lettre minuscule' },
  PwdDigit:      { es: 'Un dígito',                        en: 'One digit',                       fr: 'Un chiffre' },
  PwdSpecial:    { es: 'Un carácter especial',             en: 'One special character',           fr: 'Un caractère spécial' },

  // === Measurements ===
  TabMeasurements:  { es: 'Medidas',                    en: 'Measurements',       fr: 'Mesures' },
  MyMeasurements:   { es: 'Mis Medidas',                en: 'My Measurements',    fr: 'Mes Mesures' },
  AddMeasurement:   { es: 'Registrar medida',           en: 'Add measurement',    fr: 'Ajouter mesure' },
  MeasWeight:       { es: 'Peso (kg)',                   en: 'Weight (kg)',         fr: 'Poids (kg)' },
  MeasHeight:       { es: 'Altura (cm)',                 en: 'Height (cm)',         fr: 'Taille (cm)' },
  MeasChest:        { es: 'Pecho (cm)',                  en: 'Chest (cm)',          fr: 'Poitrine (cm)' },
  MeasWaist:        { es: 'Cintura (cm)',                en: 'Waist (cm)',          fr: 'Taille (cm)' },
  MeasHips:         { es: 'Cadera (cm)',                 en: 'Hips (cm)',           fr: 'Hanches (cm)' },
  MeasBicepL:       { es: 'Bíceps izq (cm)',             en: 'Bicep L (cm)',        fr: 'Biceps G (cm)' },
  MeasBicepR:       { es: 'Bíceps der (cm)',             en: 'Bicep R (cm)',        fr: 'Biceps D (cm)' },
  MeasThighL:       { es: 'Muslo izq (cm)',              en: 'Thigh L (cm)',        fr: 'Cuisse G (cm)' },
  MeasThighR:       { es: 'Muslo der (cm)',              en: 'Thigh R (cm)',        fr: 'Cuisse D (cm)' },
  MeasCalfL:        { es: 'Gemelo izq (cm)',             en: 'Calf L (cm)',         fr: 'Mollet G (cm)' },
  MeasCalfR:        { es: 'Gemelo der (cm)',             en: 'Calf R (cm)',         fr: 'Mollet D (cm)' },
  MeasNeck:         { es: 'Cuello (cm)',                 en: 'Neck (cm)',           fr: 'Cou (cm)' },
  MeasBodyFat:      { es: 'Grasa corporal (%)',          en: 'Body fat (%)',        fr: 'Graisse (%)',  },
  MeasNotes:        { es: 'Notas',                       en: 'Notes',               fr: 'Notes' },
  NoMeasurements:   { es: 'Sin medidas registradas',     en: 'No measurements yet', fr: 'Aucune mesure' },
  MeasHistory:      { es: 'Historial de medidas',        en: 'Measurement history', fr: 'Historique des mesures' },
  MeasSelectFields: { es: 'Campos a registrar',           en: 'Fields to record',    fr: 'Champs à enregistrer' },
  MeasSaved:        { es: 'Medida guardada',             en: 'Measurement saved',   fr: 'Mesure enregistrée' },
  ConfirmDeleteMeas:{ es: '¿Eliminar esta medida?',      en: 'Delete this measurement?', fr: 'Supprimer cette mesure ?' },

  // === Auto-update ===
  AppUpdated:    { es: 'Aplicación actualizada',                                  en: 'App Updated',                                            fr: 'Application mise à jour' },
  AppUpdatedMsg: { es: 'Hay una nueva versión disponible. ¿Reiniciar ahora?',     en: 'A new version is available. Restart now?',                fr: 'Une nouvelle version est disponible. Redémarrer maintenant ?' },
  UpdateNow:     { es: 'Reiniciar',                                               en: 'Restart',                                                fr: 'Redémarrer' },
  Later:         { es: 'Más tarde',                                               en: 'Later',                                                  fr: 'Plus tard' },

  // === Language ===
  Language:       { es: 'Idioma',              en: 'Language',          fr: 'Langue' },
  SelectLanguage: { es: 'Seleccionar idioma',  en: 'Select language',  fr: 'Sélectionner la langue' },

  // === Home ===
  Welcome:         { es: 'Hola, {0}!',                         en: 'Hello, {0}!',                     fr: 'Bonjour, {0} !' },
  GoToRoutines:    { es: 'Ir a mis rutinas',                    en: 'Go to my routines',               fr: 'Aller à mes routines' },
  ViewStats:       { es: 'Ver estadísticas',                    en: 'View statistics',                 fr: 'Voir les statistiques' },
  LastWorkout:     { es: 'Último entreno',                      en: 'Last workout',                    fr: 'Dernier entraînement' },
  Streak:          { es: 'Racha',                               en: 'Streak',                          fr: 'Série' },
  Today:           { es: 'Hoy',                                 en: 'Today',                           fr: "Aujourd'hui" },
  Yesterday:       { es: 'Ayer',                                en: 'Yesterday',                       fr: 'Hier' },

  // === EditDay extras ===
  ShowSets:        { es: 'Ver series',                          en: 'Show sets',                       fr: 'Voir séries' },
  HideSets:        { es: 'Ocultar series',                      en: 'Hide sets',                       fr: 'Masquer séries' },

  // === Templates extras ===
  Days:            { es: 'días',                                en: 'days',                            fr: 'jours' },

  // === Measurements extras ===
  BMI:             { es: 'IMC',                                 en: 'BMI',                             fr: 'IMC' },
  BMIValue:        { es: 'Tu IMC: {0}',                         en: 'Your BMI: {0}',                   fr: 'Votre IMC : {0}' },
  Underweight:     { es: 'Bajo peso',                           en: 'Underweight',                     fr: 'Insuffisance pondérale' },
  NormalWeight:    { es: 'Peso normal',                         en: 'Normal weight',                   fr: 'Poids normal' },
  Overweight:      { es: 'Sobrepeso',                           en: 'Overweight',                      fr: 'Surpoids' },
  Obese:           { es: 'Obesidad',                            en: 'Obese',                           fr: 'Obésité' },
  MeasTrend:       { es: 'Tendencia',                           en: 'Trend',                           fr: 'Tendance' },
  SelectField:     { es: 'Seleccionar campo',                   en: 'Select field',                    fr: 'Sélectionner champ' },

  // === Summary extras ===
  NewPR:           { es: 'Nuevo récord personal!',              en: 'New personal record!',            fr: 'Nouveau record personnel !' },
  PRExercise:      { es: '{0}: {1} kg',                         en: '{0}: {1} kg',                     fr: '{0} : {1} kg' },

  // === Admin Panel ===
  AdminPanel:          { es: 'Panel Admin',                              en: 'Admin Panel',                       fr: 'Panneau Admin' },
  SqlConsole:          { es: 'Consola SQL',                              en: 'SQL Console',                       fr: 'Console SQL' },
  ExecuteQuery:        { es: 'Ejecutar',                                 en: 'Execute',                           fr: 'Exécuter' },
  NoResults:           { es: 'Sin resultados',                           en: 'No results',                        fr: 'Aucun résultat' },
  Rows:                { es: 'filas',                                    en: 'rows',                              fr: 'lignes' },
  Truncated:           { es: 'truncado a 500',                           en: 'truncated to 500',                  fr: 'tronqué à 500' },
  Backups:             { es: 'Backups',                                  en: 'Backups',                           fr: 'Sauvegardes' },
  CreateBackup:        { es: 'Crear Backup',                             en: 'Create Backup',                     fr: 'Créer Sauvegarde' },
  CreatingBackup:      { es: 'Creando backup...',                        en: 'Creating backup...',                fr: 'Création...' },
  BackupCreated:       { es: 'Backup creado',                            en: 'Backup created',                    fr: 'Sauvegarde créée' },
  RestoreBackup:       { es: 'Restaurar',                                en: 'Restore',                           fr: 'Restaurer' },
  Restoring:           { es: 'Restaurando...',                           en: 'Restoring...',                      fr: 'Restauration...' },
  BackupRestored:      { es: 'BD restaurada correctamente',              en: 'Database restored successfully',    fr: 'Base de données restaurée' },
  ConfirmRestore:      { es: '¿Restaurar la BD desde {0}? Se creará un backup previo automáticamente.', en: 'Restore DB from {0}? A pre-restore backup will be created automatically.', fr: 'Restaurer la BD depuis {0} ? Une sauvegarde préalable sera créée automatiquement.' },
  ConfirmRestoreDouble:{ es: '¿Estás seguro? Esta acción reemplazará toda la base de datos.', en: 'Are you sure? This will replace the entire database.', fr: 'Êtes-vous sûr ? Cela remplacera toute la base de données.' },
  NoBackups:           { es: 'No hay backups disponibles',               en: 'No backups available',              fr: 'Aucune sauvegarde disponible' },
  Download:            { es: 'Descargar',                                en: 'Download',                          fr: 'Télécharger' },

  // === Tutorial / User Guide ===
  UserGuide:           { es: 'Guía de uso',                              en: 'User Guide',                        fr: "Guide d'utilisation" },
  TutorialTitle:       { es: 'Guía de uso de FitCycle',                  en: 'FitCycle User Guide',               fr: "Guide d'utilisation FitCycle" },
  TutorialSubtitle:    { es: 'Aprende a sacar el máximo partido a la app', en: 'Learn how to get the most out of the app', fr: "Apprenez à tirer le meilleur parti de l'app" },
  TutStep:             { es: 'Paso',                                     en: 'Step',                              fr: 'Étape' },
  BackToHome:          { es: 'Volver al inicio',                         en: 'Back to Home',                      fr: "Retour à l'accueil" },

  TutWelcomeTitle:     { es: 'Bienvenido a FitCycle',                    en: 'Welcome to FitCycle',               fr: 'Bienvenue sur FitCycle' },
  TutWelcomeDesc:      { es: 'FitCycle es tu compañero de entrenamiento personal. Organiza tu rutina semanal, registra cada serie con peso y repeticiones, y sigue tu progreso con estadísticas detalladas. Funciona sin conexión gracias al modo offline.', en: 'FitCycle is your personal workout companion. Organize your weekly routine, log every set with weight and reps, and track your progress with detailed stats. Works offline too.', fr: "FitCycle est votre compagnon d'entraînement personnel. Organisez votre routine hebdomadaire, enregistrez chaque série avec poids et répétitions, et suivez vos progrès avec des statistiques détaillées. Fonctionne aussi hors ligne." },
  TutWelcomeMockup:    { es: 'Tu entrenamiento, tu ritmo',               en: 'Your workout, your rhythm',         fr: 'Votre entraînement, votre rythme' },

  TutRegisterTitle:    { es: 'Registro y activación',                    en: 'Registration & activation',         fr: "Inscription et activation" },
  TutRegisterDesc:     { es: 'Crea tu cuenta con usuario, email y contraseña. Recibirás un email para activar tu cuenta. Haz clic en el enlace del email y ya podrás iniciar sesión.', en: 'Create your account with username, email and password. You will receive an activation email. Click the link and you can sign in.', fr: "Créez votre compte avec nom d'utilisateur, email et mot de passe. Vous recevrez un email d'activation. Cliquez sur le lien et connectez-vous." },
  TutRegisterTip:      { es: 'La contraseña debe tener al menos 8 caracteres, mayúscula, minúscula, número y carácter especial.', en: 'Password must have at least 8 characters, uppercase, lowercase, number and special character.', fr: 'Le mot de passe doit contenir au moins 8 caractères, une majuscule, une minuscule, un chiffre et un caractère spécial.' },

  TutHomeTitle:        { es: 'Pantalla de inicio',                       en: 'Home Screen',                       fr: "Écran d'accueil" },
  TutHomeDesc:         { es: 'Desde el inicio ves tus stats rápidos: entrenamientos totales, racha actual y último entrenamiento. Accede directamente a tus rutinas o estadísticas.', en: 'From the home screen you see quick stats: total workouts, current streak and last workout. Jump straight to routines or stats.', fr: "Depuis l'accueil vous voyez vos stats rapides : entraînements totaux, série en cours et dernier entraînement. Accédez directement aux routines ou statistiques." },

  TutRoutinesTitle:    { es: 'Crea tu rutina semanal',                   en: 'Create your weekly routine',        fr: 'Créez votre routine hebdomadaire' },
  TutRoutinesDesc:     { es: 'Organiza tu semana asignando grupos musculares a cada día. Puedes configurar los 7 días de la semana y dejar días de descanso vacíos.', en: 'Organize your week by assigning muscle groups to each day. Configure all 7 days and leave rest days empty.', fr: "Organisez votre semaine en assignant des groupes musculaires à chaque jour. Configurez les 7 jours et laissez les jours de repos vides." },
  TutRoutinesTip:      { es: 'Toca un día para editarlo. Puedes añadir varios grupos musculares al mismo día.', en: 'Tap a day to edit it. You can add multiple muscle groups to the same day.', fr: "Appuyez sur un jour pour le modifier. Vous pouvez ajouter plusieurs groupes musculaires au même jour." },
  TutChest:            { es: 'Pecho',                                    en: 'Chest',                             fr: 'Poitrine' },
  TutTriceps:          { es: 'Tríceps',                                  en: 'Triceps',                           fr: 'Triceps' },
  TutBack:             { es: 'Espalda',                                  en: 'Back',                              fr: 'Dos' },
  TutBiceps:           { es: 'Bíceps',                                   en: 'Biceps',                            fr: 'Biceps' },
  TutLegs:             { es: 'Piernas',                                  en: 'Legs',                              fr: 'Jambes' },
  TutGlutes:           { es: 'Glúteos',                                  en: 'Glutes',                            fr: 'Fessiers' },

  TutExercisesTitle:   { es: 'Configura tus ejercicios',                 en: 'Configure your exercises',          fr: 'Configurez vos exercices' },
  TutExercisesDesc:    { es: 'Para cada ejercicio, configura el número de series con repeticiones y peso individual por serie. Opcionalmente añade tempo, agrupa en supersets o añade notas.', en: 'For each exercise, set the number of sets with individual reps and weight per set. Optionally add tempo, group into supersets or add notes.', fr: "Pour chaque exercice, définissez le nombre de séries avec répétitions et poids individuels par série. Ajoutez optionnellement un tempo, groupez en supersets ou ajoutez des notes." },
  TutExercisesTip:     { es: 'Cada serie puede tener su propio peso y repeticiones. Ideal para pirámides o drop sets.', en: 'Each set can have its own weight and reps. Ideal for pyramids or drop sets.', fr: 'Chaque série peut avoir son propre poids et ses répétitions. Idéal pour les pyramides ou drop sets.' },

  TutWorkoutTitle:     { es: 'Entrena',                                  en: 'Work Out',                          fr: 'Entraînez-vous' },
  TutWorkoutDesc:      { es: 'Inicia el entrenamiento del día y registra cada serie. Ajusta repeticiones y peso en tiempo real. Un temporizador de descanso de 90 segundos te ayuda a mantener el ritmo.', en: 'Start your daily workout and log each set. Adjust reps and weight in real time. A 90-second rest timer helps you keep pace.', fr: "Commencez votre entraînement du jour et enregistrez chaque série. Ajustez les répétitions et le poids en temps réel. Un minuteur de repos de 90 secondes vous aide à garder le rythme." },
  TutCurrentExercise:  { es: 'Ejercicio actual',                         en: 'Current exercise',                  fr: 'Exercice en cours' },
  TutLogSet:           { es: 'Registrar serie',                          en: 'Log set',                           fr: 'Enregistrer série' },
  TutRestTimer:        { es: 'Descanso',                                 en: 'Rest',                              fr: 'Repos' },

  TutSummaryTitle:     { es: 'Resumen del entrenamiento',                en: 'Workout Summary',                   fr: "Résumé de l'entraînement" },
  TutSummaryDesc:      { es: 'Al terminar, ve un resumen completo: duración, ejercicios, series totales y récords personales. También un desglose por ejercicio con cada serie registrada.', en: 'After finishing, see a full summary: duration, exercises, total sets and personal records. Plus a per-exercise breakdown with every logged set.', fr: "Après l'entraînement, consultez un résumé complet : durée, exercices, séries totales et records personnels. Plus un détail par exercice avec chaque série enregistrée." },
  TutWorkoutComplete:  { es: '¡Entrenamiento completado!',               en: 'Workout Complete!',                 fr: 'Entraînement terminé !' },

  TutStatsTitle:       { es: 'Estadísticas',                             en: 'Statistics',                        fr: 'Statistiques' },
  TutStatsDesc:        { es: 'Consulta tus estadísticas generales: entrenamientos totales, series, repeticiones y racha. Visualiza tu actividad semanal con gráficos de barras y revisa el historial completo.', en: 'View your overall stats: total workouts, sets, reps and streak. See your weekly activity with bar charts and review your full history.', fr: "Consultez vos statistiques globales : entraînements totaux, séries, répétitions et série. Visualisez votre activité hebdomadaire avec des graphiques à barres et consultez l'historique complet." },
  TutWeeklyChart:      { es: 'Actividad semanal',                        en: 'Weekly activity',                   fr: 'Activité hebdomadaire' },

  TutMeasurementsTitle:{ es: 'Medidas corporales',                       en: 'Body Measurements',                 fr: 'Mensurations corporelles' },
  TutMeasurementsDesc: { es: 'Registra tus medidas: peso, altura, pecho, cintura, caderas, bíceps, muslos, gemelos, cuello y porcentaje de grasa. La app calcula tu IMC automáticamente y muestra gráficas de evolución.', en: 'Log your measurements: weight, height, chest, waist, hips, biceps, thighs, calves, neck and body fat. The app calculates BMI automatically and shows progress charts.', fr: "Enregistrez vos mensurations : poids, taille, poitrine, tour de taille, hanches, biceps, cuisses, mollets, cou et masse grasse. L'app calcule l'IMC automatiquement et affiche des graphiques d'évolution." },
  TutMeasurementsTip:  { es: 'Añade medidas regularmente para ver tu evolución',  en: 'Add measurements regularly to see your progress', fr: 'Ajoutez des mesures régulièrement pour voir votre évolution' },

  TutAccountTitle:     { es: 'Cuenta y ajustes',                         en: 'Account & Settings',                fr: 'Compte et paramètres' },
  TutAccountDesc:      { es: 'Edita tu perfil (nombre, email), cambia el idioma de la app entre español, inglés y francés, y cierra sesión cuando quieras. Los administradores también tienen acceso al panel de control.', en: 'Edit your profile (name, email), change the app language between Spanish, English and French, and log out anytime. Admins also have access to the control panel.', fr: "Modifiez votre profil (nom, email), changez la langue de l'app entre espagnol, anglais et français, et déconnectez-vous à tout moment. Les administrateurs ont aussi accès au panneau de contrôle." },
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
