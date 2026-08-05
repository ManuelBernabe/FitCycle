using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

/// <summary>
/// End-to-end tests for the "PLAN DE ENTRENAMIENTO MANU JULIO FINISH" PDF. This plan wraps
/// long exercise headers across PDF lines and mixes bold coaching notes between tables,
/// which used to import a dozen bogus "exercises" ("PARA QUE SEA SUFICIENTE RECORRIDO…",
/// "ABAJO ESTA COLOCADA", "Codo completamente estirado…", "1 MINUTO DE DESCANSO X SERIE",
/// "FATIGADO DEL ANTERIOR", "+super serie con" fragments…) that also STOLE the rep table
/// from the real exercise above them.
///
/// The embedded text below is the raw pre-fix extraction (worst case: the wrapped header
/// continuations still carry bogus [EX] markers) so these tests prove the parser rejects
/// them even when the extraction layer lets them through.
/// </summary>
public class PdfImportManuJulioTests
{
    private const string JulioText = """
        --- Pagina 2 ---
        PLAN DE ENTRENAMIENTO MANU
        PECTORAL+HOMBRO (DÍA 1)
        1º.Movilidad articular (hombros, codos muñecas,cuello,etc)
        CALENTAMIENTO DE TRÍCEPS PREVIO A LOS EMPUJES, SIN ALCANZAR FATIGA
        Tríceps barra recta desde polea alta 3X10 reps
        [EX] APERTURAS INCLINADAS CON MANCUERNAS
        Serie 1 2 3 4
        Reps 15 12 10 8
        FASE POSITIVA 2 seg 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 3 seg 4 seg
        SEG. EJECUCIÓN
        Tiempo de descanso: 1 minuto
        [EX] PRESS PLANO EN BANCO CON MANCUERNAS
        Serie 1 2 3 4
        Reps 15 12 10 8
        FASE POSITIVA 2 seg 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 3 seg 4 seg
        SEG. EJECUCIÓN

        --- Pagina 3 ---
        Descanso 1m 1m 1m 1m
        [EX] APERTURAS EN BANCO PLANO (FIJATE EN LA DISTANCIA QUE PONES EL BANCO
        [EX] PARA QUE SEA SUFICIENTE RECORRIDO Y NO QUEDARTE CORTO)
        Serie 1 2 3 4 MAX PESO
        Reps 12 10 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 3 3 4 2
        Seg. Ejecución
        Tiempo de descanso: 1 minuto

        --- Pagina 4 ---
        [EX] PRESS AGARRE NEUTRO NAUTILIUS (EL CONTRARIO AL DE LA FOTO)
        Serie 1 2 3 4
        Reps 15 12 10 8
        FASE POSITIVA 2 seg 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 3 seg 4 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m 1m

        --- Pagina 5 ---
        [EX] PRESS DECLINADO EN BARRA
        2 SEMANAS Y 2 SEMANAS EN MÁQUINA FRENTE AL ASEO DE
        [EX] ABAJO ESTA COLOCADA
        Serie 1 2 3 4
        Reps 15 12 10 8 +PESO
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 3 4 3 3
        Seg. Ejecución
        Tiempo de descanso: 1 minuto por serie
        HOMBRO DÍA 1
        [EX] Elevación frontal de hombro con mancuerna- agarre neutro-como en la foto:
        Codo completamente estirado, sin involucrar al bíceps flexionandolo
        Los dos pies en el suelo y el banco menos inclinado que en la imagen y un brazo primero y luego el otro,
        Dual no, mantenemos espalda recta constantemente (pecho arriba y cadera atrás)
        Serie 1 2 3
        Reps 15 12 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 2 2 2
        Seg. Ejecución
        Descanso 90s 90s 90s
        Intentamos manejar máximo peso, siempre que no flexionemos el codo de más y estemos tirando mucho de
        bíceps en la elevación frontal

        --- Pagina 6 ---
        [EX] ELEVACIÓN LATERAL DESDE POLEA BAJA
        El cable siempre situarlo por detrás del cuerpo, justo al revés que hace este chico
        Serie 1 2 3
        Reps 10 10 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 3 3 3
        Seg. Ejecución
        Descanso 60s 60s 60s
        [EX] PRESS MILITAR EN BARRA MULTIPOWER (Adelanta los codos en la ejecución)
        Serie 1 2 3
        Reps 15 12 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 3 3 4
        Seg. Ejecución
        Descanso 60s 60s 60s

        --- Pagina 7 ---
        DÍA 2 FEMORAL + ABDUCTOR+ ADUCTOR+ GLUTEO
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

        --- Pagina 8 ---
        [EX] Glúteo en máquina de elevación de lumbar: Nos posicionamos encorvando la espalda (espalda recta no)
        [EX] Hiperextensiones enfoque glúteo
        4 series con peso añadido (disco)
        Haremos pausa al final del recorrido de la fase positiva (como en la imagen, contrayendo glúteo)
        Y en la fase negativa haremos otra pausa, pisando fuerte atrás, generando tensión desde el arranque
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

        --- Pagina 9 ---
        recuerda no sacar mucho el lumbar y tenerlo en una posición neutra
        Fase negativa: Vuelve en 2-3 segundos sin intentar perder esa tensión
        Fase positiva: 2 segundos, recuerda siempre abrir un poco rodilla hacía afuera antes de hacer la patada
        Descanso: 1 minuto
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

        --- Pagina 10 ---
        Seg. Ejecución
        Descanso 1m 1m 1m 1m
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
        DÍA 3 ESPALDA+ BÍCEPS+ TRÍCEPS
        Calentamos los bíceps con 3 series*10reps de curl predicador con un peso ligero

        --- Pagina 11 ---
        [EX] Pull over en máquina hummer:
        Serie 1 2 3 4
        Reps 15 12 10 8
        FASE POSITIVA 2 seg 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 3 seg 3 seg 3 seg 4 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m 1m
        [EX] REMO AGARRE NEUTRO (Fijate en la posición, alcanza máximo recorrido abajo,
        manteniendo siempre la espalda recta) EN TODO MOMENTO PECHO ARRIBA, CADERA
        [EX] ATRÁS
        1 MINUTO DE DESCANSO X SERIE
        Serie 1 2 3 4
        Reps 15 12 10 8

        --- Pagina 12 ---
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 3 2
        Seg. Ejecución
        [EX] Elevación al mentón (trapecio) con barra montada o desde polea baja y barra
        Hacemos pausa al alcanzar máximo recorrido como en la segunda figura (de 2-3 segundos)
        Serie 1 2 3
        Reps 12 10 8
        FASE POSITIVA 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 3 seg 3 seg 3 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m

        --- Pagina 13 ---
        [EX] Pájaro en banco inclinado mancuernas
        Serie 1 2 3
        Reps 12 10 8
        FASE POSITIVA 1 seg 1 seg 1 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 1 seg 1 seg 1 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m
        [EX] Remo desde polea alta agarre supino abierto y cerrado

        --- Pagina 14 ---
        Serie 1 2 3 4
        Agarre cerrado
        Agarre cerrado Agarre abierto Agarre abierto
        supino
        supino supino supino
        Reps 15 12 10 8
        MAX PESO
        FASE POSITIVA 2 seg 2 seg 2 seg 2
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 4 seg 2 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m 1m

        --- Pagina 15 ---
        [EX] Remo agarre prono- dorsal
        Serie 1 2 3 4
        Reps 15 12 10 8
        MAX PESO
        FASE POSITIVA 2 seg 2 seg 2 seg 2
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 4 seg 2 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m 1m

        --- Pagina 16 ---
        [EX] BARRA LIBRE Z (CURL BÍCEPS CON BANCO INCLINADO)
        Serie 1 2 3
        Reps 12 10 10+KG
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 4 3 2
        Seg. Ejecución
        Descanso 1m 1m 1m
        [EX] TRÍCEPS DESDE POLEA BAJA- CON BARRA RECTA- BANCO INCLINADO
        Banco menos inclinado que en la foto, codos como referencia miran al techo constantemente
        Y los matenemos cerrados, pisada atrás y conseguimos el arco lumbar con su retracción escapular

        --- Pagina 17 ---
        Serie 1 2 3
        Reps 15 12 8
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 4
        Seg. Ejecución
        Descanso 1m 1m 1m
        AL ALCANZAR MÁXIMO RECORRIDO (Fase negativa) EN POLEA BAJA TRÍCEPS, HACEMOS UNA MINIPAUSA, GANAMOS
        POSICIÓN Y REALIZAMOS LA EJECUCIÓN
        [EX] MARTILLO BÍCEPS MANCUERNAS (CON UN PESO CONTROLADO YA QUE VIENE EL BÍCEPS
        [EX] FATIGADO DEL ANTERIOR
        Serie 1 2 3
        Reps 10 8 8
        FASE POSITIVA 2 seg 2 seg 2 seg
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 3 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m
        [EX] Tríceps en máquina de fondos (con los codos cerrados)
        Serie 1 2 3 4
        Buscamos mayor kg
        Reps 15 12 8 6
        Fase positiva 2 2 2 2

        --- Pagina 18 ---
        Seg. Ejecución
        Fase negativa 2 3 4 3
        Seg. Ejecución
        Descanso 1m 1m 1m 1m

        --- Pagina 19 ---
        CUADRÍCEPS+ ABDUCTOR+ ADUCTOR+ GEMELO (DÍA 4)
        CALENTAMIENTO DE 3 SERIES DE EXTENSIÓN DE CUADRÍCEPS X 10 REPS (PESO LIGERO)
        [EX] SENTADILLA EN MULTIPOWER
        Serie 1 2 3 4
        Reps 15 12 10 8
        MAX PESO
        FASE POSITIVA 2 seg 2 seg 2 seg 2
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 4 seg 2 seg
        SEG. EJECUCIÓN
        Descanso 90s 90s 90s 90s
        [EX] EXTENSIÓN DE CUADRÍCEPS DUAL
        Serie 1 2 3 4
        Reps 20 15 12 10
        MAX PESO
        FASE POSITIVA 2 seg 2 seg 2 seg 2
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 4 seg 2 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m 1m

        --- Pagina 20 ---
        [EX] Abductor:
        Serie 1 2 3
        Reps 20 12 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 2 4 4
        Seg. Ejecución
        Tiempo de descanso 1m 1m 1m
        [EX] PESO MUERTO SUMO
        FIJATE EN LA PISADA QUE ADOPTA EN LA IMAGEN

        --- Pagina 21 ---
        Serie 1 2 3 4
        Reps 15 12 10 8
        MAX PESO
        FASE POSITIVA 2 seg 2 seg 2 seg 2
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 4 seg 2 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m 1m
        [EX] PRENSA CYBEX
        EJECUCIÓN DE MANERA DUAL
        Serie 1 2 3 4
        Reps 8-10 8-10 8-10 8-10
        FASE POSITIVA 2 seg 2 seg 2 seg 2
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 4 seg 2 seg
        SEG. EJECUCIÓN
        Descanso 2m 2m 2m 2m
        TODAS LAS SERIES INTENTAMOS HACERLAS CON EL 80% DE PESO DE NUESTRA MÁXIMA
        [EX] ADUCTOR:
        Serie 1 2 3 4
        Reps 20 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 4 4 3-4

        --- Pagina 22 ---
        Seg. Ejecución
        Tiempo de descanso 1m 1m 1m 1m
        DÍA 5 PECHO ESPALDA HOMBRO BICEPS TRÍCEPS
        Calentamiento:
        LATERALES PESO LIGERO 2 SERIES DE 10 REPS--- 1 MINUTO DE DESCANSO POR SERIE
        BÍCEPS (CURL PREDICADOR) 2 SERIES DE 10 REPS--- 1 MINUTO DE DESCANSO POR SERIE
        TRÍCEPS EN BARRA 2 SERIES DE 10 REPS--- 1 MINUTO DE DESCANSO POR SERIE
        [EX] PRESS DE PIE
        Serie 1 2 3 4
        Reps 15 12 10 8
        MAX PESO
        FASE POSITIVA 2 seg 2 seg 2 seg 2
        SEG. EJECUCIÓN
        FASE NEGATIVA 2 seg 3 seg 4 seg 2 seg
        SEG. EJECUCIÓN
        Descanso 1m 1m 1m 1m

        --- Pagina 23 ---
        [EX] REMO EN T
        [EX] EN ESTE CASO COJEREMOS EL AGARRE DE ABAJO Y CODOS HACÍA ADENTRO NO ABIERTOS
        Serie 1 2 3 4
        Reps 20 12 10 8
        Fase positiva 2 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 3 2
        Seg. Ejecución
        Tiempo de descanso: 1 minuto por serie
        [EX] APERTURAS INCLINADAS EN MÁQUINA
        Serie 1 2 3
        Reps 10 10 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 3 3 3
        Seg. Ejecución
        Tiempo de descanso 1m 1m 1m

        --- Pagina 24 ---
        [EX] PAJARO DESDE POLEA ALTA CON AMBOS BRAZOS
        Serie 1 2 3
        Reps 10 10 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 3 3 3
        Seg. Ejecución
        NO ES UN EJERCICIO DE MANEJAR GRANDES CARGAS, MANTENER TENSIÓN CONSTANTE Y PESO LIGERO
        HACEMOS PAUSA AL FINAL DE RECORRIDO DE 2 SEGUNDOS
        [EX] Agarre supino en máquina que está enfrente de abdomen
        ESTA MÁQUINA PERO AGARRE SUPINO,CODOS HACÍA DENTRO

        --- Pagina 25 ---
        Serie 1 2 3
        Reps 15 12 8
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 2 3 4
        Seg. Ejecución
        Tiempo de descanso: 1 minuto por serie
        [EX] ELEVACIÓN FRONTAL- AGARRE SUPINO
        Serie 1 2 3
        12
        Reps 10 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 2 2 2
        Seg. Ejecución
        Descanso 1m 1m 1m

        --- Pagina 26 ---
        [EX] LATERALES EN MÁQUINA HUMMER SENTADO
        Serie 1 2 3
        12
        Reps 10 10
        Fase positiva 2 2 2
        Seg. Ejecución
        Fase negativa 2 2 2
        Seg. Ejecución
        Descanso 1m 1m 1m
        [EX] REMO CON BARRA AGARRE PRONO
        Serie 1 2
        Agarre estrecho Agarre como en la
        foto
        Reps 15 12
        Fase positiva 2 2
        Seg. Ejecución
        Fase negativa 2 3
        Seg. Ejecución
        Descanso 1m 1m

        --- Pagina 27 ---
        [EX] CURL PREDICADOR EN MÁQUINA DUAL (ambas mano agarre)
        Serie 1 2
        Reps 15 12
        Fase positiva 2 2
        Seg. Ejecución
        Fase negativa 2 3
        Seg. Ejecución
        Descanso 1m 1m
        [EX] BÍCEPS BARRA MONTADA (SE SITUAN FRENTE AL ESPEJO)
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

        --- Pagina 28 ---
        Descanso 1m 1m
        [EX] TRÍCEPS EN CUERDA
        Serie 1 2
        Reps 15 12
        Fase positiva 2 2
        Seg. Ejecución
        Fase negativa 2 2
        Seg. Ejecución
        Descanso 1m 1m
        [EX] TRÍCEPS EN BARRA CORTA
        Serie 1 2
        Reps 15 12
        Fase positiva 2 2
        Seg. Ejecución
        Fase negativa 2 2
        Seg. Ejecución
        Descanso 1m 1m

        --- Pagina 30 ---
        [EX] Abductor:

        --- Pagina 31 ---
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

        --- Pagina 32 ---
        g
        """;

