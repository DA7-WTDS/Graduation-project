# QuantWise UML Diagrams Portfolio

This document contains standard-compliant UML 2.5 diagrams for the **QuantWise** decision-support stock advisory platform. It covers the **Use Case Diagram**, **Class Diagram**, and **Sequence Diagrams** representing key system flows.

For maximum portability and tool support, every diagram is provided in two formats:
1. **PlantUML**: The industry-standard textual UML notation for enterprise software architecture tools.
2. **Mermaid**: Renders natively in modern Markdown viewers (like GitHub, GitLab, and IDE preview extensions).

---

## 1. Use Case Diagram

A UML Use Case Diagram maps the external actors, the system boundary, and the relationships (associations, generalizations, inclusions, and extensions) between use cases.

### UML Standards Applied
* **Actors**: Represented as stick figures (PlantUML) or clean boundary blocks (Mermaid). Primary actors are on the left; secondary system/API actors are on the right.
* **System Boundary**: A clearly defined rectangle grouping the internal modules of the QuantWise application, keeping actors external.
* **Relationships**:
  * **Association**: Solid lines connecting actors to use cases.
  * **Inclusion (`<<include>>`)**: Dashed arrows pointing from base use cases to included behaviors (e.g., retrieval requiring portfolio checks).
  * **Extension (`<<extend>>`)**: Dashed arrows pointing from extending features to the base use case.

### PlantUML Definition
```plantuml
@startuml QuantWise Use Case Diagram
left to right direction
skinparam packageStyle rectangle

actor "Retail User" as User
actor "Administrator" as Admin
actor "FastAPI Pipeline" as Pipeline
actor "Google Gemini API" as Gemini

rectangle "QuantWise Platform" {
    package "Authentication & Users" {
        usecase "Register Account" as UC_Register
        usecase "Log In" as UC_Login
        usecase "View Profile" as UC_Profile
    }
    
    package "Portfolio Management" {
        usecase "Create Portfolio" as UC_CreatePortfolio
        usecase "View Portfolio & Allocation" as UC_ViewPortfolio
        usecase "Update Allocation & Risk Settings" as UC_UpdatePortfolio
    }
    
    package "AI Recommendations" {
        usecase "View Daily Recommendations" as UC_ViewRecs
        usecase "Personalize Recommendations (LLM)" as UC_PersonalizeRecs
        usecase "View Raw Predictions (Simulator)" as UC_ViewPredictions
        usecase "Ingest Daily ML Scoring Run" as UC_IngestResults
    }
    
    package "Notifications" {
        usecase "View Notifications" as UC_ViewNotifications
        usecase "Mark Notification as Read" as UC_MarkRead
        usecase "Mark All Read" as UC_MarkAllRead
        usecase "Trigger Test Notification" as UC_TestNotification
    }
    
    package "Admin Services" {
        usecase "Manage Users" as UC_ManageUsers
        usecase "View Audit Logs" as UC_ViewAudit
        usecase "Configure System Parameters" as UC_ConfigParams
    }
}

User --> UC_Register
User --> UC_Login
User --> UC_Profile

User --> UC_CreatePortfolio
User --> UC_ViewPortfolio
User --> UC_UpdatePortfolio

User --> UC_ViewRecs
User --> UC_ViewPredictions
User --> UC_ViewNotifications
User --> UC_MarkRead
User --> UC_MarkAllRead
User --> UC_TestNotification

Admin --> UC_ManageUsers
Admin --> UC_ViewAudit
Admin --> UC_ConfigParams

UC_ViewRecs ..> UC_PersonalizeRecs : <<include>>
UC_PersonalizeRecs ..> Gemini : <<use>>

Pipeline --> UC_IngestResults
@endum
```

