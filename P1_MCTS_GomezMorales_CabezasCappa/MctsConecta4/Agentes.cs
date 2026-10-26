namespace MctsConecta4;

/// <summary>Cualquier cosa que sepa elegir una columna.</summary>
public interface IAgente
{
    string Nombre { get; }
    int Elegir(Conecta4 estado);
    /// <summary>Milisegundos que costó la última decisión. Para las métricas.</summary>
    double UltimosMs { get; }
}

/// <summary>El suelo contra el que se mide todo. Si no le ganas, algo va mal.</summary>
public sealed class AgenteAleatorio : IAgente
{
    private readonly Random _rnd;
    public string Nombre => "Aleatorio";
    public double UltimosMs { get; private set; }

    public AgenteAleatorio(int semilla) => _rnd = new Random(semilla);

    public int Elegir(Conecta4 estado)
    {
        var reloj = System.Diagnostics.Stopwatch.StartNew();
        var m = estado.MovimientosLegales();
        int elegido = m[_rnd.Next(m.Count)];
        UltimosMs = reloj.Elapsed.TotalMilliseconds;
        return elegido;
    }
}

/// <summary>Cómo puntúa el minimax una posición que no ha terminado.</summary>
public enum Evaluacion
{
    /// <summary>La función por ventanas de la hoja de ejercicios de la sesión 2.</summary>
    Ventanas,
    /// <summary>
    /// Devuelve 0 siempre: es minimax SIN función de evaluación. No es un capricho —
    /// es la situación normal en un juego recién diseñado, y es el experimento que
    /// contesta a la pregunta del bloque.
    /// </summary>
    Ciega,
}

/// <summary>
/// Negamax con poda alfa-beta y función de evaluación: el rival de verdad.
///
/// Está ENTERO y funciona. Es el algoritmo de la sesión 2, con la función de
/// evaluación por ventanas que diseñasteis en su hoja de ejercicios. Sirve para dos
/// cosas: para tener contra quién medir, y para que se vea lo que a MCTS le sobra —
/// todo el conocimiento de Conecta 4 del curso está en <see cref="Evaluar"/>, y MCTS
/// no tiene nada de eso.
/// </summary>
public sealed class AgenteMinimax : IAgente
{
    private readonly int _profundidad;
    private readonly Random _rnd;
    private readonly Evaluacion _evaluacion;
    public string Nombre => _evaluacion == Evaluacion.Ciega
        ? $"MinimaxCiego-{_profundidad}"
        : $"Minimax-{_profundidad}";
    public double UltimosMs { get; private set; }
    /// <summary>Nodos evaluados en la última decisión.</summary>
    public long UltimosNodos { get; private set; }

    public AgenteMinimax(int profundidad = 4, int semilla = 42,
                         Evaluacion evaluacion = Evaluacion.Ventanas)
    {
        _profundidad = profundidad;
        _rnd = new Random(semilla);
        _evaluacion = evaluacion;
    }

    public int Elegir(Conecta4 estado)
    {
        var reloj = System.Diagnostics.Stopwatch.StartNew();
        UltimosNodos = 0;
        int yo = estado.JugadorEnTurno;

        int mejorMov = -1;
        double mejor = double.NegativeInfinity;
        var candidatos = new List<int>();

        foreach (int m in OrdenCentro(estado.MovimientosLegales()))
        {
            double v = -Negamax(estado.AplicarC4(m), _profundidad - 1,
                                double.NegativeInfinity, double.PositiveInfinity,
                                yo == 1 ? 2 : 1);
            if (v > mejor) { mejor = v; mejorMov = m; candidatos.Clear(); candidatos.Add(m); }
            else if (v == mejor) candidatos.Add(m);
        }
        // Con varias jugadas empatadas, desempatar al azar evita que dos partidas
        // entre los mismos agentes sean siempre idénticas.
        if (candidatos.Count > 1) mejorMov = candidatos[_rnd.Next(candidatos.Count)];

        UltimosMs = reloj.Elapsed.TotalMilliseconds;
        return mejorMov;
    }

