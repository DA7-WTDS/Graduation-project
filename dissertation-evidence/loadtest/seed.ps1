$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5000'
$root = 'D:\Grad\Backend\Graduation-project'

# --- read ingest key from repo-root .env ---
$envMap = @{}
Get-Content "$root\.env" | Where-Object { $_ -match '^\s*[^#].*=' } | ForEach-Object {
  $kv = $_ -split '=', 2
  $envMap[$kv[0].Trim()] = $kv[1].Trim().Trim('"')
}
$ingestKey = $envMap['Recommendations__Ingest__ApiKey']
if (-not $ingestKey) { throw 'ingest key not found in .env' }

# --- register a fresh user (unique email so portfolio create is always clean) ---
$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$email = "loadtest+$stamp@quantwise.test"
$pwd   = 'LoadTest!2026'
$reg = @{ email = $email; password = $pwd; firstName = 'Load'; lastName = 'Test' } | ConvertTo-Json
Invoke-RestMethod -Uri "$base/api/users/register" -Method Post -ContentType 'application/json' -Body $reg | Out-Null
"registered $email"

# --- login -> token ---
$login = @{ email = $email; password = $pwd } | ConvertTo-Json
$tok = (Invoke-RestMethod -Uri "$base/api/users/login" -Method Post -ContentType 'application/json' -Body $login).accessToken
if (-not $tok) { throw 'no token' }
$hdr = @{ Authorization = "Bearer $tok" }
"got token (len $($tok.Length))"

# --- create a portfolio ---
$pf = @{
  primaryGoal = 'Growth'; timeHorizon = 'Long'; riskTolerance = 3
  marketReaction = 'Hold'; investmentExperience = 'Beginner'
  stocksPercentage = 60; bondsPercentage = 20; etfsPercentage = 15; cashPercentage = 5
  riskProfile = 'Moderate'; investmentAmount = 10000
} | ConvertTo-Json
Invoke-RestMethod -Uri "$base/api/portfolios" -Method Post -ContentType 'application/json' -Headers $hdr -Body $pf | Out-Null
"portfolio created"

# --- ingest the saved daily run (idempotent on generated_at) ---
$runJson = Get-Content "$root\Pipeline\last_score_output.json" -Raw
try {
  $ing = Invoke-RestMethod -Uri "$base/api/internal/daily-results" -Method Post -ContentType 'application/json' -Headers @{ 'X-Pipeline-Key' = $ingestKey } -Body $runJson
  "ingested run: $($ing | ConvertTo-Json -Compress)"
} catch {
  "ingest note: $($_.Exception.Message) (may already exist - continuing)"
}

# --- prime the recommendations cache (one Gemini call) ---
try {
  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  $rec = Invoke-RestMethod -Uri "$base/api/recommendations" -Method Get -Headers $hdr -TimeoutSec 60
  $sw.Stop()
  "primed recommendations in $($sw.ElapsedMilliseconds) ms (cache-miss/Gemini)"
  $recOk = $true
} catch {
  "recommendations prime FAILED: $($_.Exception.Message)"
  $recOk = $false
}

# --- persist token + flags for k6 ---
$out = @{ token = $tok; recOk = $recOk } | ConvertTo-Json
Set-Content -Path "$root\dissertation-evidence\loadtest\session.json" -Value $out -Encoding UTF8
"SEED DONE"
