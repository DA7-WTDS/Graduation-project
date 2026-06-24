# System Sequence Diagrams

These sequence diagrams detail the key transactional and asynchronous event-driven flows across the **QuantWise** platform, using the actual classes and interfaces in the codebase.

---

## 1. User Registration Flow

This flow illustrates the user registration process, focusing on **CQRS** handlers and persistence in the database.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Register as RegisterComponent<br/>(React)
    participant Endpoint as RegisterUser<br/>(Endpoint)
    participant MediatR as ISender<br/>(MediatR)
    participant Handler as CreateUserCommandHandler
    participant DB as UsersDbContext<br/>(EF Core)

    User->>Register: Input credentials
    Register->>Endpoint: HTTP POST /api/users/register
    Endpoint->>MediatR: Send(CreateUserCommand)
    MediatR->>Handler: Handle(CreateUserCommand)
    Handler->>Handler: Hash password (BCrypt)
    
    rect rgb(200, 220, 240)
        note over Handler,DB: Database Transaction (Atomic)
        Handler->>DB: AddAsync(User)
        DB-->>Handler: SaveChangesAsync() Commit
    end
    
    Handler-->>MediatR: Result.Ok(UserId)
    MediatR-->>Endpoint: Result
    Endpoint-->>Register: 201 Created (Location)
    Register-->>User: Redirect to Onboarding / Login
```

---

## 2. User Authentication & JWT Session Setup

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Login as LoginComponent<br/>(React)
    participant Endpoint as LoginUser<br/>(Endpoint)
    participant MediatR as ISender<br/>(MediatR)
    participant Handler as LoginUserCommandHandler
    participant DB as UsersDbContext<br/>(EF Core)
    participant Generator as IJwtTokenGenerator<br/>(JwtTokenGenerator)

    User->>Login: Input email & password
    Login->>Endpoint: HTTP POST /api/users/login
    Endpoint->>MediatR: Send(LoginUserCommand)
    MediatR->>Handler: Handle(LoginUserCommand)
    Handler->>DB: Query User by Email
    DB-->>Handler: User entity (hashed password)
    Handler->>Handler: Verify password matching
    
    alt Verification Fails
        Handler-->>MediatR: Result.Fail(InvalidCredentials)
        MediatR-->>Endpoint: Result
        Endpoint-->>Login: 400 Bad Request
        Login-->>User: Show authentication error
    else Verification Succeeds
        Handler->>Generator: GenerateToken(UserId, Role)
        Generator-->>Handler: access JWT Token (string)
        Handler-->>MediatR: Result.Ok(LoginUserResponse)
        MediatR-->>Endpoint: Result
        Endpoint-->>Login: 200 OK (access token)
        Login->>Login: Store token in State / localStorage
        Login-->>User: Redirect to Dashboard
    end
```

---

## 3. Portfolio Creation & Onboarding Risk Profiling

This flow shows how the user submits their questionnaire answers and desired asset allocation target percentages to set up their portfolio.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Onboard as OnboardingComponent<br/>(React)
    participant Endpoint as CreatePortfolio<br/>(Endpoint)
    participant MediatR as ISender<br/>(MediatR)
    participant Handler as CreatePortfolioCommandHandler
    participant Repository as IPortfolioRepository<br/>(PortfolioRepository)
    participant DB as PortfolioDbContext<br/>(EF Core)
    participant UoW as IUnitOfWork

    User->>Onboard: Fill quiz & allocations
    Onboard->>Endpoint: HTTP POST /api/portfolios (quiz answers & percentages)
    Endpoint->>MediatR: Send(CreatePortfolioCommand)
    MediatR->>Handler: Handle(CreatePortfolioCommand)
    Handler->>Repository: GetByUserIdAsync(UserId)
    Repository->>DB: Query Portfolio by UserId
    DB-->>Repository: null
    Repository-->>Handler: null
    
    Handler->>Handler: Validate target allocation sum equals 100%
    Handler->>Handler: Parse RiskProfile enum (Conservative/Moderate/Aggressive)
    Handler->>Handler: Create Portfolio domain aggregate (Portfolio.Create)
    
    rect rgb(240, 220, 240)
        note over Handler,DB: Write Database Operation
        Handler->>Repository: AddAsync(Portfolio)
        Handler->>UoW: SaveChangesAsync()
        UoW->>DB: Insert row into Portfolios table
        DB-->>UoW: Commit
    end
    
    Handler-->>MediatR: Result.Ok(PortfolioId)
    MediatR-->>Endpoint: Result
    Endpoint-->>Onboard: 201 Created (Portfolio Details)
    Onboard-->>User: Load Dashboard with target mix Allocations
