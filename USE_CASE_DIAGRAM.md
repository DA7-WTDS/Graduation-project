# Use Case Diagram

A UML Use Case Diagram mapping the actors, system boundary, and implemented use cases of the **QuantWise** platform, adhering strictly to UML 2.5 standards.

## Diagram

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

## Description of Actors & Use Cases

### Actors
* **Retail User (Primary Actor)**: Individual investor utilizing the platform to onboarding, construct portfolios, and obtain daily risk-tailored buy/sell advisory.
* **Administrator (Primary Actor)**: Backend manager handling user accounts, system configuration thresholds, and reviewing operational logs.
* **FastAPI Pipeline (System Actor)**: Background ML processing system that executes price time-series scoring, sentiment NLP, and risk grading, pushing daily results to the API.
* **Google Gemini API (System Actor)**: External generative language model serving as a constrained synthesizer to personalize output text signals based on user-specific portfolio criteria.

### 1. Authentication & Users
* **Register Account**: Initiates a new user profile, hashes passwords via BCrypt, and schedules a welcome integration event.
* **Log In**: Authenticates credentials and issues a secure JWT Bearer token.
* **View Profile**: Queries user details via active token context.

### 2. Portfolio Management
* **Create Portfolio**: Onboards user through a risk questionnaire to compute and set up initial asset target allocation percentages.
* **View Portfolio & Allocation**: Reads asset target mix percentages and total investment values.
* **Update Allocation & Risk Settings**: Modifies risk tolerance profiles and target asset class allocation settings.

### 3. AI Recommendations
* **View Daily AI Recommendations**: Reads risk-tailored recommendation picks. **Includes** `Personalize Recommendations (LLM)` on a cache miss.
* **Personalize Recommendations (LLM)**: Integrates current market predictions with the user's specific risk settings via Gemini. **Uses** `Google Gemini API` as a supporting system service.
* **View Raw Predictions (Simulator)**: Exposes the full market-wide daily pipeline outputs to allow users to run simulations in the learning terminal.
* **Ingest Daily ML Scoring Run**: Receives incoming batch runs from the Python pipeline, persists database entities, and triggers domain fanning out.

### 4. Notifications
* **View Notifications**: Fetches user-specific inbox alerts.
* **Mark Notification as Read / Mark All Read**: Updates target notification status properties.
* **Trigger Test Notification**: Internal testing helper to verify transactional notification flows.

### 5. Admin Management
* **Manage Users**: Review, disable, or modify user profile scopes.
* **View Audit Logs**: Inspect system activity and database logs.
* **Configure System Parameters**: Fine-tune global threshold variables.