### Mermaid Flowchart Representation
```mermaid
flowchart LR
    %% Actors
    User["  o  \n /|\\ \n / \\ \nUser"]
    Admin["  o  \n /|\\ \n / \\ \nAdmin"]
    Pipeline["[FastAPI Pipeline]"]
    Gemini["[Google Gemini API]"]

    subgraph QuantWise ["QuantWise System Boundary"]
        direction TB

        subgraph UsersModule["Authentication & Users Module"]
            UC_Register(["Register Account"])
            UC_Login(["Log In"])
            UC_Profile(["View Profile"])
        end

        subgraph PortfolioModule["Portfolio Management Module"]
            UC_CreatePortfolio(["Create Portfolio"])
            UC_ViewPortfolio(["View Portfolio & Allocation"])
            UC_UpdatePortfolio(["Update Allocation & Risk Settings"])
        end

        subgraph RecsModule["AI Recommendations Module"]
            UC_ViewRecs(["View Daily AI Recommendations"])
            UC_PersonalizeRecs(["Personalize Recommendations (LLM)"])
            UC_ViewPredictions(["View Raw Predictions (Simulator)"])
            UC_IngestResults(["Ingest Daily ML Scoring Run"])
        end

        subgraph NotifModule["Notifications Module"]
            UC_ViewNotifications(["View Notifications"])
            UC_MarkRead(["Mark Notification as Read"])
            UC_MarkAllRead(["Mark All Read"])
            UC_TestNotification(["Trigger Test Notification"])
        end

        subgraph AdminModule["Admin Management Module"]
            UC_ManageUsers(["Manage Users"])
            UC_ViewAudit(["View Audit Logs"])
            UC_ConfigParams(["Configure System Parameters"])
        end
    end

    %% User associations
    User --> UC_Register
    User --> UC_Login
    User --> UC_Profile
    User --> UC_CreatePortfolio
    User --> UC_ViewPortfolio
    User --> UC_UpdatePortfolio
    User --> UC_ViewRecs
    User --> UC_ViewPredictions
    User --> UC_ViewNotifications
    User --> UC_MarkRead
    User --> UC_MarkAllRead
    User --> UC_TestNotification

    %% Admin associations
    Admin --> UC_ManageUsers
    Admin --> UC_ViewAudit
    Admin --> UC_ConfigParams

    %% System and Includes
    Pipeline --> UC_IngestResults
    UC_ViewRecs -.->|"<<include>>"| UC_PersonalizeRecs
    UC_PersonalizeRecs -.->|"<<use>>"| Gemini

    %% Styling
    classDef actor fill:#12161B,stroke:#FFB000,stroke-width:2px,color:#E8EAED;
    classDef usecase fill:#181D24,stroke:#3DDC84,stroke-width:1.5px,color:#E8EAED;
    classDef boundary fill:#0B0E11,stroke:#64A0FF,stroke-width:2px,color:#E8EAED;

    class User,Admin,Pipeline,Gemini actor;
    class UC_Register,UC_Login,UC_Profile,UC_CreatePortfolio,UC_ViewPortfolio,UC_UpdatePortfolio,UC_ViewRecs,UC_PersonalizeRecs,UC_ViewPredictions,UC_IngestResults,UC_ViewNotifications,UC_MarkRead,UC_MarkAllRead,UC_TestNotification,UC_ManageUsers,UC_ViewAudit,UC_ConfigParams usecase;
    class QuantWise boundary;
```

---

## 2. Class Diagram

The UML Class Diagram shows the system's static structure, detailing the classes, interfaces, attributes, operations (methods), and their design-time relationships.

### UML Standards Applied
* **Stereotypes**: Indicated via `<<stereotype>>` headers for specific class roles:
  * `«entity»`: Db-persisted domain models (deriving from the `Entity` base class).
  * `«aggregate-root»`: Parent entity coordinating consistency boundaries.
  * `«interface»`: Abstract operations.
  * `«handler»`: CQRS command or query handlers.
  * `«command»` / `«query»`: CQRS message payloads.
* **Access Modifiers**:
  * `+` Public
  * `-` Private
  * `#` Protected
* **Relationship Lines**:
  * `--|>` Generalization (Inheritance)
  * `..|>` Realization (Interface Implementation)
  * `*--` Composition (Aggregation where the part cannot exist without the whole)
  * `..>` Dependency (Usage relationship)

