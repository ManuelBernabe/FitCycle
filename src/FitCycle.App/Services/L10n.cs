namespace FitCycle.App.Services;

public static class L10n
{
    private static string _lang = "es";
    public static string CurrentLanguage => _lang;

    public static void Init() => _lang = Preferences.Get("app_language", "es");

    public static void SetLanguage(string lang)
    {
        _lang = lang;
        Preferences.Set("app_language", lang);
    }

    public static string[] AvailableLanguages => ["es", "en", "fr"];

    public static string LanguageDisplayName(string lang) => lang switch
    {
        "es" => "Español",
        "en" => "English",
        "fr" => "Français",
        _ => lang
    };

    public static string T(string key)
    {
        if (Strings.TryGetValue(key, out var langs))
        {
            if (langs.TryGetValue(_lang, out var val)) return val;
            if (langs.TryGetValue("es", out var fallback)) return fallback;
        }
        return key;
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);

    public static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => T("Monday"),
        DayOfWeek.Tuesday => T("Tuesday"),
        DayOfWeek.Wednesday => T("Wednesday"),
        DayOfWeek.Thursday => T("Thursday"),
        DayOfWeek.Friday => T("Friday"),
        DayOfWeek.Saturday => T("Saturday"),
        DayOfWeek.Sunday => T("Sunday"),
        _ => day.ToString()
    };

    public static string MuscleGroup(string spanishName)
    {
        if (_lang == "es") return spanishName;
        var key = $"MG_{spanishName}";
        if (Strings.TryGetValue(key, out var langs) && langs.TryGetValue(_lang, out var val))
            return val;
        return spanishName;
    }

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        // === Days ===
        ["Monday"] = new() { ["es"] = "Lunes", ["en"] = "Monday", ["fr"] = "Lundi" },
        ["Tuesday"] = new() { ["es"] = "Martes", ["en"] = "Tuesday", ["fr"] = "Mardi" },
        ["Wednesday"] = new() { ["es"] = "Miércoles", ["en"] = "Wednesday", ["fr"] = "Mercredi" },
        ["Thursday"] = new() { ["es"] = "Jueves", ["en"] = "Thursday", ["fr"] = "Jeudi" },
        ["Friday"] = new() { ["es"] = "Viernes", ["en"] = "Friday", ["fr"] = "Vendredi" },
        ["Saturday"] = new() { ["es"] = "Sábado", ["en"] = "Saturday", ["fr"] = "Samedi" },
        ["Sunday"] = new() { ["es"] = "Domingo", ["en"] = "Sunday", ["fr"] = "Dimanche" },

        // === Muscle Groups (key = MG_{spanish name}) ===
        ["MG_Pecho"] = new() { ["en"] = "Chest", ["fr"] = "Poitrine" },
        ["MG_Espalda"] = new() { ["en"] = "Back", ["fr"] = "Dos" },
        ["MG_Hombros"] = new() { ["en"] = "Shoulders", ["fr"] = "Épaules" },
        ["MG_Bíceps"] = new() { ["en"] = "Biceps", ["fr"] = "Biceps" },
        ["MG_Tríceps"] = new() { ["en"] = "Triceps", ["fr"] = "Triceps" },
        ["MG_Piernas"] = new() { ["en"] = "Legs", ["fr"] = "Jambes" },
        ["MG_Abdominales"] = new() { ["en"] = "Abs", ["fr"] = "Abdominaux" },
        ["MG_Glúteos"] = new() { ["en"] = "Glutes", ["fr"] = "Fessiers" },

        // === General ===
        ["Loading"] = new() { ["es"] = "Cargando...", ["en"] = "Loading...", ["fr"] = "Chargement..." },
        ["ServiceUnavailable"] = new() { ["es"] = "Servicio no disponible", ["en"] = "Service unavailable", ["fr"] = "Service indisponible" },
        ["ErrorFmt"] = new() { ["es"] = "Error: {0}", ["en"] = "Error: {0}", ["fr"] = "Erreur : {0}" },
        ["Saving"] = new() { ["es"] = "Guardando...", ["en"] = "Saving...", ["fr"] = "Enregistrement..." },
        ["UnknownError"] = new() { ["es"] = "Error desconocido", ["en"] = "Unknown error", ["fr"] = "Erreur inconnue" },

        // === Buttons ===
        ["Save"] = new() { ["es"] = "Guardar", ["en"] = "Save", ["fr"] = "Enregistrer" },
        ["Cancel"] = new() { ["es"] = "Cancelar", ["en"] = "Cancel", ["fr"] = "Annuler" },
        ["Yes"] = new() { ["es"] = "Sí", ["en"] = "Yes", ["fr"] = "Oui" },
        ["No"] = new() { ["es"] = "No", ["en"] = "No", ["fr"] = "Non" },
        ["Confirm"] = new() { ["es"] = "Confirmar", ["en"] = "Confirm", ["fr"] = "Confirmer" },
        ["Next"] = new() { ["es"] = "Siguiente", ["en"] = "Next", ["fr"] = "Suivant" },
        ["Previous"] = new() { ["es"] = "Anterior", ["en"] = "Previous", ["fr"] = "Précédent" },
        ["Start"] = new() { ["es"] = "Iniciar", ["en"] = "Start", ["fr"] = "Démarrer" },
        ["Pause"] = new() { ["es"] = "Pausar", ["en"] = "Pause", ["fr"] = "Pause" },
        ["Reset"] = new() { ["es"] = "Reiniciar", ["en"] = "Reset", ["fr"] = "Réinitialiser" },
        ["Finish"] = new() { ["es"] = "Finalizar", ["en"] = "Finish", ["fr"] = "Terminer" },
        ["Edit"] = new() { ["es"] = "Editar", ["en"] = "Edit", ["fr"] = "Modifier" },
        ["Delete"] = new() { ["es"] = "Borrar", ["en"] = "Delete", ["fr"] = "Supprimer" },
        ["Create"] = new() { ["es"] = "Crear", ["en"] = "Create", ["fr"] = "Créer" },
        ["Add"] = new() { ["es"] = "Agregar", ["en"] = "Add", ["fr"] = "Ajouter" },
        ["Back"] = new() { ["es"] = "Volver", ["en"] = "Back", ["fr"] = "Retour" },
        ["Logout"] = new() { ["es"] = "Cerrar Sesión", ["en"] = "Log Out", ["fr"] = "Déconnexion" },

        // === Login ===
        ["AppName"] = new() { ["es"] = "FitCycle", ["en"] = "FitCycle", ["fr"] = "FitCycle" },
        ["SignIn"] = new() { ["es"] = "Iniciar Sesión", ["en"] = "Sign In", ["fr"] = "Connexion" },
        ["CreateAccount"] = new() { ["es"] = "Crear Cuenta", ["en"] = "Create Account", ["fr"] = "Créer un compte" },
        ["Username"] = new() { ["es"] = "Nombre de usuario", ["en"] = "Username", ["fr"] = "Nom d'utilisateur" },
        ["Email"] = new() { ["es"] = "Email", ["en"] = "Email", ["fr"] = "E-mail" },
        ["Password"] = new() { ["es"] = "Contraseña", ["en"] = "Password", ["fr"] = "Mot de passe" },
        ["Register"] = new() { ["es"] = "Registrarse", ["en"] = "Register", ["fr"] = "S'inscrire" },
        ["NoAccountSignUp"] = new() { ["es"] = "¿No tienes cuenta? Regístrate", ["en"] = "Don't have an account? Sign up", ["fr"] = "Pas de compte ? Inscrivez-vous" },
        ["HaveAccountSignIn"] = new() { ["es"] = "¿Ya tienes cuenta? Inicia sesión", ["en"] = "Already have an account? Sign in", ["fr"] = "Déjà un compte ? Connectez-vous" },

        // === Routines ===
        ["MyWeeklyRoutine"] = new() { ["es"] = "Mi Rutina Semanal", ["en"] = "My Weekly Routine", ["fr"] = "Ma Routine Hebdomadaire" },
        ["ConfigureWeekly"] = new() { ["es"] = "Configura y entrena tu rutina semanal", ["en"] = "Configure and train your weekly routine", ["fr"] = "Configurez et entraînez votre routine hebdomadaire" },
        ["NoGroupsAssigned"] = new() { ["es"] = "Sin grupos asignados", ["en"] = "No groups assigned", ["fr"] = "Aucun groupe assigné" },
        ["DeleteRoutineMsg"] = new() { ["es"] = "¿Eliminar la rutina de este día?", ["en"] = "Delete this day's routine?", ["fr"] = "Supprimer la routine de ce jour ?" },
        ["StartWorkout"] = new() { ["es"] = "Empezar", ["en"] = "Start", ["fr"] = "Commencer" },
        ["TabRoutines"] = new() { ["es"] = "Rutinas", ["en"] = "Routines", ["fr"] = "Routines" },
        ["TabStats"] = new() { ["es"] = "Estadísticas", ["en"] = "Statistics", ["fr"] = "Statistiques" },

        // === EditDay ===
        ["EditDay"] = new() { ["es"] = "Editar Día", ["en"] = "Edit Day", ["fr"] = "Modifier le jour" },
        ["SelectGroupsExercises"] = new() { ["es"] = "Selecciona grupos musculares y ejercicios:", ["en"] = "Select muscle groups and exercises:", ["fr"] = "Sélectionnez les groupes musculaires et exercices :" },
        ["Sets"] = new() { ["es"] = "Series:", ["en"] = "Sets:", ["fr"] = "Séries :" },
        ["Reps"] = new() { ["es"] = "Reps:", ["en"] = "Reps:", ["fr"] = "Reps :" },
        ["AddExercise"] = new() { ["es"] = "+ Agregar ejercicio", ["en"] = "+ Add exercise", ["fr"] = "+ Ajouter un exercice" },
        ["CustomName"] = new() { ["es"] = "Escribir nombre personalizado...", ["en"] = "Type custom name...", ["fr"] = "Saisir un nom personnalisé..." },
        ["NewExercise"] = new() { ["es"] = "Nuevo ejercicio", ["en"] = "New exercise", ["fr"] = "Nouvel exercice" },
        ["ExerciseNameFor"] = new() { ["es"] = "Nombre del ejercicio para {0}:", ["en"] = "Exercise name for {0}:", ["fr"] = "Nom de l'exercice pour {0} :" },
        ["AddExerciseTo"] = new() { ["es"] = "Agregar ejercicio a {0}", ["en"] = "Add exercise to {0}", ["fr"] = "Ajouter un exercice à {0}" },
        ["SelectExercise"] = new() { ["es"] = "Seleccionar ejercicio", ["en"] = "Select exercise", ["fr"] = "Sélectionner un exercice" },
        ["ConfirmDeleteExercise"] = new() { ["es"] = "¿Eliminar '{0}' de este día?", ["en"] = "Remove '{0}' from this day?", ["fr"] = "Supprimer '{0}' de ce jour ?" },
        ["Weight"] = new() { ["es"] = "Peso", ["en"] = "Weight", ["fr"] = "Poids" },
        ["WeightKg"] = new() { ["es"] = "kg", ["en"] = "kg", ["fr"] = "kg" },
        ["WeightProgress"] = new() { ["es"] = "Progreso de peso", ["en"] = "Weight Progress", ["fr"] = "Progression du poids" },

        // === Workout ===
        ["Workout"] = new() { ["es"] = "Entrenamiento", ["en"] = "Workout", ["fr"] = "Entraînement" },
        ["Rest"] = new() { ["es"] = "DESCANSO", ["en"] = "REST", ["fr"] = "REPOS" },
        ["Min"] = new() { ["es"] = "Min:", ["en"] = "Min:", ["fr"] = "Min :" },
        ["Sec"] = new() { ["es"] = "Seg:", ["en"] = "Sec:", ["fr"] = "Sec :" },
        ["ExerciseProgress"] = new() { ["es"] = "Ejercicio {0} de {1}", ["en"] = "Exercise {0} of {1}", ["fr"] = "Exercice {0} sur {1}" },
        ["SetsRepsFormat"] = new() { ["es"] = "{0} series x {1} repeticiones", ["en"] = "{0} sets x {1} reps", ["fr"] = "{0} séries x {1} répétitions" },
        ["NoExercisesDay"] = new() { ["es"] = "No hay ejercicios configurados para este día.", ["en"] = "No exercises configured for this day.", ["fr"] = "Aucun exercice configuré pour ce jour." },
        ["NoImage"] = new() { ["es"] = "Sin imagen", ["en"] = "No image", ["fr"] = "Pas d'image" },

        // === Summary ===
        ["Summary"] = new() { ["es"] = "Resumen", ["en"] = "Summary", ["fr"] = "Résumé" },
        ["WorkoutCompleted"] = new() { ["es"] = "Entrenamiento completado", ["en"] = "Workout completed", ["fr"] = "Entraînement terminé" },
        ["Duration"] = new() { ["es"] = "Duración", ["en"] = "Duration", ["fr"] = "Durée" },
        ["Exercises"] = new() { ["es"] = "Ejercicios", ["en"] = "Exercises", ["fr"] = "Exercices" },
        ["TotalSets"] = new() { ["es"] = "Series totales", ["en"] = "Total sets", ["fr"] = "Séries totales" },
        ["WeeklyWorkouts"] = new() { ["es"] = "Entrenamientos esta semana", ["en"] = "Workouts this week", ["fr"] = "Entraînements cette semaine" },
        ["ExercisesDone"] = new() { ["es"] = "Ejercicios realizados", ["en"] = "Exercises done", ["fr"] = "Exercices réalisés" },
        ["BackToRoutines"] = new() { ["es"] = "Volver a Rutinas", ["en"] = "Back to Routines", ["fr"] = "Retour aux routines" },

        // === Stats ===
        ["YourProgress"] = new() { ["es"] = "Tu Progreso", ["en"] = "Your Progress", ["fr"] = "Votre Progrès" },
        ["Workouts"] = new() { ["es"] = "Entrenamientos", ["en"] = "Workouts", ["fr"] = "Entraînements" },
        ["TotalReps"] = new() { ["es"] = "Reps totales", ["en"] = "Total reps", ["fr"] = "Reps totales" },
        ["WeeklyWorkoutsChart"] = new() { ["es"] = "Entrenamientos por semana", ["en"] = "Workouts per week", ["fr"] = "Entraînements par semaine" },
        ["MostFrequent"] = new() { ["es"] = "Ejercicios más frecuentes", ["en"] = "Most frequent exercises", ["fr"] = "Exercices les plus fréquents" },
        ["RecentHistory"] = new() { ["es"] = "Historial reciente", ["en"] = "Recent history", ["fr"] = "Historique récent" },
        ["NoWorkoutsYet"] = new() { ["es"] = "Aún no hay entrenamientos registrados", ["en"] = "No workouts recorded yet", ["fr"] = "Aucun entraînement enregistré" },
        ["ExercisesCount"] = new() { ["es"] = "{0} ejercicios · {1}", ["en"] = "{0} exercises · {1}", ["fr"] = "{0} exercices · {1}" },
        ["MinSuffix"] = new() { ["es"] = " min", ["en"] = " min", ["fr"] = " min" },
        ["SecSuffix"] = new() { ["es"] = "s", ["en"] = "s", ["fr"] = "s" },

        // === Account ===
        ["MyAccount"] = new() { ["es"] = "Mi Cuenta", ["en"] = "My Account", ["fr"] = "Mon Compte" },
        ["EditProfile"] = new() { ["es"] = "Editar Perfil", ["en"] = "Edit Profile", ["fr"] = "Modifier le profil" },
        ["UserManagement"] = new() { ["es"] = "Gestión de Usuarios", ["en"] = "User Management", ["fr"] = "Gestion des utilisateurs" },
        ["CreateUser"] = new() { ["es"] = "+ Crear Usuario", ["en"] = "+ Create User", ["fr"] = "+ Créer un utilisateur" },
        ["CreateUserTitle"] = new() { ["es"] = "Crear Usuario", ["en"] = "Create User", ["fr"] = "Créer un utilisateur" },
        ["EditUserTitle"] = new() { ["es"] = "Editar Usuario", ["en"] = "Edit User", ["fr"] = "Modifier l'utilisateur" },
        ["SelectRole"] = new() { ["es"] = "Seleccionar Rol", ["en"] = "Select Role", ["fr"] = "Sélectionner le rôle" },
        ["CreatingUser"] = new() { ["es"] = "Creando usuario...", ["en"] = "Creating user...", ["fr"] = "Création de l'utilisateur..." },
        ["UserCreated"] = new() { ["es"] = "Usuario creado correctamente", ["en"] = "User created successfully", ["fr"] = "Utilisateur créé avec succès" },
        ["ErrorCreatingUser"] = new() { ["es"] = "Error al crear usuario", ["en"] = "Error creating user", ["fr"] = "Erreur lors de la création" },
        ["UpdatingUser"] = new() { ["es"] = "Actualizando usuario...", ["en"] = "Updating user...", ["fr"] = "Mise à jour de l'utilisateur..." },
        ["UserUpdated"] = new() { ["es"] = "Usuario actualizado", ["en"] = "User updated", ["fr"] = "Utilisateur mis à jour" },
        ["ErrorUpdatingUser"] = new() { ["es"] = "Error al actualizar usuario", ["en"] = "Error updating user", ["fr"] = "Erreur lors de la mise à jour" },
        ["ChangePassword"] = new() { ["es"] = "Cambiar Contraseña", ["en"] = "Change Password", ["fr"] = "Changer le mot de passe" },
        ["NewPasswordFor"] = new() { ["es"] = "Nueva contraseña para '{0}':", ["en"] = "New password for '{0}':", ["fr"] = "Nouveau mot de passe pour '{0}' :" },
        ["PasswordMinLength"] = new() { ["es"] = "La contraseña debe tener al menos 6 caracteres", ["en"] = "Password must be at least 6 characters", ["fr"] = "Le mot de passe doit comporter au moins 6 caractères" },
        ["ChangingPassword"] = new() { ["es"] = "Cambiando contraseña...", ["en"] = "Changing password...", ["fr"] = "Changement du mot de passe..." },
        ["PasswordUpdated"] = new() { ["es"] = "Contraseña actualizada", ["en"] = "Password updated", ["fr"] = "Mot de passe mis à jour" },
        ["ErrorChangingPassword"] = new() { ["es"] = "Error al cambiar contraseña", ["en"] = "Error changing password", ["fr"] = "Erreur lors du changement" },
        ["ConfirmDeleteUser"] = new() { ["es"] = "¿Eliminar al usuario '{0}'?", ["en"] = "Delete user '{0}'?", ["fr"] = "Supprimer l'utilisateur '{0}' ?" },
        ["DeletingUser"] = new() { ["es"] = "Eliminando usuario...", ["en"] = "Deleting user...", ["fr"] = "Suppression..." },
        ["UserDeleted"] = new() { ["es"] = "Usuario eliminado", ["en"] = "User deleted", ["fr"] = "Utilisateur supprimé" },
        ["ErrorDeletingUser"] = new() { ["es"] = "Error al eliminar usuario", ["en"] = "Error deleting user", ["fr"] = "Erreur lors de la suppression" },
        ["Updating"] = new() { ["es"] = "Actualizando...", ["en"] = "Updating...", ["fr"] = "Mise à jour..." },
        ["ProfileUpdated"] = new() { ["es"] = "Perfil actualizado", ["en"] = "Profile updated", ["fr"] = "Profil mis à jour" },
        ["ErrorUpdatingProfile"] = new() { ["es"] = "Error al actualizar perfil", ["en"] = "Error updating profile", ["fr"] = "Erreur lors de la mise à jour" },
        ["UserInfoError"] = new() { ["es"] = "No se pudo obtener la información del usuario", ["en"] = "Could not get user information", ["fr"] = "Impossible d'obtenir les informations" },
        ["PasswordKey"] = new() { ["es"] = "Clave", ["en"] = "Key", ["fr"] = "Clé" },
        ["DeleteUserBtn"] = new() { ["es"] = "Eliminar", ["en"] = "Delete", ["fr"] = "Supprimer" },
        ["BackToRoutinesBtn"] = new() { ["es"] = "← Volver a Rutinas", ["en"] = "← Back to Routines", ["fr"] = "← Retour aux routines" },

        // === Language ===
        ["Language"] = new() { ["es"] = "Idioma", ["en"] = "Language", ["fr"] = "Langue" },
        ["SelectLanguage"] = new() { ["es"] = "Seleccionar idioma", ["en"] = "Select language", ["fr"] = "Sélectionner la langue" },
    };
}
