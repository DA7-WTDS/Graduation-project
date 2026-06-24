# System Sequence Diagrams

These sequence diagrams detail the key transactional and asynchronous event-driven flows across the **QuantWise** platform, using the actual classes and interfaces in the codebase and adhering to UML 2.5 standards.

---

## 1. User Registration Flow (CQRS & Transactional Outbox)
This flow models the registration process, demonstrating how a user's details are persisted and how a domain event is transactionally saved to the outbox for asynchronous cross-module notification.

```mermaid
sequenceDiagram
    autonumber
    actor User as Retail User
    participant UI as RegisterComponent (React)
    participant API as RegisterUserEndpoint (Api)
    participant Mediator as ISender (MediatR)
    participant Handler as CreateUserCommandHandler
    participant DB as UsersDbContext (EF Core)
    participant Outbox as OutboxMessages (Table)

    User->>UI: Submit form (email, password, etc)
    activate UI
    UI->>API: HTTP POST /api/users/register
    activate API
    API->>Mediator: Send(CreateUserCommand)
    activate Mediator
    Mediator->>Handler: Handle(CreateUserCommand)
    activate Handler

    Handler->>Handler: Hash password (BCrypt)

    rect rgb(18, 22, 27)
        note over Handler, DB: Atomic SQL Transaction
        Handler->>DB: AddAsync(New User Entity)
        Handler->>Handler: Raise UserCreatedDomainEvent
        Handler->>Outbox: AddAsync(UserCreatedDomainEvent JSON)
        Handler->>DB: SaveChangesAsync()
        DB-->>Handler: Commit Confirmation
    end

    Handler-->>Mediator: Result.Ok(UserId)
    deactivate Handler
    Mediator-->>API: Result
    deactivate Mediator
    API-->>UI: 201 Created (Location Header)
    deactivate API
    UI-->>User: Redirect to Onboarding
    deactivate UI
```

---

## 2. User Authentication & Session JWT Setup
Details credential checks and JWT authorization token generation.

```mermaid
sequenceDiagram
    autonumber
    actor User as Retail User
    participant UI as LoginComponent (React)
    participant API as LoginEndpoint (Api)
    participant Mediator as ISender (MediatR)
    participant Handler as LoginUserCommandHandler
    participant DB as UsersDbContext
    participant TokenGen as IJwtTokenGenerator

    User->>UI: Enter email & password
    activate UI
    UI->>API: HTTP POST /api/users/login
    activate API
    API->>Mediator: Send(LoginUserCommand)
    activate Mediator
    Mediator->>Handler: Handle(LoginUserCommand)
    activate Handler

    Handler->>DB: Query User by Email
    activate DB
    DB-->>Handler: User entity (Hashed password)
    deactivate DB

    Handler->>Handler: Verify password match (BCrypt)

    alt Credentials Invalid
        Handler-->>Mediator: Result.Fail(InvalidCredentials)
        Mediator-->>API: Result
        API-->>UI: 401 Unauthorized (Generic message)
        UI-->>User: Display "Incorrect email or password"
    else Credentials Valid
        Handler->>TokenGen: GenerateToken(UserId, UserRole)
        activate TokenGen
        TokenGen-->>Handler: JWT Access Token
        deactivate TokenGen
        Handler-->>Mediator: Result.Ok(LoginUserResponse)
        Mediator-->>API: Result
        API-->>UI: 200 OK (access_token)
        UI->>UI: Save JWT in localStorage
        UI-->>User: Redirect to Dashboard
    end

    deactivate Handler
    deactivate Mediator
    deactivate API
    deactivate UI
```

---

## 3. Onboarding & Portfolio Allocation Target Setup
Models the onboarding process, illustrating how user survey questionnaires are converted into target asset allocations and saved.

```mermaid
sequenceDiagram
    autonumber
    actor User as Retail User
    participant UI as OnboardingComponent (React)
    participant API as CreatePortfolioEndpoint
    participant Mediator as ISender (MediatR)
    participant Handler as CreatePortfolioCommandHandler
    participant DB as PortfolioDbContext

    User->>UI: Complete survey & target percentages
    activate UI
    UI->>API: HTTP POST /api/portfolios (Quiz + allocations)
    activate API
    API->>Mediator: Send(CreatePortfolioCommand)
    activate Mediator
    Mediator->>Handler: Handle(CreatePortfolioCommand)
    activate Handler

    Handler->>Handler: Validate allocations sum equals 100%
    Handler->>Handler: Determine RiskProfile enum
    Handler->>Handler: Instantiate Portfolio domain entity

    Handler->>DB: AddAsync(Portfolio)
    Handler->>DB: SaveChangesAsync()
    DB-->>Handler: Commit Confirmation

    Handler-->>Mediator: Result.Ok(PortfolioId)
    deactivate Handler
    Mediator-->>API: Result
    deactivate Mediator
    API-->>UI: 201 Created (Portfolio DTO)
    deactivate API
    UI-->>User: Display Dashboard & target allocations
    deactivate UI
```

---

## 4. Daily ML Scoring & Recommendation Ingestion (Nightly Clock)
Models the batch ingestion pipeline, illustrating the scheduler calling FastAPI to download data, run LSTM + XGBoost predictions, compute FinBERT sentiments, apply deterministic risk grading, and POST the results into the backend relational store.

