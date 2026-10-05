using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MctsConecta4;

/// <summary>
/// Monte Carlo Tree Search. ESTE ES EL ÚNICO FICHERO QUE HAY QUE TOCAR.
///
/// Hay siete <c>TODO</c>. Los seis primeros son el algoritmo (talleres del 5 y del
/// 26 de octubre) y el séptimo es el presupuesto de tiempo. Todo lo demás del
/// proyecto —el juego, los rivales, el torneo y la exportación a CSV— está hecho
/// y no hay que modificarlo.
///
/// Todo lo aleatorio tiene que pasar por <see cref="_rnd"/>, que se construye con
/// una semilla. Si usáis <c>new Random()</c> sin semilla en algún sitio, dos
/// ejecuciones dejarán de dar lo mismo y no habrá nada que medir.
/// </summary>
public sealed class MotorMcts
{
    private readonly Random _rnd;
    private readonly double _c;

    /// <summary>Iteraciones consumidas en la última llamada a <see cref="Elegir"/>.</summary>
    public int UltimasIteraciones { get; private set; }
    /// <summary>Milisegundos consumidos en la última llamada a <see cref="Elegir"/>.</summary>
    public double UltimosMs { get; private set; }
    /// <summary>Nodos que tiene el árbol al terminar la última llamada.</summary>
    public int UltimosNodos { get; private set; }
    /// <summary>Profundidad máxima alcanzada por el árbol en la última llamada.</summary>
    public int UltimaProfundidad { get; private set; }

    public MotorMcts(double c = 1.4142135623730951, int semilla = 42)
    {
        _c = c;
        _rnd = new Random(semilla);
    }

    public int Elegir(IJuego estado, int iteraciones, double msMaximos = 0)
    {
        ElegirConRaiz(estado, iteraciones, out int movimiento, msMaximos);
        return movimiento;
    }

    /// <summary>
    /// Igual que <see cref="Elegir"/> pero devolviendo además la raíz del árbol, para
    /// poder mirar por dentro cuántas visitas se ha llevado cada jugada. Es lo que
    /// hace falta para depurar: si el reparto no tiene sentido, el fallo está en la
    /// selección o en la retropropagación.
    /// </summary>
    public Nodo ElegirConRaiz(IJuego estado, int iteraciones, out int movimiento,
                              double msMaximos = 0)
    {
        var raiz = new Nodo(estado);
        var reloj = Stopwatch.StartNew();
        int hechas = 0;

        while (hechas < iteraciones)
        {
            // ── TODO 7 · PRESUPUESTO DE TIEMPO (taller del 26 de octubre) ────────
            // Si msMaximos es mayor que 0 y el reloj ya ha pasado de ese tiempo,
            // hay que salir del bucle ANTES de empezar otra iteración. Eso es lo
            // que hace a MCTS *anytime*: la respuesta que haya está disponible.
            //   reloj.Elapsed.TotalMilliseconds da el tiempo transcurrido.
            // Mientras esto no esté, el parámetro msMaximos no hace nada y el
            // experimento E4 no se puede medir.

            if (msMaximos > 0 && reloj.ElapsedMilliseconds > msMaximos)
                break;

            Iteracion(raiz);
            hechas++;
        }

        reloj.Stop();
        UltimasIteraciones = hechas;
        UltimosMs = reloj.Elapsed.TotalMilliseconds;
        UltimosNodos = ContarNodos(raiz);
        UltimaProfundidad = Profundidad(raiz);
        movimiento = MejorMovimiento(raiz);
        return raiz;
    }

    /// <summary>Una iteración completa: las cuatro fases, en orden.</summary>
    private void Iteracion(Nodo raiz)
    {
        Nodo nodo = Seleccionar(raiz);          // 1 · selección
        nodo = Expandir(nodo);                  // 2 · expansión
        double resultado = Simular(nodo);       // 3 · simulación
        Retropropagar(nodo, resultado);         // 4 · retropropagación
    }

