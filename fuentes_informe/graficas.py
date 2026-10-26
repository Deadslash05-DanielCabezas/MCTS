"""Gráficas O4 (E1, E3, E4) a partir de referencia_s42.csv."""
import csv, sys, os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
rows = list(csv.DictReader(open(src), delimiter=";"))

BLUE, ORANGE, INK, MUTED, GRID = "#2a78d6", "#eb6834", "#0b0b0b", "#52514e", "#e4e3df"
plt.rcParams.update({
    "font.family": "Inter", "font.size": 7.5, "axes.edgecolor": MUTED,
    "axes.labelcolor": INK, "xtick.color": MUTED, "ytick.color": MUTED,
    "axes.spines.top": False, "axes.spines.right": False, "axes.linewidth": 0.6,
    "axes.grid": True, "grid.color": GRID, "grid.linewidth": 0.6,
    "axes.axisbelow": True, "svg.fonttype": "none",
})

def fig():
    f, ax = plt.subplots(figsize=(2.75, 1.95), dpi=200)
    ax.set_ylim(0, 105)
    ax.set_ylabel("% puntuación MCTS")
    ax.axhline(50, color=MUTED, lw=0.7, ls=(0, (3, 3)))
    return f, ax

def save(f, name):
    f.tight_layout(pad=0.4)
    f.savefig(os.path.join(out, name), format="svg")
    plt.close(f)

# E1
e1 = [r for r in rows if r["experimento"] == "E1"]
its = sorted({int(r["configuracion"].split("-")[1][:-2]) for r in e1})
def serie(rival):
    return [float(next(r for r in e1 if r["configuracion"] == f"MCTS-{i}it" and r["rival"] == rival)["pct_mcts"]) for i in its]
f, ax = fig()
ax.set_xscale("log"); ax.set_xticks(its); ax.set_xticklabels([str(i) for i in its])
ax.minorticks_off()
ax.set_xlabel("iteraciones por jugada (escala log)")
for rival, col in (("Aleatorio", BLUE), ("Minimax-4", ORANGE)):
    ys = serie(rival)
    ax.plot(its, ys, color=col, lw=2, marker="o", ms=4, mec="white", mew=1)
    ax.annotate(f"vs {rival}", (its[-1], ys[-1]), xytext=(-4, 7 if rival == "Aleatorio" else 8),
                textcoords="offset points", ha="right", color=INK, fontsize=7.5)
for i, y in zip(its, serie("Minimax-4")):
    if i in (200, 1000):
        ax.annotate(f"{y:.1f}".replace(".", ","), (i, y), xytext=(5, -10), textcoords="offset points", fontsize=7, color=MUTED)
save(f, "e1.svg")

# E3
e3 = [r for r in rows if r["experimento"] == "E3"]
cs = [r["configuracion"].replace("C=", "") for r in e3]
ys = [float(r["pct_mcts"]) for r in e3]
f, ax = fig()
x = range(len(cs))
ax.bar(x, ys, width=0.62, color=ORANGE, edgecolor="white", linewidth=1)
ax.set_xticks(list(x)); ax.set_xticklabels([c.replace(".", ",") for c in cs])
ax.set_xlabel("constante C  (MCTS-2000 vs Minimax-4)")
ax.grid(axis="x", visible=False)
for xi, y in zip(x, ys):
    ax.annotate(f"{y:.1f}".replace(".", ","), (xi, y), xytext=(0, 2), textcoords="offset points",
                ha="center", fontsize=7, color=INK)
save(f, "e3.svg")

# E4
e4 = [r for r in rows if r["experimento"] == "E4"]
ms = [int(r["configuracion"].split()[0]) for r in e4]
ys = [float(r["pct_mcts"]) for r in e4]
f, ax = fig()
ax.set_xscale("log"); ax.set_xticks(ms); ax.set_xticklabels([str(m) for m in ms]); ax.minorticks_off()
ax.set_xlabel("ms por jugada (escala log)  ·  vs Minimax-4")
ax.set_xlim(4.2, 125)
ax.axvline(16, color=BLUE, lw=0.8, ls=(0, (2, 2)))
ax.annotate("16 ms\n(60 fps)", (16, 8), xytext=(3, 0), textcoords="offset points", fontsize=7, color=MUTED)
ax.plot(ms, ys, color=ORANGE, lw=2, marker="o", ms=4, mec="white", mew=1)
for m, y in zip(ms, ys):
    ax.annotate(f"{y:.1f}".replace(".", ","), (m, y), xytext=(0, 6), textcoords="offset points",
                ha="center", fontsize=7, color=INK)
save(f, "e4.svg")
print("ok")
