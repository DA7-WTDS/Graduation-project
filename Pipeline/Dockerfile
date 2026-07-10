# QuantWise — Unified Pipeline Service (LSTM+XGBoost prediction + FinBERT sentiment + risk rules)
# Replaces the three separate services (predict:8000, sentiment:8001, n8n).
# The .NET FetchDailyPipelineJob calls POST /api/score once per day.

FROM python:3.11-slim

# libgomp1: OpenMP runtime required by xgboost + torch. curl: for HEALTHCHECK.
RUN apt-get update && apt-get install -y --no-install-recommends \
        libgomp1 curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Bake the HuggingFace model cache into the image so FinBERT is NOT downloaded
# on cold start (~440MB, baked during build, served from image layer at runtime).
ENV HF_HOME=/opt/hf-cache

# Install CPU-only torch FIRST — the default PyPI torch bundles CUDA (~2GB).
# The CPU index gives a slim build; the pinned torch==2.4.0 in requirements.txt
# is then already satisfied so pip won't pull the CUDA variant when processing
# the rest of requirements.
# NOTE: torchvision must NOT be installed — known ABI crash with FinBERT
#       ("Entry Point Not Found _C.pyd"). FinBERT is text-only; no vision needed.
# --timeout/--retries: the torch CPU wheel is ~190MB; pip's default 15s socket
# timeout is too tight on slow connections. BuildKit cache persists good wheels
# across build attempts.
COPY requirements.txt .
RUN --mount=type=cache,target=/root/.cache/pip \
    pip install --timeout 300 --retries 5 \
        torch==2.4.0 --index-url https://download.pytorch.org/whl/cpu \
 && pip install --timeout 300 --retries 5 -r requirements.txt

# Bake FinBERT into the image so it is ready on the first startup.
# This step downloads ~440MB once at build time, not at runtime.
RUN python -c "from transformers import pipeline; pipeline('text-classification', model='ProsusAI/finbert', top_k=None)"

# App code
COPY main.py risk_rules.py ./
COPY core/ ./core/
COPY markets/ ./markets/

# ML model artifacts (only the 6 runtime files — notebooks/backups excluded)
COPY models/lstm_backbone.pth \
     models/xgb_head.json \
     models/global_feature_scaler.pkl \
     models/global_tech_scaler.pkl \
     models/universal_config.json \
     models/target_stats.json \
     ./models/

EXPOSE 8000

# Longer start-period: LSTM + XGBoost + FinBERT all load on startup.
HEALTHCHECK --interval=30s --timeout=15s --start-period=90s --retries=3 \
  CMD curl -fsS http://localhost:8000/health || exit 1

# Single worker: the LSTM model uses a process-global lock during MC-dropout.
# Multiple workers would race on _lstm.train() / _lstm.eval().
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