### PlantUML Definition
```plantuml
@startuml QuantWise Class Diagram
skinparam classAttributeIconSize 0
left to right direction

package "Common.Domain" {
    abstract class Entity {
        # Guid Id
        - List<IDomainEvent> _domainEvents
        + IReadOnlyList<IDomainEvent> GetDomainEvents()
        # void Raise(IDomainEvent event)
        + void ClearDomainEvents()
    }
    interface IDomainEvent
}

package "Users Module" {
    class User <<aggregate-root>> {
        + string FirstName
        + string LastName
        + string Email
        + string HashedPassword
        + Role UserRole
        + DateTime CreatedAt
        + {static} User Create(string firstName, string lastName, string email, string hashedPassword)
        + void Update(string firstName, string lastName, string email)
    }
    
    interface IUserRepository <<interface>> {
        + Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        + Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        + Task AddAsync(User user, CancellationToken ct)
    }
    
    class RegisterUserCommand <<command>> {
        + string FirstName
        + string LastName
        + string Email
        + string Password
    }
    
    class RegisterUserCommandHandler <<handler>> {
        - IUserRepository _userRepository
        - IUnitOfWork _unitOfWork
        - IPasswordHasher _passwordHasher
        + Task<Result<Guid>> Handle(RegisterUserCommand cmd, CancellationToken ct)
    }
}

package "Portfolio Module" {
    class Portfolio <<aggregate-root>> {
        + Guid UserId
        + string PrimaryGoal
        + string TimeHorizon
        + int RiskTolerance
        + string MarketReaction
        + string InvestmentExperience
        + int StocksPercentage
        + int BondsPercentage
        + int EtfsPercentage
        + int CashPercentage
        + RiskProfile Profile
        + decimal InvestmentAmount
        + DateTime CreatedAt
        + DateTime? UpdatedAt
        + {static} Portfolio Create(Guid userId, string primaryGoal, ...)
        + void Update(string primaryGoal, string timeHorizon, ...)
    }
    
    interface IPortfolioRepository <<interface>> {
        + Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken ct)
        + Task<Portfolio?> GetByUserIdAsync(Guid userId, CancellationToken ct)
        + Task AddAsync(Portfolio portfolio, CancellationToken ct)
    }
    
    class CreatePortfolioCommand <<command>> {
        + Guid UserId
        + string PrimaryGoal
        + int RiskTolerance
        + int StocksPercentage
        + int BondsPercentage
        + int EtfsPercentage
        + int CashPercentage
        + string RiskProfile
        + decimal InvestmentAmount
    }
    
    class CreatePortfolioCommandHandler <<handler>> {
        - IPortfolioRepository _portfolioRepository
        - IUnitOfWork _unitOfWork
        + Task<Result<Guid>> Handle(CreatePortfolioCommand cmd, CancellationToken ct)
        - bool ValidateAllocations(CreatePortfolioCommand req)
    }
    
    interface IPortfolioApi <<interface>> {
        + Task<string?> GetUserRiskProfileAsync(Guid userId)
    }
}

package "Recommendations Module" {
    class DailyRun <<aggregate-root>> {
        + DateTime GeneratedAt
        + int Count
        + DateTime CreatedAt
        + IReadOnlyCollection<StockPrediction> Predictions
        + {static} DailyRun Create(DateTime generatedAt, List<StockPrediction> predictions)
    }
    
    class StockPrediction <<entity>> {
        + Guid DailyRunId
        + string Ticker
        + string Direction
        + double ChangePct
        + double Confidence
        + double SentimentScore
        + string Signal
        + double? AnalystRating
        + string Agreement
        + string RiskLevel
        + double ConvictionScore
        + string[] RiskFlags
        + string Rationale
        + {static} StockPrediction Create(...)
    }
    
    interface IDailyRunRepository <<interface>> {
        + Task<DailyRun?> GetLatestAsync(CancellationToken ct)
        + Task AddAsync(DailyRun dailyRun, CancellationToken ct)
    }
    
    class GetRecommendationsQuery <<query>> {
        + Guid UserId
    }
    
    class GetRecommendationsQueryHandler <<handler>> {
        - HybridCache _cache
        - IPortfolioApi _portfolioApi
        - IDailyRunRepository _dailyRunRepository
        - ILlmClient _llmClient
        + Task<Result<RecommendationsDTO>> Handle(GetRecommendationsQuery query, CancellationToken ct)
    }
    
    interface ILlmClient <<interface>> {
        + Task<string> CompleteAsync(string prompt, string schema, CancellationToken ct)
    }
    
    class GeminiLlmClient {
        - HttpClient _httpClient
        - string _apiKey
        + Task<string> CompleteAsync(string prompt, string schema, CancellationToken ct)
    }
}

package "Notifications Module" {
    class Notification <<entity>> {
        + Guid UserId
        + string Title
        + string Message
        + NotificationType Type
        + bool IsRead
        + DateTime CreatedAt
    }
    
    class UserRegisteredIntegrationEventConsumer <<consumer>> {
        + Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> ctx)
    }
}

package "Python Pipeline" {
    class LSTMModel {
        - int lookback_window
        - int input_features
        + forward(x : Tensor) : Tensor
        + predict_with_uncertainty(ticker : str) : tuple
    }
    
    class XGBoostHead {
        + predict(lstm_features : Tensor, tech_indicators : array) : float
    }
    
    class FinBERTSentimentEngine {
        + compute_composite_sentiment(ticker : str) : float
    }
    
    class RiskRulesEngine {
        + apply_risk_rules(prediction : dict, sentiment : float) : ScoredStock
        + compute_conviction_score(conf : float, s : float, a : str) : float
    }
}

' Inheritance
User --|> Entity
Portfolio --|> Entity
DailyRun --|> Entity
StockPrediction --|> Entity
Notification --|> Entity

' Interface Implementations
GeminiLlmClient ..|> ILlmClient

' Composition (Part-Whole lifecycle binding)
DailyRun *-- StockPrediction : composition

' CQRS / Handler Dependencies
RegisterUserCommandHandler ..> IUserRepository : uses
RegisterUserCommandHandler ..> User : creates
CreatePortfolioCommandHandler ..> IPortfolioRepository : uses
CreatePortfolioCommandHandler ..> Portfolio : creates

GetRecommendationsQueryHandler ..> IPortfolioApi : calls
GetRecommendationsQueryHandler ..> IDailyRunRepository : queries
GetRecommendationsQueryHandler ..> ILlmClient : executes

' Python coupling
LSTMModel ..> XGBoostHead : feeds state
XGBoostHead ..> RiskRulesEngine : feeds return prediction
FinBERTSentimentEngine ..> RiskRulesEngine : feeds sentiment
@endum
```

