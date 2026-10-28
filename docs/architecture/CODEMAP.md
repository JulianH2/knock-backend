# Mapa de código

> Generado por `scripts/codemap.py` el 2026-10-28T16:47:49Z. No editar a mano.
> **Antes de crear una clase, método o archivo, busca aquí o corre `python scripts/codemap.py --query "<qué necesitas>"`.**

## Resumen

| Módulo | Capas | Archivos | Tipos | Métodos públicos |
|---|---|---|---|---|
| `Assistant` | -, services | 3 | 3 | 1 |
| `Catalog` | -, data, domain, services | 11 | 11 | 23 |
| `Identity` | -, controllers, dtos, services | 11 | 15 | 10 |
| `Knock.Api` | -, common, hosting | 19 | 31 | 40 |
| `Media` | -, controllers, data, domain, dtos, services, validators | 18 | 35 | 48 |
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
  - `AddCatalogModule()`:13
- `src/Knock.Api/Modules/Catalog/ICatalogApi.cs` — interface `ICatalogApi`

**data**

- `src/Knock.Api/Modules/Catalog/Data/CatalogSeeder.cs` — class `CatalogSeeder`
  - `CatalogSeeder()`:13, `SeedAsync()`:19, `SeedAsync()`:29
- `src/Knock.Api/Modules/Catalog/Data/CatalogStore.cs` — class `CatalogStore`
  - `EnsureIndexesAsync()`:60, `FindIntersectingAsync()`:83, `FindNearestCenterAsync()`:87, `FindNamesByPostalCodeAsync()`:90, `ColoniaExistsAsync()`:96, `IntersectingQuery()`:100, `NearestCenterQuery()`:107, `NamesByPostalCodeQuery()`:113, `ColoniaExistsQuery()`:119

**domain**

- `src/Knock.Api/Modules/Catalog/Domain/CatalogCoordinates.cs` — class `CatalogCoordinates`
  - `ToPosition()`:7
- `src/Knock.Api/Modules/Catalog/Domain/Colonia.cs` — record `Colonia`
  - `Colonia()`:7, `IsPostalCode()`:14
- `src/Knock.Api/Modules/Catalog/Domain/GeoPosition.cs` — record struct `GeoPosition`
- `src/Knock.Api/Modules/Catalog/Domain/Oficio.cs` — class `Oficio`
  - `IsValidModalidad()`:10
- `src/Knock.Api/Modules/Catalog/Domain/SearchNameNormalizer.cs` — class `SearchNameNormalizer`
  - `Normalize()`:12

**services**

- `src/Knock.Api/Modules/Catalog/Services/CatalogApi.cs` — class `CatalogApi`
  - `FindNeighborhoodAtAsync()`:11, `FindNeighborhoodsByPostalCodeAsync()`:14, `NeighborhoodExistsAsync()`:25
- `src/Knock.Api/Modules/Catalog/Services/NeighborhoodLocator.cs` — class `NeighborhoodLocator`
  - `NeighborhoodLocator()`:10, `FindAtAsync()`:19

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
  - `Derive()`:18

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
- `src/Knock.Api/Hosting/HealthExtensions.cs` — class `HealthExtensions`
  - `AddKnockHealth()`:10, `MapKnockHealth()`:16
- `src/Knock.Api/Hosting/KnockApplicationExtensions.cs` — class `KnockApplicationExtensions`
  - `AddKnockApi()`:7, `UseKnockApi()`:27
- `src/Knock.Api/Hosting/LoggingExtensions.cs` — class `LoggingExtensions`, class `ScopeRequestPathEnricher`
  - `AddKnockLogging()`:15, `ConfigureRequestLogging()`:29, `Enrich()`:96
- `src/Knock.Api/Hosting/ModulesExtensions.cs` — class `ModulesExtensions`
  - `AddKnockModules()`:15
- `src/Knock.Api/Hosting/MongoExtensions.cs` — class `KnockMongoOptions`, class `KnockMongoOptionsValidator`, class `MongoExtensions`, class `MongoReadinessHealthCheck`
  - `Seconds()`:51, `Validate()`:67, `AddKnockMongo()`:135, `AddKnockMongoDatabase()`:157, `CreateSettings()`:171, `CheckHealthAsync()`:214
- `src/Knock.Api/Hosting/OpenApiExtensions.cs` — class `OpenApiExtensions`
  - `AddKnockOpenApi()`:7, `MapKnockOpenApi()`:14
- `src/Knock.Api/Hosting/ProblemDetailsExtensions.cs` — class `ProblemDetailsExtensions`
  - `AddKnockProblemDetails()`:8
- `src/Knock.Api/Hosting/RateLimitingExtensions.cs` — class `RateLimitPolicySettings`, class `RateLimitSettings`, class `RateLimitingExtensions`
  - `AddKnockRateLimiting()`:40, `ClientKey()`:102
