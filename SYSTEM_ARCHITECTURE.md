
graph TB
    subgraph "Client Layer"
        Frontend[React Frontend<br/>Vite + React Router]
    end
    
    subgraph "API Layer - Modular Monolith"
        API[ASP.NET Core API<br/>Minimal APIs]
        
        subgraph "Users Module"
            UsersEndpoints[Endpoints]
            UsersLogic[Application Logic<br/>MediatR Handlers]
            UsersDB[(Users DbContext)]
        end
        
        subgraph "Questionnaire Module"
            QuestionnaireEndpoints[Endpoints]
            QuestionnaireLogic[Application Logic<br/>MediatR Handlers]
            QuestionnaireDB[(Questionnaire DbContext)]
        end
        
        subgraph "Notifications Module"
            NotificationEndpoints[Endpoints]
            NotificationLogic[Application Logic<br/>MediatR Handlers]
            NotificationDB[(Notifications DbContext)]
        end
        
        subgraph "Portfolio Module - Future"
            PortfolioEndpoints[Endpoints]
            PortfolioLogic[Application Logic<br/>MediatR Handlers]
            PortfolioDB[(Portfolio DbContext)]
        end
        
        subgraph "Analytics Module - Future"
            AnalyticsEndpoints[Endpoints]
            AnalyticsLogic[Application Logic<br/>MediatR Handlers]
            AnalyticsDB[(Analytics DbContext)]
        end
        
        subgraph "Simulator Module - Future"
            SimulatorEndpoints[Endpoints]
            SimulatorLogic[Application Logic<br/>MediatR Handlers]
            SimulatorDB[(Simulator DbContext)]
        end
        
        API --> UsersEndpoints
        API --> QuestionnaireEndpoints
        API --> NotificationEndpoints
        API -.Future.-> PortfolioEndpoints
        API -.Future.-> AnalyticsEndpoints
        API -.Future.-> SimulatorEndpoints
        
        UsersEndpoints --> UsersLogic
        QuestionnaireEndpoints --> QuestionnaireLogic
        NotificationEndpoints --> NotificationLogic
        PortfolioEndpoints -.-> PortfolioLogic
        AnalyticsEndpoints -.-> AnalyticsLogic
        SimulatorEndpoints -.-> SimulatorLogic
        
        UsersLogic --> UsersDB
        QuestionnaireLogic --> QuestionnaireDB
        NotificationLogic --> NotificationDB
        PortfolioLogic -.-> PortfolioDB
        AnalyticsLogic -.-> AnalyticsDB
        SimulatorLogic -.-> SimulatorDB
    end
    
    subgraph "Data Layer"
        PostgreSQL[(PostgreSQL Database)]
        Redis[(Redis Cache)]
        
        UsersDB -.-> PostgreSQL
        QuestionnaireDB -.-> PostgreSQL
        NotificationDB -.-> PostgreSQL
        PortfolioDB -.-> PostgreSQL
        AnalyticsDB -.-> PostgreSQL
        SimulatorDB -.-> PostgreSQL
    end
    
    subgraph "Message Queue"
        QuestionnaireLogic -.Outbox Pattern.-> RabbitMQ
        PortfolioLogic -.Outbox Pattern.-> RabbitMQ
        AnalyticsLogic -.Outbox Pattern.-> RabbitMQ
        SimulatorLogic -.Outbox Pattern.-> RabbitMQ
        RabbitMQ -.Inbox Pattern.-> NotificationLogic
        RabbitMQ -.Inbox Pattern.-> Analytics
        UsersLogic -.Outbox Pattern.-> RabbitMQ
        PortfolioLogic -.Outbox Pattern.-> RabbitMQ
        RabbitMQ -.Inbox Pattern.-> NotificationLogic
    end
    
    subgraph "External Services"
        StockAPI[Third-Party Stock API<br/>Market Data Provider]
    end
    
    Frontend <-->|HTTP/HTTPS<br/>JWT Auth| API
    API <-->|REST API| StockAPI
    API <--> Redis
    
    style Frontend fill:#e1f5ff
    style API fill:#fff4e1
    style PostgreSQL fill:#e8f5e9
    style Redis fill:#ffebee
    style RabbitMQ fill:#f3e5f5
    style StockAPI fill:#fff9c4