### Mermaid Class Diagram Representation
```mermaid
classDiagram
    %% Core Entities & Bases
    class Entity {
        <<abstract>>
        #Guid Id
        -List~IDomainEvent~ _domainEvents
        +GetDomainEvents() List~IDomainEvent~
        #Raise(event) void
        +ClearDomainEvents() void
    }

    class User {
        <<aggregate-root>>
        +string FirstName
        +string LastName
        +string Email
        +string HashedPassword
        +Role UserRole
        +DateTime CreatedAt
        +Create(firstName, lastName, email, hashedPassword) User$
        +Update(firstName, lastName, email) void
    }

    class Portfolio {
        <<aggregate-root>>
        +Guid UserId
        +string PrimaryGoal
        +string TimeHorizon
        +int RiskTolerance
        +string MarketReaction
        +string InvestmentExperience
        +int StocksPercentage
        +int BondsPercentage
        +int EtfsPercentage
        +int CashPercentage
        +RiskProfile Profile
        +decimal InvestmentAmount
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +Create(userId, primaryGoal, timeHorizon, ...) Portfolio$
        +Update(primaryGoal, timeHorizon, ...) void
    }

    class DailyRun {
        <<aggregate-root>>
        +DateTime GeneratedAt
        +int Count
        +DateTime CreatedAt
        +IReadOnlyCollection~StockPrediction~ Predictions
        +Create(generatedAt, predictions) DailyRun$
    }

    class StockPrediction {
        <<entity>>
        +Guid DailyRunId
        +string Ticker
        +string Direction
        +double ChangePct
        +double Confidence
        +double SentimentScore
        +string Signal
        +double? AnalystRating
        +string Agreement
        +string RiskLevel
        +double ConvictionScore
        +string[] RiskFlags
        +string Rationale
        +Create(ticker, direction, ...) StockPrediction$
    }

    class Notification {
        <<entity>>
        +Guid UserId
        +string Title
        +string Message
        +NotificationType Type
        +bool IsRead
        +DateTime CreatedAt
    }

    %% Interfaces
    class IPortfolioApi {
        <<interface>>
        +GetUserRiskProfileAsync(userId) Task~string~
    }

    class ILlmClient {
        <<interface>>
        +CompleteAsync(prompt, schema, ct) Task~string~
    }

    class GeminiLlmClient {
        -HttpClient _httpClient
        -string _apiKey
        +CompleteAsync(prompt, schema, ct) Task~string~
    }

    %% Python Pipeline Classes
    class LSTMModel {
        -int lookback_window
        -int input_features
        +forward(x) Tensor
        +predict_with_uncertainty(ticker) tuple
    }

    class XGBoostHead {
        +predict(lstm_features, tech_indicators) float
    }

    class FinBERTSentimentEngine {
        +compute_composite_sentiment(ticker) float
    }

    class RiskRulesEngine {
        +apply_risk_rules(prediction, sentiment) ScoredStock
        +compute_conviction_score(conf, s, a) float
    }

    %% Inheritance relationships
    User --|> Entity
    Portfolio --|> Entity
    DailyRun --|> Entity
    StockPrediction --|> Entity
    Notification --|> Entity

    %% Composition relationship (Stocks can't exist outside their Daily Run)
    DailyRun *-- StockPrediction : composition

    %% Realization
    GeminiLlmClient ..|> ILlmClient

    %% Dependencies
    LSTMModel ..> XGBoostHead : feeds state
    XGBoostHead ..> RiskRulesEngine : prediction
    FinBERTSentimentEngine ..> RiskRulesEngine : sentiment
```

---

## 3. Sequence Diagrams

UML Sequence Diagrams show object interactions arranged in time sequence. The following 5 diagrams cover the critical operational loops of QuantWise.

