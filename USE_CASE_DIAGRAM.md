---
config:
  layout: dagre
  look: handDrawn
---
flowchart LR
    User(["User"])
    Admin(["Admin"])
    StockAPI(["Stock API"])
    
    subgraph System["Stock Portfolio Investment Platform"]
        direction TB
        
        subgraph Auth["Authentication & User Management"]
            UC1["Register Account"]
            UC2["Login"]
            UC3["Update Profile"]
            UC4["Manage User Settings"]
        end
        
        subgraph Risk["Risk Assessment"]
            UC5["Complete Risk Questionnare"]
            UC6["View Risk Profile"]
        end
        
        subgraph Portfolio["Portfolio Management"]
            UC7["Create Portfolio"]
            UC8["View Portfolio"]
            UC9["Update Portfolio"]
            UC10["View Portfolio Performance"]
        end
        
        subgraph Stock["Stock Research"]
            UC13["Search Stock"]
            UC14["View Stock Details"]
        end
        
        subgraph Watch["Watchlist Management"]
            UC15["Create Watchlist"]
            UC16["Update Watchlist"]
        end
        
        subgraph Notify["Alerts & Notifications"]
            UC18["Set Alerts"]
            UC19["View Notifications"]
            UC20["Mark Notifications as Read"]
        end
        
        subgraph Learning["Learning & Simulation"]
            UC21["Run Simulation"]
        end
        
        subgraph AdminFunctions["Admin Functions"]
            UC23["Manage Users"]
            UC24["View Audit Logs"]
            UC25["Manage System Settings"]
        end
    end
    
    User --> UC1
    User --> UC2
    User --> UC3
    User --> UC4
    User --> UC5
    User --> UC6
    User --> UC7
    User --> UC8
    User --> UC9
    User --> UC10
    User --> UC13
    User --> UC14
    User --> UC15
    User --> UC16
    User --> UC18
    User --> UC19
    User --> UC20
    User --> UC21
    
    Admin --> UC23
    Admin --> UC24
    Admin --> UC25
    
    UC9 -.-> StockAPI
    UC10 -.-> StockAPI
    UC13 -.-> StockAPI
    UC14 -.-> StockAPI
    UC21 -.-> StockAPI