- `src/Knock.Api/Hosting/RequestBodyLimitMiddleware.cs` — class `ProblemResponses`, class `RequestBodyLimitMiddleware`
  - `WriteAsync()`:9, `RequestBodyLimitMiddleware()`:30, `InvokeAsync()`:32
- `src/Knock.Api/Hosting/ValidationExtensions.cs` — class `ValidationExtensions`, class `ValidationFilter`
  - `AddKnockValidation()`:11, `OnActionExecutionAsync()`:27, `ToCamelCasePath()`:63

### Media

**-**

- `src/Knock.Api/Modules/Media/IMediaApi.cs` — interface `IMediaApi`
- `src/Knock.Api/Modules/Media/MediaModuleExtensions.cs` — class `MediaModuleExtensions`
  - `AddMediaModule()`:12

**controllers**

- `src/Knock.Api/Modules/Media/Controllers/V1/FilesController.cs` — class `FilesController`
  - `CreateUpload()`:26, `Confirm()`:47, `Read()`:64

**data**

- `src/Knock.Api/Modules/Media/Data/BlobMediaFileStore.cs` — class `BlobMediaFileStore`, record `MediaFileRecord`
  - `AddAsync()`:25, `FindAsync()`:40, `TryReplaceAsync()`:56, `FromDomain()`:105, `ToDomain()`:109
- `src/Knock.Api/Modules/Media/Data/IMediaFileStore.cs` — record `StoredMediaFile`, interface `IMediaFileStore`
- `src/Knock.Api/Modules/Media/Data/MediaOptions.cs` — class `MediaOptions`

**domain**

- `src/Knock.Api/Modules/Media/Domain/MediaFile.cs` — enum `MediaUsage`, enum `MediaFileState`, enum `ImageFormat`, record `MediaFile`
  - `MediaFile()`:30, `IsReadyFor()`:52, `CreatePending()`:54, `MarkReady()`:58, `MarkRejected()`:74
- `src/Knock.Api/Modules/Media/Domain/MediaRules.cs` — class `MediaRules`
  - `IsAllowedContentType()`:31, `IsAllowedSize()`:34, `AreAllowedDimensions()`:36, `DetectFormat()`:40, `ParseUsage()`:50, `ToContractValue()`:58, `ToContractValue()`:65

**dtos**

- `src/Knock.Api/Modules/Media/Dtos/FileDtos.cs` — record `CreateUploadRequest`, record `ReadLinkQuery`, record `UploadCreatedResponse`, record `ConfirmedFileResponse`, record `ReadLinkResponse`
  - `ReadLinkQuery()`:15

**services**

- `src/Knock.Api/Modules/Media/Services/BlobReadinessHealthCheck.cs` — class `BlobReadinessHealthCheck`
  - `CheckHealthAsync()`:11
- `src/Knock.Api/Modules/Media/Services/IModerationHook.cs` — interface `IModerationHook`, class `NoModerationHook`
  - `ReviewAsync()`:18
- `src/Knock.Api/Modules/Media/Services/ImageProcessor.cs` — record `ProcessedImage`, class `ImageProcessor`
  - `ProcessAsync()`:24, `RunLimitedAsync()`:28, `Dispose()`:42
- `src/Knock.Api/Modules/Media/Services/MediaApi.cs` — class `MediaApi`
  - `IsReadyAndOwnedByAsync()`:7, `CreateReadLinkAsync()`:10
- `src/Knock.Api/Modules/Media/Services/MediaBlobStorage.cs` — record `SasLink`, record `UploadedBlob`, class `MediaBlobStorage`
  - `MediaBlobStorage()`:21, `EnsureContainersAsync()`:28, `CreateUploadLink()`:40, `CreateReadLink()`:43, `GetUploadedBlobAsync()`:47, `DownloadQuarantineAsync()`:65, `DeleteQuarantineAsync()`:86, `UploadFileAsync()`:89
- `src/Knock.Api/Modules/Media/Services/MediaOutcome.cs` — enum `MediaOutcomeStatus`, record `MediaOutcome`, class `MediaRejection`
  - `MediaOutcome()`:21, `Ok()`:23, `NotFound()`:25, `Conflict()`:27, `Unprocessable()`:29
- `src/Knock.Api/Modules/Media/Services/MediaService.cs` — record `UploadTicket`, record `ConfirmedFile`, class `MediaService`
  - `MediaService()`:15, `CreateUploadAsync()`:22, `ConfirmAsync()`:37, `CreateOwnerReadLinkAsync()`:68, `CreateAuthorizedReadLinkAsync()`:77, `IsReadyAndOwnedByAsync()`:83

**validators**

- `src/Knock.Api/Modules/Media/Validators/CreateUploadRequestValidator.cs` — class `CreateUploadRequestValidator`
- `src/Knock.Api/Modules/Media/Validators/ReadLinkQueryValidator.cs` — class `ReadLinkQueryValidator`

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