    // ── 1 · SELECCIÓN ────────────────────────────────────────────────────────
    /// <summary>
    /// Baja por el árbol eligiendo con UCT mientras el nodo esté expandido del todo
    /// y no sea terminal. Devuelve el nodo donde la búsqueda se detiene.
    /// </summary>
    private Nodo Seleccionar(Nodo nodo)
    {
        // ── TODO 1 · SELECCIÓN ──────────────────────────────────────────────────
        // Mientras el nodo NO sea terminal (nodo.Estado.EsTerminal()) y esté
        // expandido del todo (nodo.EstaExpandidoDelTodo), bajar al mejor hijo
        // según UCT: nodo = MejorHijoUct(nodo).
        // Cuando ya no se cumpla, devolver el nodo donde te has quedado.

        while (!nodo.Estado.EsTerminal() && nodo.EstaExpandidoDelTodo)
            nodo = MejorHijoUct(nodo);

        return nodo;
    }

    /// <summary>
    /// UCT: UCB1 aplicado a los hijos de un nodo del árbol.
    ///
    ///     indice = recompensa/visitas + C * sqrt( ln(visitas del PADRE) / visitas del HIJO )
    /// </summary>
    private Nodo MejorHijoUct(Nodo nodo)
    {
        // ── TODO 2 · UCT ────────────────────────────────────────────────────────
        // Recorrer nodo.Hijos y devolver el de mayor índice.
        //   · las visitas del PADRE son nodo.Visitas  (van dentro del logaritmo)
        //   · las visitas del HIJO son hijo.Visitas   (van en el DENOMINADOR)
        //     Escribir nodo.Visitas en el denominador compila y es el error clásico:
        //     el término queda igual para todos los hijos y UCT deja de explorar.
        //   · un hijo con 0 visitas vale infinito (double.PositiveInfinity), que es
        //     lo que obliga a probar todos los hijos una vez.
        //   · la constante C está en el campo _c.

        Nodo mejorNodo = null;
        double mejorindice = double.NegativeInfinity;

        foreach (Nodo hijo in nodo.Hijos)
        {
            if (hijo.Visitas == 0)
                return hijo;   // índice infinito

            double valor = hijo.Recompensa / hijo.Visitas
                         + _c * Math.Sqrt(Math.Log(nodo.Visitas) / hijo.Visitas);
            if (valor > mejorindice)
            {
                mejorindice = valor;
                mejorNodo = hijo;
            }

        }

        return mejorNodo;

        throw new Exception("No suitable child found for UCT selection");


    }

    // ── 2 · EXPANSIÓN ────────────────────────────────────────────────────────
    /// <summary>
    /// Si al nodo le quedan movimientos sin probar, saca uno y le cuelga un hijo
    /// nuevo. Si no le queda ninguno —o es terminal— devuelve el mismo nodo.
    /// </summary>
    private Nodo Expandir(Nodo nodo)
    {
        // ── TODO 3 · EXPANSIÓN ──────────────────────────────────────────────────
        // Si el nodo es terminal o nodo.MovimientosPendientes está vacío, devolver
        // el mismo nodo. Si no:
        //   1. sacar UN movimiento de nodo.MovimientosPendientes, elegido con _rnd,
        //      y quitarlo de la lista (RemoveAt);
        //   2. crear el hijo: new Nodo(nodo.Estado.Aplicar(movimiento), nodo, movimiento);
        //   3. añadirlo a nodo.Hijos y devolverlo.
        // UN hijo, no todos: los que no se simulen no aportan ningún dato.

        if (nodo.Estado.EsTerminal() || nodo.MovimientosPendientes.Count == 0)
            return nodo;

        int indice = _rnd.Next(nodo.MovimientosPendientes.Count);
        int movimiento = nodo.MovimientosPendientes[indice];
        nodo.MovimientosPendientes.RemoveAt(indice);

        Nodo hijo = new Nodo(nodo.Estado.Aplicar(movimiento), nodo, movimiento);
        nodo.Hijos.Add(hijo);

        return hijo;
    }

