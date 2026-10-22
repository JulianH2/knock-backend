# Mapa de código

> Generado por `scripts/codemap.py` el 2026-10-22T05:27:53Z. No editar a mano.
> **Antes de crear una clase, método o archivo, busca aquí o corre `python scripts/codemap.py --query "<qué necesitas>"`.**

## Resumen

| Módulo | Capas | Archivos | Tipos | Métodos públicos |
|---|---|---|---|---|
| `Assistant` | -, services | 3 | 3 | 1 |
| `Catalog` | -, data, domain, services | 10 | 10 | 11 |
| `Identity` | -, controllers, dtos, services | 11 | 15 | 10 |
| `Knock.Api` | -, common, hosting | 18 | 28 | 35 |
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
  - `AddCatalogModule()`:9
- `src/Knock.Api/Modules/Catalog/ICatalogApi.cs` — interface `ICatalogApi`

**data**

- `src/Knock.Api/Modules/Catalog/Data/CatalogDbContext.cs` — class `CatalogDbContext`
  - `Configure()`:19, `OnModelCreating()`:24
- `src/Knock.Api/Modules/Catalog/Data/CatalogDbContextFactory.cs` — class `CatalogDbContextFactory`
  - `CreateDbContext()`:9
- `src/Knock.Api/Modules/Catalog/Data/CatalogSeeder.cs` — class `CatalogSeeder`
  - `CatalogSeeder()`:10, `SeedAsync()`:12
- `src/Knock.Api/Modules/Catalog/Data/LocalMigrationConnectionGuard.cs` — class `LocalMigrationConnectionGuard`
  - `EnsureLocalHost()`:7

**domain**

- `src/Knock.Api/Modules/Catalog/Domain/CatalogCoordinates.cs` — class `CatalogCoordinates`
  - `ToPoint()`:11
- `src/Knock.Api/Modules/Catalog/Domain/Colonia.cs` — class `Colonia`
  - `Create()`:26
- `src/Knock.Api/Modules/Catalog/Domain/Oficio.cs` — class `Oficio`

**services**

- `src/Knock.Api/Modules/Catalog/Services/CatalogApi.cs` — class `CatalogApi`
  - `FindNeighborhoodAtAsync()`:17, `FindNeighborhoodsByPostalCodeAsync()`:36

### Identity

**-**

- `src/Knock.Api/Modules/Identity/IIdentityApi.cs` — interface `IIdentityApi`
- `src/Knock.Api/Modules/Identity/IdentityModuleExtensions.cs` — class `IdentityModuleExtensions`
  - `AddIdentityModule()`:9

**controllers**

- `src/Knock.Api/Modules/Identity/Controllers/V1/AuthController.cs` — class `AuthController`
  - `ExchangeFirebaseToken()`:24
- `src/Knock.Api/Modules/Identity/Controllers/V1/MeController.cs` — class `MeController`
  - `Get()`:18

**dtos**

- `src/Knock.Api/Modules/Identity/Dtos/SessionDtos.cs` — record `FirebaseExchangeRequest`, record `SessionResponse`, record `MeResponse`

**services**

- `src/Knock.Api/Modules/Identity/Services/FirebaseIdTokenVerifier.cs` — class `FirebaseIdTokenVerifier`
  - `VerifyAsync()`:16, `Dispose()`:36
- `src/Knock.Api/Modules/Identity/Services/FirebaseSessionService.cs` — class `FirebaseSessionService`
  - `FirebaseSessionService()`:4, `ExchangeAsync()`:10
- `src/Knock.Api/Modules/Identity/Services/IFirebaseIdTokenVerifier.cs` — record `FirebaseIdentity`, interface `IFirebaseIdTokenVerifier`
- `src/Knock.Api/Modules/Identity/Services/IdentityApi.cs` — class `IdentityApi`
- `src/Knock.Api/Modules/Identity/Services/SessionTokenIssuer.cs` — record `IssuedSession`, class `SessionTokenIssuer`
  - `SessionTokenIssuer()`:12, `Issue()`:18
