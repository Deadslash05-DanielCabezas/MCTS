using System.Globalization;

namespace MctsConecta4;

public readonly record struct ResultadoPartida(
    int Numero, string Blancas, string Negras, int Ganador, int Jugadas,
    double MsMediosBlancas, double MsMediosNegras);

/// <summary>
/// Enfrenta dos agentes N partidas alternando quién empieza y vuelca los resultados
/// a CSV. Alternar es obligatorio: en Conecta 4 empezar es una ventaja enorme, y sin
/// alternar se mide quién salió primero, no quién juega mejor.
/// </summary>
public static class Torneo
{
    public static List<ResultadoPartida> Jugar(
        Func<int, IAgente> fabricaA, Func<int, IAgente> fabricaB,
        int partidas, int semillaBase, bool verboso = false)
    {
        var resultados = new List<ResultadoPartida>();

        for (int i = 0; i < partidas; i++)
        {
            // Semilla distinta por partida y derivada de la base: reproducible y
            // sin que las dos partidas del mismo par salgan idénticas.
            int semilla = semillaBase + i * 1000;
            var a = fabricaA(semilla);
            var b = fabricaB(semilla + 7);

            bool aEmpieza = i % 2 == 0;
            IAgente j1 = aEmpieza ? a : b;
            IAgente j2 = aEmpieza ? b : a;

            var estado = new Conecta4();
            double ms1 = 0, ms2 = 0;
            int n1 = 0, n2 = 0;

            while (!estado.EsTerminal())
            {
                var quien = estado.JugadorEnTurno == 1 ? j1 : j2;
                int mov = quien.Elegir(estado);
                if (estado.JugadorEnTurno == 1) { ms1 += quien.UltimosMs; n1++; }
                else { ms2 += quien.UltimosMs; n2++; }
                estado = estado.AplicarC4(mov);
            }

            resultados.Add(new ResultadoPartida(
                i + 1, j1.Nombre, j2.Nombre, estado.Ganador, estado.Jugadas,
                n1 == 0 ? 0 : ms1 / n1, n2 == 0 ? 0 : ms2 / n2));

            if (verboso)
                Console.WriteLine($"  partida {i + 1,3}: {j1.Nombre} vs {j2.Nombre} → " +
                    (estado.Ganador == 0 ? "tablas" : $"gana {(estado.Ganador == 1 ? j1.Nombre : j2.Nombre)}") +
                    $" en {estado.Jugadas} jugadas");
        }
        return resultados;
    }

    /// <summary>Victorias, derrotas y tablas del agente <paramref name="nombreA"/>.</summary>
    public static (int Gana, int Pierde, int Tablas) Resumen(
        List<ResultadoPartida> r, string nombreA)
    {
        int g = 0, p = 0, t = 0;
        foreach (var x in r)
        {
            if (x.Ganador == 0) { t++; continue; }
            string ganador = x.Ganador == 1 ? x.Blancas : x.Negras;
            if (ganador == nombreA) g++; else p++;
        }
        return (g, p, t);
    }

    public static void ExportarCsv(string ruta, List<ResultadoPartida> r)
    {
        using var w = new StreamWriter(ruta);
        w.WriteLine("partida;jugador1;jugador2;ganador;jugadas;ms_medios_j1;ms_medios_j2");
        foreach (var x in r)
            w.WriteLine(string.Join(';', new[]
            {
                x.Numero.ToString(CultureInfo.InvariantCulture),
                x.Blancas, x.Negras, x.Ganador.ToString(CultureInfo.InvariantCulture),
                x.Jugadas.ToString(CultureInfo.InvariantCulture),
                x.MsMediosBlancas.ToString("0.000", CultureInfo.InvariantCulture),
                x.MsMediosNegras.ToString("0.000", CultureInfo.InvariantCulture),
            }));
        Console.WriteLine($"  → {ruta}");
    }
}
