erDiagram
    %% ========================================
    %% CURRENT TABLES (Existing Implementation)
    %% ========================================
    
    Users {
        uuid Id PK
        string FirstName
        string LastName
        string Email UK
        string HashedPassword
        enum Role "User, Admin"
        datetime CreatedAt
    }
    
    Questionnare {
        uuid Id PK
        uuid UserId FK
        string PrimaryGoal
        string TimeHorizon
        int RiskTolerance
        string MarketReaction
        string InvestmentExperience
        enum RiskProfile "Conservative, Moderate, Aggressive"
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    Notifications {
        uuid Id PK
        uuid UserId FK
        string Title
        string Message
        enum Type "Info, Warning, Success"
        boolean IsRead
        datetime CreatedAt
    }
    
    OutboxMessages {
        uuid Id PK
        string Type
        string Content
        datetime OccurredOnUtc
        datetime ProcessedOnUtc
        string Error
    }
    
    OutboxMessageConsumers {
        uuid OutboxMessageId PK,FK
        string Name PK
    }
    
    InboxMessages {
        uuid Id PK
        string Type
        string Content
        datetime OccurredOnUtc
        datetime ProcessedOnUtc
        string Error
    }
    
    InboxMessageConsumers {
        uuid InboxMessageId PK,FK
        string Name PK
    }
    
    Portfolios {
        uuid Id PK
        uuid UserId FK
        string Name
        decimal InitialBalance
        decimal CurrentBalance
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    %% ========================================
    %% FUTURE TABLES - Portfolio Analytics Service
    %% ========================================
    
    PortfolioHoldings {
        uuid Id PK
        uuid PortfolioId FK
        string StockSymbol
        string StockName
        decimal Quantity
        decimal AverageBuyPrice
        decimal CurrentPrice
        decimal TotalValue
        decimal GainLoss
        decimal GainLossPercent
        datetime LastUpdated
        datetime CreatedAt
    }
    
    PortfolioPerformance {
        uuid Id PK
        uuid PortfolioId FK
        decimal TotalValue
        decimal TotalInvested
        decimal TotalGainLoss
        decimal TotalReturn
        decimal DayReturn
        decimal WeekReturn
        decimal MonthReturn
        decimal YearReturn
        decimal AllTimeReturn
        datetime CalculatedAt
    }
    
    AssetAllocation {
        uuid Id PK
        uuid PortfolioId FK
        string Sector
        decimal Percentage
        decimal Value
        datetime CalculatedAt
    }
    
    %% ========================================
    %% FUTURE TABLES - Additional Services
    %% ========================================
    
    Watchlists {
        uuid Id PK
        uuid UserId FK
        string Name
        string Description
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    WatchlistItems {
        uuid Id PK
        uuid WatchlistId FK
        string StockSymbol
        string StockName
        decimal TargetPrice
        string Notes
        datetime AddedAt
    }
    
    UserAlerts {
        uuid Id PK
        uuid UserId FK
        string StockSymbol
        enum AlertType "PriceAbove, PriceBelow, PercentChange, VolumeSpike"
        decimal ThresholdValue
        boolean IsActive
        boolean IsTriggered
        datetime TriggeredAt
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    UserSettings {
        uuid Id PK
        uuid UserId FK
        boolean EmailNotifications
        boolean PushNotifications
        boolean TwoFactorEnabled
        string PreferredCurrency
        string PreferredLanguage
        string TimeZone
        jsonb DisplayPreferences
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    AuditLogs {
        uuid Id PK
        uuid UserId FK
        string EntityType
        uuid EntityId
        enum ActionType "Create, Update, Delete, View"
        jsonb OldValues
        jsonb NewValues
        string IpAddress
        string UserAgent
        datetime CreatedAt
    }
    
    %% ========================================
    %% RELATIONSHIPS - Current Tables
    %% ========================================
    
    Users ||--o{ Questionnare : "has"
    Users ||--o{ Notifications : "receives"
    Users ||--o| UserSettings : "configures"
    Users ||--o{ Watchlists : "creates"
    Users ||--o{ UserAlerts : "sets"
    Users ||--o{ AuditLogs : "generates"
    Users ||--o{ Portfolios : "owns"
    
    OutboxMessages ||--o{ OutboxMessageConsumers : "processed_by"
    InboxMessages ||--o{ InboxMessageConsumers : "processed_by"
    
    Portfolios ||--o{ PortfolioHoldings : "contains"
    Portfolios ||--o| PortfolioPerformance : "tracks"
    Portfolios ||--o{ AssetAllocation : "has"
    
    Watchlists ||--o{ WatchlistItems : "contains"