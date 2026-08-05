using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

/// <summary>
/// End-to-end tests for the "PLAN DE ENTRENAMIENTO PACO JULIO" PDF (the maintenance plan
/// imported into the second profile). Its quirks differ from the MANU plan: bold notes
/// between tables ("Aumenta peso por serie", the serie-descendente block), warm-up
/// prescriptions with glued tokens ("3 seriesx10 reps", "2 series x 10reps"), intensity
/// cells wrapped out of tables ("Ligero peso Max peso+"), "+ SUPERSERIE …" technique tails,
/// a photo pushing the Pájaro header BELOW its own table, and a día 5 that legitimately
/// repeats día 2's Aductor/Gemelos (which the trailing-duplicate suppression must keep).
/// The embedded text is the raw extraction of the real PDF.
/// </summary>
public class PdfImportPacoJulioTests
{
    private const string PacoText = """
        --- Pagina 2 ---
        PLAN DE ENTRENAMIENTO PACO
        PECTORAL+TRÍCEPS+BÍCEPS (DÍA 1)
        1º.Movilidad articular (hombros, codos muñecas,cuello,etc)
        CALENTAMIENTO DE TRÍCEPS PREVIO A LOS EMPUJES, SIN ALCANZAR FATIGA
        Tríceps barra recta desde polea alta 3X10 reps
        [EX] PRESS DECLINADO EN BARRA
        1. (CALENTAMIENTO) 15 Reps con peso ligero
        4*15*12*10*8
        Intenta ir aumentando pesos, llegando a las repeticiones marcadas
        Serie 1 2 3 4
        Reps 15 12 10 8
        FASE POSITIVA 2 seg 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 3 seg 4 seg
        SEG. EJECUCIÓN
        Tiempo de descanso: 1 minuto
        
        --- Pagina 3 ---
        [EX] APERTURA INCLINADA DESDE CABLES, CON BANCO INCLINADO
        Serie 1 2 3 4 MAX PESO
        Reps 15 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 3 3 4 2
        Seg. Ejecución
        Tiempo de descanso: 1 minuto
        
        --- Pagina 4 ---
        [EX] PRESS INCLINADO EN BARRA:
        Serie 1 2 3 4 MAX PESO
        Reps 15 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 3 3 4 2
        Seg. Ejecución
        Descanso 1 1 1 1
        [EX] CABLES DESDE POLEA A UNA ALTURA DEL NÚMERO
        5 (Desde arriba abajo, fijándonos en el
        trabajo de la zona inferior del pectoral)
        Serie 1 2 3 4
        Buscamos mayor kg
        Reps 15 12 10 10
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 2 4 2
        Seg. Ejecución
        Descanso 1m 1m 1m 1m
        Recuerda en los cruces en poleas tener siempre los hombros por detrás del pectoral.
        
        --- Pagina 5 ---
        [EX] APERTURAS EN MÁQUINA PLANA
        Serie 1 2 3
        Buscamos mayor kg
        Reps 15 12 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 3 3 2
        Seg. Ejecución
        Por serie vamos aumentando carga, siempre respetando los segundos puestos de ejecución
        en la tabla.
        Tiempo de descanso: 1 minuto por serie
        [EX] FONDOS DE PECTORAL EN MÁQUINA
        Serie 1 2 3 4
        Reps 15 12 10 8 +PESO
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 3 4 3 3
        
        --- Pagina 6 ---
        Seg. Ejecución
        Tiempo de descanso: 1 minuto por serie
        [EX] TRÍCEPS EN CUERDA
        Serie 1 2 3 4
        Reps 15 12 10 8
        FASE POSITIVA 2 seg 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 3 seg 2 seg
        SEG. EJECUCIÓN
        Tiempo de descanso: 1 minuto por serie
        Aumenta peso por serie
        [EX] TRÍCEPS EN BARRA RECTA
        Serie 1 2 3 4
        Reps 15 12 10 8
        FASE POSITIVA 2 seg 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 3 seg 2 seg
        SEG. EJECUCIÓN
        Tiempo de descanso: 1 minuto por serie
        Aumenta peso por serie
        
        --- Pagina 7 ---
        [EX] CURL PREDICADOR DUAL (DOS BRAZOS)
        + SUPERSERIE UNILATERAL FALLO
        Serie 1 2 3
        Reps 15 12 10
        FASE POSITIVA 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 3 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m
        [EX] MARTILLO BÍCEPS MANCUERNAS (CON UN PESO CONTROLADO YA QUE VIENE EL BÍCEPS
        FATIGADO DEL ANTERIOR
        Serie 1 2 3
        Reps 10 8 8
        FASE POSITIVA 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 3 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m
        
        --- Pagina 8 ---
        CUADRÍCEPS+ ABDUCTOR+ ADUCTOR+ GEMELO (DÍA 2)
        [EX] ZANCADA EN EL PASILLO DE MAQUINAS DE PECHO O EN EL DE LA ENTRADA
        4 Series* 15 zancadas por ambas piernas
        1 minuto de descanso por serie
        [EX] EXTENSIÓN DE CUADRÍCEPS DUAL
        Serie 1 2 3 4
        Reps 20 15 12 10
        MAX PESO
        FASE POSITIVA 2 seg 2 seg 2 seg 2
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 4 seg 2 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m 1m
        
        --- Pagina 9 ---
        [EX] Similar a sentadilla: PISADA NORMAL (PUNTAS RECTAS)
        Serie 1 2 3 4
        MAX PESO
        Reps 15 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 4 2
        (2 seg de pausa (2 seg de pausa
        Seg. Ejecución (2 seg de pausa
        final de recorrido) final de recorrido)
        final de recorrido)
        Tiempo de descanso 90s 90s 90s 90s
        EN LA SERIE 4, HACEMOS UNA SERIE DESCENDENTE (SIN DESCANSO, DESCARGAMOS
        DISCOS Y ENTRAMOS DE VUELTA, CON LA MITAD DE CARGA) CON MITAD DE PESO DEL QUE
        HEMOS ALCANZADO COMO MÁXIMO
        
        --- Pagina 10 ---
        [EX] Abductor:
        Serie 1 2 3 4
        Procura ir aumentando peso por serie.
        Reps 20 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 4 4 3-4
        Seg. Ejecución
        Tiempo de descanso 1m 1m 1m 1m
        
        --- Pagina 11 ---
        [EX] PISADA TIPO SUMO
        Serie 1 2 3 4
        MAX PESO
        Reps 15 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 4 2
        (2 seg de pausa (2 seg de pausa
        Seg. Ejecución (2 seg de pausa
        final de recorrido) final de recorrido)
        final de recorrido)
        Tiempo de descanso 90s 90s 90s 90s
        
        --- Pagina 12 ---
        [EX] ADUCTOR:
        Serie 1 2 3 4
        Reps 20 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 4 4 3-4
        Seg. Ejecución
        Tiempo de descanso 1m 1m 1m 1m
        [EX] GEMELO SENTADO
        2 SERIES X 15 REPS
        [EX] GEMELO DE PIE
        2 SERIES X 15 REPS
        ESPALDA+ BÍCEPS+TRÍCEPS (DÍA 3)
        1º.Movilidad articular (hombros, codos muñecas,cuello,etc)
        A continuación antes de introducirnos en los tirones, calentaremos previamente bíceps sin alcanzar
        fatiga.
        Curl predicador de bíceps 3 seriesx10 reps
        [EX] REMO AGARRE NEUTRO (Fijate en la posición, alcanza máximo recorrido abajo,
        manteniendo siempre la espalda recta) EN TODO MOMENTO PECHO ARRIBA, CADERA
        ATRÁS
        1 MINUTO DE DESCANSO X SERIE
        Serie 1 2 3 4
        
        --- Pagina 13 ---
        Reps 15 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 3 2
        Seg. Ejecución
        Serie 1 2 3
        Reps 10 10 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 3 3 3
        Seg. Ejecución
        [EX] PAJARO DESDE POLEA ALTA CON AMBOS BRAZOS
        
        --- Pagina 14 ---
        NO ES UN EJERCICIO DE MANEJAR GRANDES CARGAS, MANTENER TENSIÓN CONSTANTE Y PESO LIGERO
        HACEMOS PAUSA AL FINAL DE RECORRIDO DE 2 SEGUNDOS
        [EX] REMO EN T
        EN ESTE CASO COJEREMOS EL AGARRE DE ABAJO Y CODOS HACÍA ADENTRO NO ABIERTOS
        Serie 1 2 3 4
        Reps 20 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 3 2
        Seg. Ejecución
        Tiempo de descanso: 1 minuto por serie
        [EX] Agarre supino en máquina que está enfrente de abdomen
        ESTA MÁQUINA PERO AGARRE SUPINO,CODOS HACÍA DENTRO
        
        --- Pagina 15 ---
        Serie 1 2 3 4
        Buscamos mayor kg
        Reps 15 12 8 6
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 4 2
        Seg. Ejecución
        +SUPERSERIE DE TRAPECIO (ENCOJIMIENTO) EN LA MÁQUINA DE PESO MUERTO ENFRENTE
        DE LAS ESCALERAS DE CARDIO (12-10-8-8reps, sube peso por serie)
        Tiempo de descanso: 1 minuto por serie
        [EX] POSTERIOR EN PRESS MILITAR DE MÁQUINA HUMMER
        [EX] Te posicionaras al contrario mirando hacía el respaldo, codos ligeramente hacía detrás
        y dejamos caer el tronco hacía delante, pisada atrás de pies
        Serie 1 2 3
        Reps 15 12 8
        
        --- Pagina 16 ---
        Fase positiva 3 3 2
        Seg. Ejecución
        Fase negativa 3 3 4
        Seg. Ejecución
        Tiempo de descanso: 1 minuto por serie
        [EX] PULL OVER
        Posiciona una altura media en la polea, para cuando lleves peso no sufrir en articulaciones
        4*20*15*10*10
        En este ejercicio maneja un peso controlado ya que es el último ejercicio y quiero que
        busques el bombeo, ósea control y llevar sangre a la zona.
        Tiempo de descanso: 1 minuto por serie
        [EX] REMO CON BARRA AGARRE PRONO
        
        --- Pagina 17 ---
        Serie 1 2 3 4
        Agarre estrecho Agarre estrecho Agarre como en la Agarre como en la
        foto foto
        Reps 15 12 8 6
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 4 2
        Seg. Ejecución BÍCEPS:
        Descanso 1m 1m 1m 1m
        [EX] CURL PREDICADOR EN MÁQUINA DUAL (ambas mano agarre)
        Serie 1 2 3 4
        Buscamos mayor kg
        Reps 15 12 8 6
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 4 3
        Seg. Ejecución
        Descanso 1m 1m 1m 1m
        EN LAS 4 SERIES: añadimos super serie unilateral (con cada brazo) al fallo con peso
        [EX] controlado
        
        --- Pagina 18 ---
        [EX] BARRA MONTADA (SE SITUAN FRENTE AL ESPEJO)
        AGARRE SUPINO ABIERTO
        Serie 1 2
        Reps 12 10
        Fase positiva 2 2
        Seg. Ejecución
        Fase negativa 2 3
        Seg. Ejecución
        Descanso 1m 1m
        AGARRE PRONO CERRADO
        Serie 1 2
        Reps 12 10
        Fase positiva 2 2
        Seg. Ejecución
        Fase negativa 2 3
        Seg. Ejecución
        Descanso 1m 1m
        [EX] Gemelo de pie:
        4 series x 20 reps (maneja un peso que controles y evita flexionar rodillas para que vaya todo el peso al
        gemelo)
        
        --- Pagina 19 ---
        HOMBRO + BÍCEPS+ TRÍCEPS: (DÍA 4)
        Calentamiento previo:
        Elevaciones laterales 2 series x 10reps (Peso ligero 2,5kg)
        Elevaciones frontales 2 series x 10 reps
        [EX] Press militar en máquina hummer:
        Serie 1 2 3 4 5
        Reps 20 12 10 10 8
        Ligero peso Max peso+
        + +
        superserie mitad de
        superserie mitad de superserie mitad de
        peso al fallo
        peso al fallo peso al fallo
        Fase positiva 2 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 4 4 2 4
        Seg. Ejecución
        Descanso 90s 90s 90s 90s 90s
        
        --- Pagina 20 ---
        [EX] Elevación frontal de hombro con mancuerna- agarre neutro-como en la foto:
        Codo completamente estirado, sin involucrar al bíceps flexionandolo
        Los dos pies en el suelo y el banco menos inclinado que en la imagen y un brazo primero y luego el otro,
        Dual no, mantenemos espalda recta constantemente (pecho arriba y cadera atrás)
        Serie 1 2 3
        Reps 15 + superserie 12+ superserie 10+ superserie
        mancuernas 2,5kg mancuernas 2,5kg mancuernas 2,5kg
        dual, al fallo dual, al fallo dual, al fallo
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 2 2 2
        Seg. Ejecución
        Descanso 90s 90s 90s
        Intentamos manejar máximo peso, siempre que no flexionemos el codo de más y estemos tirando mucho de
        bíceps en la elevación frontal
        
        --- Pagina 21 ---
        [EX] CURL PREDICADOR EN MÁQUINA DUAL (ambas mano agarre)
        Serie 1 2 3 4
        Buscamos mayor kg
        Reps 15 12 8 6
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 4 3
        Seg. Ejecución
        Descanso 1m 1m 1m 1m
        [EX] TRÍCEPS DESDE POLEA BAJA- CON BARRA RECTA- BANCO INCLINADO
        Banco menos inclinado que en la foto, codos como referencia miran al techo constantemente
        Y los matenemos cerrados, pisada atrás y conseguimos el arco lumbar con su retracción escapular
        Serie 1 2 3 4
        Buscamos mayor kg
        Reps 15 12 8 6
        Fase positiva 2 2 2 2
        Seg. Ejecución
        
        --- Pagina 22 ---
        Fase negativa 2 3 4 3
        Seg. Ejecución
        Descanso 1m 1m 1m 1m
        AL ALCANZAR MÁXIMO RECORRIDO (Fase negativa) EN POLEA BAJA TRÍCEPS, HACEMOS UNA MINIPAUSA, GANAMOS
        POSICIÓN Y REALIZAMOS LA EJECUCIÓN
        [EX] ELEVACIÓN FRONTAL- AGARRE SUPINO
        Serie 1 2 3
        12
        Reps 10 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 2 2 2
        Seg. Ejecución
        Descanso 1m 1m 1m
        
        --- Pagina 23 ---
        [EX] Tríceps en máquina de fondos (con los codos cerrados)
        Serie 1 2 3 4
        Buscamos mayor kg
        Reps 15 12 8 6
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 4 3
        Seg. Ejecución
        Descanso 1m 1m 1m 1m
        
        --- Pagina 24 ---
        [EX] BARRA LIBRE Z (CURL BÍCEPS CON BANCO INCLINADO)
        Serie 1 2 3
        Reps 12 10 10+KG
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 4 3 2
        Seg. Ejecución
        Descanso 1m 1m 1m
        En la serie 3, al finalizarla bajamos a mitad de peso de lo que tengamos y hacemos una
        descendente al fallo
        [EX] Laterales en máquina hummer
        Serie 1 2 3
        Reps 12 10 10+KG
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 4 3 2
        Seg. Ejecución
        Descanso 1m 1m 1m
        +super serie en todas las series de laterales con mancuernas 10 reps (7,5kg)
        
        --- Pagina 25 ---
        FEMORAL+ ABDUCTOR+ ADUCTOR+GLUTEO (DÍA 5)
        [EX] Femoral unilateral en máquina frente espejo:
        Serie 1 2 3 4
        MAX PESO
        Reps 15 12 10 8
        +super serie con
        +super serie con +2 series
        mitad de peso
        mitad de peso descendentes
        8reps
        8reps bajando peso 8reps
        x serie
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 3 4 4 2
        Seg. Ejecución
        Tiempo de descanso 90s 90s 90s 90s
        [EX] Glúteo en máquina de elevación de lumbar: Nos posicionamos encorvando la espalda (espalda recta no)
        [EX] Hiperextensiones enfoque glúteo
        4 series con peso añadido (disco)
        Haremos pausa al final del recorrido de la fase positiva (como en la imagen, contrayendo glúteo)
        Y en la fase negativa haremos otra pausa, pisando fuerte atrás, generando tensión desde el arranque
        
        --- Pagina 26 ---
        [EX] Femoral tumbado:
        Serie 1 2 3 4
        MAX PESO
        Reps 15 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 3 4 4 2
        Seg. Ejecución
        Tiempo de descanso 60s 60s 60s 60s
        [EX] Patada de glúteo:
        Flexionamos la rodilla a unos 90 grados,abrimos la rodilla que no quede mirando hacía dentro y la patada llevando el talón
        hacía arriba, la lumbar no apretarla demasiado
        4 series* 15 reps (Peso ligero si nunca lo habéis ejecutado, simplemente buscando tensión en la zona de glúteo,
        recuerda no sacar mucho el lumbar y tenerlo en una posición neutra
        Fase negativa: Vuelve en 2-3 segundos sin intentar perder esa tensión
        Fase positiva: 2 segundos, recuerda siempre abrir un poco rodilla hacía afuera antes de hacer la patada
        Descanso: 1 minuto
        
        --- Pagina 27 ---
        [EX] Abductor:
        Serie 1 2 3 4
        Reps 20 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 4 4 3-4
        Seg. Ejecución
        Tiempo de descanso 1m 1m 1m 1m
        [EX] Femoral sentado:
        Serie 1 2 3 4
        Calentamiento Max peso
        Reps 15 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 3 2
        Seg. Ejecución
        Descanso 1m 1m 1m 1m
        
        --- Pagina 28 ---
        [EX] ADUCTOR:
        Serie 1 2 3 4
        Reps 20 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 4 4 3-4
        Seg. Ejecución
        Tiempo de descanso 1m 1m 1m 1m
        [EX] GEMELO SENTADO
        2 SERIES X 15 REPS
        [EX] GEMELO DE PIE
        2 SERIES X 15 REPS
        
        """;