- `src/Knock.Api/Modules/Identity/Services/UserIdDerivation.cs` — class `UserIdDerivation`
  - `Derive()`:16

### Knock.Api

**-**

- `src/Knock.Api/Program.cs` — —

**common**

- `src/Knock.Api/Common/Errors/DomainException.cs` — enum `DomainErrorKind`, class `DomainException`, class `DomainErrors`
  - `ToStatusCode()`:46
- `src/Knock.Api/Common/Security/JwtSettings.cs` — record `JwtSettings`
- `src/Knock.Api/Common/Security/JwtSettingsFactory.cs` — class `JwtSettingsFactory`
  - `Create()`:14

**hosting**

- `src/Knock.Api/Hosting/ApiVersioningExtensions.cs` — class `ApiVersioningExtensions`
  - `AddKnockApiVersioning()`:7
- `src/Knock.Api/Hosting/AuthenticationExtensions.cs` — class `AuthenticationExtensions`, class `InternalControllerFeatureProvider`, class `AuthenticationStartupCheck`
  - `AddKnockAuthentication()`:14, `AddInternalControllers()`:55, `IsController()`:73, `StartAsync()`:90, `StopAsync()`:108
- `src/Knock.Api/Hosting/CorrelationIdMiddleware.cs` — class `CorrelationId`, class `CorrelationIdMiddleware`
  - `IsValid()`:13, `Create()`:32, `Current()`:35, `CorrelationIdMiddleware()`:46, `InvokeAsync()`:48
- `src/Knock.Api/Hosting/DomainExceptionHandler.cs` — class `DomainExceptionHandler`
  - `TryHandleAsync()`:14
- `src/Knock.Api/Hosting/ForwardedHeadersExtensions.cs` — class `ForwardedHeadersExtensions`
  - `AddKnockForwardedHeaders()`:15
- `src/Knock.Api/Hosting/HealthExtensions.cs` — class `HealthExtensions`, class `PostgresReadinessHealthCheck`
  - `AddKnockHealth()`:12, `MapKnockHealth()`:19, `CheckHealthAsync()`:29
- `src/Knock.Api/Hosting/KnockApplicationExtensions.cs` — class `KnockApplicationExtensions`
  - `AddKnockApi()`:7, `UseKnockApi()`:26
- `src/Knock.Api/Hosting/LoggingExtensions.cs` — class `LoggingExtensions`, class `ScopeRequestPathEnricher`
  - `AddKnockLogging()`:15, `ConfigureRequestLogging()`:29, `Enrich()`:96
- `src/Knock.Api/Hosting/ModulesExtensions.cs` — class `ModulesExtensions`
  - `AddKnockModules()`:14
- `src/Knock.Api/Hosting/OpenApiExtensions.cs` — class `OpenApiExtensions`
  - `AddKnockOpenApi()`:7, `MapKnockOpenApi()`:14
- `src/Knock.Api/Hosting/ProblemDetailsExtensions.cs` — class `ProblemDetailsExtensions`
  - `AddKnockProblemDetails()`:8
- `src/Knock.Api/Hosting/RateLimitingExtensions.cs` — class `RateLimitPolicySettings`, class `RateLimitSettings`, class `RateLimitingExtensions`
  - `AddKnockRateLimiting()`:40, `ClientKey()`:99
- `src/Knock.Api/Hosting/RequestBodyLimitMiddleware.cs` — class `ProblemResponses`, class `RequestBodyLimitMiddleware`
  - `WriteAsync()`:9, `RequestBodyLimitMiddleware()`:30, `InvokeAsync()`:32
- `src/Knock.Api/Hosting/ValidationExtensions.cs` — class `ValidationExtensions`, class `ValidationFilter`
  - `AddKnockValidation()`:11, `OnActionExecutionAsync()`:27, `ToCamelCasePath()`:63

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
