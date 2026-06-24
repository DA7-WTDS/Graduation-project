import json, os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

here = os.path.dirname(os.path.abspath(__file__))
with open(os.path.join(here, "summary.json")) as f:
    d = json.load(f)
m = d["metrics"]

endpoints = [
    ("GET /users/profile", "profile_ms"),
    ("GET /portfolios/me", "portfolio_ms"),
    ("GET /notifications", "notifications_ms"),
    ("GET /recommendations\n(cache hit)", "recommendations_ms"),
]
labels = [e[0] for e in endpoints]
med = [m[e[1]]["values"]["med"] for e in endpoints]
p95 = [m[e[1]]["values"]["p(95)"] for e in endpoints]

INK, AMBER = "#1F3A5F", "#E8920C"
x = np.arange(len(labels))
w = 0.38

fig, ax = plt.subplots(figsize=(8.4, 4.4), dpi=150)
b1 = ax.bar(x - w/2, med, w, label="Median", color=INK)
b2 = ax.bar(x + w/2, p95, w, label="95th percentile", color=AMBER)

for bars in (b1, b2):
    for b in bars:
        h = b.get_height()
        ax.annotate(f"{h:.1f}", (b.get_x() + b.get_width()/2, h),
                    ha="center", va="bottom", fontsize=8.5, color="#333333",
                    xytext=(0, 2), textcoords="offset points")

ax.set_ylabel("Response time (ms)")
ax.set_title("QuantWise read-path latency under load\n50 concurrent users, ~103 req/s, 12,464 requests, 0% errors",
             fontsize=11, color=INK)
ax.set_xticks(x)
ax.set_xticklabels(labels, fontsize=9)
ax.set_ylim(0, max(p95) * 1.35)
ax.legend(frameon=False, loc="upper left")
ax.spines["top"].set_visible(False)
ax.spines["right"].set_visible(False)
ax.yaxis.grid(True, color="#E3E3E3", linewidth=0.8)
ax.set_axisbelow(True)

cap = "NFR-01 budget for cached/CRUD reads: 200 ms (all endpoints well inside)."
fig.text(0.5, 0.005, cap, ha="center", fontsize=8.5, color="#666666", style="italic")

fig.tight_layout(rect=(0, 0.03, 1, 1))
out = os.path.join(here, "fig-5-x-loadtest.png")
fig.savefig(out)
print("WROTE", out)
