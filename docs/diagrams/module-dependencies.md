# Module dependencies (C4 level 3)

```mermaid
flowchart TB
    Api["Parley.Api<br/>controllers, composition root"]

    subgraph identity["Identity"]
        Identity["Identity<br/>accounts, sessions, roles, scope, audit"]
        IdentityC["Identity.Contracts"]
    end
    subgraph crm["Crm"]
        Crm["Crm<br/>contacts, companies, deals, tasks, orders"]
        CrmC["Crm.Contracts"]
    end
    subgraph campaigns["Campaigns"]
        Campaigns["Campaigns<br/>templates, segments, freeze, send, lift"]
        CampaignsC["Campaigns.Contracts"]
    end
    subgraph insights["Insights"]
        Insights["Insights<br/>RFM, priority, scoring, ML settings"]
        InsightsC["Insights.Contracts"]
    end
    subgraph notifications["Notifications"]
        Notifications["Notifications<br/>email sender, in-app notifications, Telegram"]
        NotificationsC["Notifications.Contracts"]
    end
    Kernel["Parley.SharedKernel<br/>Result, tenancy, outbox, idempotency,<br/>jobs, localization, telemetry"]

    Api --> Identity
    Api --> Crm
    Api --> Campaigns
    Api --> Insights
    Api --> Notifications
    Api --> IdentityC
    Api --> CrmC
    Api --> CampaignsC
    Api --> InsightsC
    Api --> NotificationsC

    Identity --> IdentityC
    Identity --> NotificationsC
    Crm --> CrmC
    Crm --> CampaignsC
    Crm --> IdentityC
    Crm --> InsightsC
    Campaigns --> CampaignsC
    Campaigns --> CrmC
    Campaigns --> IdentityC
    Campaigns --> InsightsC
    Insights --> InsightsC
    Insights --> CampaignsC
    Insights --> CrmC
    Insights --> IdentityC
    Notifications --> NotificationsC
    Notifications --> CampaignsC
    Notifications --> CrmC
    Notifications --> IdentityC
    Notifications --> InsightsC

    Identity --> Kernel
    Crm --> Kernel
    Campaigns --> Kernel
    Insights --> Kernel
    Notifications --> Kernel
```

Reading guide: the diagram is drawn from the project references, not from intent. A module implementation may reference its own `Contracts` project and other modules' `Contracts` projects only; it never references another module's implementation, and it never reads another module's schema. `Parley.Api` is the only project that references every implementation, because it is the composition root and host. `Parley.SharedKernel` is referenced by every project, including the contract projects (for `Result`, errors, tenancy abstractions and the outbox base types). The `Contracts` nodes are the published surface: interfaces, DTOs and integration events. The arrows that carry events at runtime are not code references, so they are listed here rather than drawn:

| Producer | Event | Consumer |
|---|---|---|
| Identity | `OrganizationCreated` | Crm (default pipeline), Insights (daily-run row) |
| Identity | `MemberActivated`, `MemberDeactivated` | Notifications (in-app notification) |
| SharedKernel audit outbox | `AuditRecorded` | Identity (audit viewer row) |
| Crm | `TaskAssigned`, `TaskDueSoon`, `DealAssigned` | Notifications |
| Campaigns | `CampaignEmailSent`, `RecipientUnsubscribed` | Crm (timeline activity, suppression) |
| Campaigns | `CampaignCompleted` | Notifications |
| Insights | `ScoringRunCompleted` | Notifications, Insights (priority rebuild) |
| Insights | `MlSettingsChanged` | Insights (priority rebuild on ML on/off) |

Events without a registered consumer in this codebase (`CampaignStarted`, `ContactCreated`, `ContactDeleted`, `ContactRestored`, `DealClosed`, `DealStageChanged`, `OrderRecorded`, `TelegramLinked`, `TelegramUnlinked`) are written to the outbox but currently have no subscriber; they are not shown as edges to avoid claiming a delivery that does not exist. The architecture tests keep the reference graph closed, so a forbidden module-to-module implementation reference fails the build.

Verified against: every `backend/src/Modules/*/*/*.csproj` and `backend/src/Modules/*/*.Contracts/*.csproj`, the `AddIntegrationEventConsumer` registrations in each `Module.cs`, and the event types under `backend/src/Modules/*/*.Contracts/Events/`.
