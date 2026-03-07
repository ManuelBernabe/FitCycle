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
  TempoAsc:        { es: 'Asc',               en: 'Asc',              fr: 'Asc' },
  TempoDesc:       { es: 'Desc',              en: 'Desc',             fr: 'Desc' },
  TempoAscFull:    { es: 'Ascendente (concéntrica)', en: 'Ascending (concentric)', fr: 'Ascendant (concentrique)' },
  TempoDescFull:   { es: 'Descendente (excéntrica)', en: 'Descending (eccentric)', fr: 'Descendant (excentrique)' },
  Grip:            { es: 'Agarre',            en: 'Grip',             fr: 'Prise' },
  GripProne:       { es: 'Prono',             en: 'Prone',            fr: 'Pronation' },
  GripSupine:      { es: 'Supino',            en: 'Supine',           fr: 'Supination' },
  GripNeutral:     { es: 'Neutro',            en: 'Neutral',          fr: 'Neutre' },
  GripProno:       { es: 'Prono',             en: 'Prone',            fr: 'Pronation' },
  GripSupino:      { es: 'Supino',            en: 'Supine',           fr: 'Supination' },
  GripNeutro:      { es: 'Neutro',            en: 'Neutral',          fr: 'Neutre' },
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

  // === Theme ===
  Theme:      { es: 'Tema',        en: 'Theme',      fr: 'Thème' },
  ThemeAuto:  { es: 'Auto',        en: 'Auto',        fr: 'Auto' },
  ThemeLight: { es: 'Claro',       en: 'Light',       fr: 'Clair' },
  ThemeDark:  { es: 'Oscuro',      en: 'Dark',        fr: 'Sombre' },

  // === 2FA ===
  TwoFactorAuth:         { es: 'Autenticación en dos pasos',      en: 'Two-factor authentication',     fr: 'Authentification à deux facteurs' },
  Enable2FA:             { es: 'Activar 2FA',                     en: 'Enable 2FA',                    fr: 'Activer 2FA' },
  Disable2FA:            { es: 'Desactivar 2FA',                  en: 'Disable 2FA',                   fr: 'Désactiver 2FA' },
  TwoFAEnabled:          { es: '2FA activado',                    en: '2FA enabled',                   fr: '2FA activé' },
  TwoFADisabled:         { es: '2FA desactivado',                 en: '2FA disabled',                  fr: '2FA désactivé' },
  ScanQRCode:            { es: 'Escanea este código QR con tu app authenticator (Google Authenticator, Authy, etc.)',
                           en: 'Scan this QR code with your authenticator app (Google Authenticator, Authy, etc.)',
                           fr: 'Scannez ce QR code avec votre application d\'authentification (Google Authenticator, Authy, etc.)' },
  ManualEntry:           { es: 'O introduce este código manualmente:',
                           en: 'Or enter this code manually:',
                           fr: 'Ou entrez ce code manuellement :' },
  VerificationCode:      { es: 'Código de verificación',          en: 'Verification code',             fr: 'Code de vérification' },
  EnterCodeFromApp:      { es: 'Introduce el código de 6 dígitos de tu app',
                           en: 'Enter the 6-digit code from your app',
                           fr: 'Entrez le code à 6 chiffres de votre application' },
  RecoveryCodes:         { es: 'Códigos de recuperación',         en: 'Recovery codes',                fr: 'Codes de récupération' },
  RecoveryCodesWarning:  { es: 'Guarda estos códigos en un lugar seguro. Cada código solo se puede usar una vez. Si pierdes acceso a tu app authenticator, podrás usar estos códigos para iniciar sesión.',
                           en: 'Save these codes in a safe place. Each code can only be used once. If you lose access to your authenticator app, you can use these codes to sign in.',
                           fr: 'Conservez ces codes dans un endroit sûr. Chaque code ne peut être utilisé qu\'une seule fois. Si vous perdez l\'accès à votre application d\'authentification, vous pouvez utiliser ces codes pour vous connecter.' },
  UseRecoveryCode:       { es: 'Usar código de recuperación',     en: 'Use recovery code',             fr: 'Utiliser un code de récupération' },
  InvalidCode:           { es: 'Código inválido',                 en: 'Invalid code',                  fr: 'Code invalide' },
  EnterPasswordToDisable:{ es: 'Introduce tu contraseña para desactivar 2FA',
                           en: 'Enter your password to disable 2FA',
                           fr: 'Entrez votre mot de passe pour désactiver 2FA' },
  TwoFAVerification:     { es: 'Verificación en dos pasos',       en: 'Two-step verification',         fr: 'Vérification en deux étapes' },
  CopiedToClipboard:     { es: 'Copiado al portapapeles',         en: 'Copied to clipboard',           fr: 'Copié dans le presse-papiers' },
  Verify:                { es: 'Verificar',                      en: 'Verify',                        fr: 'Vérifier' },
  Copy:                  { es: 'Copiar',                         en: 'Copy',                          fr: 'Copier' },

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
  TutTOC:              { es: 'Índice',                                   en: 'Contents',                          fr: 'Sommaire' },

  // Welcome
  TutWelcomeTitle:     { es: 'Bienvenido a FitCycle',                    en: 'Welcome to FitCycle',               fr: 'Bienvenue sur FitCycle' },
  TutWelcomeDesc:      { es: 'FitCycle es tu compañero de entrenamiento personal. Organiza tu rutina semanal, registra cada serie con peso y repeticiones, y sigue tu progreso con estadísticas detalladas.', en: 'FitCycle is your personal workout companion. Organize your weekly routine, log every set with weight and reps, and track your progress with detailed stats.', fr: "FitCycle est votre compagnon d'entraînement personnel. Organisez votre routine hebdomadaire, enregistrez chaque série et suivez vos progrès." },
  TutWelcomeMockup:    { es: 'Tu entrenamiento, tu ritmo',               en: 'Your workout, your rhythm',         fr: 'Votre entraînement, votre rythme' },
  TutFeatureRoutines:  { es: 'Rutinas semanales',                        en: 'Weekly routines',                   fr: 'Routines hebdomadaires' },
  TutFeatureWorkouts:  { es: 'Tracking de series',                       en: 'Set tracking',                      fr: 'Suivi des séries' },
  TutFeatureStats:     { es: 'Estadísticas',                             en: 'Statistics',                        fr: 'Statistiques' },
  TutFeatureMeasurements: { es: 'Medidas corporales',                    en: 'Body measurements',                 fr: 'Mensurations' },
  TutFeatureMultilang: { es: '3 idiomas',                                en: '3 languages',                       fr: '3 langues' },
  TutFeatureOffline:   { es: 'Modo offline',                             en: 'Offline mode',                      fr: 'Mode hors ligne' },

  // Register
  TutRegisterTitle:    { es: 'Registro y activación',                    en: 'Registration & activation',         fr: "Inscription et activation" },
  TutRegisterDesc:     { es: 'Crea tu cuenta con usuario, email y contraseña. Recibirás un email para activar tu cuenta. Haz clic en el enlace y ya podrás iniciar sesión.', en: 'Create your account with username, email and password. You will receive an activation email. Click the link and you can sign in.', fr: "Créez votre compte avec nom d'utilisateur, email et mot de passe. Vous recevrez un email d'activation." },
  TutRegisterTip:      { es: 'La contraseña debe tener al menos 8 caracteres, mayúscula, minúscula, número y carácter especial.', en: 'Password must have at least 8 characters, uppercase, lowercase, number and special character.', fr: 'Le mot de passe doit contenir au moins 8 caractères, majuscule, minuscule, chiffre et caractère spécial.' },
  TutActivationFlow:   { es: 'Haz clic en el enlace del email para activar tu cuenta. Si no lo recibes, usa el botón "Reenviar".', en: 'Click the email link to activate. If you don\'t receive it, use the "Resend" button.', fr: 'Cliquez sur le lien pour activer. Si vous ne le recevez pas, utilisez le bouton "Renvoyer".' },

  // Home
  TutHomeTitle:        { es: 'Pantalla de inicio',                       en: 'Home Screen',                       fr: "Écran d'accueil" },
  TutHomeDesc:         { es: 'Desde el inicio ves tus stats rápidos: entrenamientos totales, racha actual y último entrenamiento. Accede directamente a tus rutinas, estadísticas o esta guía.', en: 'From the home screen you see quick stats: total workouts, current streak and last workout. Jump straight to routines, stats or this guide.', fr: "Depuis l'accueil : stats rapides, accès direct aux routines, statistiques ou ce guide." },

  // Routines
  TutRoutinesTitle:    { es: 'Crea tu rutina semanal',                   en: 'Create your weekly routine',        fr: 'Créez votre routine hebdomadaire' },
  TutRoutinesDesc:     { es: 'Organiza tu semana asignando grupos musculares a cada día. Configura los 7 días y deja los días de descanso vacíos. Toca cualquier día para editarlo.', en: 'Organize your week by assigning muscle groups to each day. Configure all 7 days and leave rest days empty. Tap any day to edit.', fr: "Organisez votre semaine en assignant des groupes musculaires. Configurez les 7 jours, laissez les jours de repos vides." },
  TutRoutinesTip:      { es: 'Puedes añadir varios grupos musculares al mismo día para entrenamientos combinados.', en: 'You can add multiple muscle groups to the same day for combined workouts.', fr: "Vous pouvez ajouter plusieurs groupes musculaires au même jour." },
  TutRoutinesTip2:     { es: 'Los días sin grupos musculares se muestran como días de descanso.', en: 'Days without muscle groups show as rest days.', fr: 'Les jours sans groupes musculaires sont des jours de repos.' },
  TutRestDay:          { es: 'Día de descanso',                          en: 'Rest day',                          fr: 'Jour de repos' },
  TutChest:            { es: 'Pecho',     en: 'Chest',    fr: 'Poitrine' },
  TutTriceps:          { es: 'Tríceps',   en: 'Triceps',  fr: 'Triceps' },
  TutBack:             { es: 'Espalda',   en: 'Back',     fr: 'Dos' },
  TutBiceps:           { es: 'Bíceps',    en: 'Biceps',   fr: 'Biceps' },
  TutLegs:             { es: 'Piernas',   en: 'Legs',     fr: 'Jambes' },
  TutGlutes:           { es: 'Glúteos',   en: 'Glutes',   fr: 'Fessiers' },

  // Exercises
  TutExercisesTitle:   { es: 'Configura tus ejercicios',                 en: 'Configure your exercises',          fr: 'Configurez vos exercices' },
  TutExercisesDesc:    { es: 'Para cada ejercicio configura series individuales con repeticiones y peso. Añade opciones avanzadas: tempo (velocidad de ejecución), tipo de agarre, agrupación en supersets y notas. También puedes crear ejercicios personalizados.', en: 'For each exercise set individual sets with reps and weight. Add advanced options: tempo (execution speed), grip type, superset grouping and notes. You can also create custom exercises.', fr: "Pour chaque exercice, configurez des séries individuelles. Options avancées : tempo, type de prise, supersets et notes. Créez aussi des exercices personnalisés." },
  TutExercisesTip:     { es: 'Cada serie puede tener su propio peso y repeticiones. Ideal para pirámides o drop sets.', en: 'Each set can have its own weight and reps. Ideal for pyramids or drop sets.', fr: 'Chaque série peut avoir son propre poids. Idéal pour les pyramides ou drop sets.' },
  TutExercisesTip2:    { es: 'El tempo define la velocidad: excéntrica (bajar) y concéntrica (subir). Ej: 3↓ 1⏸ 2↑', en: 'Tempo defines speed: eccentric (lower) and concentric (lift). E.g. 3↓ 1⏸ 2↑', fr: 'Le tempo définit la vitesse : excentrique (descendre) et concentrique (monter).' },
  TutExercisesTip3:    { es: 'Usa supersets para agrupar dos ejercicios que se alternan sin descanso entre ellos.', en: 'Use supersets to pair two exercises that alternate with no rest between them.', fr: 'Utilisez les supersets pour alterner deux exercices sans repos entre eux.' },
  TutExercisesTip4:    { es: 'El tempo y agarre se configuran por serie individual. Al importar un PDF, se extraen automáticamente.', en: 'Tempo and grip are configured per individual set. When importing a PDF, they are extracted automatically.', fr: 'Le tempo et la prise sont configurés par série. Lors de l\'import PDF, ils sont extraits automatiquement.' },
  TutTempoExplain:     { es: 'Velocidad de cada fase del movimiento', en: 'Speed of each movement phase', fr: 'Vitesse de chaque phase du mouvement' },
  TutGripExplain:      { es: 'Tipo de agarre para el ejercicio', en: 'Grip type for the exercise', fr: "Type de prise pour l'exercice" },
  TutGrip:             { es: 'Agarre',    en: 'Grip',     fr: 'Prise' },
  TutGripProne:        { es: 'Prono',     en: 'Prone',    fr: 'Pronation' },
  TutExampleNote:      { es: 'Mantener codos pegados al cuerpo', en: 'Keep elbows close to body', fr: 'Garder les coudes près du corps' },

  // Workout
  TutWorkoutTitle:     { es: 'Entrena',                                  en: 'Work Out',                          fr: 'Entraînez-vous' },
  TutWorkoutDesc:      { es: 'Inicia el entrenamiento del día y registra cada serie en tiempo real. Ajusta repeticiones y peso, ve el tempo y agarre configurados, y usa el temporizador de descanso con alerta sonora.', en: 'Start your daily workout and log each set in real time. Adjust reps and weight, see configured tempo and grip, and use the rest timer with audio alert.', fr: "Commencez l'entraînement du jour. Ajustez répétitions et poids, voyez le tempo et la prise, utilisez le minuteur avec alerte sonore." },
  TutCurrentExercise:  { es: 'Ejercicio',                                en: 'Exercise',                          fr: 'Exercice' },
  TutLogSet:           { es: 'Registrar serie',                          en: 'Log set',                           fr: 'Enregistrer série' },
  TutRestTimer:        { es: 'Descanso',                                 en: 'Rest',                              fr: 'Repos' },
  TutWorkoutTip:       { es: 'El peso se rellena automáticamente con el de la serie anterior si no lo cambias.', en: 'Weight auto-fills from the previous set if you don\'t change it.', fr: 'Le poids se remplit automatiquement depuis la série précédente.' },
  TutWorkoutTip2:      { es: 'Usa la lista de ejercicios (arriba) para saltar directamente a cualquier ejercicio.', en: 'Use the exercise list (top) to jump directly to any exercise.', fr: "Utilisez la liste d'exercices pour sauter directement à un exercice." },
  TutWorkoutTip3:      { es: 'En supersets, la app alterna automáticamente entre los ejercicios emparejados.', en: 'In supersets, the app automatically alternates between paired exercises.', fr: "En supersets, l'app alterne automatiquement entre les exercices." },

  // Summary
  TutSummaryTitle:     { es: 'Resumen del entrenamiento',                en: 'Workout Summary',                   fr: "Résumé de l'entraînement" },
  TutSummaryDesc:      { es: 'Al terminar, ve un resumen completo con duración, ejercicios, series y récords personales. Incluye un desglose detallado por ejercicio mostrando cada serie con su peso.', en: 'After finishing, see a full summary: duration, exercises, sets and personal records. Includes a detailed per-exercise breakdown showing each set with weight.', fr: "Résumé complet : durée, exercices, séries et records personnels. Détail par exercice avec chaque série." },
  TutWorkoutComplete:  { es: '¡Entrenamiento completado!',               en: 'Workout Complete!',                 fr: 'Entraînement terminé !' },
  TutExerciseBreakdown:{ es: 'Desglose por ejercicio',                   en: 'Exercise breakdown',                fr: 'Détail par exercice' },

  // Stats
  TutStatsTitle:       { es: 'Estadísticas y progreso',                  en: 'Statistics & Progress',             fr: 'Statistiques et progrès' },
  TutStatsDesc:        { es: 'Consulta tus estadísticas: entrenamientos, series, repeticiones y racha. Gráficos de actividad semanal, ejercicios más frecuentes y progresión de peso por ejercicio. Historial completo con detalle expandible por serie.', en: 'View your stats: workouts, sets, reps and streak. Weekly activity charts, most frequent exercises and weight progression per exercise. Full history with expandable set details.', fr: "Stats globales, graphiques d'activité hebdomadaire, exercices fréquents et progression de poids. Historique complet avec détails par série." },
  TutWeeklyChart:      { es: 'Actividad semanal',                        en: 'Weekly activity',                   fr: 'Activité hebdomadaire' },
  TutTopExercises:     { es: 'Ejercicios más frecuentes',                en: 'Most frequent exercises',           fr: 'Exercices les plus fréquents' },
  TutWeightProgression:{ es: 'Progresión de peso (toca un ejercicio)',    en: 'Weight progression (tap an exercise)', fr: 'Progression de poids (appuyez sur un exercice)' },
  TutStatsTip:         { es: 'Toca un ejercicio para ver tu progresión de peso a lo largo del tiempo.',  en: 'Tap an exercise to see your weight progression over time.', fr: "Appuyez sur un exercice pour voir votre progression de poids." },
  TutStatsTip2:        { es: 'El historial muestra tus últimos 10 entrenamientos. Toca uno para ver el detalle serie a serie.', en: 'History shows your last 10 workouts. Tap one to see set-by-set details.', fr: "L'historique montre vos 10 derniers entraînements. Appuyez pour voir le détail." },
  Reps:                { es: 'Reps',                                     en: 'Reps',                              fr: 'Reps' },

  // Measurements
  TutMeasurementsTitle:{ es: 'Medidas corporales',                       en: 'Body Measurements',                 fr: 'Mensurations corporelles' },
  TutMeasurementsDesc: { es: 'Registra hasta 13 medidas diferentes: peso, altura, pecho, cintura, caderas, bíceps (L/R), muslos (L/R), gemelos (L/R), cuello y grasa corporal. Cálculo automático de IMC con categorías coloreadas y gráficas de tendencia.', en: 'Track up to 13 measurements: weight, height, chest, waist, hips, biceps (L/R), thighs (L/R), calves (L/R), neck and body fat. Auto BMI calculation with color-coded categories and trend charts.', fr: "Suivez jusqu'à 13 mensurations avec calcul IMC automatique, catégories colorées et graphiques de tendance." },
  TutMeasurementsTip:  { es: 'Añade medidas regularmente para ver tu evolución en las gráficas de tendencia.', en: 'Add measurements regularly to see your progress in trend charts.', fr: 'Ajoutez des mesures régulièrement pour voir votre évolution.' },
  TutMeasurementsTip2: { es: 'Los valores se pre-rellenan con tu última medición. Selecciona qué campos quieres trackear.', en: 'Values pre-fill from your last measurement. Select which fields to track.', fr: 'Les valeurs se pré-remplissent depuis votre dernière mesure.' },
  TutBMINormal:        { es: 'Peso normal (18.5-24.9)',                  en: 'Normal weight (18.5-24.9)',         fr: 'Poids normal (18.5-24.9)' },

  // Account
  TutAccountTitle:     { es: 'Cuenta y ajustes',                         en: 'Account & Settings',                fr: 'Compte et paramètres' },
  TutAccountDesc:      { es: 'Edita tu perfil (nombre, email), cambia tu contraseña, activa la autenticación en dos pasos (2FA), selecciona idioma y tema, y cierra sesión.', en: 'Edit your profile (name, email), change your password, enable two-factor authentication (2FA), select language and theme, and log out.', fr: "Modifiez votre profil, changez votre mot de passe, activez l'authentification à deux facteurs (2FA), sélectionnez la langue et le thème, et déconnectez-vous." },
  TutChangePassword:   { es: 'Cambiar contraseña',                       en: 'Change password',                   fr: 'Changer le mot de passe' },
  Tut2FADesc:          { es: 'Protege tu cuenta con código temporal',    en: 'Protect your account with a temporary code', fr: 'Protégez votre compte avec un code temporaire' },
  Tut2FAFlow:          { es: 'Añade una capa extra de seguridad. Al activar 2FA, cada vez que inicies sesión necesitarás un código de 6 dígitos generado por tu app authenticator (Google Authenticator, Authy, etc.).',
                         en: 'Add an extra layer of security. When 2FA is enabled, you will need a 6-digit code from your authenticator app (Google Authenticator, Authy, etc.) every time you log in.',
                         fr: 'Ajoutez une couche de sécurité supplémentaire. Avec le 2FA activé, vous aurez besoin d\'un code à 6 chiffres de votre application d\'authentification à chaque connexion.' },
  Tut2FAStep1:         { es: 'Escanear QR',       en: 'Scan QR',         fr: 'Scanner QR' },
  Tut2FAStep2:         { es: 'Verificar código',   en: 'Verify code',     fr: 'Vérifier code' },
  Tut2FAStep3:         { es: '2FA activado',        en: '2FA enabled',     fr: '2FA activé' },
  Tut2FATip:           { es: 'Al activar 2FA recibirás 8 códigos de recuperación. Guárdalos en un lugar seguro por si pierdes acceso a tu app.',
                         en: 'When enabling 2FA you will receive 8 recovery codes. Save them in a safe place in case you lose access to your app.',
                         fr: 'En activant le 2FA, vous recevrez 8 codes de récupération. Conservez-les en lieu sûr.' },
  Tut2FATip2:          { es: 'Para desactivar 2FA necesitarás introducir tu contraseña actual.',
                         en: 'To disable 2FA you will need to enter your current password.',
                         fr: 'Pour désactiver le 2FA, vous devrez entrer votre mot de passe actuel.' },

  // Offline / PWA
  TutOfflineTitle:     { es: 'Modo offline y PWA',                       en: 'Offline Mode & PWA',                fr: 'Mode hors ligne et PWA' },
  TutOfflineDesc:      { es: 'FitCycle funciona como app nativa en tu móvil. Instálala desde el navegador y úsala sin conexión. Incluye modo oscuro automático según la preferencia de tu dispositivo.', en: 'FitCycle works like a native app on your phone. Install it from the browser and use it offline. Includes automatic dark mode based on your device preference.', fr: "FitCycle fonctionne comme une app native. Installez-la depuis le navigateur et utilisez-la hors ligne. Mode sombre automatique selon les préférences de votre appareil." },
  TutPWA:              { es: 'Instalar como app',                        en: 'Install as app',                    fr: "Installer comme app" },
  TutOffline1:         { es: 'Funciona sin conexión a internet',         en: 'Works without internet connection',  fr: 'Fonctionne sans connexion internet' },
  TutOffline2:         { es: 'Se actualiza automáticamente cuando hay conexión', en: 'Auto-updates when connected', fr: 'Se met à jour automatiquement avec connexion' },
  TutOffline3:         { es: 'Instálala: menú del navegador → "Añadir a pantalla de inicio"', en: 'Install: browser menu → "Add to Home Screen"', fr: "Installer : menu du navigateur → \"Ajouter à l'écran d'accueil\"" },
  TutOffline4:         { es: 'Icono propio en tu pantalla, como una app nativa', en: 'Own icon on your screen, like a native app', fr: "Icône propre sur votre écran, comme une app native" },
  TutOfflineTip:       { es: 'Los datos se sincronizan automáticamente cuando vuelves a tener conexión.', en: 'Data syncs automatically when you reconnect.', fr: 'Les données se synchronisent automatiquement à la reconnexion.' },

  // Admin
  TutAdminTitle:       { es: 'Panel de administración',                  en: 'Admin Panel',                       fr: "Panneau d'administration" },
  TutAdminDesc:        { es: 'Herramientas avanzadas disponibles para administradores y superusuarios.', en: 'Advanced tools available for administrators and superusers.', fr: 'Outils avancés pour administrateurs et superutilisateurs.' },
  TutAdminFeatures:    { es: 'Funcionalidades de administrador',         en: 'Admin features',                    fr: "Fonctionnalités d'administration" },
  TutCopyRoutines:     { es: 'Copiar rutinas',                           en: 'Copy routines',                     fr: 'Copier des routines' },
  TutCopyRoutinesDesc: { es: 'Copia la rutina completa de un usuario a otro', en: 'Copy a user\'s full routine to another user', fr: "Copier la routine complète d'un utilisateur à un autre" },
  TutImportPDF:        { es: 'Importar rutina desde PDF',                en: 'Import routine from PDF',           fr: 'Importer une routine depuis PDF' },
  TutImportPDFDesc:    { es: 'Sube un PDF y se parsea automáticamente en ejercicios', en: 'Upload a PDF and it auto-parses into exercises', fr: "Téléchargez un PDF, il est automatiquement converti en exercices" },
  TutTemplates:        { es: 'Plantillas de rutina',                     en: 'Routine templates',                 fr: 'Modèles de routine' },
  TutTemplatesDesc:    { es: 'Guarda, visualiza y aplica rutinas como plantillas reutilizables', en: 'Save, view and apply routines as reusable templates', fr: 'Sauvegardez et appliquez des routines comme modèles réutilisables' },
  TutUserMgmt:         { es: 'Gestión de usuarios',                      en: 'User management',                   fr: 'Gestion des utilisateurs' },
  TutUserMgmtDesc:     { es: 'Crear, editar, activar/desactivar y asignar roles a usuarios', en: 'Create, edit, activate/deactivate and assign roles to users', fr: 'Créer, modifier, activer/désactiver et assigner des rôles' },
  TutSQLConsole:       { es: 'Consola SQL',                              en: 'SQL Console',                       fr: 'Console SQL' },
  TutSQLConsoleDesc:   { es: 'Ejecuta consultas SQL directamente sobre la base de datos', en: 'Run SQL queries directly on the database', fr: 'Exécutez des requêtes SQL directement sur la base de données' },
  TutBackups:          { es: 'Backups',                                  en: 'Backups',                           fr: 'Sauvegardes' },
  TutBackupsDesc:      { es: 'Crea, descarga y restaura backups de la base de datos', en: 'Create, download and restore database backups', fr: 'Créez, téléchargez et restaurez des sauvegardes' },
  TutDownloadDB:       { es: 'Descargar base de datos',                  en: 'Download database',                 fr: 'Télécharger la base de données' },
  TutDownloadDBDesc:   { es: 'Descarga el archivo SQLite completo',      en: 'Download the full SQLite file',     fr: 'Téléchargez le fichier SQLite complet' },
  TutImpersonate:      { es: 'Iniciar sesión como otro usuario',         en: 'Login as another user',             fr: "Se connecter en tant qu'autre utilisateur" },
  TutImpersonateDesc:  { es: 'Accede a la cuenta de otro usuario para soporte', en: 'Access another user\'s account for support', fr: "Accédez au compte d'un autre utilisateur pour le support" },

  // === Cardio & Abs ===
  Cardio:              { es: 'Cardio',                                    en: 'Cardio',                            fr: 'Cardio' },
  CardioType:          { es: 'Tipo de cardio',                            en: 'Cardio type',                       fr: 'Type de cardio' },
  CardioMinutes:       { es: 'Minutos de cardio',                         en: 'Cardio minutes',                    fr: 'Minutes de cardio' },
  Abs:                 { es: 'Abdominales',                               en: 'Abs',                               fr: 'Abdominaux' },
  AbsExercise:         { es: 'Ejercicio de abdominales',                  en: 'Abs exercise',                      fr: 'Exercice abdominaux' },
  AbsSets:             { es: 'Series de abdominales',                     en: 'Abs sets',                          fr: 'Séries abdominaux' },
  AbsReps:             { es: 'Repeticiones de abdominales',               en: 'Abs reps',                          fr: 'Répétitions abdominaux' },
  Treadmill:           { es: 'Cinta',                                     en: 'Treadmill',                         fr: 'Tapis roulant' },
  Bike:                { es: 'Bicicleta',                                 en: 'Bike',                              fr: 'Vélo' },
  Elliptical:          { es: 'Elíptica',                                  en: 'Elliptical',                        fr: 'Elliptique' },
  Rowing:              { es: 'Remo',                                      en: 'Rowing',                            fr: 'Rameur' },
  Stairmaster:         { es: 'Escaladora',                                en: 'Stairmaster',                       fr: 'Escalier' },
  Crunch:              { es: 'Crunch',                                    en: 'Crunch',                            fr: 'Crunch' },
  Plank:               { es: 'Plancha',                                   en: 'Plank',                             fr: 'Planche' },
  LegRaise:            { es: 'Elevación piernas',                         en: 'Leg Raise',                         fr: 'Relevé de jambes' },
  RussianTwist:        { es: 'Russian twist',                             en: 'Russian Twist',                     fr: 'Russian Twist' },
  AbWheel:             { es: 'Ab Wheel',                                  en: 'Ab Wheel',                          fr: 'Ab Wheel' },
  Other:               { es: 'Otro',                                      en: 'Other',                             fr: 'Autre' },
  MinUnit:             { es: 'min',                                       en: 'min',                               fr: 'min' },

  // === Utils / Modals ===
  OK:                  { es: 'Aceptar',                                   en: 'OK',                                fr: 'OK' },
  WorkoutSaveError:    { es: 'Error al guardar, tu progreso se mantiene',  en: 'Save failed, your progress is kept', fr: 'Erreur de sauvegarde, votre progression est conservée' },
  ThisWeek:            { es: 'Esta semana',                                en: 'This week',                         fr: 'Cette semaine' },
  WeeksAgo:            { es: 'Hace {0} sem.',                              en: '{0} weeks ago',                      fr: 'Il y a {0} sem.' },
  CurrentPassword:     { es: 'Contraseña actual',                          en: 'Current password',                   fr: 'Mot de passe actuel' },
  NewPassword:         { es: 'Nueva contraseña',                           en: 'New password',                       fr: 'Nouveau mot de passe' },
  PasswordChanged:     { es: 'Contraseña cambiada correctamente',          en: 'Password changed successfully',      fr: 'Mot de passe changé avec succès' },
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
