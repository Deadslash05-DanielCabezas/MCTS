namespace MctsConecta4;

/// <summary>
/// Conecta 4 en un tablero de 7 columnas × 6 filas. Un movimiento es el número de
/// columna, de 0 a 6. Inmutable hacia fuera: <see cref="Aplicar"/> devuelve un estado
/// nuevo, que es lo que le hace falta a MCTS para tener un árbol y no un lío.
/// </summary>
public sealed class Conecta4 : IJuego
{
    public const int Columnas = 7;
    public const int Filas = 6;

    private readonly sbyte[] _casillas;   // 0 vacío, 1 jugador 1, 2 jugador 2
    private readonly sbyte[] _alturas;    // fichas ya puestas en cada columna
    private readonly int _ultimaColumna;
    private readonly int _ultimaFila;

    public int JugadorEnTurno { get; }
    public int Ganador { get; }           // 0 = nadie (todavía)
    public int Jugadas { get; }

    public Conecta4()
    {
        _casillas = new sbyte[Columnas * Filas];
        _alturas = new sbyte[Columnas];
        _ultimaColumna = -1;
        _ultimaFila = -1;
        JugadorEnTurno = 1;
        Ganador = 0;
        Jugadas = 0;
    }

    private Conecta4(sbyte[] casillas, sbyte[] alturas, int turno, int ganador,
                    int jugadas, int col, int fila)
    {
        _casillas = casillas;
        _alturas = alturas;
        JugadorEnTurno = turno;
        Ganador = ganador;
        Jugadas = jugadas;
        _ultimaColumna = col;
        _ultimaFila = fila;
    }

    public sbyte Casilla(int columna, int fila) => _casillas[fila * Columnas + columna];

    public bool EsTerminal() => Ganador != 0 || Jugadas == Columnas * Filas;

    public double Resultado(int jugador)
    {
        if (Ganador == 0) return 0.5;                 // tablas
        return Ganador == jugador ? 1.0 : 0.0;
    }

    public IReadOnlyList<int> MovimientosLegales()
    {
        if (EsTerminal()) return Array.Empty<int>();
        var libres = new List<int>(Columnas);
        for (int c = 0; c < Columnas; c++)
            if (_alturas[c] < Filas) libres.Add(c);
        return libres;
    }

    public IJuego Aplicar(int movimiento) => AplicarC4(movimiento);

    /// <summary>Igual que <see cref="Aplicar"/> pero sin castear en cada llamada.</summary>
    public Conecta4 AplicarC4(int columna)
    {
        if (columna < 0 || columna >= Columnas)
            throw new ArgumentOutOfRangeException(nameof(columna));
        if (_alturas[columna] >= Filas)
            throw new InvalidOperationException($"la columna {columna} está llena");

        var casillas = (sbyte[])_casillas.Clone();
        var alturas = (sbyte[])_alturas.Clone();
        int fila = alturas[columna];
        casillas[fila * Columnas + columna] = (sbyte)JugadorEnTurno;
        alturas[columna]++;

        int ganador = HayCuatro(casillas, columna, fila, JugadorEnTurno) ? JugadorEnTurno : 0;
        return new Conecta4(casillas, alturas, JugadorEnTurno == 1 ? 2 : 1, ganador,
                            Jugadas + 1, columna, fila);
    }

    // Sólo se comprueban las cuatro direcciones que pasan por la ficha recién puesta:
    // es lo único que puede haber cambiado, y esto se llama millones de veces.
    private static readonly (int dc, int df)[] Direcciones =
        { (1, 0), (0, 1), (1, 1), (1, -1) };

    private static bool HayCuatro(sbyte[] casillas, int col, int fila, int jugador)
    {
        foreach (var (dc, df) in Direcciones)
        {
            int seguidas = 1;
            for (int signo = -1; signo <= 1; signo += 2)
            {
                int c = col + dc * signo, f = fila + df * signo;
                while (c >= 0 && c < Columnas && f >= 0 && f < Filas &&
                       casillas[f * Columnas + c] == jugador)
                {
                    seguidas++;
                    c += dc * signo;
                    f += df * signo;
                }
            }
            if (seguidas >= 4) return true;
        }
        return false;
    }

    public string Dibujar()
    {
        var sb = new System.Text.StringBuilder();
        for (int f = Filas - 1; f >= 0; f--)
        {
            sb.Append('|');
            for (int c = 0; c < Columnas; c++)
                sb.Append(Casilla(c, f) switch { 1 => " X ", 2 => " O ", _ => " . " }).Append('|');
            sb.AppendLine();
        }
        sb.Append(' ');
        for (int c = 0; c < Columnas; c++) sb.Append($"  {c} ");
        return sb.ToString();
    }
}