```

---

## 4. Daily AI Batch Prediction & Ingest Pipeline

This flow explains how the background prediction service scrapes data, generates predictions, applies risk grading rules, and ingests the market-wide signals into the backend Recommendations database.

```mermaid
sequenceDiagram
    autonumber
    participant Job as FetchDailyPipelineJob<br/>(Quartz.NET scheduler)
    participant FastAPI as main.py<br/>(FastAPI python service)
    participant yFinance as yFinance API
    participant Finnhub as Finnhub API
    participant Endpoint as IngestDailyResults<br/>(Endpoint)
    participant MediatR as ISender<br/>(MediatR)
    participant Handler as IngestDailyRunCommandHandler
    participant Repository as IDailyRunRepository<br/>(DailyRunRepository)
    participant DB as RecommendationsDbContext<br/>(EF Core)
    participant UoW as IUnitOfWork

    loop Every 24 Hours
        Job->>FastAPI: Trigger Scoring (HTTP POST /api/score)
    end
    
    FastAPI->>yFinance: Download historical prices
    yFinance-->>FastAPI: Data frame
    FastAPI->>Finnhub: Query news & consensus analyst targets
    Finnhub-->>FastAPI: Headlines & ratings
    
    FastAPI->>FastAPI: Run Stage 1: LSTM model predictions
    FastAPI->>FastAPI: Run Stage 2: XGBoost regressor indicators
    FastAPI->>FastAPI: Run FinBERT sentiment analysis on news titles
    FastAPI->>FastAPI: Apply apply_risk_rules() (risk_rules.py)
    
    FastAPI->>Endpoint: HTTP POST /api/internal/daily-results (JSON payload)
    Endpoint->>MediatR: Send(IngestDailyRunCommand)
    MediatR->>Handler: Handle(IngestDailyRunCommand)
    
    rect rgb(200, 220, 240)
        note over Handler,DB: Persist Daily run results
        Handler->>Repository: AddAsync(DailyRun entity containing predictions list)
        Handler->>Handler: Raise DailyRunIngestedDomainEvent (Outbox)
        Handler->>UoW: SaveChangesAsync()
        UoW->>DB: Insert DailyRuns, StockPredictions & OutboxMessages rows
        DB-->>UoW: Commit
    end
    
    Handler-->>MediatR: Result.Ok(DailyRunId)
    MediatR-->>Endpoint: Result
    Endpoint-->>FastAPI: 200 OK
```

---

## 5. Get Recommendations (Request-Time Gemini Personalization Flow)

This sequence details what happens when a user loads their dashboard. The Recommendations module acts as the orchestrator, retrieving the user's risk profile from the Portfolio module, loading market-wide signals, calling Gemini, caching the response in Redis, and returning personalized picks.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Dashboard as DashboardComponent<br/>(React)
    participant Endpoint as GetRecommendations<br/>(Endpoint)
    participant MediatR as ISender<br/>(MediatR)
    participant Handler as GetRecommendationsQueryHandler
    participant Cache as HybridCache<br/>(Redis + Memory)
    participant PortfolioAPI as IPortfolioApi<br/>(Public Contract)
    participant DB as RecommendationsDbContext<br/>(EF Core)
    participant GeminiClient as ILlmClient<br/>(GeminiLlmClient)
    participant Gemini as Google Gemini API

    User->>Dashboard: Open Dashboard page
    Dashboard->>Endpoint: HTTP GET /api/recommendations (Header: JWT)
    Endpoint->>MediatR: Send(GetRecommendationsQuery)
    MediatR->>Handler: Handle(GetRecommendationsQuery)
    
    Handler->>Cache: GetOrCreateAsync(CacheKey, Factory)
    
    alt Cache Hit (Within 12h)
        Cache-->>Handler: Return cached RecommendationsDTO
    else Cache Miss / Expired (Execute Factory)
        Handler->>PortfolioAPI: GetUserRiskProfileAsync(UserId)
        PortfolioAPI-->>Handler: Return RiskProfile enum string
        
        Handler->>DB: Fetch latest DailyRun including StockPredictions
        DB-->>Handler: DailyRun object with predictions collection
        
        Handler->>GeminiClient: CompleteAsync(Prompt, ResponseSchema)
        GeminiClient->>GeminiClient: Construct user prompt context with risk & stock records
        GeminiClient->>Gemini: HTTP POST /v1/models/gemini-1.5-flash:generateContent
        
        alt Gemini output parse success
            Gemini-->>GeminiClient: JSON string matching schema
        else Gemini output parse fail
            loop Retry up to 3 times
                GeminiClient->>Gemini: HTTP POST Retry
            end
            Gemini-->>GeminiClient: JSON string
        end
        
        GeminiClient-->>Handler: Deserialized picks list
        Handler->>Handler: Construct RecommendationsDTO
        Handler-->>Cache: Save RecommendationsDTO & Return
    end
    
    Handler-->>MediatR: Result.Ok(RecommendationsDTO)
    MediatR-->>Endpoint: Result
    Endpoint-->>Dashboard: 200 OK (JSON picks payload)
    Dashboard->>Dashboard: Render signal-coded recommendations rows
    Dashboard-->>User: View BUY / WATCH / AVOID recommendations
```
