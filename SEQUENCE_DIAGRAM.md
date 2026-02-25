# Sequence Diagrams

## User Login and Onboarding Flow

### 1. User Registration and Login

```mermaid
sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant RegisterEndpoint
    participant MediatR
    participant UserService
    participant Database
    participant JWTService
    
    %% Registration Flow
    rect rgb(200, 220, 240)
        note over User,Database: Registration Process
        User->>Frontend: Enter registration details
        Frontend->>API: POST /users/register
        API->>RegisterEndpoint: Route to endpoint
        RegisterEndpoint->>MediatR: Send CreateUserCommand
        MediatR->>UserService: Handle command
        UserService->>UserService: Validate email format
        UserService->>UserService: Hash password
        UserService->>Database: Save to Users table
        Database-->>UserService: User created (ID, Email)
        UserService->>Database: Save to OutboxMessages (UserCreated event)
        Database-->>UserService: Event saved
        UserService-->>MediatR: Success result
        MediatR-->>RegisterEndpoint: Command result
        RegisterEndpoint-->>API: 201 Created
        API-->>Frontend: Response with user ID
        Frontend-->>User: Show success message
    end
    
    %% Login Flow
    rect rgb(240, 220, 200)
        note over User,JWTService: Login Process
        User->>Frontend: Enter credentials (email, password)
        Frontend->>API: POST /users/login
        API->>RegisterEndpoint: Route to LoginUser endpoint
        RegisterEndpoint->>MediatR: Send LoginUserCommand
        MediatR->>UserService: Handle command
        UserService->>Database: Query Users by email
        Database-->>UserService: User record
        UserService->>UserService: Verify hashed password
        UserService->>JWTService: Generate JWT token
        JWTService-->>UserService: Token (userId, role, expiry)
        UserService-->>MediatR: Success result
        MediatR-->>RegisterEndpoint: Command result
        RegisterEndpoint-->>API: 200 OK + JWT token
        API-->>Frontend: Response with token
        Frontend->>Frontend: Store token in localStorage
        Frontend-->>User: Redirect to dashboard
    end
```

### 2. Onboarding - Risk Questionnaire

```mermaid
sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant QuestionnaireEndpoint
    participant MediatR
    participant PortfolioService
    participant Database
    participant MassTransit
    participant NotificationModule
    
    rect rgb(220, 240, 220)
        note over User,NotificationModule: Risk Assessment Onboarding
        User->>Frontend: Start onboarding wizard
        Frontend->>API: GET /questionnaire/check
        API->>QuestionnaireEndpoint: Route to endpoint
        QuestionnaireEndpoint->>MediatR: Send GetQuestionnaireQuery
        MediatR->>PortfolioService: Handle query
        PortfolioService->>Database: Query Questionnare by UserId
        Database-->>PortfolioService: Not found
        PortfolioService-->>MediatR: Query result
        MediatR-->>QuestionnaireEndpoint: No questionnaire
        QuestionnaireEndpoint-->>API: 404 Not Found
        API-->>Frontend: Response
        Frontend-->>User: Display questionnaire form
        
        User->>Frontend: Answer questions (goal, timeline, risk tolerance, etc)
        Frontend->>Frontend: Calculate risk profile based on answers
        Frontend->>API: POST /questionnaire
        API->>QuestionnaireEndpoint: Route to endpoint
        QuestionnaireEndpoint->>MediatR: Send SubmitQuestionnaireCommand
        MediatR->>PortfolioService: Handle command
        
        PortfolioService->>PortfolioService: Validate answers
        PortfolioService->>PortfolioService: Calculate RiskProfile (Conservative/Moderate/Aggressive)
        PortfolioService->>Database: Save to Questionnare table
        Database-->>PortfolioService: Questionnaire saved
        
        PortfolioService->>Database: Save to OutboxMessages (RiskProfileCompleted event)
        Database-->>PortfolioService: Event saved
        
        PortfolioService-->>MediatR: Success result
        MediatR-->>QuestionnaireEndpoint: Command result
        QuestionnaireEndpoint-->>API: 201 Created
        API-->>Frontend: Success + RiskProfile
        Frontend-->>User: Show risk profile result
        
        %% Background notification
        note over Database,NotificationModule: Async Event Processing via MassTransit
        Database->>MassTransit: Outbox processor publishes RiskProfileCompleted event
        MassTransit->>NotificationModule: Deliver event
        NotificationModule->>Database: Save to InboxMessages
        NotificationModule->>Database: Create welcome notification
        Database-->>NotificationModule: Notification created
    end
```

### 3. Onboarding - Create First Portfolio