### UML Standards Applied
* **Lifelines**: Dashed lines dropping down from named participant boxes.
* **Focus of Control**: Activation bars (vertical boxes on lifelines) indicating that a process is active.
* **Message Arrows**:
  * Solid line, solid arrow (`->Block`) represents synchronous operations.
  * Solid line, open arrow (`->`) represents asynchronous signals.
  * Dashed line, open arrow (`-->>`) represents execution return value pathways.

---

### Flow 1: User Registration (CQRS & Transactional Outbox)
This flow models the registration process, demonstrating how a user's login details are persisted and how a domain event is transactionally saved to the outbox for asynchronous cross-module notification.

#### PlantUML
```plantuml
@startuml User Registration Flow
autonumber
actor User as "Retail User"
participant UI as "RegisterComponent (React)"
participant API as "RegisterUserEndpoint (Api)"
participant Mediator as "ISender (MediatR)"
participant Handler as "CreateUserCommandHandler"
database DB as "UsersDbContext (EF Core)"
database Outbox as "OutboxMessages (Table)"

User -> UI : Submit form (email, password, etc)
activate UI
UI -> API : HTTP POST /api/users/register
activate API
API -> Mediator : Send(CreateUserCommand)
activate Mediator
Mediator -> Handler : Handle(CreateUserCommand)
activate Handler

Handler -> Handler : Hash password (BCrypt)

note over Handler, DB, Outbox: Atomic SQL Transaction Start
Handler -> DB : AddAsync(New User Entity)
Handler -> Handler : Raise UserCreatedDomainEvent
Handler -> Outbox : AddAsync(UserCreatedDomainEvent JSON)
Handler -> DB : SaveChangesAsync()
DB --> Handler : Commit Confirmation
note over Handler, DB, Outbox: Atomic SQL Transaction End

Handler --> Mediator : Result.Ok(UserId)
deactivate Handler
Mediator --> API : Result
deactivate Mediator
API --> UI : 201 Created (Location Header)
deactivate API
UI --> User : Redirect to Onboarding
deactivate UI
@endum
```

#### Mermaid
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

### Flow 2: User Authentication & Session JWT Setup
Details credential checks and JWT authorization token generation.

#### PlantUML
```plantuml
@startuml Authentication Flow
autonumber
actor User as "Retail User"
participant UI as "LoginComponent (React)"
participant API as "LoginEndpoint (Api)"
participant Mediator as "ISender (MediatR)"
participant Handler as "LoginUserCommandHandler"
database DB as "UsersDbContext"
participant TokenGen as "IJwtTokenGenerator"

User -> UI : Enter email & password
activate UI
UI -> API : HTTP POST /api/users/login
activate API
API -> Mediator : Send(LoginUserCommand)
activate Mediator
Mediator -> Handler : Handle(LoginUserCommand)
activate Handler

Handler -> DB : Query User by Email
activate DB
DB --> Handler : User entity (Hashed password)
deactivate DB

Handler -> Handler : Verify password match (BCrypt)

alt Credentials Invalid
    Handler --> Mediator : Result.Fail(InvalidCredentials)
    Mediator --> API : Result
    API --> UI : 401 Unauthorized (Generic message)
    UI --> User : Display "Incorrect email or password"
else Credentials Valid
    Handler -> TokenGen : GenerateToken(UserId, UserRole)
    activate TokenGen
    TokenGen --> Handler : JWT Access Token
    deactivate TokenGen
    Handler --> Mediator : Result.Ok(LoginUserResponse)
    Mediator --> API : Result
    API --> UI : 200 OK (access_token)
    UI -> UI : Save JWT in localStorage
    UI --> User : Redirect to Dashboard
end

deactivate Handler
deactivate Mediator
deactivate API
deactivate UI
@endum
```

#### Mermaid
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

### Flow 3: Onboarding & Portfolio Allocation Target Setup
Models the onboarding process, illustrating how user questionnaires are converted into target asset allocations and saved.

#### PlantUML
```plantuml
@startuml Portfolio Setup Flow
autonumber
actor User as "Retail User"
participant UI as "OnboardingComponent (React)"
participant API as "CreatePortfolioEndpoint"
participant Mediator as "ISender (MediatR)"
participant Handler as "CreatePortfolioCommandHandler"
database DB as "PortfolioDbContext"

User -> UI : Complete survey & target percentages
activate UI
UI -> API : HTTP POST /api/portfolios (Quiz + allocations)
activate API
API -> Mediator : Send(CreatePortfolioCommand)
activate Mediator
Mediator -> Handler : Handle(CreatePortfolioCommand)
activate Handler

Handler -> Handler : Validate allocations sum equals 100%
Handler -> Handler : Determine RiskProfile enum
Handler -> Handler : Instantiate Portfolio domain entity

Handler -> DB : AddAsync(Portfolio)
Handler -> DB : SaveChangesAsync()
DB --> Handler : Commit Confirmation

Handler --> Mediator : Result.Ok(PortfolioId)
deactivate Handler
Mediator --> API : Result
deactivate Mediator
API --> UI : 201 Created (Portfolio DTO)
deactivate API
UI --> User : Display Dashboard & target allocations
deactivate UI
@endum
```