    private static PdfExtraction Parse() => LocalPdfParser.Parse(JulioText);

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
    public void Dia1_WarmupAndWrappedHeaderNotes_DoNotBecomeExercises()
    {
        var dia1 = Day(1);
        AssertNoExerciseContaining(dia1,
            "Calentamiento",          // "CALENTAMIENTO DE TRÍCEPS PREVIO A LOS EMPUJES…"
            "Barra Recta Desde",      // "Tríceps barra recta desde polea alta 3X10 reps" (warm-up)
            "Suficiente Recorrido",   // wrapped tail of APERTURAS EN BANCO PLANO
            "Semanas",                // "2 SEMANAS Y 2 SEMANAS EN MÁQUINA…"
            "Colocada",               // "ABAJO ESTA COLOCADA"
            "Codo");                  // "Codo completamente estirado…"

        Assert.Equal(8, dia1.Exercises.Count);
    }

    [Fact]
    public void Dia1_TablesAttachToTheRealExercises_NotToTheNotes()
    {
        var dia1 = Day(1);

        // Before the fix the wrapped continuation "PARA QUE SEA SUFICIENTE RECORRIDO…"
        // became its own exercise and stole this 12/10/10/8 table.
        Assert.Equal(new[] { 12, 10, 10, 8 },
            Find(dia1, "Aperturas En Banco Plano").Sets.Select(s => s.Reps));

        // …and "ABAJO ESTA COLOCADA" stole this one from Press declinado.
        Assert.Equal(new[] { 15, 12, 10, 8 },
            Find(dia1, "Press Declinado").Sets.Select(s => s.Reps));

        // …and "Codo completamente estirado…" stole this one from Elevación frontal.
        Assert.Equal(new[] { 15, 12, 10 },
            Find(dia1, "Elevación Frontal").Sets.Select(s => s.Reps));
    }