```mermaid
sequenceDiagram
    autonumber
    participant Quartz as FetchDailyPipelineJob (Scheduler)
    participant PyPipeline as FastAPI Pipeline (main.py)
    participant yFin as Yahoo Finance API
    participant FHub as Finnhub API
    participant RecEndpoint as IngestDailyResultsEndpoint
    participant Mediator as ISender (MediatR)
    participant IngestHandler as IngestDailyRunCommandHandler
    participant DB as RecommendationsDbContext

    Quartz->>PyPipeline: HTTP POST /api/score
    activate PyPipeline

    PyPipeline->>yFin: Batch fetch OHLCV prices (6m data)
    activate yFin
    yFin-->>PyPipeline: Historical prices dataframes
    deactivate yFin

    PyPipeline->>FHub: Fetch news & analyst consensus ratings
    activate FHub
    FHub-->>PyPipeline: Headlines & analyst ratings array
    deactivate FHub

    PyPipeline->>PyPipeline: Engineer features (SMA, EMA, RSI, etc.)
    PyPipeline->>PyPipeline: Run PyTorch LSTM (MC-Dropout)
    PyPipeline->>PyPipeline: Feed LSTM states + tech features to XGBoost
    PyPipeline->>PyPipeline: Run NLP sentiment on headlines via FinBERT
    PyPipeline->>PyPipeline: Run apply_risk_rules() (Compute conviction & risk level)

    PyPipeline->>RecEndpoint: HTTP POST /api/internal/daily-results (JSON body)
    activate RecEndpoint
    RecEndpoint->>Mediator: Send(IngestDailyRunCommand)
    activate Mediator
    Mediator->>IngestHandler: Handle(IngestDailyRunCommand)
    activate IngestHandler

    IngestHandler->>DB: AddAsync(DailyRun Entity with Predictions)
    IngestHandler->>IngestHandler: Raise DailyRunIngestedDomainEvent
    IngestHandler->>DB: SaveChangesAsync()
    DB-->>IngestHandler: Commit Confirmation

    IngestHandler-->>Mediator: Result.Ok(DailyRunId)
    deactivate IngestHandler
    Mediator-->>RecEndpoint: Result
    deactivate Mediator
    RecEndpoint-->>PyPipeline: 200 OK
    deactivate RecEndpoint
    PyPipeline-->>Quartz: Job Completed successfully
    deactivate PyPipeline
```

---

## 5. Recommendations Retrieval & request-time Gemini Personalization
This is the core live retrieval logic: user visits dashboard → backend checks cache → on miss, calls Portfolio Module public API to fetch risk levels → queries latest DB run → passes both to Gemini to output risk-adherent allocation recommendations → caches results.

```mermaid
sequenceDiagram
    autonumber
    actor User as Retail User
    participant UI as DashboardComponent (React)
    participant API as GetRecommendationsEndpoint
    participant Mediator as ISender (MediatR)
    participant Handler as GetRecommendationsQueryHandler
    participant Cache as HybridCache (Redis)
    participant PortApi as IPortfolioApi (Public Contract)
    participant DB as RecommendationsDbContext
    participant LlmClient as ILlmClient (GeminiLlmClient)
    participant Gemini as Google Gemini API

    User->>UI: Open dashboard page
    activate UI
    UI->>API: HTTP GET /api/recommendations (Bearer JWT)
    activate API
    API->>Mediator: Send(GetRecommendationsQuery)
    activate Mediator
    Mediator->>Handler: Handle(GetRecommendationsQuery)
    activate Handler

    Handler->>Cache: GetOrCreateAsync(CacheKey, Factory)
    activate Cache

    alt Cache Hit (Data present in Redis/Memory)
        Cache-->>Handler: Return cached RecommendationsDTO
    else Cache Miss (Executes Factory logic)
        Handler->>PortApi: GetUserRiskProfileAsync(UserId)
        activate PortApi
        PortApi-->>Handler: Return RiskProfile enum string
        deactivate PortApi

        Handler->>DB: Query latest DailyRun including StockPredictions
        activate DB
        DB-->>Handler: Query result
        deactivate DB

        Handler->>LlmClient: CompleteAsync(Prompt, ResponseSchema)
        activate LlmClient
        LlmClient->>LlmClient: Build prompt injecting user risk + stock scores

        LlmClient->>Gemini: HTTP POST /v1/models/gemini-2.5-flash:generateContent (JSON Schema forced)
        activate Gemini
        Gemini-->>LlmClient: JSON string matching schema
        deactivate Gemini

        LlmClient-->>Handler: Deserialized RecommendationsDTO
        deactivate LlmClient

        Handler->>Cache: Write DTO back to cache (24h TTL)
        Cache-->>Handler: Factory execution returns DTO
    end
    deactivate Cache

    Handler-->>Mediator: Result.Ok(RecommendationsDTO)
    deactivate Handler
    Mediator-->>API: Result
    deactivate Mediator
    API-->>UI: 200 OK (JSON picks payload)
    deactivate API
    UI->>UI: Render BUY / WATCH / AVOID lists
    UI-->>User: View personalized recommendation profile
    deactivate UI
```
