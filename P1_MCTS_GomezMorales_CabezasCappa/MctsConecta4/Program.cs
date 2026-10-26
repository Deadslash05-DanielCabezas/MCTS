using System.Globalization;

namespace MctsConecta4;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string modo = args.Length > 0 ? args[0] : "menu";

        switch (modo)
        {
            case "humano": Humano(); break;
            case "duelo": Duelo(args); break;
            case "barrido": Barrido(args); break;
            case "traza": Traza(); break;
            case "referencia": Referencia(args); break;
            case "calibrar": Calibrar(args); break;
            case "arbol": VolcarArbol(args); break;
            default: Menu(); break;
        }
    }

    private static void Menu()
    {
        Console.WriteLine("MCTS sobre Conecta 4 — IA4, Práctica 1");
        Console.WriteLine();
        Console.WriteLine("  dotnet run -- humano                     jugar contra la IA");
        Console.WriteLine("  dotnet run -- traza                      una jugada, con las estadísticas del árbol");
        Console.WriteLine("  dotnet run -- duelo  [partidas] [iter] [prof] [semilla]");
        Console.WriteLine("                                           MCTS contra Minimax");
        Console.WriteLine("  dotnet run -- barrido [partidas] [semilla]");
        Console.WriteLine("                                           experimento 1: presupuesto de simulaciones");
        Console.WriteLine("  dotnet run -- referencia [partidas] [semilla]");
        Console.WriteLine("                                           LOS CUATRO EXPERIMENTOS → referencia_sNN.csv");
        Console.WriteLine("  dotnet run -- calibrar [semilla]         cuántas iteraciones caben en 5, 10, 25, 50 y 100 ms");
        Console.WriteLine("  dotnet run -- arbol [iteraciones]         vuelca la forma del árbol a CSV");
        Console.WriteLine();
        Console.WriteLine("Valores por defecto: 40 partidas · 2000 iteraciones · profundidad 4 · semilla 42");
        Console.WriteLine("La entrega se genera con:  dotnet run -- referencia 40 42");
    }

    private static void Humano()
    {
        var estado = new Conecta4();
        var ia = new AgenteMcts(iteraciones: 4000, semilla: 42);
        Console.WriteLine("Tú eres X y empiezas. Columna de 0 a 6, 'q' para salir.\n");

        while (!estado.EsTerminal())
        {
            Console.WriteLine(estado.Dibujar());
            if (estado.JugadorEnTurno == 1)
            {
                Console.Write("\nTu columna: ");
                string? linea = Console.ReadLine();
                if (linea is null || linea.Trim() == "q") return;
                if (!int.TryParse(linea, out int col) || !estado.MovimientosLegales().Contains(col))
                {
                    Console.WriteLine("Columna no válida.");
                    continue;
                }
                estado = estado.AplicarC4(col);
            }
            else
            {
                int col = ia.Elegir(estado);
                Console.WriteLine($"\nLa IA juega {col}  ({ia.UltimasIteraciones} simulaciones, {ia.UltimosMs:0} ms)\n");
                estado = estado.AplicarC4(col);
            }
        }
        Console.WriteLine(estado.Dibujar());
        Console.WriteLine(estado.Ganador switch
        {
            0 => "\nTablas.",
            1 => "\nHas ganado.",
            _ => "\nGana la IA."
        });
    }

    private static void Traza()
    {
        // Posición elegida a propósito: el jugador 2 (O) tiene 4, 5 y 6 en la fila de
        // abajo, así que amenaza con cerrar en la 3. Y X NO tiene ninguna victoria
        // inmediata: la única jugada que no pierde es tapar la 3.
        // Lo que hay que mirar NO es si acierta —con la implementación correcta
        // acierta ya con 25 simulaciones—, sino CON CUÁNTA CONFIANZA: a 25 el
        // reparto es 7 visitas contra 5, que es ruido; a 2.000 es 1.680 contra 68.
        // Ésa es la diferencia entre «ha salido» y «lo sabe», y es la que se ve
        // en la diapositiva del comportamiento *anytime* de B1.3a.
        var e = new Conecta4()
            .AplicarC4(0).AplicarC4(4)
            .AplicarC4(0).AplicarC4(5)
            .AplicarC4(1).AplicarC4(6);
        Console.WriteLine(e.Dibujar());
        Console.WriteLine($"\nTurno del jugador {e.JugadorEnTurno} (X). O amenaza cerrar en la columna 3.");
        Console.WriteLine("X no tiene victoria inmediata: la única jugada que no pierde es la 3.\n");

        foreach (int it in new[] { 25, 50, 100, 500, 2000, 10000 })
        {
            var motor = new MotorMcts(semilla: 42);
            var raiz = motor.ElegirConRaiz(e, it, out int mov);
            string reparto = string.Join("  ", MotorMcts.EstadisticasRaiz(raiz)
                .Select(x => $"{x.Movimiento}:{x.Visitas}"));
            Console.WriteLine($"  {it,6} sim → juega {mov} {(mov == 3 ? "OK " : "MAL")}" +
                              $"  {motor.UltimosMs,6:0.0} ms   visitas por columna → {reparto}");
        }
        Console.WriteLine("\n  (columna:visitas, de más a menos visitada)");
    }

    private static void Duelo(string[] args)
    {
        int partidas = Arg(args, 1, 40), iter = Arg(args, 2, 2000),
            prof = Arg(args, 3, 4), semilla = Arg(args, 4, 42);

        Console.WriteLine($"MCTS-{iter}it  contra  Minimax-{prof}   ·   {partidas} partidas   ·   semilla {semilla}\n");
        var r = Torneo.Jugar(
            s => new AgenteMcts(iter, semilla: s),
            s => new AgenteMinimax(prof, semilla: s),
            partidas, semilla);

        var (g, p, t) = Torneo.Resumen(r, $"MCTS-{iter}it");
        Console.WriteLine($"  MCTS gana {g}, pierde {p}, tablas {t}   →   " +
                          $"{100.0 * (g + 0.5 * t) / partidas:0.0} % de puntuación");
        Console.WriteLine($"  ms medios por jugada: MCTS {r.Average(x => x.Blancas.StartsWith("MCTS") ? x.MsMediosBlancas : x.MsMediosNegras):0.0}" +
                          $"   ·   Minimax {r.Average(x => x.Blancas.StartsWith("Minimax") ? x.MsMediosBlancas : x.MsMediosNegras):0.0}");
        Torneo.ExportarCsv($"duelo_{iter}it_prof{prof}_s{semilla}.csv", r);
    }

    private static void Barrido(string[] args)
    {
        int partidas = Arg(args, 1, 40), semilla = Arg(args, 2, 42);
        Console.WriteLine($"Experimento 1 — presupuesto de simulaciones contra Aleatorio y contra Minimax-4");
        Console.WriteLine($"{partidas} partidas por punto · semilla {semilla}\n");
        Console.WriteLine("  iter | vs Aleatorio | vs Minimax-4 |  ms/jugada");
        Console.WriteLine("  -----+--------------+--------------+-----------");

        var filas = new List<string>();
        foreach (int it in new[] { 50, 100, 200, 500, 1000, 2000, 5000 })
        {
            var ra = Torneo.Jugar(s => new AgenteMcts(it, semilla: s),
                                  s => new AgenteAleatorio(s), partidas, semilla);
            var (ga, pa, ta) = Torneo.Resumen(ra, $"MCTS-{it}it");
            double pctA = 100.0 * (ga + 0.5 * ta) / partidas;

            var rm = Torneo.Jugar(s => new AgenteMcts(it, semilla: s),
                                  s => new AgenteMinimax(4, semilla: s), partidas, semilla);
            var (gm, pm, tm) = Torneo.Resumen(rm, $"MCTS-{it}it");
            double pctM = 100.0 * (gm + 0.5 * tm) / partidas;

            double ms = rm.Average(x => x.Blancas.StartsWith("MCTS") ? x.MsMediosBlancas : x.MsMediosNegras);
            Console.WriteLine($"  {it,5} | {pctA,11:0.0} % | {pctM,11:0.0} % | {ms,9:0.0}");
            filas.Add(string.Join(';', new[]
            {
                it.ToString(CultureInfo.InvariantCulture),
                pctA.ToString("0.0", CultureInfo.InvariantCulture),
                pctM.ToString("0.0", CultureInfo.InvariantCulture),
                ms.ToString("0.0", CultureInfo.InvariantCulture),
            }));
        }
        File.WriteAllLines($"barrido_s{semilla}.csv",
            new[] { "iteraciones;pct_vs_aleatorio;pct_vs_minimax4;ms_por_jugada" }.Concat(filas));
        Console.WriteLine($"\n  → barrido_s{semilla}.csv");
    }

    /// <summary>
    /// Corre los cuatro experimentos de la práctica y deja los números en CSV. Es lo
    /// que genera la tabla de referencia del enunciado: si un grupo obtiene algo muy
    /// distinto con la misma semilla, su implementación tiene un fallo.
    /// </summary>
    private static void Referencia(string[] args)
    {
        int partidas = Arg(args, 1, 40), semilla = Arg(args, 2, 42);
        var csv = new List<string> { "experimento;configuracion;rival;pct_mcts;gana;pierde;tablas;ms_por_jugada_mcts" };

        void Medir(string exp, string conf, Func<int, IAgente> mcts, Func<int, IAgente> rival)
        {
            var r = Torneo.Jugar(mcts, rival, partidas, semilla);
            string nom = mcts(0).Nombre, nomRival = rival(0).Nombre;
            var (g, p, t) = Torneo.Resumen(r, nom);
            double pct = 100.0 * (g + 0.5 * t) / partidas;
            double ms = r.Average(x => x.Blancas == nom ? x.MsMediosBlancas : x.MsMediosNegras);
            Console.WriteLine($"  {exp,-4} {conf,-18} vs {nomRival,-18} → {pct,5:0.0} %  ({g}-{p}-{t})  {ms,6:0.0} ms");
            csv.Add(string.Join(';', new[] { exp, conf, nomRival,
                pct.ToString("0.0", CultureInfo.InvariantCulture),
                g.ToString(), p.ToString(), t.ToString(),
                ms.ToString("0.0", CultureInfo.InvariantCulture) }));
        }

        Console.WriteLine($"VALORES DE REFERENCIA · {partidas} partidas por punto · semilla {semilla}\n");

        Console.WriteLine("E1 · presupuesto de simulaciones");
        foreach (int it in new[] { 50, 100, 200, 500, 1000, 2000, 5000 })
        {
            Medir("E1", $"MCTS-{it}it", s2 => new AgenteMcts(it, semilla: s2), s2 => new AgenteAleatorio(s2));
            Medir("E1", $"MCTS-{it}it", s2 => new AgenteMcts(it, semilla: s2), s2 => new AgenteMinimax(4, s2));
        }

        Console.WriteLine("\nE2 · con heurística y sin ella");
        foreach (int prof in new[] { 2, 4, 6 })
        {
            Medir("E2", "MCTS-2000it", s2 => new AgenteMcts(2000, semilla: s2),
                  s2 => new AgenteMinimax(prof, s2, Evaluacion.Ventanas));
            Medir("E2", "MCTS-2000it", s2 => new AgenteMcts(2000, semilla: s2),
                  s2 => new AgenteMinimax(prof, s2, Evaluacion.Ciega));
        }

        Console.WriteLine("\nE3 · la constante de exploración C");
        foreach (double c in new[] { 0.0, 0.5, 1.0, 1.4142135623730951, 2.0, 4.0 })
            Medir("E3", $"C={c:0.00}", s2 => new AgenteMcts(2000, c, s2), s2 => new AgenteMinimax(4, s2));

        // E4 mide contra el RELOJ, así que sus cifras dependen de la máquina y de
        // lo cargada que esté. Por eso se calibra primero: cuántas iteraciones
        // caben de verdad aquí en cada presupuesto. Sin este dato, los números de
        // E4 de dos personas no se pueden comparar.
        Console.WriteLine("\nE4 · calibración de esta máquina");
        Console.WriteLine("     (iteraciones que caben en cada presupuesto, medidas en la posición inicial)");
        foreach (int ms in new[] { 5, 10, 25, 50, 100 })
        {
            var m = new MotorMcts(semilla: semilla);
            m.Elegir(new Conecta4(), int.MaxValue, ms);
            Console.WriteLine($"     {ms,4} ms → {m.UltimasIteraciones,8} iteraciones");
            csv.Add(string.Join(';', new[] { "E4cal", $"{ms} ms", "-",
                "-", "-", "-", "-", m.UltimasIteraciones.ToString(CultureInfo.InvariantCulture) }));
        }

        Console.WriteLine("\nE4 · presupuesto de TIEMPO en vez de iteraciones");
        foreach (int ms in new[] { 5, 10, 25, 50, 100 })
            Medir("E4", $"{ms} ms", s2 => new AgenteMcts(1000000, semilla: s2, msMaximos: ms),
                  s2 => new AgenteMinimax(4, s2));

        File.WriteAllLines($"referencia_s{semilla}.csv", csv);
        Console.WriteLine($"\n  → referencia_s{semilla}.csv");
    }

    /// <summary>
    /// Vuelca la FORMA del árbol construido en una posición, para poder dibujarlo.
    /// Una línea por nodo: id;padre;movimiento;visitas;profundidad.
    /// </summary>
    private static void VolcarArbol(string[] args)
    {
        int it = Arg(args, 1, 2000);
        var e = new Conecta4()
            .AplicarC4(0).AplicarC4(4)
            .AplicarC4(0).AplicarC4(5)
            .AplicarC4(1).AplicarC4(6);
        var motor = new MotorMcts(semilla: 42);
        var raiz = motor.ElegirConRaiz(e, it, out int mov);

        var lineas = new List<string> { "id;padre;movimiento;visitas;profundidad" };
        int siguiente = 0;
        void Recorrer(Nodo n, int idPadre, int prof)
        {
            int id = siguiente++;
            lineas.Add($"{id};{idPadre};{n.Movimiento};{n.Visitas};{prof}");
            foreach (var h in n.Hijos.OrderBy(x => x.Movimiento)) Recorrer(h, id, prof + 1);
        }
        Recorrer(raiz, -1, 0);
        File.WriteAllLines($"arbol_{it}it.csv", lineas);
        Console.WriteLine($"  {it} iteraciones → juega {mov} · {lineas.Count - 1} nodos · " +
                          $"profundidad {motor.UltimaProfundidad} → arbol_{it}it.csv");
    }


    /// <summary>
    /// Cuántas iteraciones caben de verdad en esta máquina para cada presupuesto de
    /// tiempo. Es el dato que hace interpretable a E4: sin él, el porcentaje de dos
    /// ordenadores distintos no se puede comparar. Va en la ficha de resultados.
    /// </summary>
    private static void Calibrar(string[] args)
    {
        int semilla = Arg(args, 1, 42);
        const int TOPE = 500_000;               // salvaguarda: ver el aviso de abajo

        Console.WriteLine($"Calibración de esta máquina · semilla {semilla}");
        Console.WriteLine("  (iteraciones que caben en cada presupuesto, medidas en la posición inicial,");
        Console.WriteLine("   que es la más cara de la partida)\n");

        // Calentamiento. Sin esto, el PRIMER presupuesto sale hasta tres veces peor
        // que los demás: .NET compila por niveles y todavía no ha optimizado los
        // métodos calientes. Se comprobó invirtiendo el orden de la tabla.
        new MotorMcts(semilla: semilla).Elegir(new Conecta4(), 20_000);

        var filas = new List<string> { "ms;iteraciones;ms_reales" };
        foreach (int ms in new[] { 5, 10, 25, 50, 100 })
        {
            // Tres medidas y se guarda la de en medio: una sola puede caer justo en
            // una pausa del recolector de basura y salir un 50 % baja.
            var v = new List<(int it, double ms)>();
            for (int r = 0; r < 3; r++)
            {
                var m = new MotorMcts(semilla: semilla + r);
                m.Elegir(new Conecta4(), TOPE, ms);

                if (m.UltimasIteraciones >= TOPE)
                {
                    Console.WriteLine("  EL CORTE POR TIEMPO NO ESTÁ PUESTO (TODO 7): el motor ha gastado el");
                    Console.WriteLine("  tope de iteraciones sin mirar el reloj. Mientras siga así, E4 no se");
                    Console.WriteLine("  puede medir y este modo no sirve de nada.");
                    return;
                }
                v.Add((m.UltimasIteraciones, m.UltimosMs));
            }
            v.Sort((a, b) => a.it.CompareTo(b.it));
            var (it, real) = v[1];

            Console.WriteLine($"  {ms,4} ms → {it,8} iteraciones   (tardó {real,6:0.0} ms; " +
                              $"las tres medidas: {v[0].it}, {v[1].it}, {v[2].it})");
            filas.Add(string.Join(';', new[]
            {
                ms.ToString(CultureInfo.InvariantCulture),
                it.ToString(CultureInfo.InvariantCulture),
                real.ToString("0.0", CultureInfo.InvariantCulture),
            }));
        }

        File.WriteAllLines($"calibracion_s{semilla}.csv", filas);
        Console.WriteLine($"\n  → calibracion_s{semilla}.csv");
        Console.WriteLine("  Esta tabla va en la ficha. Y no es la misma en dos ordenadores: ése es el punto.");
    }

    private static int Arg(string[] a, int i, int porDefecto)
        => a.Length > i && int.TryParse(a[i], out int v) ? v : porDefecto;
}