    private static PdfExtraction Parse() => LocalPdfParser.Parse(PacoText);

    private static PdfDayRoutine Day(int day) =>
        Parse().Routines.First(r => r.DayOfWeek == day);

    private static PdfExercise Find(PdfDayRoutine day, string nameContains) =>
        day.Exercises.First(e =>
            e.Name!.Contains(nameContains, StringComparison.OrdinalIgnoreCase));

    private static void AssertNoExerciseContaining(PdfDayRoutine day, params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            Assert.DoesNotContain(day.Exercises, e =>
                e.Name!.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void AllFiveDays_AreDetected()
    {
        var days = Parse().Routines.Select(r => r.DayOfWeek).OrderBy(d => d).ToList();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, days);
    }

    [Fact]
    public void Dia1_NotesAndTechniques_DoNotBecomeExercises()
    {
        var dia1 = Day(1);
        AssertNoExerciseContaining(dia1,
            "Aumenta",            // "Aumenta peso por serie" ×2
            "Unilateral Fallo",   // "+ SUPERSERIE UNILATERAL FALLO" is a technique note
            "Calentamiento");

        Assert.Equal(10, dia1.Exercises.Count);
        Assert.Equal(new[] { 15, 12, 10, 8 },
            Find(dia1, "Press Declinado").Sets.Select(s => s.Reps));
        Assert.Equal(new[] { 15, 12, 10 },
            Find(dia1, "Curl Predicador Dual").Sets.Select(s => s.Reps));
    }

    [Fact]
    public void Dia2_SerieDescendenteNote_DoesNotBecomeExercises()
    {
        var dia2 = Day(2);
        AssertNoExerciseContaining(dia2, "Discos", "Hemos Alcanzado");
        Assert.Equal(8, dia2.Exercises.Count);
    }

    [Fact]
    public void Dia3_WarmupAndDisplacedTable_AreHandled()
    {
        var dia3 = Day(3);

        // "Curl predicador de bíceps 3 seriesx10 reps" is the warm-up (glued "seriesx10"
        // used to defeat the prescription detector and it imported as an exercise).
        AssertNoExerciseContaining(dia3, "3 Series");

        // The photo pushes the Pájaro header BELOW its 10/10/10 table: that table must
        // reach Pájaro instead of clobbering Remo agarre neutro's 15/12/10/8.
        Assert.Equal(new[] { 15, 12, 10, 8 },
            Find(dia3, "Remo Agarre Neutro").Sets.Select(s => s.Reps));
        Assert.Equal(new[] { 10, 10, 10 },
            Find(dia3, "Pajaro Desde Polea Alta").Sets.Select(s => s.Reps));

        // "+SUPERSERIE DE TRAPECIO (ENCOJIMIENTO)…" IS a real partner exercise.
        Assert.Contains(dia3.Exercises, e =>
            e.Name!.Contains("Trapecio", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Dia4_WarmupsAndIntensityCells_DoNotBecomeExercises()
    {
        var dia4 = Day(4);
        AssertNoExerciseContaining(dia4,
            "Elevaciones Laterales 2",  // warm-up "2 series x 10reps (Peso ligero 2,5kg)"
            "Ligero Peso");             // wrapped intensity cells "Ligero peso Max peso+"

        Assert.Equal(new[] { 20, 12, 10, 10, 8 },
            Find(dia4, "Press Militar En Máquina Hummer").Sets.Select(s => s.Reps));

        // "+super serie en todas las series de laterales con mancuernas 10 reps" is a
        // technique with an inline rep prescription → note, not a partner exercise.
        AssertNoExerciseContaining(dia4, "Todas Las Series");
    }

    [Fact]
    public void Dia5_LegitimatelyRepeatedLegWork_IsNotSuppressed()
    {
        // Día 5 (FEMORAL+ABDUCTOR+ADUCTOR+GLUTEO) re-trains the Aductor and Gemelos it
        // already did on día 2 — the trailing-duplicate suppression (built for the MANU
        // plan's leftover pages) must keep them because they BELONG to this day's groups.
        var dia5 = Day(5);
        Assert.Contains(dia5.Exercises, e => e.Name!.Contains("Aductor", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(dia5.Exercises, e => e.Name!.Contains("Gemelo Sentado", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(dia5.Exercises, e => e.Name!.Contains("Gemelo De Pie", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(9, dia5.Exercises.Count);
    }
}