    private double Negamax(Conecta4 estado, int prof, double alfa, double beta, int turno)
    {
        UltimosNodos++;
        if (estado.EsTerminal())
        {
            if (estado.Ganador == 0) return 0;
            // Ganar antes vale más que ganar después: sin esto la IA alarga las partidas.
            return estado.Ganador == turno ? 1e6 - estado.Jugadas : -1e6 + estado.Jugadas;
        }
        if (prof == 0)
            return _evaluacion == Evaluacion.Ciega ? 0.0 : Evaluar(estado, turno);

        double mejor = double.NegativeInfinity;
        foreach (int m in OrdenCentro(estado.MovimientosLegales()))
        {
            double v = -Negamax(estado.AplicarC4(m), prof - 1, -beta, -alfa,
                                turno == 1 ? 2 : 1);
            if (v > mejor) mejor = v;
            if (mejor > alfa) alfa = mejor;
            if (alfa >= beta) break;              // poda
        }
        return mejor;
    }

    /// <summary>
    /// Mirar primero las columnas centrales es la "ordenación" de la sesión 2: no
    /// cambia el resultado, cambia cuánto se poda. Aquí vale casi el doble de
    /// profundidad por el mismo tiempo.
    /// </summary>
    private static IEnumerable<int> OrdenCentro(IReadOnlyList<int> movimientos)
        => movimientos.OrderBy(c => Math.Abs(c - Conecta4.Columnas / 2));

    /// <summary>
    /// Evaluación por ventanas: se recorren las 69 alineaciones de cuatro casillas y
    /// se puntúan las que sólo tiene uno de los dos jugadores.
    /// </summary>
    public static double Evaluar(Conecta4 e, int yo)
    {
        int rival = yo == 1 ? 2 : 1;
        double total = 0;

        // columna central: controlarla abre más ventanas que ninguna otra casilla
        for (int f = 0; f < Conecta4.Filas; f++)
        {
            sbyte v = e.Casilla(Conecta4.Columnas / 2, f);
            if (v == yo) total += 6;
            else if (v == rival) total -= 6;
        }

        foreach (var (dc, df) in new[] { (1, 0), (0, 1), (1, 1), (1, -1) })
            for (int c = 0; c < Conecta4.Columnas; c++)
                for (int f = 0; f < Conecta4.Filas; f++)
                {
                    int cf = c + 3 * dc, ff = f + 3 * df;
                    if (cf < 0 || cf >= Conecta4.Columnas || ff < 0 || ff >= Conecta4.Filas)
                        continue;
                    int mias = 0, suyas = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        sbyte v = e.Casilla(c + k * dc, f + k * df);
                        if (v == yo) mias++;
                        else if (v == rival) suyas++;
                    }
                    if (mias > 0 && suyas > 0) continue;      // ventana bloqueada: no cuenta
                    if (mias == 3) total += 50;
                    else if (mias == 2) total += 10;
                    else if (mias == 1) total += 1;
                    // bloquear pesa algo más que amenazar: si no, no se defiende
                    else if (suyas == 3) total -= 80;
                    else if (suyas == 2) total -= 10;
                    else if (suyas == 1) total -= 1;
                }
        return total;
    }
}

/// <summary>El agente de la práctica: MCTS con un presupuesto de simulaciones.</summary>
public sealed class AgenteMcts : IAgente
{
    private readonly MotorMcts _motor;
    private readonly int _iteraciones;
    private readonly double _msMaximos;

    public string Nombre => _msMaximos > 0
        ? $"MCTS-{_iteraciones}it-{_msMaximos:0}ms"
        : $"MCTS-{_iteraciones}it";
    public double UltimosMs => _motor.UltimosMs;
    public int UltimasIteraciones => _motor.UltimasIteraciones;
    public int UltimosNodos => _motor.UltimosNodos;
    public int UltimaProfundidad => _motor.UltimaProfundidad;

    public AgenteMcts(int iteraciones = 2000, double c = 1.4142135623730951,
                      int semilla = 42, double msMaximos = 0)
    {
        _iteraciones = iteraciones;
        _msMaximos = msMaximos;
        _motor = new MotorMcts(c, semilla);
    }

    public int Elegir(Conecta4 estado) => _motor.Elegir(estado, _iteraciones, _msMaximos);
}
