// FitCycle Exercises helper — caches muscle groups and exercises + suggestion data

import { api } from './api.js';
import { currentLanguage } from './l10n.js';

let _muscleGroups = null;
let _exercisesByGroup = {};

/**
 * Fetch all muscle groups (cached).
 */
export async function getMuscleGroups() {
  if (_muscleGroups) return _muscleGroups;
  _muscleGroups = await api.get('/musclegroups');
  return _muscleGroups;
}

/**
 * Fetch exercises for a muscle group (cached by groupId).
 */
export async function getExercises(muscleGroupId) {
  if (_exercisesByGroup[muscleGroupId]) return _exercisesByGroup[muscleGroupId];
  const exercises = await api.get(`/exercises?muscleGroupId=${muscleGroupId}`);
  _exercisesByGroup[muscleGroupId] = exercises;
  return exercises;
}

/**
 * Fetch all exercises (no filter).
 */
export async function getAllExercises() {
  return api.get('/exercises');
}

/**
 * Clear the cache (e.g., after adding a new exercise).
 */
export function clearCache() {
  _muscleGroups = null;
  _exercisesByGroup = {};
}

/**
 * Returns exercise suggestions for a given muscle group (keyed by Spanish name).
 * Ported from FitCycle.App/Services/ExerciseData.cs
 * @param {string} muscleGroupSpanishName - The Spanish name of the muscle group (e.g., "Pecho").
 * @returns {Array<{name: string, imageUrl: string}>}
 */
export function getSuggestions(muscleGroupSpanishName) {
  const lang = currentLanguage();
  if (suggestionData[lang] && suggestionData[lang][muscleGroupSpanishName]) {
    return suggestionData[lang][muscleGroupSpanishName];
  }
  if (suggestionData['es'] && suggestionData['es'][muscleGroupSpanishName]) {
    return suggestionData['es'][muscleGroupSpanishName];
  }
  return [];
}