    // ── 3 · SIMULACIÓN ───────────────────────────────────────────────────────
    /// <summary>
    /// Juega al azar hasta el final y devuelve el resultado DESDE EL PUNTO DE VISTA
    /// del jugador que movió para llegar a <paramref name="nodo"/>.
    /// </summary>
    private double Simular(Nodo nodo)
    {
        // ── TODO 4 · SIMULACIÓN ─────────────────────────────────────────────────
        // 1. Guardar el punto de vista: nodo.JugadorQueMovio. Ojo: en la raíz vale 0,
        //    y en ese caso hay que usar nodo.Estado.JugadorEnTurno.
        // 2. Partiendo de nodo.Estado, mientras no sea terminal, aplicar un
        //    movimiento legal elegido al azar con _rnd.
        // 3. Devolver estado.Resultado(puntoDeVista): 1 gana, 0 pierde, 0,5 tablas.
        // Aquí NO va ninguna heurística: es exactamente lo que MCTS no necesita.

        int puntoDeVista = nodo.JugadorQueMovio != 0 ? nodo.JugadorQueMovio
                                                     : nodo.Estado.JugadorEnTurno;
        IJuego estado = nodo.Estado;

        while (!estado.EsTerminal())
        {
            var legales = estado.MovimientosLegales();
            estado = estado.Aplicar(legales[_rnd.Next(legales.Count)]);
        }

        return estado.Resultado(puntoDeVista);


        throw new NotImplementedException("TODO 4 · Simular");
    }

    // ── 4 · RETROPROPAGACIÓN ─────────────────────────────────────────────────
    /// <summary>
    /// Sube el resultado hasta la raíz sumando una visita en cada nodo.
    /// </summary>
    private void Retropropagar(Nodo nodo, double resultado)
    {
        // ── TODO 5 · RETROPROPAGACIÓN ───────────────────────────────────────────
        // Recorrer desde el nodo hasta la raíz siguiendo .Padre. En cada nodo:
        //   nodo.Visitas++;  nodo.Recompensa += resultado;
        // y ANTES de subir al padre, INVERTIR el resultado:  resultado = 1 - resultado;
        // Los niveles alternan de jugador: lo que es victoria en uno es derrota en el
        // de arriba. Si no se invierte, no da error — sólo hace que la IA juegue bien
        // un turno y regale el siguiente.

        Nodo? actual = nodo;
        while (actual != null)
        {
            actual.Visitas++;
            actual.Recompensa += resultado;
            resultado = 1 - resultado;
            actual = actual.Padre;
        }

        
    }

    // ── La jugada que se juega de verdad ─────────────────────────────────────
    /// <summary>El movimiento que se juega al final de la búsqueda.</summary>
    private static int MejorMovimiento(Nodo raiz)
    {
        // ── TODO 6 · LA JUGADA FINAL ────────────────────────────────────────────
        // Devolver el movimiento del hijo MÁS VISITADO, no el de mejor media ni el
        // de mayor índice UCT (ver la diapositiva 11 de B1.3a y el ejercicio 6.3 de
        // la hoja de la sesión 3).
        // Caso borde: si la raíz no tiene hijos —presupuesto ridículo o posición
        // terminal—, devolver el primer movimiento legal, o -1 si no hay ninguno.

        float maxVisitas = float.NegativeInfinity;
        int mejorMovimiento = -1;

        foreach (Nodo hijo in raiz.Hijos)
        {
            if (hijo.Visitas > maxVisitas)
            {
                maxVisitas = hijo.Visitas;
                mejorMovimiento = hijo.Movimiento;
            }
        }

        if (mejorMovimiento == -1)
        {
            IReadOnlyList<int> movimientosLegales = raiz.Estado.MovimientosLegales();
            return movimientosLegales.Count > 0 ? movimientosLegales[0] : -1;
        }

        return mejorMovimiento;
    }

    /// <summary>Estadísticas de la raíz, para poder imprimirlas y depurar.</summary>
    public static IEnumerable<(int Movimiento, int Visitas, double Tasa)> EstadisticasRaiz(Nodo raiz)
        => raiz.Hijos.Select(h => (h.Movimiento, h.Visitas, h.TasaVictorias))
                     .OrderByDescending(x => x.Visitas);

    private static int ContarNodos(Nodo n) => 1 + n.Hijos.Sum(ContarNodos);

    private static int Profundidad(Nodo n)
        => n.Hijos.Count == 0 ? 0 : 1 + n.Hijos.Max(Profundidad);
}
