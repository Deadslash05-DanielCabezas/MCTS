# Práctica 1 — MCTS sobre Conecta 4 · proyecto base

**Inteligencia Artificial Avanzada (IA4) · Curso 2026/2027**

## Arrancar

Hace falta el **SDK de .NET 8**. Desde esta carpeta:

```
dotnet build
dotnet run
```

`dotnet run` sin argumentos imprime el menú de modos.

## Qué hay que tocar

**Sólo `MotorMcts.cs`.** Tiene **siete `TODO`**:

| TODO | Qué es | Taller |
|---|---|---|
| 1 | `Seleccionar` — bajar por el árbol con UCT | 5 oct |
| 2 | `MejorHijoUct` — la fórmula de UCT | 5 oct |
| 3 | `Expandir` — crear un hijo nuevo | 5 oct |
| 4 | `Simular` — jugar al azar hasta el final | 5 oct |
| 5 | `Retropropagar` — subir el resultado invirtiendo el signo | 5 oct |
| 6 | `MejorMovimiento` — la jugada que se juega de verdad | 5 oct |
| 7 | El corte por tiempo dentro del bucle | 26 oct |

El resto del proyecto está hecho y **no hay que modificarlo**:

| Fichero | Qué es |
|---|---|
| `Conecta4.cs` | El juego: tablero de 7 × 6, movimientos legales, detección de cuatro en raya |
| `IJuego.cs` | Lo único que MCTS necesita saber de un juego. Fijaos en que **no hay función de evaluación** |
| `Nodo.cs` | Un nodo del árbol: visitas, recompensa, hijos y movimientos pendientes |
| `Agentes.cs` | `AgenteAleatorio`, `AgenteMinimax` (con y sin heurística) y `AgenteMcts` |
| `Torneo.cs` | Enfrenta dos agentes N partidas alternando quién empieza, y exporta a CSV |
| `Program.cs` | Los modos de ejecución |

## Modos

| Orden | Para qué |
|---|---|
| `dotnet run -- traza` | Una sola jugada en una posición con amenaza, enseñando el reparto de visitas. **Es lo primero que hay que hacer funcionar** |
| `dotnet run -- humano` | Jugar contra vuestra IA. La mejor prueba de humo |
| `dotnet run -- duelo 40 2000 4 42` | MCTS contra Minimax: partidas, iteraciones, profundidad y semilla |
| `dotnet run -- barrido 40 42` | Sólo el experimento 1 |
| `dotnet run -- referencia 40 42` | **Los cuatro experimentos.** Es lo que genera la entrega |
| `dotnet run -- calibrar 42` | Cuántas iteraciones caben en 5, 10, 25, 50 y 100 ms **en vuestra máquina**. Va en la ficha, y avisa si falta el TODO 7 |
| `dotnet run -- arbol 2000` | Vuelca la forma del árbol a CSV, por si queréis dibujarlo |

## Cómo saber si va bien

1. `dotnet run -- traza` tiene que elegir la **columna 3** en los seis presupuestos, y el
   porcentaje de visitas de esa columna tiene que **subir** con el presupuesto.
2. `dotnet run -- duelo 40 2000 4 42` tiene que dar a MCTS **más del 45 %**. Si da mucho menos,
   hay un fallo: mirad primero el signo de la retropropagación y después el denominador de UCT.
3. Contra `AgenteAleatorio` con 500 iteraciones o más, MCTS tiene que ganar **prácticamente
   siempre**. Si no, el fallo es gordo y está en las cuatro fases.

## Reproducibilidad

Todo lo aleatorio pasa por el `Random` que se construye con la **semilla**. No uséis
`new Random()` sin semilla en ningún sitio: si lo hacéis, dos ejecuciones dejan de dar lo mismo
y no hay nada que medir. La semilla que uséis va **en la ficha de resultados**.
