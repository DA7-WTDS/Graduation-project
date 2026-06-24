# 1. infra
docker compose up -d postgres redis rabbitmq mailpit
# 2. (optional) ML pipeline — heavy build (torch + FinBERT)
docker compose up -d --build pipeline       # :8000/health → models+finbert loaded
# 3. backend (from repo root so DotNetEnv finds .env)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Backend/src/API/Project.Api   # :5000/health
# 4. frontend
cd frontend && npm run dev   