#### Mermaid
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

### Flow 4: Daily ML Scoring & Recommendation Ingestion (Nightly Clock)
Models the batch ingestion pipeline, illustrating the scheduler calling FastAPI to download data, run LSTM + XGBoost predictions, compute FinBERT sentiments, apply deterministic risk grading, and POST the results into the backend relational store.

#### PlantUML
```plantuml
@startuml Ingest Pipeline Flow
autonumber
participant Quartz as "FetchDailyPipelineJob (Scheduler)"
participant PyPipeline as "FastAPI Pipeline (main.py)"
entity yFin as "Yahoo Finance API"
entity FHub as "Finnhub API"
participant RecEndpoint as "IngestDailyResultsEndpoint"
participant Mediator as "ISender (MediatR)"
participant IngestHandler as "IngestDailyRunCommandHandler"
database DB as "RecommendationsDbContext"

Quartz -> PyPipeline : HTTP POST /api/score
activate PyPipeline

PyPipeline -> yFin : Batch fetch OHLCV prices (6m data)
activate yFin
yFin --> PyPipeline : Historical prices dataframes
deactivate yFin

PyPipeline -> FHub : Fetch news & analyst consensus ratings
activate FHub
FHub --> PyPipeline : Headlines & analyst ratings array
deactivate FHub

PyPipeline -> PyPipeline : Engineer features (SMA, EMA, RSI, etc.)
PyPipeline -> PyPipeline : Run PyTorch LSTM (MC-Dropout)
PyPipeline -> PyPipeline : Feed LSTM states + tech features to XGBoost
PyPipeline -> PyPipeline : Run NLP sentiment on headlines via FinBERT
PyPipeline -> PyPipeline : Run apply_risk_rules() (Compute conviction & risk level)

PyPipeline -> RecEndpoint : HTTP POST /api/internal/daily-results (JSON body)
activate RecEndpoint
RecEndpoint -> Mediator : Send(IngestDailyRunCommand)
activate Mediator
Mediator -> IngestHandler : Handle(IngestDailyRunCommand)
activate IngestHandler

IngestHandler -> DB : AddAsync(DailyRun Entity with Predictions)
IngestHandler -> IngestHandler : Raise DailyRunIngestedDomainEvent
IngestHandler -> DB : SaveChangesAsync()
DB --> IngestHandler : Commit Confirmation

IngestHandler --> Mediator : Result.Ok(DailyRunId)
deactivate IngestHandler
Mediator --> RecEndpoint : Result
deactivate Mediator
RecEndpoint --> PyPipeline : 200 OK
deactivate RecEndpoint
PyPipeline --> Quartz : Job Completed successfully
deactivate PyPipeline
@endum
```

#### Mermaid
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

### Flow 5: Recommendations Fetching & request-time Gemini Personalization
This is the core live retrieval logic: user visits dashboard → backend checks cache → on miss, calls Portfolio Module public API to fetch risk levels → queries latest DB run → passes both to Gemini to output risk-adherent allocation recommendations → caches results.

#### PlantUML
```plantuml
@startuml Retrieval Flow
autonumber
actor User as "Retail User"
participant UI as "DashboardComponent (React)"
participant API as "GetRecommendationsEndpoint"
participant Mediator as "ISender (MediatR)"
participant Handler as "GetRecommendationsQueryHandler"
participant Cache as "HybridCache (Redis)"
participant PortApi as "IPortfolioApi (Public Contract)"
database DB as "RecommendationsDbContext"
participant LlmClient as "ILlmClient (GeminiLlmClient)"
participant Gemini as "Google Gemini API"

User -> UI : Open dashboard page
activate UI
UI -> API : HTTP GET /api/recommendations (Bearer JWT)
activate API
API -> Mediator : Send(GetRecommendationsQuery)
activate Mediator
Mediator -> Handler : Handle(GetRecommendationsQuery)
activate Handler

Handler -> Cache : GetOrCreateAsync(CacheKey, Factory)
activate Cache

alt Cache Hit (Data present in Redis/Memory)
    Cache --> Handler : Return cached RecommendationsDTO
else Cache Miss (Executes Factory logic)
    Handler -> PortApi : GetUserRiskProfileAsync(UserId)
    activate PortApi
    PortApi --> Handler : Return RiskProfile enum string
    deactivate PortApi
    
    Handler -> DB : Query latest DailyRun including StockPredictions
    activate DB
    DB --> Handler : DailyRun object graph
    deactivate DB
    
    Handler -> LlmClient : CompleteAsync(Prompt, ResponseSchema)
    activate LlmClient
    LlmClient -> LlmClient : Build prompt injecting user risk + stock scores
    
    LlmClient -> Gemini : HTTP POST /v1/models/gemini-2.5-flash:generateContent (JSON Schema forced)
    activate Gemini
    Gemini --> LlmClient : JSON string matching schema
    deactivate Gemini
    
    LlmClient --> Handler : Deserialized RecommendationsDTO
    deactivate LlmClient
    
    Handler -> Cache : Write DTO back to cache (24h TTL)
    Cache --> Handler : Factory execution returns DTO
end
deactivate Cache

Handler --> Mediator : Result.Ok(RecommendationsDTO)
deactivate Handler
Mediator --> API : Result
deactivate Mediator
API --> UI : 200 OK (JSON picks payload)
deactivate API
UI -> UI : Render BUY / WATCH / AVOID lists
UI --> User : View personalized recommendation profile
deactivate UI
@endum
```