    [Fact]
    public void Dia2_SupersetTableFragments_DoNotBecomeConExercises()
    {
        var dia2 = Day(2);

        // "+super serie con / mitad de peso / 8reps" cell fragments used to create
        // exercises literally named "Con" and "Con +2 Series".
        Assert.DoesNotContain(dia2.Exercises, e =>
            e.Name!.Equals("Con", StringComparison.OrdinalIgnoreCase) ||
            e.Name!.StartsWith("Con ", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(new[] { 15, 12, 10, 8 },
            Find(dia2, "Femoral Unilateral").Sets.Select(s => s.Reps));
    }

    [Fact]
    public void Dia2_GluteoLumbarHeaderAndHiperextensiones_AreOneExercise()
    {
        var dia2 = Day(2);

        // "Glúteo en máquina de elevación de lumbar: Nos posicionamos…" is a section-style
        // header for the very next green line "Hiperextensiones enfoque glúteo" — they must
        // import as ONE exercise, named after the concrete movement, with the header kept
        // as a note and "4 series con peso añadido" captured as 4 sets.
        AssertNoExerciseContaining(dia2, "Glúteo En Máquina");

        var hiper = Find(dia2, "Hiperextensiones");
        Assert.Equal(4, hiper.Sets.Count);
        Assert.Contains("Glúteo", hiper.Notes ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dia3_HeaderContinuationsAndRestNote_DoNotStealRemoTable()
    {
        var dia3 = Day(3);
        AssertNoExerciseContaining(dia3,
            "Atrás",                  // wrapped tail of REMO AGARRE NEUTRO header
            "Minuto",                 // "1 MINUTO DE DESCANSO X SERIE"
            "Fatigado",               // wrapped tail of MARTILLO BÍCEPS header
            "Posición");              // "POSICIÓN Y REALIZAMOS LA EJECUCIÓN"

        Assert.Equal(new[] { 15, 12, 10, 8 },
            Find(dia3, "Remo Agarre Neutro").Sets.Select(s => s.Reps));
        Assert.Equal(new[] { 10, 8, 8 },
            Find(dia3, "Martillo").Sets.Select(s => s.Reps));
    }

    [Fact]
    public void ExerciseNames_KeepDescriptiveTails_ButDropCoachingNotes()
    {
        var dia3 = Day(3);

        // The trainer's full wording is part of the exercise identity and must survive:
        // "(trapecio)" and everything after it used to be truncated at the parenthesis.
        Assert.Equal("Elevación Al Mentón (Trapecio) Con Barra Montada O Desde Polea Baja Y Barra",
            Find(dia3, "Elevación Al Mentón").Name);
        Assert.Contains("Curl Bíceps Con Banco Inclinado", Find(dia3, "Barra Libre Z").Name!);
        Assert.Contains("Con Barra Recta", Find(dia3, "Tríceps Desde Polea Baja").Name!);

        // …but pure coaching instructions still never reach the name.
        Assert.DoesNotContain("Fijate", Find(dia3, "Remo Agarre Neutro").Name!,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Contrario", Find(Day(1), "Press Agarre Neutro Nautilius").Name!,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Foto", Find(Day(1), "Elevación Frontal De Hombro").Name!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dia4_CoachingNotes_DoNotBecomeExercises_AndRangesAreOneSet()
    {
        var dia4 = Day(4);
        AssertNoExerciseContaining(dia4,
            "Fijate",                 // "FIJATE EN LA PISADA QUE ADOPTA EN LA IMAGEN"
            "Ejecución De Manera",    // "EJECUCIÓN DE MANERA DUAL"
            "Todas Las Series");      // "TODAS LAS SERIES INTENTAMOS HACERLAS…"

        Assert.Equal(new[] { 15, 12, 10, 8 },
            Find(dia4, "Peso Muerto Sumo").Sets.Select(s => s.Reps));

        // "Reps 8-10 8-10 8-10 8-10" is FOUR sets (range = one set), not eight.
        Assert.Equal(new[] { 10, 10, 10, 10 },
            Find(dia4, "Prensa Cybex").Sets.Select(s => s.Reps));
    }

    [Fact]
    public void Dia5_ContinuationAndPauseNotes_DoNotBecomeExercises()
    {
        var dia5 = Day(5);
        AssertNoExerciseContaining(dia5,
            "Este Caso",              // "EN ESTE CASO COJEREMOS EL AGARRE DE ABAJO…"
            "Hacemos Pausa");         // "HACEMOS PAUSA AL FINAL DE RECORRIDO…"

        Assert.Equal(new[] { 20, 12, 10, 8 },
            Find(dia5, "Remo En T").Sets.Select(s => s.Reps));
    }

    [Fact]
    public void Dia5_TrailingLeftoverPages_DuplicatingDia2_AreDropped()
    {
        var dia5 = Day(5);

        // Pages 30-31 of the PDF repeat día 2's Abductor / Femoral sentado / Aductor /
        // Gemelos block after día 5 ends — a copy-paste leftover that must not import.
        AssertNoExerciseContaining(dia5, "Abductor", "Femoral", "Aductor", "Gemelo");

        Assert.Contains("Tríceps En Barra Corta", dia5.Exercises[^1].Name!);
    }
}
