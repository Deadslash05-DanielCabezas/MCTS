namespace MctsConecta4;

/// <summary>
/// Un nodo del árbol de MCTS. Guarda el estado, quién llegó hasta aquí y las dos
/// cifras que lo deciden todo: <see cref="Visitas"/> y <see cref="Recompensa"/>.
/// </summary>
public sealed class Nodo
{
    public IJuego Estado { get; }
    public Nodo? Padre { get; }
    /// <summary>Movimiento que llevó del padre a este nodo. −1 en la raíz.</summary>
    public int Movimiento { get; }

    /// <summary>Veces que la búsqueda ha pasado por este nodo. La N de UCB1.</summary>
    public int Visitas;
    /// <summary>
    /// Suma de resultados de las simulaciones que pasaron por aquí, DESDE EL PUNTO
    /// DE VISTA del jugador que movió para llegar a este nodo.
    /// </summary>
    public double Recompensa;

    /// <summary>Hijos ya creados. Vacío mientras el nodo no se haya expandido.</summary>
    public List<Nodo> Hijos { get; } = new();

    /// <summary>Movimientos que aún no tienen hijo. Se vacía al expandir.</summary>
    public List<int> MovimientosPendientes { get; }

    public Nodo(IJuego estado, Nodo? padre = null, int movimiento = -1)
    {
        Estado = estado;
        Padre = padre;
        Movimiento = movimiento;
        MovimientosPendientes = new List<int>(estado.MovimientosLegales());
    }

    /// <summary>Jugador que movió para llegar aquí; es a quien "pertenece" la recompensa.</summary>
    public int JugadorQueMovio => Padre is null ? 0 : Padre.Estado.JugadorEnTurno;

    public bool EstaExpandidoDelTodo => MovimientosPendientes.Count == 0;
    public bool EsHoja => Hijos.Count == 0;

    public double TasaVictorias => Visitas == 0 ? 0.0 : Recompensa / Visitas;
}