#### Mermaid
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

---

## 4. System Architecture Diagram (C4 Model Context & Container)

These C4 architecture diagrams map the high-level system boundaries, external integrations (Level 1 System Context), and internal application containers, communications, and database persistence layers (Level 2 Container Diagram).

### A. C4 Level 1: System Context Diagram
Deconstructs the platform boundaries, showing external systems (yFinance, Finnhub, Gemini API, Mailpit SMTP) interacting with QuantWise.

#### PlantUML Definition
```plantuml
@startuml System Context Diagram
skinparam actorStyle awesome
left to right direction

actor "Retail Investor" as user
node "QuantWise Platform" as quantwise

cloud "Google Gemini API\n(LLM Provider)" as gemini
cloud "Market Data APIs\n(yFinance & Finnhub)" as market
cloud "SMTP Mail Server\n(Mailpit)" as mail

user --> quantwise : Uses browser client to query stock picks, setup risk profile, view simulator
quantwise --> gemini : Sends predictions data and queries user-tailored explanations
quantwise --> market : Scrapes prices and news sentiment
quantwise --> mail : Dispatches emails via SMTP
@endum
```

#### Mermaid Flowchart Representation
```mermaid
flowchart TB
    %% Nodes
    User["👤 Retail Investor\n(User)"]
    QuantWise["💻 QuantWise Platform\n(System Boundary)"]
    Gemini["🤖 Google Gemini API\n(LLM Provider)"]
    MarketAPIs["📈 Market Data Providers\n(yFinance & Finnhub)"]
    EmailServer["📧 SMTP Server\n(Mailpit / SMTP)"]

    %% Links
    User -->|Uses browser client to query stock picks, setup risk profile| QuantWise
    QuantWise -->|Requests personalized allocation & recommendations| Gemini
    QuantWise -->|Scrapes pricing arrays & analyst sentiment| MarketAPIs
    QuantWise -->|Dispatches transactional notifications| EmailServer

    %% Styles
    classDef default fill:#12161B,stroke:#FFB000,stroke-width:1.5px,color:#E8EAED;
    classDef external fill:#171C22,stroke:#8A9099,stroke-dasharray: 4,stroke-width:1px,color:#8A9099;
    classDef boundary fill:#0B0E11,stroke:#3DDC84,stroke-dasharray: 5,stroke-width:2px,color:#E8EAED;
    
    class User default;
    class QuantWise boundary;
    class Gemini,MarketAPIs,EmailServer external;
```

---

### B. C4 Level 2: Container Diagram
Details the internal modules, databases, pipeline runner, cache, and message queues that compose the modular-monolith and pipeline services.

