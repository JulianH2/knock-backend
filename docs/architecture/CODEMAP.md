# Mapa de código

> Generado por `scripts/codemap.py` el 2026-10-15T22:28:32Z. No editar a mano.
> **Antes de crear una clase, método o archivo, busca aquí o corre `python scripts/codemap.py --query "<qué necesitas>"`.**

## Resumen

| Módulo | Capas | Archivos | Tipos | Métodos públicos |
|---|---|---|---|---|
| `Assistant` | -, services | 3 | 3 | 1 |
| `Catalog` | -, services | 3 | 3 | 1 |
| `Identity` | -, services | 3 | 3 | 1 |
| `Knock.Api` | -, hosting | 8 | 7 | 10 |
| `Messaging` | -, services | 3 | 3 | 1 |
| `Notifications` | -, services | 3 | 3 | 1 |
| `Reputation` | -, services | 3 | 3 | 1 |
| `Requests` | -, services | 3 | 3 | 1 |
| `Trust` | -, services | 3 | 3 | 1 |

## Índice por módulo

### Assistant

**-**

- `src/Knock.Api/Modules/Assistant/AssistantModuleExtensions.cs` — class `AssistantModuleExtensions`
  - `AddAssistantModule()`:8
- `src/Knock.Api/Modules/Assistant/IAssistantApi.cs` — interface `IAssistantApi`

**services**

- `src/Knock.Api/Modules/Assistant/Services/AssistantApi.cs` — class `AssistantApi`

### Catalog

**-**

- `src/Knock.Api/Modules/Catalog/CatalogModuleExtensions.cs` — class `CatalogModuleExtensions`
  - `AddCatalogModule()`:8
- `src/Knock.Api/Modules/Catalog/ICatalogApi.cs` — interface `ICatalogApi`

**services**

- `src/Knock.Api/Modules/Catalog/Services/CatalogApi.cs` — class `CatalogApi`

### Identity

**-**

- `src/Knock.Api/Modules/Identity/IIdentityApi.cs` — interface `IIdentityApi`
- `src/Knock.Api/Modules/Identity/IdentityModuleExtensions.cs` — class `IdentityModuleExtensions`
  - `AddIdentityModule()`:8

**services**

- `src/Knock.Api/Modules/Identity/Services/IdentityApi.cs` — class `IdentityApi`

### Knock.Api

**-**

- `src/Knock.Api/Program.cs` — —

**hosting**

- `src/Knock.Api/Hosting/ApiVersioningExtensions.cs` — class `ApiVersioningExtensions`
  - `AddKnockApiVersioning()`:7
- `src/Knock.Api/Hosting/HealthExtensions.cs` — class `HealthExtensions`
  - `AddKnockHealth()`:10, `MapKnockHealth()`:16
- `src/Knock.Api/Hosting/KnockApplicationExtensions.cs` — class `KnockApplicationExtensions`
  - `AddKnockApi()`:7, `UseKnockApi()`:21
- `src/Knock.Api/Hosting/LoggingExtensions.cs` — class `LoggingExtensions`
  - `AddKnockLogging()`:9
- `src/Knock.Api/Hosting/ModulesExtensions.cs` — class `ModulesExtensions`
  - `AddKnockModules()`:14
- `src/Knock.Api/Hosting/OpenApiExtensions.cs` — class `OpenApiExtensions`
  - `AddKnockOpenApi()`:7, `MapKnockOpenApi()`:14
- `src/Knock.Api/Hosting/ProblemDetailsExtensions.cs` — class `ProblemDetailsExtensions`
  - `AddKnockProblemDetails()`:8

### Messaging

**-**

- `src/Knock.Api/Modules/Messaging/IMessagingApi.cs` — interface `IMessagingApi`
- `src/Knock.Api/Modules/Messaging/MessagingModuleExtensions.cs` — class `MessagingModuleExtensions`
  - `AddMessagingModule()`:8

**services**

- `src/Knock.Api/Modules/Messaging/Services/MessagingApi.cs` — class `MessagingApi`

### Notifications

**-**

- `src/Knock.Api/Modules/Notifications/INotificationsApi.cs` — interface `INotificationsApi`
- `src/Knock.Api/Modules/Notifications/NotificationsModuleExtensions.cs` — class `NotificationsModuleExtensions`
  - `AddNotificationsModule()`:8

**services**

- `src/Knock.Api/Modules/Notifications/Services/NotificationsApi.cs` — class `NotificationsApi`

### Reputation

**-**

- `src/Knock.Api/Modules/Reputation/IReputationApi.cs` — interface `IReputationApi`
- `src/Knock.Api/Modules/Reputation/ReputationModuleExtensions.cs` — class `ReputationModuleExtensions`
  - `AddReputationModule()`:8

**services**

- `src/Knock.Api/Modules/Reputation/Services/ReputationApi.cs` — class `ReputationApi`

### Requests

**-**

- `src/Knock.Api/Modules/Requests/IRequestsApi.cs` — interface `IRequestsApi`
- `src/Knock.Api/Modules/Requests/RequestsModuleExtensions.cs` — class `RequestsModuleExtensions`
  - `AddRequestsModule()`:8

**services**

- `src/Knock.Api/Modules/Requests/Services/RequestsApi.cs` — class `RequestsApi`

### Trust

**-**

- `src/Knock.Api/Modules/Trust/ITrustApi.cs` — interface `ITrustApi`
- `src/Knock.Api/Modules/Trust/TrustModuleExtensions.cs` — class `TrustModuleExtensions`
  - `AddTrustModule()`:8

**services**

- `src/Knock.Api/Modules/Trust/Services/TrustApi.cs` — class `TrustApi`
