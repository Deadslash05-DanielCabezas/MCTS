namespace MctsConecta4;

/// <summary>
/// Lo único que MCTS necesita saber de un juego. Fijaos en lo que NO hay aquí:
/// no hay función de evaluación. Ésa es toda la diferencia con minimax.
/// </summary>
public interface IJuego
{
    /// <summary>Jugador al que le toca mover: 1 o 2.</summary>
    int JugadorEnTurno { get; }

    /// <summary>Movimientos legales desde este estado. Vacío si la partida acabó.</summary>
    IReadOnlyList<int> MovimientosLegales();

    /// <summary>Copia independiente con el movimiento aplicado. NO modifica this.</summary>
    IJuego Aplicar(int movimiento);

    /// <summary>¿Se acabó la partida?</summary>
    bool EsTerminal();

    /// <summary>
    /// Resultado de una partida terminada DESDE EL PUNTO DE VISTA de <paramref name="jugador"/>:
    /// 1 gana, 0 pierde, 0,5 tablas. Sólo tiene sentido si EsTerminal().
    /// </summary>
    double Resultado(int jugador);
}