#### PlantUML Definition
```plantuml
@startuml System Container Diagram
left to right direction

actor "Retail Investor" as user

package "QuantWise System Boundary" {
    component "React Frontend Client\n(React 18, TS, Vite)" as frontend
    
    node "API Monolith (.NET 10)" {
        component "Minimal API Gateway" as gateway
        component "Users Module" as users
        component "Portfolio Module" as portfolio
        component "Recommendations Module" as recommendations
        component "Notifications Module" as notifications
    }
    
    component "AI Scoring Pipeline\n(FastAPI Service)" as pipeline
    
    database "PostgreSQL Database\n(Isolated Schemas)" as postgres
    database "Redis Cache Tier" as redis
    queue "RabbitMQ Event Broker" as rabbitmq
}

cloud "Google Gemini API" as gemini
cloud "Market Data APIs\n(yFinance & Finnhub)" as market

user --> frontend : Interacts with UI (HTTPS)
frontend --> gateway : Queries REST endpoints (JWT, HTTPS)

gateway --> users : Routes auth endpoints
gateway --> portfolio : Routes profiles/allocations
gateway --> recommendations : Routes daily recommendations
gateway --> notifications : Routes notification feeds

users --> postgres : Reads/writes user profiles (EF Core)
portfolio --> postgres : Reads/writes client portfolios (EF Core)
recommendations --> postgres : Reads/writes daily runs (EF Core)
notifications --> postgres : Reads/writes inbox alerts (EF Core)

recommendations --> redis : Caches DTOs (12h TTL)

users --> rabbitmq : Publishes event via Outbox
rabbitmq --> notifications : Consumes events to Inbox

recommendations --> portfolio : Requests User Risk Profile (Public API)

pipeline --> market : Scrapes historical OHLCV & news
pipeline --> gateway : POSTs raw daily runs (HTTPS/JSON)

recommendations --> gemini : Requests personalized LLM picks (HTTPS/JSON)
@endum
```

#### Mermaid Flowchart Representation
```mermaid
flowchart TB
    subgraph Client["Client Layer"]
        Frontend["💻 React Frontend\n(React, TS, Vite)\nInteractive terminal dashboard"]
      end

    subgraph API["API Monolith (.NET 10)"]
        Gateway["⚡ ASP.NET Minimal API Gateway\nUnified gateway routing"]
        
        subgraph Modules["Monolith Domain Modules"]
            UsersMod["👤 Users Module"]
            PortfolioMod["💼 Portfolio Module"]
            RecsMod["🧠 Recommendations Module"]
            NotifMod["🔔 Notifications Module"]
        end
    end

    subgraph MessageQueue["Message Broker"]
        RabbitMQ["📨 RabbitMQ Bus\n(MassTransit Event Broker)"]
    end

    subgraph Data["Storage Layer"]
        PostgreSQL[("🐘 PostgreSQL 18 DB\n(Strictly Partitioned Tables)")]
        Redis[("⚡ Redis Cache Tier\n(Recommendations Cache)")]
    end

    subgraph AI["AI Pipeline Service"]
        FastAPI["🐍 FastAPI AI Service\n(Python Scikit/PyTorch/Transformers)"]
    end

    subgraph External["External APIs"]
        StockAPI["📈 yFinance & Finnhub"]
        GeminiAPI["🤖 Google Gemini API"]
    end

    %% Interactions
    Frontend <-->|HTTP REST & JWT Auth| Gateway
    Gateway --> UsersMod
    Gateway --> PortfolioMod
    Gateway --> RecsMod
    Gateway --> NotifMod
    
    %% Database connections
    UsersMod -->|Isolated EF DbContexts| PostgreSQL
    PortfolioMod -->|Isolated EF DbContexts| PostgreSQL
    RecsMod -->|Isolated EF DbContexts| PostgreSQL
    NotifMod -->|Isolated EF DbContexts| PostgreSQL
    
    RecsMod <-->|HybridCache 12h TTL| Redis
    
    %% Asynchronous communication via Outbox
    UsersMod -.->|Publish Outbox Events| RabbitMQ
    PortfolioMod -.->|Publish Outbox Events| RabbitMQ
    RecsMod -.->|Publish Outbox Events| RabbitMQ
    RabbitMQ -.->|Consume Inbox Events| NotifMod
    
    %% Internal Module Calls
    RecsMod -->|Fetch Risk Profile Contract| PortfolioMod

    %% AI Pipeline interactions
    FastAPI -->|1. Price scraping & sentiment| StockAPI
    FastAPI -->|2. Ingest daily run (HTTP POST)| Gateway
    RecsMod <-->|Request personalized LLM picks| GeminiAPI

    %% Styling
    classDef client fill:#0B1C24,stroke:#00B0FF,stroke-width:1.5px,color:#E8EAED;
    classDef gateway fill:#24190B,stroke:#FF9100,stroke-width:1.5px,color:#E8EAED;
    classDef module fill:#0B2414,stroke:#00E676,stroke-width:1.5px,color:#E8EAED;
    classDef storage fill:#240B24,stroke:#D500F9,stroke-width:1.5px,color:#E8EAED;
    classDef ai fill:#24240B,stroke:#FFEA00,stroke-width:1.5px,color:#E8EAED;
    classDef ext fill:#171C22,stroke:#8A9099,stroke-dasharray:4,color:#8A9099;

    class Frontend client;
    class Gateway gateway;
    class UsersMod,PortfolioMod,RecsMod,NotifMod module;
    class PostgreSQL,Redis,RabbitMQ storage;
    class FastAPI ai;
    class StockAPI,GeminiAPI ext;
```