// =====================================================================
// Suggestion data by language and muscle group (Spanish key)
// =====================================================================
const suggestionData = {
  // === SPANISH (es) ===
  es: {
    Pecho: [
      { name: 'Press banca', imageUrl: 'https://wger.de/media/exercise-images/192/Bench-press-1.png' },
      { name: 'Press inclinado', imageUrl: 'https://wger.de/media/exercise-images/41/Incline-bench-press-1.png' },
      { name: 'Press declinado', imageUrl: 'https://wger.de/media/exercise-images/100/Decline-bench-press-1.png' },
      { name: 'Aperturas con mancuernas', imageUrl: 'https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png' },
      { name: 'Aperturas en polea', imageUrl: 'https://wger.de/media/exercise-images/71/Cable-crossover-2.png' },
      { name: 'Fondos', imageUrl: 'https://wger.de/media/exercise-images/83/Bench-dips-1.png' },
      { name: 'Pullover', imageUrl: 'https://wger.de/media/exercise-images/1634/9a4704d3-1b25-43e3-b244-3885f4d3db87.png' },
      { name: 'Press con mancuernas', imageUrl: 'https://wger.de/media/exercise-images/100/Decline-bench-press-1.png' },
      { name: 'Cruces en polea alta', imageUrl: 'https://wger.de/media/exercise-images/71/Cable-crossover-2.png' },
      { name: 'Cruces en polea baja', imageUrl: 'https://wger.de/media/exercise-images/71/Cable-crossover-2.png' },
      { name: 'Flexiones', imageUrl: 'https://wger.de/media/exercise-images/1551/a6a9e561-3965-45c6-9f2b-ee671e1a3a45.png' },
      { name: 'Flexiones diamante', imageUrl: 'https://wger.de/media/exercise-images/1551/a6a9e561-3965-45c6-9f2b-ee671e1a3a45.png' },
      { name: 'Press en maquina', imageUrl: 'https://wger.de/media/exercise-images/192/Bench-press-1.png' },
      { name: 'Peck deck', imageUrl: 'https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png' },
    ],
    Espalda: [
      { name: 'Dominadas', imageUrl: 'https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg' },
      { name: 'Remo con barra', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Jalon al pecho', imageUrl: 'https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp' },
      { name: 'Remo con mancuerna', imageUrl: 'https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png' },
      { name: 'Remo en polea baja', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Jalon tras nuca', imageUrl: 'https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp' },
      { name: 'Peso muerto', imageUrl: 'https://wger.de/media/exercise-images/184/1709c405-620a-4d07-9658-fade2b66a2df.jpeg' },
      { name: 'Pullover en polea', imageUrl: 'https://wger.de/media/exercise-images/1634/9a4704d3-1b25-43e3-b244-3885f4d3db87.png' },
      { name: 'Remo en maquina', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Dominadas supinas', imageUrl: 'https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg' },
      { name: 'Remo T', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Face pull', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Hiperextensiones', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Encogimientos con barra', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
    ],
    Hombros: [
      { name: 'Press militar', imageUrl: 'https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png' },
      { name: 'Elevaciones laterales', imageUrl: 'https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png' },
      { name: 'Elevaciones frontales', imageUrl: 'https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png' },
      { name: 'Pajaros', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Press Arnold', imageUrl: 'https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png' },
      { name: 'Remo al menton', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Face pull', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Elevaciones laterales en polea', imageUrl: 'https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png' },
      { name: 'Press con mancuernas', imageUrl: 'https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png' },
      { name: 'Shrugs', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Rotacion externa', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Plancha lateral', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
    ],
    'Biceps': [
      { name: 'Curl con barra', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl con mancuernas', imageUrl: 'https://wger.de/media/exercise-images/81/Biceps-curl-1.png' },
      { name: 'Curl martillo', imageUrl: 'https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png' },
      { name: 'Curl concentrado', imageUrl: 'https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg' },
      { name: 'Curl en predicador', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl en polea', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl 21s', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl arana', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl con barra Z', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl inclinado', imageUrl: 'https://wger.de/media/exercise-images/81/Biceps-curl-1.png' },
      { name: 'Curl en maquina', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl inverso', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
    ],
    'Triceps': [
      { name: 'Fondos en paralelas', imageUrl: 'https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png' },
      { name: 'Extension con polea', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Press frances', imageUrl: 'https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png' },
      { name: 'Patada de triceps', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Press cerrado', imageUrl: 'https://wger.de/media/exercise-images/192/Bench-press-1.png' },
      { name: 'Extension sobre cabeza', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Dips en banco', imageUrl: 'https://wger.de/media/exercise-images/83/Bench-dips-1.png' },
      { name: 'Extension con mancuerna', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Jalon con cuerda', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Press de triceps en maquina', imageUrl: 'https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png' },
      { name: 'Extension en polea alta', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
    ],
    Piernas: [
      { name: 'Sentadilla', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Prensa', imageUrl: 'https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp' },
      { name: 'Extension de cuadriceps', imageUrl: 'https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp' },
      { name: 'Curl femoral', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
      { name: 'Zancadas', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Sentadilla bulgara', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Sentadilla hack', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Peso muerto rumano', imageUrl: 'https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp' },
      { name: 'Elevacion de gemelos', imageUrl: 'https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp' },
      { name: 'Sentadilla goblet', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Prensa de gemelos', imageUrl: 'https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp' },
      { name: 'Step up', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Sentadilla sumo', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Abductores', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
      { name: 'Aductores', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
      { name: 'Leg curl sentado', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
    ],
    Abdominales: [
      { name: 'Crunch', imageUrl: 'https://wger.de/media/exercise-images/91/Crunches-1.png' },
      { name: 'Plancha', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Elevacion de piernas', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Russian twist', imageUrl: 'https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png' },
      { name: 'Crunch en polea', imageUrl: 'https://wger.de/media/exercise-images/91/Crunches-1.png' },
      { name: 'Ab wheel', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Mountain climbers', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Plancha lateral', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Crunch bicicleta', imageUrl: 'https://wger.de/media/exercise-images/91/Crunches-1.png' },
      { name: 'V-ups', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Dead bug', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Hollow hold', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Elevacion de piernas colgado', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Woodchop', imageUrl: 'https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png' },
    ],
    'Gluteos': [
      { name: 'Hip thrust', imageUrl: 'https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg' },
      { name: 'Peso muerto rumano', imageUrl: 'https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp' },
      { name: 'Patada de gluteo', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
      { name: 'Puente de gluteos', imageUrl: 'https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg' },
      { name: 'Sentadilla sumo', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Step up', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Abduccion de cadera', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
      { name: 'Kickback en polea', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
      { name: 'Clamshell', imageUrl: 'https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg' },
      { name: 'Sentadilla bulgara', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Peso muerto sumo', imageUrl: 'https://wger.de/media/exercise-images/184/1709c405-620a-4d07-9658-fade2b66a2df.jpeg' },
      { name: 'Fire hydrant', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
    ],
  },

  // === ENGLISH (en) ===
  en: {
    Pecho: [
      { name: 'Bench press', imageUrl: 'https://wger.de/media/exercise-images/192/Bench-press-1.png' },
      { name: 'Incline bench press', imageUrl: 'https://wger.de/media/exercise-images/41/Incline-bench-press-1.png' },
      { name: 'Decline bench press', imageUrl: 'https://wger.de/media/exercise-images/100/Decline-bench-press-1.png' },
      { name: 'Dumbbell flyes', imageUrl: 'https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png' },
      { name: 'Cable flyes', imageUrl: 'https://wger.de/media/exercise-images/71/Cable-crossover-2.png' },
      { name: 'Dips', imageUrl: 'https://wger.de/media/exercise-images/83/Bench-dips-1.png' },
      { name: 'Pullover', imageUrl: 'https://wger.de/media/exercise-images/1634/9a4704d3-1b25-43e3-b244-3885f4d3db87.png' },
      { name: 'Dumbbell press', imageUrl: 'https://wger.de/media/exercise-images/100/Decline-bench-press-1.png' },
      { name: 'High cable crossover', imageUrl: 'https://wger.de/media/exercise-images/71/Cable-crossover-2.png' },
      { name: 'Low cable crossover', imageUrl: 'https://wger.de/media/exercise-images/71/Cable-crossover-2.png' },
      { name: 'Push-ups', imageUrl: 'https://wger.de/media/exercise-images/1551/a6a9e561-3965-45c6-9f2b-ee671e1a3a45.png' },
      { name: 'Diamond push-ups', imageUrl: 'https://wger.de/media/exercise-images/1551/a6a9e561-3965-45c6-9f2b-ee671e1a3a45.png' },
      { name: 'Machine press', imageUrl: 'https://wger.de/media/exercise-images/192/Bench-press-1.png' },
      { name: 'Pec deck', imageUrl: 'https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png' },
    ],
    Espalda: [
      { name: 'Pull-ups', imageUrl: 'https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg' },
      { name: 'Barbell row', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Lat pulldown', imageUrl: 'https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp' },
      { name: 'Dumbbell row', imageUrl: 'https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png' },
      { name: 'Seated cable row', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Behind neck pulldown', imageUrl: 'https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp' },
      { name: 'Deadlift', imageUrl: 'https://wger.de/media/exercise-images/184/1709c405-620a-4d07-9658-fade2b66a2df.jpeg' },
      { name: 'Cable pullover', imageUrl: 'https://wger.de/media/exercise-images/1634/9a4704d3-1b25-43e3-b244-3885f4d3db87.png' },
      { name: 'Machine row', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Chin-ups', imageUrl: 'https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg' },
      { name: 'T-bar row', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Face pull', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Hyperextensions', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Barbell shrugs', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
    ],
    Hombros: [
      { name: 'Military press', imageUrl: 'https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png' },
      { name: 'Lateral raises', imageUrl: 'https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png' },
      { name: 'Front raises', imageUrl: 'https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png' },
      { name: 'Reverse flyes', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Arnold press', imageUrl: 'https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png' },
      { name: 'Upright row', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Face pull', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Cable lateral raises', imageUrl: 'https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png' },
      { name: 'Dumbbell press', imageUrl: 'https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png' },
      { name: 'Shrugs', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'External rotation', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Side plank', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
    ],
    'Biceps': [
      { name: 'Barbell curl', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Dumbbell curl', imageUrl: 'https://wger.de/media/exercise-images/81/Biceps-curl-1.png' },
      { name: 'Hammer curl', imageUrl: 'https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png' },
      { name: 'Concentration curl', imageUrl: 'https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg' },
      { name: 'Preacher curl', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Cable curl', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: '21s curl', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Spider curl', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'EZ bar curl', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Incline curl', imageUrl: 'https://wger.de/media/exercise-images/81/Biceps-curl-1.png' },
      { name: 'Machine curl', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Reverse curl', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
    ],
    'Triceps': [
      { name: 'Parallel bar dips', imageUrl: 'https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png' },
      { name: 'Cable pushdown', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Skull crushers', imageUrl: 'https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png' },
      { name: 'Tricep kickback', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Close grip bench press', imageUrl: 'https://wger.de/media/exercise-images/192/Bench-press-1.png' },
      { name: 'Overhead extension', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Bench dips', imageUrl: 'https://wger.de/media/exercise-images/83/Bench-dips-1.png' },
      { name: 'Dumbbell extension', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Rope pushdown', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Machine tricep press', imageUrl: 'https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png' },
      { name: 'High cable extension', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
    ],
    Piernas: [
      { name: 'Squat', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Leg press', imageUrl: 'https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp' },
      { name: 'Leg extension', imageUrl: 'https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp' },
      { name: 'Leg curl', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
      { name: 'Lunges', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Bulgarian split squat', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Hack squat', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Romanian deadlift', imageUrl: 'https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp' },
      { name: 'Calf raises', imageUrl: 'https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp' },
      { name: 'Goblet squat', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Calf press', imageUrl: 'https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp' },
      { name: 'Step up', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Sumo squat', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Hip abduction', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
      { name: 'Hip adduction', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
      { name: 'Seated leg curl', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
    ],
    Abdominales: [
      { name: 'Crunch', imageUrl: 'https://wger.de/media/exercise-images/91/Crunches-1.png' },
      { name: 'Plank', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Leg raises', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Russian twist', imageUrl: 'https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png' },
      { name: 'Cable crunch', imageUrl: 'https://wger.de/media/exercise-images/91/Crunches-1.png' },
      { name: 'Ab wheel', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Mountain climbers', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Side plank', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Bicycle crunch', imageUrl: 'https://wger.de/media/exercise-images/91/Crunches-1.png' },
      { name: 'V-ups', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Dead bug', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Hollow hold', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Hanging leg raises', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Woodchop', imageUrl: 'https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png' },
    ],
    'Gluteos': [
      { name: 'Hip thrust', imageUrl: 'https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg' },
      { name: 'Romanian deadlift', imageUrl: 'https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp' },
      { name: 'Glute kickback', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
      { name: 'Glute bridge', imageUrl: 'https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg' },
      { name: 'Sumo squat', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Step up', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Hip abduction', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
      { name: 'Cable kickback', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
      { name: 'Clamshell', imageUrl: 'https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg' },
      { name: 'Bulgarian split squat', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Sumo deadlift', imageUrl: 'https://wger.de/media/exercise-images/184/1709c405-620a-4d07-9658-fade2b66a2df.jpeg' },
      { name: 'Fire hydrant', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
    ],
  },

  // === FRENCH (fr) ===
  fr: {
    Pecho: [
      { name: 'Developpe couche', imageUrl: 'https://wger.de/media/exercise-images/192/Bench-press-1.png' },
      { name: 'Developpe incline', imageUrl: 'https://wger.de/media/exercise-images/41/Incline-bench-press-1.png' },
      { name: 'Developpe decline', imageUrl: 'https://wger.de/media/exercise-images/100/Decline-bench-press-1.png' },
      { name: 'Ecartes halteres', imageUrl: 'https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png' },
      { name: 'Ecartes poulie', imageUrl: 'https://wger.de/media/exercise-images/71/Cable-crossover-2.png' },
      { name: 'Dips', imageUrl: 'https://wger.de/media/exercise-images/83/Bench-dips-1.png' },
      { name: 'Pullover', imageUrl: 'https://wger.de/media/exercise-images/1634/9a4704d3-1b25-43e3-b244-3885f4d3db87.png' },
      { name: 'Developpe halteres', imageUrl: 'https://wger.de/media/exercise-images/100/Decline-bench-press-1.png' },
      { name: 'Croise poulie haute', imageUrl: 'https://wger.de/media/exercise-images/71/Cable-crossover-2.png' },
      { name: 'Croise poulie basse', imageUrl: 'https://wger.de/media/exercise-images/71/Cable-crossover-2.png' },
      { name: 'Pompes', imageUrl: 'https://wger.de/media/exercise-images/1551/a6a9e561-3965-45c6-9f2b-ee671e1a3a45.png' },
      { name: 'Pompes diamant', imageUrl: 'https://wger.de/media/exercise-images/1551/a6a9e561-3965-45c6-9f2b-ee671e1a3a45.png' },
      { name: 'Presse pectorale', imageUrl: 'https://wger.de/media/exercise-images/192/Bench-press-1.png' },
      { name: 'Pec deck', imageUrl: 'https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png' },
    ],
    Espalda: [
      { name: 'Tractions', imageUrl: 'https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg' },
      { name: 'Rowing barre', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Tirage poitrine', imageUrl: 'https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp' },
      { name: 'Rowing haltere', imageUrl: 'https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png' },
      { name: 'Rowing poulie basse', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Tirage nuque', imageUrl: 'https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp' },
      { name: 'Souleve de terre', imageUrl: 'https://wger.de/media/exercise-images/184/1709c405-620a-4d07-9658-fade2b66a2df.jpeg' },
      { name: 'Pullover poulie', imageUrl: 'https://wger.de/media/exercise-images/1634/9a4704d3-1b25-43e3-b244-3885f4d3db87.png' },
      { name: 'Rowing machine', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Tractions supination', imageUrl: 'https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg' },
      { name: 'Rowing en T', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Face pull', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Hyperextensions', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Haussements barre', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
    ],
    Hombros: [
      { name: 'Developpe militaire', imageUrl: 'https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png' },
      { name: 'Elevations laterales', imageUrl: 'https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png' },
      { name: 'Elevations frontales', imageUrl: 'https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png' },
      { name: 'Oiseau', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Developpe Arnold', imageUrl: 'https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png' },
      { name: 'Rowing menton', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Face pull', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Elevations laterales poulie', imageUrl: 'https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png' },
      { name: 'Developpe halteres', imageUrl: 'https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png' },
      { name: 'Shrugs', imageUrl: 'https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png' },
      { name: 'Rotation externe', imageUrl: 'https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png' },
      { name: 'Planche laterale', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
    ],
    'Biceps': [
      { name: 'Curl barre', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl halteres', imageUrl: 'https://wger.de/media/exercise-images/81/Biceps-curl-1.png' },
      { name: 'Curl marteau', imageUrl: 'https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png' },
      { name: 'Curl concentre', imageUrl: 'https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg' },
      { name: 'Curl pupitre', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl poulie', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl 21s', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl araignee', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl barre EZ', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl incline', imageUrl: 'https://wger.de/media/exercise-images/81/Biceps-curl-1.png' },
      { name: 'Curl machine', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
      { name: 'Curl inverse', imageUrl: 'https://wger.de/media/exercise-images/74/Bicep-curls-1.png' },
    ],
    'Triceps': [
      { name: 'Dips paralleles', imageUrl: 'https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png' },
      { name: 'Extension poulie', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Barre au front', imageUrl: 'https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png' },
      { name: 'Extension triceps', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Developpe serre', imageUrl: 'https://wger.de/media/exercise-images/192/Bench-press-1.png' },
      { name: 'Extension au-dessus tete', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Dips banc', imageUrl: 'https://wger.de/media/exercise-images/83/Bench-dips-1.png' },
      { name: 'Extension haltere', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Tirage corde', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
      { name: 'Presse triceps machine', imageUrl: 'https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png' },
      { name: 'Extension poulie haute', imageUrl: 'https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg' },
    ],
    Piernas: [
      { name: 'Squat', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Presse a cuisses', imageUrl: 'https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp' },
      { name: 'Extension jambes', imageUrl: 'https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp' },
      { name: 'Curl ischio-jambiers', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
      { name: 'Fentes', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Squat bulgare', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Hack squat', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Souleve de terre roumain', imageUrl: 'https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp' },
      { name: 'Mollets debout', imageUrl: 'https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp' },
      { name: 'Squat goblet', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Mollets presse', imageUrl: 'https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp' },
      { name: 'Step up', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Squat sumo', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Abducteurs', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
      { name: 'Adducteurs', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
      { name: 'Leg curl assis', imageUrl: 'https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png' },
    ],
    Abdominales: [
      { name: 'Crunch', imageUrl: 'https://wger.de/media/exercise-images/91/Crunches-1.png' },
      { name: 'Planche', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Releve de jambes', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Russian twist', imageUrl: 'https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png' },
      { name: 'Crunch poulie', imageUrl: 'https://wger.de/media/exercise-images/91/Crunches-1.png' },
      { name: 'Ab wheel', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Mountain climbers', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Planche laterale', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Crunch bicyclette', imageUrl: 'https://wger.de/media/exercise-images/91/Crunches-1.png' },
      { name: 'V-ups', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Dead bug', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Hollow hold', imageUrl: 'https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png' },
      { name: 'Releve jambes suspendues', imageUrl: 'https://wger.de/media/exercise-images/125/Leg-raises-2.png' },
      { name: 'Woodchop', imageUrl: 'https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png' },
    ],
    'Gluteos': [
      { name: 'Hip thrust', imageUrl: 'https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg' },
      { name: 'Souleve terre roumain', imageUrl: 'https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp' },
      { name: 'Kickback fessier', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
      { name: 'Pont fessier', imageUrl: 'https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg' },
      { name: 'Squat sumo', imageUrl: 'https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg' },
      { name: 'Step up', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Abduction hanche', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
      { name: 'Kickback poulie', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
      { name: 'Clamshell', imageUrl: 'https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg' },
      { name: 'Squat bulgare', imageUrl: 'https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png' },
      { name: 'Souleve terre sumo', imageUrl: 'https://wger.de/media/exercise-images/184/1709c405-620a-4d07-9658-fade2b66a2df.jpeg' },
      { name: 'Fire hydrant', imageUrl: 'https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg' },
    ],
  },
};