```mermaid
sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant PortfolioEndpoint
    participant MediatR
    participant PortfolioService
    participant Database
    participant StockAPI
    participant MassTransit
    
    rect rgb(240, 220, 240)
        note over User,StockAPI: Portfolio Creation
        User->>Frontend: Click "Create Portfolio"
        Frontend-->>User: Show portfolio creation form
        
        User->>Frontend: Enter portfolio name & initial balance
        Frontend->>API: POST /portfolios
        API->>PortfolioEndpoint: Route to CreatePortfolio endpoint
        PortfolioEndpoint->>MediatR: Send CreatePortfolioCommand
        MediatR->>PortfolioService: Handle command
        
        PortfolioService->>PortfolioService: Validate portfolio data
        PortfolioService->>Database: BEGIN TRANSACTION
        PortfolioService->>Database: Save to Portfolios table
        Database-->>PortfolioService: Portfolio created (ID)
        
        PortfolioService->>Database: Initialize PortfolioPerformance record
        Database-->>PortfolioService: Performance record created
        
        PortfolioService->>Database: Save to OutboxMessages (PortfolioCreated event)
        Database-->>PortfolioService: Event saved
        PortfolioService->>Database: COMMIT TRANSACTION
        
        PortfolioService-->>MediatR: Success result
        MediatR-->>PortfolioEndpoint: Command result
        PortfolioEndpoint-->>API: 201 Created + Portfolio ID
        API-->>Frontend: Success + Portfolio details
        Frontend-->>User: Redirect to portfolio page
        
        note over Database,MassTransit: Background process
        Database->>MassTransit: Outbox processor publishes PortfolioCreated event
        
        %% Add first stock
        User->>Frontend: Click "Add Stock"
        Frontend->>API: GET /stocks/search?symbol=AAPL
        API->>StockAPI: Proxy request to external API
        StockAPI-->>API: Stock details (name, price, sector)
        API-->>Frontend: Stock information
        Frontend-->>User: Display stock details
        
        User->>Frontend: Enter quantity to buy
        Frontend->>Frontend: Calculate total cost (quantity × price)
        Frontend->>API: POST /portfolios/{id}/holdings
        API->>PortfolioEndpoint: Route to AddHolding endpoint
        PortfolioEndpoint->>MediatR: Send AddHoldingCommand
        MediatR->>PortfolioService: Handle command
        
        PortfolioService->>StockAPI: Fetch current stock price
        StockAPI-->>PortfolioService: Current price
        
        PortfolioService->>PortfolioService: Validate sufficient balance
        PortfolioService->>Database: BEGIN TRANSACTION
        PortfolioService->>Database: Insert into PortfolioHoldings
        Database-->>PortfolioService: Holding created
        
        PortfolioService->>Database: Update Portfolios.CurrentBalance
        Database-->>PortfolioService: Balance updated
        
        PortfolioService->>Database: Calculate and update AssetAllocation
        Database-->>PortfolioService: Allocation updated
        
        PortfolioService->>Database: Save to OutboxMessages (HoldingAdded event)
        Database-->>PortfolioService: Event saved
        PortfolioService->>Database: COMMIT TRANSACTION
        
        PortfolioService-->>MediatR: Success result
        MediatR-->>PortfolioEndpoint: Command result
        PortfolioEndpoint-->>API: 201 Created
        API-->>Frontend: Stock added successfully
        Frontend-->>User: Show updated portfolio with holdings
    end
```

## Flow Summary

### Complete Onboarding Journey

1. **Registration** → User creates account, password is hashed and stored
2. **Login** → User authenticates, receives JWT token
3. **Risk Questionnaire** → User answers questions, system calculates risk profile
4. **Portfolio Creation** → User creates first portfolio with initial balance
5. **Add First Stock** → User searches and adds their first stock holding

### Key Architecture Patterns

- **Modular Monolith** - Single API hosting multiple business modules (Users, Portfolio, Notifications)
- **CQRS with MediatR** - Commands and queries separated using MediatR library
- **Minimal API Endpoints** - Each module exposes endpoints that route to MediatR handlers
- **Event-Driven** - Cross-module communication via MassTransit (RabbitMQ)
- **Outbox Pattern** - Events saved in same transaction as data, published asynchronously
- **Transaction Safety** - Database transactions ensure atomicity

### Key Security Features

- **Password Hashing** - Passwords never stored in plain text
- **JWT Authentication** - Token-based authentication for API requests
- **Input Validation** - All user inputs validated before processing
- **Balance Verification** - System checks sufficient funds before adding stocks
- **Authorization** - Endpoints require authentication via JWT

### Async Event Processing

- Events saved to OutboxMessages in same transaction as data
- Background jobs publish events to MassTransit/RabbitMQ
- Other modules consume events via InboxMessages pattern
- Ensures data consistency and reliable event delivery across modules

