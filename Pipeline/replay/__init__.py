# QuantWise — point-in-time replay (MVP_PLAN § C).
#
# Manufactures an out-of-sample track record over history instead of waiting for
# it in real time. The governing rule is that every stage must be a function of
# data available at replay date t, using the same code paths as live scoring and
# differing only in where the data comes from.
#
# Two lanes, deliberately separate (§ C.2 rule 4):
#   • fast lane (this package) — pure Python, for research iteration. Produces
#     ScoreRecord-shaped rows per date. NOT the number we publish.
#   • fidelity lane — those rows ingested as market=us_sim through the real .NET
#     optimizer and shadow jobs, so the published figure comes from the actual
#     product code rather than a Python reimplementation of it.
