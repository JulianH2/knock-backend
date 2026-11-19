# Mapa de código

> Generado por `scripts/codemap.py` el 2026-11-19T16:21:42Z. No editar a mano.
> **Antes de crear una clase, método o archivo, busca aquí o corre `python scripts/codemap.py --query "<qué necesitas>"`.**

## Resumen

| Módulo | Capas | Archivos | Tipos | Métodos públicos |
|---|---|---|---|---|
| `Assistant` | data | 2 | 7 | 5 |
| `Catalog` | -, controllers, data, domain, dtos, services, validators | 49 | 87 | 113 |
| `Chat` | -, data, hubs, services | 6 | 18 | 26 |
| `Conversaciones` | domain | 1 | 2 | 1 |
| `Knock.Contracts` | - | 9 | 16 | 7 |
| `Knock.Gateway` | -, hosting | 15 | 40 | 51 |
| `Knock.Identity.Api` | -, common, hosting | 29 | 61 | 85 |
| `Knock.Market.Api` | -, common, hosting | 30 | 63 | 89 |
| `Knock.Messaging.Api` | -, hosting | 15 | 38 | 48 |
| `Media` | -, data, domain, dtos, services, validators | 17 | 29 | 53 |
| `Requests` | -, controllers, data, domain, dtos, services, validators | 57 | 126 | 118 |
| `Safety` | -, controllers, data, domain, dtos, services, validators | 15 | 38 | 33 |
| `Sesiones` | -, controllers, data, domain, dtos, services, validators | 25 | 46 | 53 |
| `Tecnicos` | -, controllers, data, domain, dtos, services, validators | 22 | 55 | 57 |
| `Usuarios` | -, controllers, data, domain, dtos, services | 15 | 28 | 35 |

## ⚠️ Código duplicado (misma estructura)

- `src/Knock.Gateway/Hosting/CorsExtensions.cs:36 CorsExtensions.AddKnockGatewayForwardedHeaders` · `src/Knock.Identity.Api/Hosting/ForwardedHeadersExtensions.cs:16 ForwardedHeadersExtensions.AddKnockForwardedHeaders` · `src/Knock.Market.Api/Hosting/ForwardedHeadersExtensions.cs:15 ForwardedHeadersExtensions.AddKnockForwardedHeaders` · `src/Knock.Messaging.Api/Hosting/ForwardedHeadersExtensions.cs:16 ForwardedHeadersExtensions.AddKnockForwardedHeaders`
- `src/Knock.Gateway/Hosting/LoggingExtensions.cs:20 LoggingExtensions.AddKnockLogging` · `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:20 LoggingExtensions.AddKnockLogging` · `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:19 LoggingExtensions.AddKnockLogging` · `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:20 LoggingExtensions.AddKnockLogging`
- `src/Knock.Gateway/Hosting/LoggingExtensions.cs:36 LoggingExtensions.ConfigureRequestLogging` · `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:36 LoggingExtensions.ConfigureRequestLogging` · `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:35 LoggingExtensions.ConfigureRequestLogging` · `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:36 LoggingExtensions.ConfigureRequestLogging`
- `src/Knock.Gateway/Hosting/LoggingExtensions.cs:67 LoggingExtensions.IsQueryBearingBelowWarning` · `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:67 LoggingExtensions.IsQueryBearingBelowWarning` · `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:66 LoggingExtensions.IsQueryBearingBelowWarning` · `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:67 LoggingExtensions.IsQueryBearingBelowWarning`
- `src/Knock.Gateway/Hosting/LoggingExtensions.cs:80 LoggingExtensions.RoutePath` · `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:80 LoggingExtensions.RoutePath` · `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:79 LoggingExtensions.RoutePath` · `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:80 LoggingExtensions.RoutePath`
- `src/Knock.Gateway/Hosting/LoggingExtensions.cs:112 LoggingExtensions.OriginalRoute` · `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:112 LoggingExtensions.OriginalRoute` · `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:111 LoggingExtensions.OriginalRoute` · `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:112 LoggingExtensions.OriginalRoute`
- `src/Knock.Gateway/Hosting/LoggingExtensions.cs:123 LoggingExtensions.IsVersion` · `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:123 LoggingExtensions.IsVersion` · `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:122 LoggingExtensions.IsVersion` · `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:123 LoggingExtensions.IsVersion`
- `src/Knock.Gateway/Hosting/LoggingExtensions.cs:145 ScopeRequestPathEnricher.Enrich` · `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:145 ScopeRequestPathEnricher.Enrich` · `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:144 ScopeRequestPathEnricher.Enrich` · `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:145 ScopeRequestPathEnricher.Enrich`
- `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:71 ObservabilityExtensions.AddKnockObservability` · `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:71 ObservabilityExtensions.AddKnockObservability` · `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:67 ObservabilityExtensions.AddKnockObservability`
- `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:154 ObservabilityExtensions.ConfigureServerSpans` · `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:154 ObservabilityExtensions.ConfigureServerSpans` · `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:154 ObservabilityExtensions.ConfigureServerSpans`
- `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:177 TelemetryExceptionRecorder.TryHandleAsync` · `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:177 TelemetryExceptionRecorder.TryHandleAsync` · `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:173 TelemetryExceptionRecorder.TryHandleAsync` · `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:177 TelemetryExceptionRecorder.TryHandleAsync`
- `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:224 OpenTelemetryLogSink.Emit` · `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:224 OpenTelemetryLogSink.Emit` · `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:220 OpenTelemetryLogSink.Emit` · `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:224 OpenTelemetryLogSink.Emit`
- `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:243 OpenTelemetryLogSink.Level` · `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:243 OpenTelemetryLogSink.Level` · `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:239 OpenTelemetryLogSink.Level` · `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:243 OpenTelemetryLogSink.Level`
- `src/Knock.Gateway/Hosting/RedisExtensions.cs:41 KnockRedisOptionsValidator.Validate` · `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:41 KnockRedisOptionsValidator.Validate` · `src/Knock.Market.Api/Hosting/RedisExtensions.cs:39 KnockRedisOptionsValidator.Validate` · `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:40 KnockRedisOptionsValidator.Validate`
- `src/Knock.Gateway/Hosting/RedisExtensions.cs:105 RedisExtensions.AddKnockRedis` · `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:105 RedisExtensions.AddKnockRedis` · `src/Knock.Market.Api/Hosting/RedisExtensions.cs:103 RedisExtensions.AddKnockRedis` · `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:104 RedisExtensions.AddKnockRedis`
- `src/Knock.Gateway/Hosting/RedisExtensions.cs:123 RedisExtensions.CreateConfiguration` · `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:123 RedisExtensions.CreateConfiguration` · `src/Knock.Market.Api/Hosting/RedisExtensions.cs:121 RedisExtensions.CreateConfiguration` · `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:122 RedisExtensions.CreateConfiguration`
- `src/Knock.Gateway/Hosting/RedisExtensions.cs:139 RedisExtensions.Bind` · `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:139 RedisExtensions.Bind` · `src/Knock.Market.Api/Hosting/RedisExtensions.cs:137 RedisExtensions.Bind` · `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:138 RedisExtensions.Bind`
- `src/Knock.Gateway/Hosting/RedisExtensions.cs:162 RedisReadinessHealthCheck.CheckHealthAsync` · `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:162 RedisReadinessHealthCheck.CheckHealthAsync` · `src/Knock.Market.Api/Hosting/RedisExtensions.cs:160 RedisReadinessHealthCheck.CheckHealthAsync` · `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:161 RedisReadinessHealthCheck.CheckHealthAsync`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:44 TelemetryRedactor.Redact` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:44 TelemetryRedactor.Redact` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:43 TelemetryRedactor.Redact` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:44 TelemetryRedactor.Redact`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:76 TelemetryRedactor.RedactAttribute` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:76 TelemetryRedactor.RedactAttribute` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:75 TelemetryRedactor.RedactAttribute` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:76 TelemetryRedactor.RedactAttribute`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:100 TelemetryRedactor.RedactUrl` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:100 TelemetryRedactor.RedactUrl` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:99 TelemetryRedactor.RedactUrl` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:100 TelemetryRedactor.RedactUrl`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:122 TelemetryRedactor.ExceptionEvent` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:122 TelemetryRedactor.ExceptionEvent` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:121 TelemetryRedactor.ExceptionEvent` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:122 TelemetryRedactor.ExceptionEvent`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:180 CoordinatePairs.Redact` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:180 CoordinatePairs.Redact` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:179 CoordinatePairs.Redact` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:180 CoordinatePairs.Redact`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:205 CoordinatePairs.TryFirstNumber` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:205 CoordinatePairs.TryFirstNumber` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:204 CoordinatePairs.TryFirstNumber` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:205 CoordinatePairs.TryFirstNumber`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:230 CoordinatePairs.TrySecondNumber` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:230 CoordinatePairs.TrySecondNumber` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:229 CoordinatePairs.TrySecondNumber` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:230 CoordinatePairs.TrySecondNumber`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:265 CoordinatePairs.DecimalsEnd` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:265 CoordinatePairs.DecimalsEnd` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:264 CoordinatePairs.DecimalsEnd` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:265 CoordinatePairs.DecimalsEnd`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:298 TelemetryRedactionProcessor.OnEnd` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:298 TelemetryRedactionProcessor.OnEnd` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:297 TelemetryRedactionProcessor.OnEnd` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:298 TelemetryRedactionProcessor.OnEnd`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:331 LogRedactionProcessor.OnEnd` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:331 LogRedactionProcessor.OnEnd` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:330 LogRedactionProcessor.OnEnd` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:331 LogRedactionProcessor.OnEnd`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:362 RedactingJsonFormatter.Format` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:362 RedactingJsonFormatter.Format` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:361 RedactingJsonFormatter.Format` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:362 RedactingJsonFormatter.Format`
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:382 RedactingJsonFormatter.RedactNode` · `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:382 RedactingJsonFormatter.RedactNode` · `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:381 RedactingJsonFormatter.RedactNode` · `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:382 RedactingJsonFormatter.RedactNode`
- `src/Knock.Identity.Api/Common/Security/JwtSettingsFactory.cs:15 JwtSettingsFactory.Create` · `src/Knock.Market.Api/Common/Security/JwtSettingsFactory.cs:14 JwtSettingsFactory.Create` · `src/Knock.Messaging.Api/Hosting/JwtSettings.cs:18 JwtSettingsFactory.Create`
- `src/Knock.Identity.Api/Common/Security/JwtSettingsFactory.cs:30 JwtSettingsFactory.ResolveKey` · `src/Knock.Market.Api/Common/Security/JwtSettingsFactory.cs:29 JwtSettingsFactory.ResolveKey` · `src/Knock.Messaging.Api/Hosting/JwtSettings.cs:33 JwtSettingsFactory.ResolveKey`
- `src/Knock.Identity.Api/Hosting/ApiVersioningExtensions.cs:8 ApiVersioningExtensions.AddKnockApiVersioning` · `src/Knock.Market.Api/Hosting/ApiVersioningExtensions.cs:7 ApiVersioningExtensions.AddKnockApiVersioning`
- `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:56 AuthenticationExtensions.AddInternalControllers` · `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:56 AuthenticationExtensions.AddInternalControllers`
- `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:80 InternalControllerFeatureProvider.IsInternalController` · `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:80 InternalControllerFeatureProvider.IsInternalController`
- `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:91 AuthenticationStartupCheck.StartAsync` · `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:91 AuthenticationStartupCheck.StartAsync`
- `src/Knock.Identity.Api/Hosting/CorrelationIdMiddleware.cs:31 CorrelationIdMiddleware` · `src/Knock.Market.Api/Hosting/CorrelationIdMiddleware.cs:30 CorrelationIdMiddleware`
- `src/Knock.Identity.Api/Hosting/CorrelationIdMiddleware.cs:33 CorrelationIdMiddleware.InvokeAsync` · `src/Knock.Market.Api/Hosting/CorrelationIdMiddleware.cs:32 CorrelationIdMiddleware.InvokeAsync`
- `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs:25 CorsMiddleware.InvokeAsync` · `src/Knock.Market.Api/Hosting/CorsMiddleware.cs:24 CorsMiddleware.InvokeAsync`
- `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs:51 CorsMiddleware.AnswerPreflightAsync` · `src/Knock.Market.Api/Hosting/CorsMiddleware.cs:50 CorsMiddleware.AnswerPreflightAsync`
- `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs:69 CorsMiddleware.IsGranted` · `src/Knock.Market.Api/Hosting/CorsMiddleware.cs:68 CorsMiddleware.IsGranted`
- `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs:96 CorsMiddleware.AddVaryOrigin` · `src/Knock.Market.Api/Hosting/CorsMiddleware.cs:95 CorsMiddleware.AddVaryOrigin`
- `src/Knock.Identity.Api/Hosting/DomainExceptionHandler.cs:15 DomainExceptionHandler.TryHandleAsync` · `src/Knock.Market.Api/Hosting/DomainExceptionHandler.cs:14 DomainExceptionHandler.TryHandleAsync`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:21 JsonCompressionMiddleware` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:20 JsonCompressionMiddleware`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:28 JsonCompressionMiddleware.InvokeAsync` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:27 JsonCompressionMiddleware.InvokeAsync`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:106 JsonCompressionMiddleware.WriteBufferedAsync` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:105 JsonCompressionMiddleware.WriteBufferedAsync`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:153 JsonCompressionMiddleware.AddVaryAcceptEncoding` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:152 JsonCompressionMiddleware.AddVaryAcceptEncoding`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:180 CappedBufferStream.WriteAsync` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:179 CappedBufferStream.WriteAsync`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:199 CappedBufferStream.WriteCore` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:198 CappedBufferStream.WriteCore`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:76 KnockMongoOptionsValidator.Validate` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:74 KnockMongoOptionsValidator.Validate` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:76 KnockMongoOptionsValidator.Validate`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:149 MongoExtensions.AddKnockMongo` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:147 MongoExtensions.AddKnockMongo` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:149 MongoExtensions.AddKnockMongo`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:171 MongoExtensions.AddKnockMongoDatabase` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:169 MongoExtensions.AddKnockMongoDatabase` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:171 MongoExtensions.AddKnockMongoDatabase`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:185 MongoExtensions.CreateSettings` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:183 MongoExtensions.CreateSettings` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:185 MongoExtensions.CreateSettings`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:196 MongoExtensions.Bind` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:194 MongoExtensions.Bind` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:196 MongoExtensions.Bind`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:229 MongoReadinessHealthCheck.CheckHealthAsync` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:227 MongoReadinessHealthCheck.CheckHealthAsync` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:229 MongoReadinessHealthCheck.CheckHealthAsync`
- `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:79 RateLimitingExtensions.GlobalPartition` · `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:103 RateLimitingExtensions.GlobalPartition`
- `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:119 RateLimitingExtensions.IsAuthPath` · `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:143 RateLimitingExtensions.IsAuthPath`
- `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:132 RateLimitingExtensions.ClientKey` · `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:156 RateLimitingExtensions.ClientKey` · `src/Knock.Messaging.Api/Hosting/RateLimitingExtensions.cs:99 RateLimitingExtensions.ClientKey`
- `src/Knock.Identity.Api/Hosting/SecurityExtensions.cs:13 SecurityExtensions.AddKnockSecurity` · `src/Knock.Market.Api/Hosting/SecurityExtensions.cs:12 SecurityExtensions.AddKnockSecurity`
- `src/Knock.Identity.Api/Hosting/SecurityHeadersMiddleware.cs:12 SecurityHeadersMiddleware` · `src/Knock.Market.Api/Hosting/SecurityHeadersMiddleware.cs:11 SecurityHeadersMiddleware`
- `src/Knock.Identity.Api/Hosting/SecurityHeadersMiddleware.cs:25 SecurityHeadersMiddleware.Apply` · `src/Knock.Market.Api/Hosting/SecurityHeadersMiddleware.cs:24 SecurityHeadersMiddleware.Apply`
- `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:50 SecuritySettingsValidator.Errors` · `src/Knock.Market.Api/Hosting/SecuritySettings.cs:49 SecuritySettingsValidator.Errors`
- `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:84 SecuritySettingsValidator.IsValidOrigin` · `src/Knock.Market.Api/Hosting/SecuritySettings.cs:83 SecuritySettingsValidator.IsValidOrigin`
- `src/Knock.Identity.Api/Hosting/ValidationExtensions.cs:70 ValidationFilter.ToCamelCasePath` · `src/Knock.Market.Api/Hosting/ValidationExtensions.cs:65 ValidationFilter.ToCamelCasePath`
- `src/Knock.Identity.Api/Hosting/ValidationExtensions.cs:74 ValidationFilter.Collect` · `src/Knock.Market.Api/Hosting/ValidationExtensions.cs:69 ValidationFilter.Collect`
- `src/Knock.Identity.Api/Modules/Usuarios/Controllers/V1/MeController.cs:81 MeController.Matches` · `src/Knock.Market.Api/Modules/Catalog/Controllers/V1/OficiosController.cs:52 OficiosController.Matches`
- `src/Knock.Identity.Api/Modules/Usuarios/Data/ProfileCacheOptions.cs:25 ProfileCacheOptionsValidator.Validate` · `src/Knock.Market.Api/Modules/Catalog/Data/CatalogCacheOptions.cs:25 CatalogCacheOptionsValidator.Validate`

## Tipos con el mismo nombre en varios lugares

- `ApiVersioningExtensions`: `src/Knock.Identity.Api/Hosting/ApiVersioningExtensions.cs:6`, `src/Knock.Market.Api/Hosting/ApiVersioningExtensions.cs:5`
- `AuthenticationExtensions`: `src/Knock.Gateway/Hosting/AuthenticationExtensions.cs:42`, `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:10`, `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:10`, `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs:28`
- `AuthenticationStartupCheck`: `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:89`, `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:89`, `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs:138`
- `AzureMonitorExportStatus`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:39`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:39`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:38`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:39`
- `AzureMonitorSignalSettings`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:36`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:36`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:35`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:36`
- `BusquedaStore`: `src/Knock.Market.Api/Modules/Catalog/Data/BusquedaStore.cs:17`, `src/Knock.Market.Api/Modules/Catalog/Data/BusquedaStorePaging.cs:11`
- `CoordinatePairs`: `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:175`, `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:175`, `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:174`, `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:175`
- `CorrelationId`: `src/Knock.Contracts/Correlation/CorrelationId.cs:9`, `src/Knock.Identity.Api/Hosting/CorrelationIdMiddleware.cs:8`, `src/Knock.Market.Api/Hosting/CorrelationIdMiddleware.cs:7`
- `CorrelationIdMiddleware`: `src/Knock.Contracts/Correlation/CorrelationIdMiddleware.cs:11`, `src/Knock.Identity.Api/Hosting/CorrelationIdMiddleware.cs:31`, `src/Knock.Market.Api/Hosting/CorrelationIdMiddleware.cs:30`
- `CorsMiddleware`: `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs:14`, `src/Knock.Market.Api/Hosting/CorsMiddleware.cs:13`
- `CorsSettings`: `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:21`, `src/Knock.Market.Api/Hosting/SecuritySettings.cs:20`
- `DataExtensions`: `src/Knock.Gateway/Hosting/DataExtensions.cs:7`, `src/Knock.Identity.Api/Hosting/DataExtensions.cs:7`, `src/Knock.Messaging.Api/Hosting/DataExtensions.cs:7`
- `DomainErrorKind`: `src/Knock.Identity.Api/Common/Errors/DomainException.cs:5`, `src/Knock.Market.Api/Common/Errors/DomainException.cs:4`
- `DomainErrors`: `src/Knock.Identity.Api/Common/Errors/DomainException.cs:56`, `src/Knock.Market.Api/Common/Errors/DomainException.cs:54`
- `DomainException`: `src/Knock.Identity.Api/Common/Errors/DomainException.cs:19`, `src/Knock.Market.Api/Common/Errors/DomainException.cs:17`
- `DomainExceptionHandler`: `src/Knock.Identity.Api/Hosting/DomainExceptionHandler.cs:13`, `src/Knock.Market.Api/Hosting/DomainExceptionHandler.cs:12`
- `ForwardedHeadersExtensions`: `src/Knock.Identity.Api/Hosting/ForwardedHeadersExtensions.cs:7`, `src/Knock.Market.Api/Hosting/ForwardedHeadersExtensions.cs:6`, `src/Knock.Messaging.Api/Hosting/ForwardedHeadersExtensions.cs:7`
- `HealthExtensions`: `src/Knock.Gateway/Hosting/HealthExtensions.cs:10`, `src/Knock.Identity.Api/Hosting/HealthExtensions.cs:10`, `src/Knock.Market.Api/Hosting/HealthExtensions.cs:5`, `src/Knock.Messaging.Api/Hosting/HealthExtensions.cs:10`
- `HstsSettings`: `src/Knock.Gateway/Hosting/HstsExtensions.cs:10`, `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:7`, `src/Knock.Market.Api/Hosting/SecuritySettings.cs:6`
- `HttpsSettings`: `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:15`, `src/Knock.Market.Api/Hosting/SecuritySettings.cs:14`
- `IRevokedSessions`: `src/Knock.Gateway/Hosting/RevokedSessions.cs:10`, `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs:12`
- `IgnoreIncomingTraceContextPropagator`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:194`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:194`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:190`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:194`
- `InternalControllerFeatureProvider`: `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:72`, `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:72`
- `JsonCompressionMiddleware`: `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:21`, `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:20`
- `JwtSettings`: `src/Knock.Identity.Api/Common/Security/JwtSettings.cs:7`, `src/Knock.Market.Api/Common/Security/JwtSettings.cs:6`, `src/Knock.Messaging.Api/Hosting/JwtSettings.cs:8`
- `JwtSettingsFactory`: `src/Knock.Identity.Api/Common/Security/JwtSettingsFactory.cs:11`, `src/Knock.Market.Api/Common/Security/JwtSettingsFactory.cs:10`, `src/Knock.Messaging.Api/Hosting/JwtSettings.cs:14`
- `KnockApplicationExtensions`: `src/Knock.Identity.Api/Hosting/KnockApplicationExtensions.cs:7`, `src/Knock.Market.Api/Hosting/KnockApplicationExtensions.cs:6`
- `KnockMongoOptions`: `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:16`, `src/Knock.Market.Api/Hosting/MongoExtensions.cs:14`, `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:16`
- `KnockMongoOptionsValidator`: `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:71`, `src/Knock.Market.Api/Hosting/MongoExtensions.cs:69`, `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:71`
- `KnockRedisOptions`: `src/Knock.Gateway/Hosting/RedisExtensions.cs:15`, `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:15`, `src/Knock.Market.Api/Hosting/RedisExtensions.cs:13`, `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:14`
- `KnockRedisOptionsValidator`: `src/Knock.Gateway/Hosting/RedisExtensions.cs:39`, `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:39`, `src/Knock.Market.Api/Hosting/RedisExtensions.cs:37`, `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:38`
- `LogRedactionProcessor`: `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:329`, `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:329`, `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:328`, `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:329`
- `LoggingExtensions`: `src/Knock.Gateway/Hosting/LoggingExtensions.cs:13`, `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:13`, `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:12`, `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:13`
- `ModulesExtensions`: `src/Knock.Identity.Api/Hosting/ModulesExtensions.cs:8`, `src/Knock.Market.Api/Hosting/ModulesExtensions.cs:8`
- `MongoExtensions`: `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:138`, `src/Knock.Market.Api/Hosting/MongoExtensions.cs:136`, `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:138`
- `MongoReadinessHealthCheck`: `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:227`, `src/Knock.Market.Api/Hosting/MongoExtensions.cs:225`, `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:227`
- `ObservabilityExtensions`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:51`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:51`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:50`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:51`
- `ObservabilityOptions`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:22`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:22`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:21`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:22`
- `OpenApiExtensions`: `src/Knock.Identity.Api/Hosting/OpenApiExtensions.cs:7`, `src/Knock.Market.Api/Hosting/OpenApiExtensions.cs:5`
- `OpenTelemetryLogSink`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:215`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:215`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:211`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:215`
- `ProblemDetailsExtensions`: `src/Knock.Identity.Api/Hosting/ProblemDetailsExtensions.cs:6`, `src/Knock.Market.Api/Hosting/ProblemDetailsExtensions.cs:5`
- `ProblemResponses`: `src/Knock.Gateway/Hosting/ProblemResponses.cs:7`, `src/Knock.Identity.Api/Hosting/RequestBodyLimitMiddleware.cs:8`, `src/Knock.Market.Api/Hosting/RequestBodyLimitMiddleware.cs:9`, `src/Knock.Messaging.Api/Hosting/ProblemResponses.cs:7`
- `RateLimitPolicySettings`: `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:12`, `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:12`, `src/Knock.Messaging.Api/Hosting/RateLimitingExtensions.cs:12`
- `RateLimitSettings`: `src/Knock.Gateway/Hosting/RateLimitingExtensions.cs:20`, `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:22`, `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:22`, `src/Knock.Messaging.Api/Hosting/RateLimitingExtensions.cs:22`
- `RateLimitingExtensions`: `src/Knock.Gateway/Hosting/RateLimitingExtensions.cs:65`, `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:34`, `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:34`, `src/Knock.Messaging.Api/Hosting/RateLimitingExtensions.cs:32`
- `RedactingJsonFormatter`: `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:358`, `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:358`, `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:357`, `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:358`
- `RedisExtensions`: `src/Knock.Gateway/Hosting/RedisExtensions.cs:93`, `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:93`, `src/Knock.Market.Api/Hosting/RedisExtensions.cs:91`, `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:92`
- `RedisReadinessHealthCheck`: `src/Knock.Gateway/Hosting/RedisExtensions.cs:160`, `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:160`, `src/Knock.Market.Api/Hosting/RedisExtensions.cs:158`, `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:159`
- `RedisRevokedSessions`: `src/Knock.Gateway/Hosting/RevokedSessions.cs:16`, `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs:18`
- `RequestBodyLimitMiddleware`: `src/Knock.Identity.Api/Hosting/RequestBodyLimitMiddleware.cs:37`, `src/Knock.Market.Api/Hosting/RequestBodyLimitMiddleware.cs:34`
- `ScopeRequestPathEnricher`: `src/Knock.Gateway/Hosting/LoggingExtensions.cs:141`, `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:141`, `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:140`, `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:141`
- `SecurityExtensions`: `src/Knock.Identity.Api/Hosting/SecurityExtensions.cs:7`, `src/Knock.Market.Api/Hosting/SecurityExtensions.cs:6`
- `SecurityHeadersMiddleware`: `src/Knock.Gateway/Hosting/SecurityHeadersMiddleware.cs:13`, `src/Knock.Identity.Api/Hosting/SecurityHeadersMiddleware.cs:12`, `src/Knock.Market.Api/Hosting/SecurityHeadersMiddleware.cs:11`, `src/Knock.Messaging.Api/Hosting/SecurityHeadersMiddleware.cs:11`
- `SecuritySettings`: `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:29`, `src/Knock.Market.Api/Hosting/SecuritySettings.cs:28`
- `SecuritySettingsValidator`: `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:41`, `src/Knock.Market.Api/Hosting/SecuritySettings.cs:40`
- `SerilogLogState`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:255`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:255`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:251`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:255`
- `TelemetryExceptionRecorder`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:175`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:175`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:171`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:175`
- `TelemetryRedactionProcessor`: `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:293`, `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:293`, `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:292`, `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:293`
- `TelemetryRedactor`: `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:25`, `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:25`, `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:24`, `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:25`
- `UnavailableContentModerator`: `src/Knock.Identity.Api/Modules/Safety/Services/UnavailableContentModerator.cs:9`, `src/Knock.Market.Api/Modules/Requests/Services/RequestPorts.cs:29`
- `ValidationExtensions`: `src/Knock.Identity.Api/Hosting/ValidationExtensions.cs:9`, `src/Knock.Market.Api/Hosting/ValidationExtensions.cs:9`
- `ValidationFilter`: `src/Knock.Identity.Api/Hosting/ValidationExtensions.cs:25`, `src/Knock.Market.Api/Hosting/ValidationExtensions.cs:24`

## Índice por módulo

### Assistant

**data**

- `src/Knock.Market.Api/Modules/Assistant/Data/ConsumoIaStore.cs` — enum `ConsumptionResult`, enum `AdjustmentResult`, record `ConsumptionReservation`, record `ReservationResult`, class `ConsumoIaStore`
  - `ConsumoIaStore()`:13, `ReserveAsync()`:44, `AdjustAsync()`:68
- `src/Knock.Market.Api/Modules/Assistant/Data/UsoIaStore.cs` — record `UsageRecord`, class `UsoIaStore`
  - `UsoIaStore()`:8, `SaveAsync()`:10

### Catalog

**-**

- `src/Knock.Market.Api/Modules/Catalog/CatalogModuleExtensions.cs` — class `CatalogModuleExtensions`
  - `AddCatalogModule()`:16
- `src/Knock.Market.Api/Modules/Catalog/ICatalogApi.cs` — interface `ICatalogApi`
- `src/Knock.Market.Api/Modules/Catalog/ModelBinders/SingleValueQueryModelBinder.cs` — class `SingleValueQueryModelBinder`, class `SingleValueFromQueryAttribute`
  - `BindModelAsync()`:16

**controllers**

- `src/Knock.Market.Api/Modules/Catalog/Controllers/SearchProblems.cs` — class `SearchProblems`
  - `WriteAsync()`:9
- `src/Knock.Market.Api/Modules/Catalog/Controllers/V1/BusquedaController.cs` — class `BusquedaController`
  - `Get()`:25
- `src/Knock.Market.Api/Modules/Catalog/Controllers/V1/ColoniaExistsController.cs` — class `ColoniaExistsController`
  - `Exists()`:28
- `src/Knock.Market.Api/Modules/Catalog/Controllers/V1/ColoniasController.cs` — class `ColoniasController`
  - `GetAtPoint()`:29
- `src/Knock.Market.Api/Modules/Catalog/Controllers/V1/ListaEsperaController.cs` — class `ListaEsperaController`
  - `Join()`:26
- `src/Knock.Market.Api/Modules/Catalog/Controllers/V1/OficiosController.cs` — class `OficiosController`
  - `ETag()`:42, `ETagPayload()`:44, `Matches()`:52
- `src/Knock.Market.Api/Modules/Catalog/Controllers/V1/ZonasController.cs` — class `ZonasController`

**data**

- `src/Knock.Market.Api/Modules/Catalog/Data/BusquedaSource.cs` — class `BusquedaSource`
  - `GetContextAsync()`:14, `FindPageAsync()`:41, `CountAsync()`:47
- `src/Knock.Market.Api/Modules/Catalog/Data/BusquedaStore.cs` — record `SearchCandidate`, class `BusquedaStore`
  - `SearchCandidate()`:10, `FindAsync()`:25, `CountUnmatchedAsync()`:72
- `src/Knock.Market.Api/Modules/Catalog/Data/BusquedaStorePaging.cs` — record `SearchPageRequest`, record `RankedCandidate`, class `BusquedaStore`
  - `FindPageAsync()`:13
- `src/Knock.Market.Api/Modules/Catalog/Data/CatalogCache.cs` — interface `ISearchResponseCache`, interface `ICatalogCache`, class `RedisCatalogCache`, class `DisabledCatalogCache`
  - `NeighborhoodNamesGenerationKey()`:75, `ColoniaExistsGenerationKey()`:77, `GetSearchGenerationAsync()`:81, `GetSearchAsync()`:83, `SetSearchAsync()`:99, `NeighborhoodNamesKey()`:142, `ColoniaExistsKey()`:144, `GetOficiosAsync()`:146, `RemoveOficiosAsync()`:161, `GetOficiosGenerationAsync()`:167, `SetOficiosIfGenerationAsync()`:170, `GetNeighborhoodNamesAsync()`:206, `RemoveNeighborhoodNamesAsync()`:222, `GetNeighborhoodNamesGenerationAsync()`:229, `SetNeighborhoodNamesIfGenerationAsync()`:235, `GetColoniaExistsAsync()`:255, `RemoveColoniaExistsAsync()`:271, `GetColoniaExistsGenerationAsync()`:278, `SetColoniaExistsIfGenerationAsync()`:284, `GetSearchGenerationAsync()`:323, `GetSearchAsync()`:324, `SetSearchAsync()`:326, `GetOficiosAsync()`:328, `RemoveOficiosAsync()`:331, `GetOficiosGenerationAsync()`:333 …
- `src/Knock.Market.Api/Modules/Catalog/Data/CatalogCacheOptions.cs` — class `CatalogCacheOptions`, class `CatalogCacheOptionsValidator`
  - `Validate()`:25
- `src/Knock.Market.Api/Modules/Catalog/Data/CatalogSeeder.cs` — class `CatalogSeeder`, record `ColoniasSnapshot`
  - `CatalogSeeder()`:13, `SeedAsync()`:19, `SeedAsync()`:29
- `src/Knock.Market.Api/Modules/Catalog/Data/CatalogStore.cs` — class `CatalogStore`
  - `EnsureIndexesAsync()`:68, `FindActiveOficiosAsync()`:91, `FindIntersectingAsync()`:110, `FindNearestCenterAsync()`:114, `FindNamesByPostalCodeAsync()`:117, `FindByPostalCodeAsync()`:120, `ColoniaNameExistsAsync()`:135, `ColoniaExistsAsync()`:138, `FindCenterAsync()`:151, `FindActiveOficioAsync()`:163, `FindColoniaAsync()`:172, `ActiveOficiosQuery()`:176, `ActiveOficioQuery()`:182, `ColoniaByIdQuery()`:188, `IntersectingQuery()`:194, `NearestCenterQuery()`:201, `NamesByPostalCodeQuery()`:207, `ColoniaNameExistsQuery()`:213, `ColoniaExistsQuery()`:219
- `src/Knock.Market.Api/Modules/Catalog/Data/WaitlistStore.cs` — class `WaitlistStore`
  - `JoinAsync()`:14

**domain**

- `src/Knock.Market.Api/Modules/Catalog/Domain/CatalogCoordinates.cs` — class `CatalogCoordinates`
  - `ToPosition()`:7
- `src/Knock.Market.Api/Modules/Catalog/Domain/Colonia.cs` — record `Colonia`
  - `Colonia()`:7, `IsPostalCode()`:14
- `src/Knock.Market.Api/Modules/Catalog/Domain/CursorDeBusqueda.cs` — record `SearchQuery`, record `SearchCursorPayload`, class `CursorDeBusqueda`
  - `Read()`:17, `CreateNext()`:25, `Page()`:42
- `src/Knock.Market.Api/Modules/Catalog/Domain/GeoPosition.cs` — record struct `GeoPosition`
- `src/Knock.Market.Api/Modules/Catalog/Domain/InterpretadorDeConsulta.cs` — record `SearchTrade`, record `SearchSynonym`, record `SearchInterpretation`, class `InterpretadorDeConsulta`
  - `Interpret()`:9
- `src/Knock.Market.Api/Modules/Catalog/Domain/Oficio.cs` — class `Oficio`
  - `IsValidModalidad()`:10
- `src/Knock.Market.Api/Modules/Catalog/Domain/OficioListing.cs` — record `OficioListing`
- `src/Knock.Market.Api/Modules/Catalog/Domain/OrdenDeResultados.cs` — record `SearchRank`, class `OrdenDeResultados`
  - `Classify()`:8, `Order()`:16, `Compare()`:19
- `src/Knock.Market.Api/Modules/Catalog/Domain/SearchNameNormalizer.cs` — class `SearchNameNormalizer`
  - `NormalizeQuery()`:12, `Normalize()`:44

**dtos**

- `src/Knock.Market.Api/Modules/Catalog/Dtos/BusquedaDtos.cs` — record `BusquedaQuery`, class `BusquedaQueryParameters`, record `BusquedaOficio`, record `BusquedaInterpretacion`, record `BusquedaItem`, record `BusquedaResponse`
  - `BusquedaQuery()`:9, `Get()`:11, `BusquedaResponse()`:41, `From()`:43
- `src/Knock.Market.Api/Modules/Catalog/Dtos/NeighborhoodAtPointQuery.cs` — class `NeighborhoodAtPointQuery`
- `src/Knock.Market.Api/Modules/Catalog/Dtos/NeighborhoodAtPointResponse.cs` — record `NeighborhoodAtPointResponse`, record `NeighborhoodAtPointColoniaResponse`
- `src/Knock.Market.Api/Modules/Catalog/Dtos/NeighborhoodExists.cs` — class `NeighborhoodExistsQuery`, record `NeighborhoodExistsResponse`
- `src/Knock.Market.Api/Modules/Catalog/Dtos/OficioResponse.cs` — record `OficioResponse`
- `src/Knock.Market.Api/Modules/Catalog/Dtos/PostalCodeRoute.cs` — class `PostalCodeRoute`, record `ZonaResponse`, record `ZonaColoniaResponse`
- `src/Knock.Market.Api/Modules/Catalog/Dtos/WaitlistRequest.cs` — record `WaitlistRequest`, record `WaitlistResponse`

**services**

- `src/Knock.Market.Api/Modules/Catalog/Services/BusquedaService.cs` — record `SearchContext`, record `SearchInput`, record `PublicSearchItem`, record `SearchResponse`, record `SearchServiceResult`, interface `IBusquedaSource`, class `BusquedaService`
  - `BusquedaService()`:23, `SearchAsync()`:25
- `src/Knock.Market.Api/Modules/Catalog/Services/CatalogApi.cs` — class `CatalogApi`
  - `ListActiveTradesAsync()`:12, `FindNeighborhoodCenterAsync()`:15, `FindNeighborhoodAtAsync()`:18, `FindNeighborhoodsByPostalCodeAsync()`:21, `NeighborhoodExistsAsync()`:32, `FindActiveTradeAsync()`:48, `FindNeighborhoodAsync()`:59
- `src/Knock.Market.Api/Modules/Catalog/Services/INeighborhoodLocator.cs` — interface `INeighborhoodLocator`
- `src/Knock.Market.Api/Modules/Catalog/Services/INeighborhoodNames.cs` — interface `INeighborhoodNames`
- `src/Knock.Market.Api/Modules/Catalog/Services/NeighborhoodLocator.cs` — class `NeighborhoodLocator`
  - `FindAtAsync()`:19
- `src/Knock.Market.Api/Modules/Catalog/Services/NeighborhoodNames.cs` — class `NeighborhoodNames`
  - `ExistsAsync()`:8
- `src/Knock.Market.Api/Modules/Catalog/Services/OficioQueries.cs` — class `OficioQueries`
  - `OficioQueries()`:7, `ListActiveAsync()`:14
- `src/Knock.Market.Api/Modules/Catalog/Services/SearchSafety.cs` — interface `IFiltroDatosPersonales`, class `FiltroDatosPersonalesNoDisponible`, class `SearchCursorOptions`, class `DevelopmentSearchCursorKey`, class `SearchCursorOptionsValidator`
  - `CanCount()`:14, `PostConfigure()`:28, `Validate()`:49
- `src/Knock.Market.Api/Modules/Catalog/Services/WaitlistService.cs` — record `WaitlistEntry`, record `WaitlistResult`, interface `IWaitlistStore`, class `WaitlistService`
  - `WaitlistService()`:14, `JoinAsync()`:16
- `src/Knock.Market.Api/Modules/Catalog/Services/ZonaService.cs` — class `ZonaService`
  - `ZonaService()`:6, `FindAsync()`:8

**validators**

- `src/Knock.Market.Api/Modules/Catalog/Validators/BusquedaQueryValidator.cs` — class `BusquedaQueryValidator`
- `src/Knock.Market.Api/Modules/Catalog/Validators/NeighborhoodAtPointQueryValidator.cs` — class `NeighborhoodAtPointQueryValidator`
  - `Parse()`:31
- `src/Knock.Market.Api/Modules/Catalog/Validators/NeighborhoodExistsQueryValidator.cs` — class `NeighborhoodExistsQueryValidator`
- `src/Knock.Market.Api/Modules/Catalog/Validators/PostalCodeRouteValidator.cs` — class `PostalCodeRouteValidator`
- `src/Knock.Market.Api/Modules/Catalog/Validators/WaitlistRequestValidator.cs` — class `WaitlistRequestValidator`

### Chat

**-**

- `src/Knock.Messaging.Api/Modules/Chat/ChatModuleExtensions.cs` — class `ChatModuleExtensions`
  - `AddChatModule()`:15, `MapChatHub()`:35
- `src/Knock.Messaging.Api/Modules/Chat/Options/ChatOptions.cs` — class `ChatOptions`, class `MarketServiceOptions`, class `ChatOptionsValidator`, class `MarketServiceOptionsValidator`
  - `Validate()`:44, `Validate()`:70

**data**

- `src/Knock.Messaging.Api/Modules/Chat/Data/ChatRedisStore.cs` — record `ChatLimitResult`, class `ChatRedisStore`
  - `ChatRedisStore()`:15, `ChargeAsync()`:29, `MarkClosedAsync()`:49, `IsClosedAsync()`:56
- `src/Knock.Messaging.Api/Modules/Chat/Data/MarketVisitaCatalog.cs` — record `VisitaVista`, class `MarketVisitaCatalog`, class `MarketVisitaCatalogRegistration`
  - `CheckAsync()`:24, `AddMarketVisitaCatalog()`:60

**hubs**

- `src/Knock.Messaging.Api/Modules/Chat/Hubs/ChatHub.cs` — class `ChatHub`
  - `OnConnectedAsync()`:20, `OnDisconnectedAsync()`:32, `UnirseAVisita()`:41, `SalirDeVisita()`:57, `EnviarMensaje()`:69

**services**

- `src/Knock.Messaging.Api/Modules/Chat/Services/ChatServices.cs` — class `ChatErrorCodes`, record `ChatResult`, record `ChatMessage`, class `ChatConnectionState`, class `ChatMetrics`, class `UnionService`, class `MensajeService`
  - `Fail()`:37, `ChatConnectionState()`:44, `Dispose()`:83, `UnionService()`:90, `JoinAsync()`:94, `Leave()`:135, `IsValidId()`:147, `MensajeService()`:160, `Group()`:167, `SendAsync()`:169, `Clean()`:220

### Conversaciones

**domain**

- `src/Knock.Messaging.Api/Modules/Conversaciones/Domain/OcultadorDeContacto.cs` — record struct `TextoOculto`, class `OcultadorDeContacto`
  - `Ocultar()`:23

### Knock.Contracts

**-**

- `src/Knock.Contracts/Catalog/INeighborhoodCenterCatalog.cs` — interface `INeighborhoodCenterCatalog`
- `src/Knock.Contracts/Colonias/IColoniaCatalog.cs` — interface `IColoniaCatalog`
- `src/Knock.Contracts/Conversations/PermisoConversacion.cs` — record `TecnicoPublicoConversacion`, record `PermisoConversacion`
- `src/Knock.Contracts/Correlation/CorrelationId.cs` — class `CorrelationId`
  - `IsValid()`:24, `Create()`:44, `Current()`:47
- `src/Knock.Contracts/Correlation/CorrelationIdMiddleware.cs` — class `CorrelationIdMiddleware`, class `CorrelationIdApplicationBuilderExtensions`
  - `CorrelationIdMiddleware()`:11, `InvokeAsync()`:14, `UseKnockCorrelationId()`:43
- `src/Knock.Contracts/Moderation/IContentModerator.cs` — enum `ModerationOutcome`, record `ModerationResult`, interface `IContentModerator`
- `src/Knock.Contracts/Pagination/CursorPage.cs` — class `PageLimit`, record `CursorPage`
  - `Resolve()`:11
- `src/Knock.Contracts/Sessions/SessionRevocationKey.cs` — class `SessionRevocationKey`
- `src/Knock.Contracts/Visits/IVisitaCatalog.cs` — enum `VisitMembershipStatus`, record `VisitMembership`, interface `IVisitaCatalog`

### Knock.Gateway

**-**

- `src/Knock.Gateway/Program.cs` — class `Program`

**hosting**

- `src/Knock.Gateway/Hosting/AuthenticationExtensions.cs` — record `GatewayJwtSettings`, class `AuthenticationExtensions`
  - `GatewayJwtSettings()`:9, `Create()`:13, `AddKnockGatewayAuthentication()`:47
- `src/Knock.Gateway/Hosting/CorsExtensions.cs` — class `CorsExtensions`
  - `AddKnockGatewayCors()`:12, `AddKnockGatewayForwardedHeaders()`:36
- `src/Knock.Gateway/Hosting/DataExtensions.cs` — class `DataExtensions`
  - `AddKnockData()`:9
- `src/Knock.Gateway/Hosting/HealthExtensions.cs` — class `HealthExtensions`
  - `MapKnockReadiness()`:16
- `src/Knock.Gateway/Hosting/HstsExtensions.cs` — class `HstsSettings`, class `HstsExtensions`
  - `AddKnockGatewayHsts()`:26
- `src/Knock.Gateway/Hosting/HubQueryTokenMiddleware.cs` — class `HubQueryTokenMiddleware`
  - `HubQueryTokenMiddleware()`:10, `InvokeAsync()`:16
- `src/Knock.Gateway/Hosting/LoggingExtensions.cs` — class `LoggingExtensions`, class `ScopeRequestPathEnricher`
  - `AddKnockLogging()`:20, `ConfigureRequestLogging()`:36, `IsQueryBearingBelowWarning()`:67, `RoutePath()`:80, `Enrich()`:145
- `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs` — class `ObservabilityOptions`, record `AzureMonitorSignalSettings`, class `AzureMonitorExportStatus`, class `ObservabilityExtensions`, class `TelemetryExceptionRecorder`, class `IgnoreIncomingTraceContextPropagator`, class `OpenTelemetryLogSink`, class `SerilogLogState`
  - `Record()`:47, `AddKnockObservability()`:71, `TryHandleAsync()`:177, `Inject()`:200, `ExtractTraceIdAndState()`:202, `ExtractBaggage()`:208, `Emit()`:224, `GetEnumerator()`:285
- `src/Knock.Gateway/Hosting/ProblemResponses.cs` — class `ProblemResponses`
  - `WriteAsync()`:9, `AddKnockGatewayProblemDetails()`:25
- `src/Knock.Gateway/Hosting/RateLimitingExtensions.cs` — class `RateLimitPolicy`, class `RateLimitSettings`, interface `IRequestCounter`, class `RedisRequestCounter`, class `RateLimitingExtensions`, class `IpRateLimitMiddleware`, class `UserRateLimitMiddleware`, class `RateLimitCheck`
  - `HitAsync()`:52, `AddKnockGatewayRateLimiting()`:67, `IsApiPath()`:80, `ClientKey()`:90, `IpRateLimitMiddleware()`:118, `InvokeAsync()`:122, `IsAuthPath()`:145, `UserRateLimitMiddleware()`:152, `InvokeAsync()`:154, `RejectedAsync()`:172
- `src/Knock.Gateway/Hosting/RedisExtensions.cs` — class `KnockRedisOptions`, class `KnockRedisOptionsValidator`, class `RedisExtensions`, class `RedisReadinessHealthCheck`
  - `Validate()`:41, `AddKnockRedis()`:105, `CreateConfiguration()`:123, `CheckHealthAsync()`:162
- `src/Knock.Gateway/Hosting/RevokedSessions.cs` — interface `IRevokedSessions`, class `RedisRevokedSessions`
  - `IsRevokedAsync()`:20
- `src/Knock.Gateway/Hosting/SecurityHeadersMiddleware.cs` — class `SecurityHeadersMiddleware`
  - `SecurityHeadersMiddleware()`:13, `InvokeAsync()`:22
- `src/Knock.Gateway/Hosting/TelemetryRedactor.cs` — class `TelemetryRedactor`, class `CoordinatePairs`, class `TelemetryRedactionProcessor`, class `LogRedactionProcessor`, class `RedactingJsonFormatter`
  - `IsSensitiveKey()`:42, `Redact()`:44, `RedactAttribute()`:76, `RedactUrl()`:100, `ExceptionEvent()`:122, `Redact()`:180, `OnEnd()`:298, `OnEnd()`:331, `Format()`:362

### Knock.Identity.Api

**-**

- `src/Knock.Identity.Api/Program.cs` — —

**common**

- `src/Knock.Identity.Api/Common/Errors/DomainException.cs` — enum `DomainErrorKind`, class `DomainException`, class `DomainErrors`
  - `ToStatusCode()`:58
- `src/Knock.Identity.Api/Common/Security/JwtSettings.cs` — record `JwtSettings`
- `src/Knock.Identity.Api/Common/Security/JwtSettingsFactory.cs` — class `JwtSettingsFactory`
  - `Create()`:15

**hosting**

- `src/Knock.Identity.Api/Hosting/ApiVersioningExtensions.cs` — class `ApiVersioningExtensions`
  - `AddKnockApiVersioning()`:8
- `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs` — class `AuthenticationExtensions`, class `InternalControllerFeatureProvider`, class `AuthenticationStartupCheck`
  - `AddKnockAuthentication()`:15, `AddInternalControllers()`:56, `IsController()`:74, `StartAsync()`:91, `StopAsync()`:109
- `src/Knock.Identity.Api/Hosting/CorrelationIdMiddleware.cs` — class `CorrelationId`, class `CorrelationIdMiddleware`
  - `IsValid()`:15, `Create()`:17, `Current()`:20, `CorrelationIdMiddleware()`:31, `InvokeAsync()`:33
- `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs` — class `CorsMiddleware`
  - `CorsMiddleware()`:14, `InvokeAsync()`:25
- `src/Knock.Identity.Api/Hosting/DataExtensions.cs` — class `DataExtensions`
  - `AddKnockData()`:12
- `src/Knock.Identity.Api/Hosting/DomainExceptionHandler.cs` — class `DomainExceptionHandler`
  - `TryHandleAsync()`:15
- `src/Knock.Identity.Api/Hosting/ForwardedHeadersExtensions.cs` — class `ForwardedHeadersExtensions`
  - `AddKnockForwardedHeaders()`:16
- `src/Knock.Identity.Api/Hosting/HealthExtensions.cs` — class `HealthExtensions`
  - `MapKnockReadiness()`:16
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs` — class `JsonCompressionMiddleware`, class `CappedBufferStream`, class `IdentityJsonSerializerContext`
  - `JsonCompressionMiddleware()`:21, `InvokeAsync()`:28, `ToMemoryStream()`:174, `Write()`:176, `Write()`:178, `WriteAsync()`:180, `Flush()`:250, `FlushAsync()`:258, `Read()`:261, `Seek()`:263, `SetLength()`:265, `Dispose()`:267
- `src/Knock.Identity.Api/Hosting/KnockApplicationExtensions.cs` — class `KnockApplicationExtensions`
  - `AddKnockApi()`:9, `UseKnockApi()`:31
- `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs` — class `LoggingExtensions`, class `ScopeRequestPathEnricher`
  - `AddKnockLogging()`:20, `ConfigureRequestLogging()`:36, `IsQueryBearingBelowWarning()`:67, `RoutePath()`:80, `Enrich()`:145
- `src/Knock.Identity.Api/Hosting/ModelStateProblems.cs` — class `ModelStateProblems`
  - `AddKnockModelStateProblems()`:13
- `src/Knock.Identity.Api/Hosting/ModulesExtensions.cs` — class `ModulesExtensions`
  - `AddKnockModules()`:11
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs` — class `KnockMongoOptions`, class `KnockMongoOptionsValidator`, class `MongoExtensions`, class `MongoReadinessHealthCheck`
  - `Seconds()`:60, `Validate()`:76, `AddKnockMongo()`:149, `AddKnockMongoDatabase()`:171, `CreateSettings()`:185, `CheckHealthAsync()`:229
- `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs` — class `ObservabilityOptions`, record `AzureMonitorSignalSettings`, class `AzureMonitorExportStatus`, class `ObservabilityExtensions`, class `TelemetryExceptionRecorder`, class `IgnoreIncomingTraceContextPropagator`, class `OpenTelemetryLogSink`, class `SerilogLogState`
  - `Record()`:47, `AddKnockObservability()`:71, `TryHandleAsync()`:177, `Inject()`:200, `ExtractTraceIdAndState()`:202, `ExtractBaggage()`:208, `Emit()`:224, `GetEnumerator()`:285
- `src/Knock.Identity.Api/Hosting/OpenApiExtensions.cs` — class `OpenApiExtensions`
  - `AddKnockOpenApi()`:9, `MapKnockOpenApi()`:41
- `src/Knock.Identity.Api/Hosting/ProblemDetailsExtensions.cs` — class `ProblemDetailsExtensions`
  - `AddKnockProblemDetails()`:9
- `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs` — class `RateLimitPolicySettings`, class `RateLimitSettings`, class `RateLimitingExtensions`
  - `AddKnockRateLimiting()`:44, `ClientKey()`:132
- `src/Knock.Identity.Api/Hosting/RedisExtensions.cs` — class `KnockRedisOptions`, class `KnockRedisOptionsValidator`, class `RedisExtensions`, class `RedisReadinessHealthCheck`
  - `Validate()`:41, `AddKnockRedis()`:105, `CreateConfiguration()`:123, `CheckHealthAsync()`:162
- `src/Knock.Identity.Api/Hosting/RequestBodyLimitMiddleware.cs` — class `ProblemResponses`, class `RequestBodyLimitMiddleware`
  - `WriteAsync()`:10, `RequestBodyLimitMiddleware()`:37, `InvokeAsync()`:39
- `src/Knock.Identity.Api/Hosting/SecurityExtensions.cs` — class `SecurityExtensions`
  - `AddKnockSecurity()`:13
- `src/Knock.Identity.Api/Hosting/SecurityHeadersMiddleware.cs` — class `SecurityHeadersMiddleware`
  - `SecurityHeadersMiddleware()`:12, `InvokeAsync()`:18
- `src/Knock.Identity.Api/Hosting/SecuritySettings.cs` — class `HstsSettings`, class `HttpsSettings`, class `CorsSettings`, class `SecuritySettings`, class `SecuritySettingsValidator`
  - `Validate()`:47, `Errors()`:50, `IsValidOrigin()`:84
- `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs` — class `TelemetryRedactor`, class `CoordinatePairs`, class `TelemetryRedactionProcessor`, class `LogRedactionProcessor`, class `RedactingJsonFormatter`
  - `IsSensitiveKey()`:42, `Redact()`:44, `RedactAttribute()`:76, `RedactUrl()`:100, `ExceptionEvent()`:122, `Redact()`:180, `OnEnd()`:298, `OnEnd()`:331, `Format()`:362
- `src/Knock.Identity.Api/Hosting/ValidationExtensions.cs` — class `ValidationExtensions`, class `ValidationFilter`
  - `AddKnockValidation()`:12, `OnActionExecutionAsync()`:29, `WriteErrorsAsync()`:56, `ToCamelCasePath()`:70

### Knock.Market.Api

**-**

- `src/Knock.Market.Api/Program.cs` — —

**common**

- `src/Knock.Market.Api/Common/Crypto/FieldCipher.cs` — class `FieldEncryptionOptions`, class `FieldCipher`
  - `FieldCipher()`:25, `Encrypt()`:31, `Decrypt()`:53
- `src/Knock.Market.Api/Common/Crypto/WorkshopAddressCipher.cs` — class `WorkshopAddressCipher`
  - `Context()`:12, `Encrypt()`:14, `Read()`:20
- `src/Knock.Market.Api/Common/Errors/DomainException.cs` — enum `DomainErrorKind`, class `DomainException`, class `DomainErrors`
  - `ToStatusCode()`:56
- `src/Knock.Market.Api/Common/Errors/ErrorCodes.cs` — class `ErrorCodes`
- `src/Knock.Market.Api/Common/Security/JwtSettings.cs` — record `JwtSettings`
- `src/Knock.Market.Api/Common/Security/JwtSettingsFactory.cs` — class `JwtSettingsFactory`
  - `Create()`:14

**hosting**

- `src/Knock.Market.Api/Hosting/ApiVersioningExtensions.cs` — class `ApiVersioningExtensions`
  - `AddKnockApiVersioning()`:7
- `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs` — class `AuthenticationExtensions`, class `InternalControllerFeatureProvider`, class `AuthenticationStartupCheck`
  - `AddKnockAuthentication()`:15, `AddInternalControllers()`:56, `IsController()`:74, `StartAsync()`:91, `StopAsync()`:109
- `src/Knock.Market.Api/Hosting/CorrelationIdMiddleware.cs` — class `CorrelationId`, class `CorrelationIdMiddleware`
  - `IsValid()`:14, `Create()`:16, `Current()`:19, `CorrelationIdMiddleware()`:30, `InvokeAsync()`:32
- `src/Knock.Market.Api/Hosting/CorsMiddleware.cs` — class `CorsMiddleware`
  - `CorsMiddleware()`:13, `InvokeAsync()`:24
- `src/Knock.Market.Api/Hosting/DomainExceptionHandler.cs` — class `DomainExceptionHandler`
  - `TryHandleAsync()`:14
- `src/Knock.Market.Api/Hosting/ForwardedHeadersExtensions.cs` — class `ForwardedHeadersExtensions`
  - `AddKnockForwardedHeaders()`:15
- `src/Knock.Market.Api/Hosting/HealthExtensions.cs` — class `HealthExtensions`
  - `AddKnockHealth()`:13, `MapKnockHealth()`:19
- `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs` — class `JsonCompressionMiddleware`, class `CappedBufferStream`, class `MarketJsonSerializerContext`
  - `JsonCompressionMiddleware()`:20, `InvokeAsync()`:27, `ToMemoryStream()`:173, `Write()`:175, `Write()`:177, `WriteAsync()`:179, `Flush()`:249, `FlushAsync()`:257, `Read()`:260, `Seek()`:262, `SetLength()`:264, `Dispose()`:266
- `src/Knock.Market.Api/Hosting/KnockApplicationExtensions.cs` — class `KnockApplicationExtensions`
  - `AddKnockApi()`:8, `UseKnockApi()`:31
- `src/Knock.Market.Api/Hosting/LoggingExtensions.cs` — class `LoggingExtensions`, class `ScopeRequestPathEnricher`
  - `AddKnockLogging()`:19, `ConfigureRequestLogging()`:35, `IsQueryBearingBelowWarning()`:66, `RoutePath()`:79, `Enrich()`:144
- `src/Knock.Market.Api/Hosting/ModulesExtensions.cs` — class `ModulesExtensions`
  - `AddKnockModules()`:10
- `src/Knock.Market.Api/Hosting/MongoExtensions.cs` — class `KnockMongoOptions`, class `KnockMongoOptionsValidator`, class `MongoExtensions`, class `MongoReadinessHealthCheck`
  - `Seconds()`:58, `Validate()`:74, `AddKnockMongo()`:147, `AddKnockMongoDatabase()`:169, `CreateSettings()`:183, `CheckHealthAsync()`:227
- `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs` — class `ObservabilityOptions`, record `AzureMonitorSignalSettings`, class `AzureMonitorExportStatus`, class `ObservabilityExtensions`, class `TelemetryExceptionRecorder`, class `IgnoreIncomingTraceContextPropagator`, class `OpenTelemetryLogSink`, class `SerilogLogState`
  - `Record()`:46, `AddKnockObservability()`:67, `TryHandleAsync()`:173, `Inject()`:196, `ExtractTraceIdAndState()`:198, `ExtractBaggage()`:204, `Emit()`:220, `GetEnumerator()`:281
- `src/Knock.Market.Api/Hosting/OpenApiExtensions.cs` — class `OpenApiExtensions`
  - `AddKnockOpenApi()`:7, `MapKnockOpenApi()`:14
- `src/Knock.Market.Api/Hosting/ProblemDetailsExtensions.cs` — class `ProblemDetailsExtensions`
  - `AddKnockProblemDetails()`:8
- `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs` — class `RateLimitPolicySettings`, class `RateLimitSettings`, class `RateLimitingExtensions`
  - `AddKnockRateLimiting()`:55, `ClientKey()`:156
- `src/Knock.Market.Api/Hosting/RedisExtensions.cs` — class `KnockRedisOptions`, class `KnockRedisOptionsValidator`, class `RedisExtensions`, class `RedisReadinessHealthCheck`
  - `Validate()`:39, `AddKnockRedis()`:103, `CreateConfiguration()`:121, `CheckHealthAsync()`:160
- `src/Knock.Market.Api/Hosting/RequestBodyLimitMiddleware.cs` — class `ProblemResponses`, class `RequestBodyLimitMiddleware`
  - `WriteAsync()`:11, `RequestBodyLimitMiddleware()`:34, `InvokeAsync()`:36
- `src/Knock.Market.Api/Hosting/SecurityExtensions.cs` — class `SecurityExtensions`
  - `AddKnockSecurity()`:12
- `src/Knock.Market.Api/Hosting/SecurityHeadersMiddleware.cs` — class `SecurityHeadersMiddleware`
  - `SecurityHeadersMiddleware()`:11, `InvokeAsync()`:17
- `src/Knock.Market.Api/Hosting/SecuritySettings.cs` — class `HstsSettings`, class `HttpsSettings`, class `CorsSettings`, class `SecuritySettings`, class `SecuritySettingsValidator`
  - `Validate()`:46, `Errors()`:49, `IsValidOrigin()`:83
- `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs` — class `TelemetryRedactor`, class `CoordinatePairs`, class `TelemetryRedactionProcessor`, class `LogRedactionProcessor`, class `RedactingJsonFormatter`
  - `IsSensitiveKey()`:41, `Redact()`:43, `RedactAttribute()`:75, `RedactUrl()`:99, `ExceptionEvent()`:121, `Redact()`:179, `OnEnd()`:297, `OnEnd()`:330, `Format()`:361
- `src/Knock.Market.Api/Hosting/ValidationExtensions.cs` — class `ValidationExtensions`, class `ValidationFilter`
  - `AddKnockValidation()`:12, `OnActionExecutionAsync()`:28, `ToCamelCasePath()`:65

### Knock.Messaging.Api

**-**

- `src/Knock.Messaging.Api/Program.cs` — —

**hosting**

- `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs` — interface `IRevokedSessions`, class `RedisRevokedSessions`, class `AuthenticationExtensions`, class `AuthenticationStartupCheck`
  - `IsRevokedAsync()`:20, `AddKnockAuthentication()`:39, `StartAsync()`:140, `StopAsync()`:146
- `src/Knock.Messaging.Api/Hosting/ChatHostingExtensions.cs` — class `ChatHostingExtensions`
  - `AddKnockChatBridge()`:22, `UseKnockChatGate()`:38
- `src/Knock.Messaging.Api/Hosting/DataExtensions.cs` — class `DataExtensions`
  - `AddKnockData()`:12
- `src/Knock.Messaging.Api/Hosting/ForwardedHeadersExtensions.cs` — class `ForwardedHeadersExtensions`
  - `AddKnockForwardedHeaders()`:16
- `src/Knock.Messaging.Api/Hosting/HealthExtensions.cs` — class `HealthExtensions`
  - `MapKnockReadiness()`:16
- `src/Knock.Messaging.Api/Hosting/JwtSettings.cs` — record `JwtSettings`, class `JwtSettingsFactory`
  - `Create()`:18
- `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs` — class `LoggingExtensions`, class `ScopeRequestPathEnricher`
  - `AddKnockLogging()`:20, `ConfigureRequestLogging()`:36, `IsQueryBearingBelowWarning()`:67, `RoutePath()`:80, `Enrich()`:145
- `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs` — class `KnockMongoOptions`, class `KnockMongoOptionsValidator`, class `MongoExtensions`, class `MongoReadinessHealthCheck`
  - `Seconds()`:60, `Validate()`:76, `AddKnockMongo()`:149, `AddKnockMongoDatabase()`:171, `CreateSettings()`:185, `CheckHealthAsync()`:229
- `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs` — class `ObservabilityOptions`, record `AzureMonitorSignalSettings`, class `AzureMonitorExportStatus`, class `ObservabilityExtensions`, class `TelemetryExceptionRecorder`, class `IgnoreIncomingTraceContextPropagator`, class `OpenTelemetryLogSink`, class `SerilogLogState`
  - `Record()`:47, `AddKnockObservability()`:71, `TryHandleAsync()`:177, `Inject()`:200, `ExtractTraceIdAndState()`:202, `ExtractBaggage()`:208, `Emit()`:224, `GetEnumerator()`:285
- `src/Knock.Messaging.Api/Hosting/ProblemResponses.cs` — class `ProblemResponses`
  - `AddKnockProblemDetails()`:9, `WriteAsync()`:16
- `src/Knock.Messaging.Api/Hosting/RateLimitingExtensions.cs` — class `RateLimitPolicySettings`, class `RateLimitSettings`, class `RateLimitingExtensions`
  - `AddKnockRateLimiting()`:43, `ClientKey()`:99
- `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs` — class `KnockRedisOptions`, class `KnockRedisOptionsValidator`, class `RedisExtensions`, class `RedisReadinessHealthCheck`
  - `Validate()`:40, `AddKnockRedis()`:104, `CreateConfiguration()`:122, `CheckHealthAsync()`:161
- `src/Knock.Messaging.Api/Hosting/SecurityHeadersMiddleware.cs` — class `SecurityHeadersMiddleware`
  - `SecurityHeadersMiddleware()`:11, `InvokeAsync()`:17
- `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs` — class `TelemetryRedactor`, class `CoordinatePairs`, class `TelemetryRedactionProcessor`, class `LogRedactionProcessor`, class `RedactingJsonFormatter`
  - `IsSensitiveKey()`:42, `Redact()`:44, `RedactAttribute()`:76, `RedactUrl()`:100, `ExceptionEvent()`:122, `Redact()`:180, `OnEnd()`:298, `OnEnd()`:331, `Format()`:362

### Media

**-**

- `src/Knock.Market.Api/Modules/Media/IMediaApi.cs` — interface `IMediaApi`
- `src/Knock.Market.Api/Modules/Media/MediaModuleExtensions.cs` — class `MediaModuleExtensions`, class `UnavailableModerator`
  - `AddMediaModule()`:13, `ReviewTextAsync()`:42, `ReviewImageAsync()`:45

**data**

- `src/Knock.Market.Api/Modules/Media/Data/BlobMediaFileStore.cs` — class `BlobMediaFileStore`, record `MediaFileRecord`
  - `AddAsync()`:25, `FindAsync()`:40, `TryReplaceAsync()`:56, `FromDomain()`:106, `ToDomain()`:110
- `src/Knock.Market.Api/Modules/Media/Data/IMediaFileStore.cs` — record `StoredMediaFile`, interface `IMediaFileStore`
- `src/Knock.Market.Api/Modules/Media/Data/InMemoryMediaFileStore.cs` — class `InMemoryMediaFileStore`
  - `AddAsync()`:13, `FindAsync()`:24, `TryReplaceAsync()`:27
- `src/Knock.Market.Api/Modules/Media/Data/MediaOptions.cs` — class `MediaOptions`

**domain**

- `src/Knock.Market.Api/Modules/Media/Domain/MediaFile.cs` — enum `MediaUsage`, enum `MediaFileState`, enum `ImageFormat`, record `MediaFile`
  - `MediaFile()`:14, `IsReadyFor()`:24, `CreatePending()`:25, `MarkReady()`:28, `MarkRejected()`:33
- `src/Knock.Market.Api/Modules/Media/Domain/MediaOutcome.cs` — enum `MediaOutcomeStatus`, record `MediaOutcome`, class `MediaRejection`, record `ProcessedImage`
  - `MediaOutcome()`:7, `Ok()`:9, `NotFound()`:10, `Conflict()`:11, `Unprocessable()`:12, `Unavailable()`:13
- `src/Knock.Market.Api/Modules/Media/Domain/MediaRules.cs` — class `MediaRules`
  - `IsAllowedContentType()`:24, `IsAllowedSize()`:27, `AreAllowedDimensions()`:29, `DetectFormat()`:32, `NewBlobKey()`:41, `ParseUsage()`:43, `ToContractValue()`:51, `ToContractValue()`:58

**dtos**

- `src/Knock.Market.Api/Modules/Media/Dtos/CreateUploadRequest.cs` — record `CreateUploadRequest`

**services**

- `src/Knock.Market.Api/Modules/Media/Services/AzureMediaBlobStore.cs` — class `AzureMediaBlobStore`
  - `EnsureContainersAsync()`:20, `GetUploadedAsync()`:32, `DownloadQuarantineAsync()`:47, `UploadFileAsync()`:70, `DeleteQuarantineAsync()`:81
- `src/Knock.Market.Api/Modules/Media/Services/IMediaBlobStore.cs` — record `UploadedBlob`, interface `IMediaBlobStore`
- `src/Knock.Market.Api/Modules/Media/Services/ImageProcessor.cs` — class `ImageProcessor`
  - `ProcessAsync()`:21, `ProcessAsync()`:24, `RunLimitedAsync()`:28, `Dispose()`:42
- `src/Knock.Market.Api/Modules/Media/Services/InMemoryMediaBlobStore.cs` — class `InMemoryMediaBlobStore`
  - `EnsureContainersAsync()`:12, `UploadRaw()`:14, `ContainsQuarantine()`:21, `GetBlob()`:23, `ClearQuarantine()`:26, `GetUploadedAsync()`:28, `DownloadQuarantineAsync()`:33, `UploadFileAsync()`:44, `DeleteQuarantineAsync()`:53
- `src/Knock.Market.Api/Modules/Media/Services/MediaApi.cs` — class `MediaApi`
  - `IsReadyAndOwnedByAsync()`:8
- `src/Knock.Market.Api/Modules/Media/Services/MediaService.cs` — record `UploadTicket`, record `ConfirmedFile`, class `MediaService`
  - `MediaService()`:18, `CreateUploadAsync()`:25, `ConfirmAsync()`:41, `IsReadyAndOwnedByAsync()`:95

**validators**

- `src/Knock.Market.Api/Modules/Media/Validators/CreateUploadRequestValidator.cs` — class `CreateUploadRequestValidator`

### Requests

**-**

- `src/Knock.Market.Api/Modules/Requests/IRequestsApi.cs` — interface `IRequestsApi`
- `src/Knock.Market.Api/Modules/Requests/RequestsModuleExtensions.cs` — class `RequestsModuleExtensions`
  - `AddRequestsModule()`:18

**controllers**

- `src/Knock.Market.Api/Modules/Requests/Controllers/BusinessRuleController.cs` — class `BusinessRuleController`
  - `Subject()`:15, `Rule()`:18
- `src/Knock.Market.Api/Modules/Requests/Controllers/PrivateNoStoreAttribute.cs` — class `PrivateNoStoreAttribute`
  - `OnActionExecuting()`:15
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/ArrivalController.cs` — class `ArrivalController`
  - `Arrive()`:40
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/ChooseQuoteController.cs` — class `ChooseQuoteController`
  - `Choose()`:36
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/ConversationPermissionController.cs` — class `ConversationPermissionController`
  - `Get()`:31
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/CotizacionesController.cs` — record `QuoteRequest`, class `CotizacionesController`
  - `Send()`:35
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/QuoteComparisonController.cs` — class `QuoteComparisonController`
  - `Compare()`:33
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/SolicitudesController.cs` — class `SolicitudesController`
  - `Create()`:38, `List()`:61, `Get()`:71
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/TrustNoticeController.cs` — class `TrustNoticeController`
  - `Prepare()`:29
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/VisitasController.cs` — class `VisitasController`
  - `Get()`:27

**data**

- `src/Knock.Market.Api/Modules/Requests/Data/ChoiceStore.cs` — record `ChosenVisit`, record `ChooseResult`, record `TechnicianPublic`, class `ChoiceStore`
  - `ListQuotesAsync()`:56, `FindTechniciansAsync()`:69, `TryFindTechnicianAsync()`:91, `FindVisitIdsAsync()`:104, `ChooseAsync()`:121
- `src/Knock.Market.Api/Modules/Requests/Data/ConversationPermissionStore.cs` — record `ConversationTechnician`, class `ConversationPermissionStore`
  - `FindTechnicianAsync()`:29, `FindTechnicianBySubAsync()`:32, `FindQuoteStateAsync()`:36, `FindVisitIdAsync()`:47
- `src/Knock.Market.Api/Modules/Requests/Data/DemoRequestStore.cs` — class `DemoRequestStore`
  - `ValidateAsync()`:20, `SaveAsync()`:26
- `src/Knock.Market.Api/Modules/Requests/Data/DemoTechnicianGuard.cs` — class `DemoTechnicianGuard`
  - `ValidateAsync()`:9
- `src/Knock.Market.Api/Modules/Requests/Data/DemoTechnicianStore.cs` — class `DemoTechnicianStore`
  - `ValidateAsync()`:14, `SaveAsync()`:17
- `src/Knock.Market.Api/Modules/Requests/Data/MongoOutbox.cs` — record `OutboxMessage`, interface `IOutbox`, class `MongoOutbox`
  - `OutboxMessage()`:10, `Create()`:12, `AddAsync()`:29
- `src/Knock.Market.Api/Modules/Requests/Data/QuoteReaders.cs` — class `MongoTechnicianDirectory`, class `MongoQuoteRequestReader`
  - `FindBySubAsync()`:16, `FindEligibleAsync()`:19, `ExistsAsync()`:60, `FindAsync()`:74
- `src/Knock.Market.Api/Modules/Requests/Data/QuoteStore.cs` — class `QuoteStore`
  - `SaveAsync()`:21, `SaveAsync()`:24
- `src/Knock.Market.Api/Modules/Requests/Data/RequestsStore.cs` — record `RequestRecord`, record `RequestRecordPage`, class `RequestsStore`
  - `InsertAsync()`:52, `FindAsync()`:66, `CountLiveQuotesAsync()`:102, `HasQuotedAsync()`:118
- `src/Knock.Market.Api/Modules/Requests/Data/VisitStore.cs` — record `VisitRecord`, record `FailedAttempt`, enum `VisitCloseReason`, class `VisitStore`
  - `VisitRecord()`:10, `FindAsync()`:65, `MarkArrivedAsync()`:75, `RegisterFailedAttemptAsync()`:99, `CloseAsync()`:133, `CountNoticeAsync()`:162

**domain**

- `src/Knock.Market.Api/Modules/Requests/Domain/ChoiceRules.cs` — class `ChoiceRules`
  - `Block()`:10
- `src/Knock.Market.Api/Modules/Requests/Domain/ChooseStatus.cs` — enum `ChooseStatus`
- `src/Knock.Market.Api/Modules/Requests/Domain/ConversationRules.cs` — class `ConversationClosures`, record struct `ConversationOpening`, class `ConversationRules`
  - `ConversationOpening()`:14, `Closed()`:18, `Evaluate()`:29
- `src/Knock.Market.Api/Modules/Requests/Domain/OleadasOptions.cs` — class `OleadasOptions`
  - `IsValid()`:17
- `src/Knock.Market.Api/Modules/Requests/Domain/QuoteComparer.cs` — record `QuoteToCompare`, record `ComparedQuote`, record `QuoteComparisonResult`, class `QuoteComparer`
  - `QuoteToCompare()`:6, `Compare()`:58, `Covers()`:69
- `src/Knock.Market.Api/Modules/Requests/Domain/QuotePricing.cs` — record `QuotePricingOptions`, record struct `QuoteCap`, class `QuotePricing`
  - `Calculate()`:37
- `src/Knock.Market.Api/Modules/Requests/Domain/RequestCursor.cs` — class `RequestCursor`
  - `Encode()`:17, `Decode()`:21
- `src/Knock.Market.Api/Modules/Requests/Domain/RequestErrorCodes.cs` — class `RequestErrorCodes`
- `src/Knock.Market.Api/Modules/Requests/Domain/RequestRules.cs` — class `RequestRules`
  - `ExpiresAt()`:17, `AcceptsQuotes()`:25, `AfterFirstQuote()`:28, `IsLive()`:32, `CanAddQuote()`:34
- `src/Knock.Market.Api/Modules/Requests/Domain/RequestStates.cs` — enum `RequestState`, enum `QuoteState`, enum `PriceType`, class `RequestStates`
  - `ToStored()`:35, `ToStored()`:47, `ParseRequest()`:56, `ParseQuote()`:68
- `src/Knock.Market.Api/Modules/Requests/Domain/SeleccionDeOleada.cs` — record `WaveCandidate`, record `WaveSelection`, class `SeleccionDeOleada`
  - `Select()`:12
- `src/Knock.Market.Api/Modules/Requests/Domain/VisitRules.cs` — class `VisitErrorCodes`, enum `ArrivalBlock`, record `VisitState`, class `VisitRules`
  - `WindowStart()`:48, `CanArrive()`:55, `TechnicianMaySeeAddress()`:68, `CanNotify()`:71, `CodeHiddenReason()`:76, `BlockForArrival()`:100, `SafetyRule()`:136, `TrustNotice()`:143

**dtos**

- `src/Knock.Market.Api/Modules/Requests/Dtos/CreateRequestBody.cs` — record `CreateRequestBody`
  - `CreateRequestBody()`:12
- `src/Knock.Market.Api/Modules/Requests/Dtos/ListRequestsQuery.cs` — class `ListRequestsQuery`
- `src/Knock.Market.Api/Modules/Requests/Dtos/QuoteComparisonDtos.cs` — record `QuoteTechnician`, record `QuoteComparisonItem`, record `QuoteComparison`, class `QuoteComparisonQuery`, record `ChooseQuoteBody`, record `ChosenTechnician`, record `AppointmentPlace`, record `Appointment`
  - `ChooseQuoteBody()`:53
- `src/Knock.Market.Api/Modules/Requests/Dtos/RequestResponses.cs` — record `RequestTrade`, record `RequestNeighborhood`, record `RequestPhoto`, record `RequestResponse`, record `RequestSummary`, record `RequestPage`
- `src/Knock.Market.Api/Modules/Requests/Dtos/VisitDtos.cs` — record `VisitTechnician`, record `VisitCustomer`, record `VisitPlace`, record `VisitResponse`, record `ArrivalBody`, record `TrustNotice`
  - `ArrivalBody()`:41

**services**

- `src/Knock.Market.Api/Modules/Requests/Services/ArrivalService.cs` — enum `ArrivalStatus`, record `ArrivalOutcome`, class `ArrivalService`
  - `ArrivalService()`:32, `ArriveAsync()`:34
- `src/Knock.Market.Api/Modules/Requests/Services/ChooseQuoteService.cs` — record `ChooseOutcome`, class `ChooseQuoteService`
  - `ChooseQuoteService()`:18, `ChooseAsync()`:26
- `src/Knock.Market.Api/Modules/Requests/Services/ConversationPermissionService.cs` — class `ConversationPermissionService`
  - `ConversationPermissionService()`:13, `GetAsync()`:22
- `src/Knock.Market.Api/Modules/Requests/Services/DemoRequestSeeder.cs` — class `DemoRequestSeeder`
  - `DemoRequestSeeder()`:12, `SeedAsync()`:26, `Validate()`:46
- `src/Knock.Market.Api/Modules/Requests/Services/DemoTechnicianSeeder.cs` — class `DemoTechnicianSeeder`
  - `DemoTechnicianSeeder()`:7, `SeedAsync()`:17
- `src/Knock.Market.Api/Modules/Requests/Services/DemoTechnicianStartup.cs` — class `DemoTechnicianStartup`
  - `ShouldRun()`:5, `StartAsync()`:7, `StopAsync()`:18
- `src/Knock.Market.Api/Modules/Requests/Services/QuoteComparisonService.cs` — class `QuoteComparisonService`
  - `QuoteComparisonService()`:13, `CompareAsync()`:19
- `src/Knock.Market.Api/Modules/Requests/Services/QuoteService.cs` — record `Technician`, record `QuoteRequestSnapshot`, interface `ITechnicianDirectory`, interface `IQuoteRequestReader`, enum `QuoteCheckResult`, interface `IQuoteWriter`, record `QuoteResponse`, interface `ICreatedQuote`, enum `NoteModerationResult`, interface `INoteModerator`, class `UnavailableNoteModerator`, class `QuoteService`
  - `ModerateAsync()`:48, `QuoteService()`:51, `SendAsync()`:55, `Covers()`:83
- `src/Knock.Market.Api/Modules/Requests/Services/RequestPorts.cs` — interface `IRequestPhotos`, class `UnavailableRequestPhotos`, class `UnavailableContentModerator`
  - `AreReadyForAsync()`:22, `ReadLinksAsync()`:24, `ReviewTextAsync()`:31, `ReviewImageAsync()`:34
- `src/Knock.Market.Api/Modules/Requests/Services/RequestsApi.cs` — class `RequestsApi`
- `src/Knock.Market.Api/Modules/Requests/Services/SolicitudService.cs` — enum `CreateRequestStatus`, record `CreateRequestOutcome`, class `SolicitudService`
  - `SolicitudService()`:31, `CreateAsync()`:46, `ListAsync()`:115, `GetAsync()`:142, `AddressContext()`:208
- `src/Knock.Market.Api/Modules/Requests/Services/TrustNoticeService.cs` — enum `TrustNoticeStatus`, record `TrustNoticeOutcome`, class `TrustNoticeService`
  - `TrustNoticeService()`:21, `PrepareAsync()`:23
- `src/Knock.Market.Api/Modules/Requests/Services/VisitCodeFactory.cs` — record `VisitCode`, class `VisitCodeFactory`
  - `VisitCodeFactory()`:15, `Create()`:19, `Context()`:26, `ExpiresAt()`:28
- `src/Knock.Market.Api/Modules/Requests/Services/VisitService.cs` — class `VisitService`
  - `VisitService()`:15, `GetAsync()`:21, `TechnicianViewAsync()`:28, `FindAsync()`:35

**validators**

- `src/Knock.Market.Api/Modules/Requests/Validators/CreateRequestBodyValidator.cs` — class `CreateRequestBodyValidator`
- `src/Knock.Market.Api/Modules/Requests/Validators/ListRequestsQueryValidator.cs` — class `ListRequestsQueryValidator`
- `src/Knock.Market.Api/Modules/Requests/Validators/QuoteChoiceValidators.cs` — class `QuoteComparisonQueryValidator`, class `ChooseQuoteBodyValidator`, class `ArrivalBodyValidator`
- `src/Knock.Market.Api/Modules/Requests/Validators/QuoteValidator.cs` — record `QuoteValidationInput`, class `QuoteValidator`

### Safety

**-**

- `src/Knock.Identity.Api/Modules/Safety/SafetyModuleExtensions.cs` — class `SafetyModuleExtensions`
  - `AddSafetyModule()`:11

**controllers**

- `src/Knock.Identity.Api/Modules/Safety/Controllers/V1/BloqueosController.cs` — class `BloqueosController`
  - `Block()`:30, `Unblock()`:48, `List()`:62
- `src/Knock.Identity.Api/Modules/Safety/Controllers/V1/ReportesController.cs` — class `ReportesController`
  - `Create()`:35, `List()`:54, `Appeal()`:71

**data**

- `src/Knock.Identity.Api/Modules/Safety/Data/BloqueosStore.cs` — record `StoredBlock`, class `BloqueosStore`
  - `EnsureIndexesAsync()`:31, `AddAsync()`:42, `RemoveAsync()`:68, `ListAsync()`:78
- `src/Knock.Identity.Api/Modules/Safety/Data/ReportesStore.cs` — record `StoredReport`, record `NewReport`, class `ReportesStore`
  - `EnsureIndexesAsync()`:36, `FindOpenDuplicateAsync()`:46, `InsertAsync()`:55, `ListByReporterAsync()`:78, `FindAsync()`:91, `AppealAsync()`:100

**domain**

- `src/Knock.Identity.Api/Modules/Safety/Domain/ReportRules.cs` — class `ReportStates`, class `ReportReferenceKinds`, class `ReportRules`
  - `NoticeFor()`:37, `IsValidText()`:40, `CanBeAppealed()`:46
- `src/Knock.Identity.Api/Modules/Safety/Domain/SafetyCursor.cs` — class `SafetyCursor`
  - `Parse()`:12
- `src/Knock.Identity.Api/Modules/Safety/Domain/SafetyErrorCodes.cs` — class `SafetyErrorCodes`

**dtos**

- `src/Knock.Identity.Api/Modules/Safety/Dtos/BlockDtos.cs` — record `BlockedPerson`, record `BlockedPage`, class `BlocksQuery`, class `BlockRoute`
- `src/Knock.Identity.Api/Modules/Safety/Dtos/ReportDtos.cs` — class `ReportReferenceBody`, class `CreateReportBody`, class `AppealBody`, class `ReportRoute`, class `ReportsQuery`, record `ReportCreated`, record `ReportSummary`, record `ReportPage`, record `AppealResult`

**services**

- `src/Knock.Identity.Api/Modules/Safety/Services/BlockService.cs` — enum `BlockOutcome`, class `BlockService`
  - `BlockService()`:21, `BlockAsync()`:26, `UnblockAsync()`:48, `ListAsync()`:57
- `src/Knock.Identity.Api/Modules/Safety/Services/ReportService.cs` — enum `ReportOutcome`, record `ReportCreation`, class `ReportService`
  - `ReportCreation()`:20, `ToString()`:22, `ReportService()`:30, `CreateAsync()`:36, `ListAsync()`:70, `AppealAsync()`:84
- `src/Knock.Identity.Api/Modules/Safety/Services/UnavailableContentModerator.cs` — class `UnavailableContentModerator`
  - `ReviewTextAsync()`:13, `ReviewImageAsync()`:15

**validators**

- `src/Knock.Identity.Api/Modules/Safety/Validators/BlockValidators.cs` — class `BlocksQueryValidator`, class `BlockRouteValidator`
- `src/Knock.Identity.Api/Modules/Safety/Validators/ReportValidators.cs` — class `CreateReportBodyValidator`, class `ReportsQueryValidator`, class `AppealRequestValidator`, class `AppealBodyValidator`

### Sesiones

**-**

- `src/Knock.Identity.Api/Modules/Sesiones/SesionesModuleExtensions.cs` — class `SesionesModuleExtensions`
  - `AddSesionesModule()`:11

**controllers**

- `src/Knock.Identity.Api/Modules/Sesiones/Controllers/V1/AuthController.cs` — class `AuthController`
  - `ExchangeFirebaseToken()`:30
- `src/Knock.Identity.Api/Modules/Sesiones/Controllers/V1/LogoutController.cs` — class `LogoutController`
  - `Logout()`:20
- `src/Knock.Identity.Api/Modules/Sesiones/Controllers/V1/MeSesionesController.cs` — class `MeSesionesController`
  - `List()`:22, `Revoke()`:42
- `src/Knock.Identity.Api/Modules/Sesiones/Controllers/V1/RefreshController.cs` — class `RefreshController`
  - `Refresh()`:27

**data**

- `src/Knock.Identity.Api/Modules/Sesiones/Data/ISesionesStore.cs` — interface `ISesionesStore`
- `src/Knock.Identity.Api/Modules/Sesiones/Data/SesionCancelada.cs` — interface `ISesionCancelada`, class `RedisSesionCancelada`
  - `Key()`:20, `CancelAsync()`:22
- `src/Knock.Identity.Api/Modules/Sesiones/Data/SesionesDataExtensions.cs` — class `SesionesDataExtensions`
  - `AddSesionesData()`:11
- `src/Knock.Identity.Api/Modules/Sesiones/Data/SesionesStore.cs` — class `SesionesStore`
  - `EnsureIndexesAsync()`:82, `FindBySubAsync()`:100, `FindByPhoneHashAsync()`:105, `CreateAccountAsync()`:113, `AddSessionAsync()`:154, `FindOpenSessionsAsync()`:189, `FindSessionByOwnerAsync()`:203, `MarkSessionRevokedAsync()`:215, `FindByIdAsync()`:252, `FindSessionByFingerprintAsync()`:263, `FindSessionByUsedFingerprintAsync()`:267, `RotateAsync()`:272, `RevokeAllAsync()`:296, `RevokeAsync()`:319, `IsDuplicateKey()`:333

**domain**

- `src/Knock.Identity.Api/Modules/Sesiones/Domain/PhoneNumberPolicy.cs` — class `PhoneNumberPolicy`
  - `IsValid()`:12
- `src/Knock.Identity.Api/Modules/Sesiones/Domain/SesionesDomain.cs` — class `AccountState`, class `ConsentDocuments`, record `UserAccount`, record `NewSession`, record `StoredSession`, record `NewAccount`, enum `AccountCreation`
  - `NewSession()`:34, `ToString()`:37, `NewAccount()`:57, `ToString()`:70

**dtos**

- `src/Knock.Identity.Api/Modules/Sesiones/Dtos/SessionDtos.cs` — record `FirebaseExchangeRequest`, record `SessionResponse`, record `RefreshRequest`, record `RefreshResponse`, record `SessionListItemResponse`

**services**

- `src/Knock.Identity.Api/Modules/Sesiones/Services/FirebaseIdTokenVerifier.cs` — class `FirebaseIdTokenVerifier`
  - `VerifyAsync()`:17, `Dispose()`:37
- `src/Knock.Identity.Api/Modules/Sesiones/Services/IFirebaseIdTokenVerifier.cs` — record `FirebaseIdentity`, interface `IFirebaseIdTokenVerifier`
  - `FirebaseIdentity()`:7, `ToString()`:9
- `src/Knock.Identity.Api/Modules/Sesiones/Services/LoginService.cs` — record `LoginRequest`, record `LoginResult`, class `ConsentVersionsOptions`, class `LoginService`
  - `LoginService()`:30, `LoginAsync()`:44
- `src/Knock.Identity.Api/Modules/Sesiones/Services/LogoutService.cs` — class `LogoutService`
  - `LogoutService()`:9, `LogoutAsync()`:20
- `src/Knock.Identity.Api/Modules/Sesiones/Services/PhoneProtectionOptions.cs` — class `PhoneProtectionOptions`, class `PhoneProtectionKeys`, class `PhoneProtectionOptionsValidator`
  - `TryCreate()`:56, `Validate()`:138
- `src/Knock.Identity.Api/Modules/Sesiones/Services/PhoneProtector.cs` — record `ProtectedPhone`, record `PhoneHash`, class `PhoneProtector`
  - `PhoneProtector()`:19, `Protect()`:29, `Hash()`:53, `AssociatedData()`:60
- `src/Knock.Identity.Api/Modules/Sesiones/Services/RefreshTokenFingerprint.cs` — class `RefreshTokenFingerprint`
  - `Compute()`:15, `TryCompute()`:36
- `src/Knock.Identity.Api/Modules/Sesiones/Services/SessionManagementService.cs` — class `SessionManagementService`
  - `SessionManagementService()`:9, `ListOpenSessionsAsync()`:15, `RevokeSessionAsync()`:37
- `src/Knock.Identity.Api/Modules/Sesiones/Services/SessionRefreshService.cs` — record `RefreshResult`, class `SessionRefreshService`
  - `SessionRefreshService()`:13, `RefreshAsync()`:20
- `src/Knock.Identity.Api/Modules/Sesiones/Services/SessionTokenIssuer.cs` — record `IssuedSession`, class `SessionTokenIssuer`
  - `SessionTokenIssuer()`:15, `Issue()`:21
- `src/Knock.Identity.Api/Modules/Sesiones/Services/UserIdDerivation.cs` — class `UserIdDerivation`
  - `Derive()`:18

**validators**

- `src/Knock.Identity.Api/Modules/Sesiones/Validators/FirebaseExchangeRequestValidator.cs` — class `FirebaseExchangeRequestValidator`
- `src/Knock.Identity.Api/Modules/Sesiones/Validators/RefreshRequestValidator.cs` — class `RefreshRequestValidator`

### Tecnicos

**-**

- `src/Knock.Market.Api/Modules/Tecnicos/TecnicosModuleExtensions.cs` — class `TecnicosModuleExtensions`
  - `AddTecnicosModule()`:15

**controllers**

- `src/Knock.Market.Api/Modules/Tecnicos/Controllers/TechnicianProblems.cs` — class `TechnicianProblems`
  - `Rule()`:12
- `src/Knock.Market.Api/Modules/Tecnicos/Controllers/V1/TecnicosController.cs` — class `TecnicosController`
  - `Register()`:38, `Profile()`:67, `Autocomplete()`:81, `Coverage()`:95
- `src/Knock.Market.Api/Modules/Tecnicos/Controllers/V1/TecnicosMeController.cs` — class `TecnicosMeController`
  - `Update()`:32, `Availability()`:54, `Schedule()`:67, `ReplaceSchedule()`:84

**data**

- `src/Knock.Market.Api/Modules/Tecnicos/Data/TechnicianLookupStore.cs` — class `TechnicianLookupStore`
  - `FindCoverageAsync()`:19, `ToCoverage()`:28, `FindBusinessNamesAsync()`:45
- `src/Knock.Market.Api/Modules/Tecnicos/Data/TechnicianProfileStore.cs` — class `TechnicianProfileStore`
  - `ExistsAsync()`:22, `InsertAsync()`:26, `FindByIdAsync()`:46, `ToProfile()`:54, `ToDocument()`:78
- `src/Knock.Market.Api/Modules/Tecnicos/Data/TechnicianSelfStore.cs` — class `TechnicianSelfStore`
  - `FindOwnAsync()`:26, `ReadOwnAsync()`:37, `SetAvailableUntilAsync()`:60, `ReadScheduleAsync()`:63, `SetScheduleAsync()`:71, `UpdateAsync()`:82
- `src/Knock.Market.Api/Modules/Tecnicos/Data/TrabajoStore.cs` — record `TrabajoVerification`, class `TrabajoStore`
  - `TrabajoStore()`:10, `ReadVerificationAsync()`:12

**domain**

- `src/Knock.Market.Api/Modules/Tecnicos/Domain/ScheduleRules.cs` — class `ScheduleRules`
  - `EndOfCityDayUtc()`:14, `AllClosed()`:23, `TryParseTime()`:25, `IsValid()`:31, `Normalize()`:61
- `src/Knock.Market.Api/Modules/Tecnicos/Domain/TechnicianRules.cs` — class `TechnicianRules`
  - `IsVisible()`:30, `IsKnownAttention()`:33, `UsesWorkshop()`:35, `VisitsHomes()`:37

**dtos**

- `src/Knock.Market.Api/Modules/Tecnicos/Dtos/MyTechnicianDtos.cs` — record `AvailabilityBody`, record `AvailabilityResponse`, record `UpdateTechnicianBody`, record `DayScheduleDto`, record `TimeRangeDto`, record `ScheduleBody`
  - `AvailabilityBody()`:4, `UpdateTechnicianBody()`:17, `ScheduleBody()`:39
- `src/Knock.Market.Api/Modules/Tecnicos/Dtos/RegisterTechnicianBody.cs` — record `RegisterTechnicianBody`, record `TechnicianProfileBody`, record `TechnicianResponse`
  - `RegisterTechnicianBody()`:13
- `src/Knock.Market.Api/Modules/Tecnicos/Dtos/TechnicianLookupDtos.cs` — class `CoverageQuery`, record `CoverageResponse`, class `AutocompleteQuery`, record `Suggestion`, record `AutocompleteResponse`
- `src/Knock.Market.Api/Modules/Tecnicos/Dtos/TechnicianProfileDtos.cs` — class `TechnicianIdRoute`, record `TechnicianProfileResponse`, record `TechnicianProfileDetail`

**services**

- `src/Knock.Market.Api/Modules/Tecnicos/Services/MyTechnicianService.cs` — class `TecnicosOptions`, record `OwnTechnician`, interface `ITechnicianSelfStore`, enum `UpdateTechnicianStatus`, record `UpdateTechnicianOutcome`, class `MyTechnicianService`
  - `MyTechnicianService()`:52, `SetAvailabilityAsync()`:61, `GetScheduleAsync()`:76, `SetScheduleAsync()`:88, `UpdateAsync()`:102
- `src/Knock.Market.Api/Modules/Tecnicos/Services/TechnicianLookupService.cs` — record `TechnicianCoverage`, record `BusinessName`, interface `ITechnicianLookupReader`, enum `CoverageStatus`, record `CoverageOutcome`, class `TechnicianLookupService`
  - `TechnicianLookupService()`:47, `CoverageAsync()`:61, `AutocompleteAsync()`:104, `Normalize()`:130, `DistanceKm()`:142
- `src/Knock.Market.Api/Modules/Tecnicos/Services/TechnicianProfileService.cs` — record `TechnicianProfileRecord`, interface `ITechnicianProfileReader`, class `TechnicianProfileService`
  - `TechnicianProfileService()`:37, `CacheKey()`:44, `GetAsync()`:46, `Forget()`:73, `IsVisible()`:75, `Shorten()`:78
- `src/Knock.Market.Api/Modules/Tecnicos/Services/TechnicianRegistrationService.cs` — enum `RegisterTechnicianStatus`, record `RegisterTechnicianOutcome`, record `NewTechnician`, interface `ITechnicianProfileStore`, class `TechnicianRegistrationService`
  - `TechnicianRegistrationService()`:47, `RegisterAsync()`:54

**validators**

- `src/Knock.Market.Api/Modules/Tecnicos/Validators/MyTechnicianValidators.cs` — class `AvailabilityBodyValidator`, class `ScheduleBodyValidator`, class `UpdateTechnicianBodyValidator`
- `src/Knock.Market.Api/Modules/Tecnicos/Validators/RegisterTechnicianBodyValidator.cs` — class `RegisterTechnicianBodyValidator`
- `src/Knock.Market.Api/Modules/Tecnicos/Validators/TechnicianIdRouteValidator.cs` — class `TechnicianIdRouteValidator`
- `src/Knock.Market.Api/Modules/Tecnicos/Validators/TechnicianLookupValidators.cs` — class `CoverageQueryValidator`, class `AutocompleteQueryValidator`

### Usuarios

**-**

- `src/Knock.Identity.Api/Modules/Usuarios/IUsuariosApi.cs` — record `PublicUser`, interface `IUsuariosApi`
- `src/Knock.Identity.Api/Modules/Usuarios/UsuariosModuleExtensions.cs` — class `UsuariosModuleExtensions`
  - `AddUsuariosModule()`:12

**controllers**

- `src/Knock.Identity.Api/Modules/Usuarios/Controllers/V1/MeController.cs` — class `MeController`
  - `Get()`:31, `Put()`:56, `ETag()`:74, `Matches()`:81

**data**

- `src/Knock.Identity.Api/Modules/Usuarios/Data/DemoSeedGuard.cs` — class `DemoSeedGuard`
  - `ValidateAsync()`:8
- `src/Knock.Identity.Api/Modules/Usuarios/Data/ProfileCache.cs` — interface `IProfileCache`, class `DemoProfileCache`, class `RedisProfileCache`
  - `TryRemoveAsync()`:21, `Key()`:42, `GetAsync()`:44, `SetAsync()`:59, `RemoveAsync()`:73
- `src/Knock.Identity.Api/Modules/Usuarios/Data/ProfileCacheOptions.cs` — class `ProfileCacheOptions`, class `ProfileCacheOptionsValidator`
  - `Validate()`:25
- `src/Knock.Identity.Api/Modules/Usuarios/Data/UsuariosStore.cs` — interface `IUsuariosStore`, class `UsuariosStore`, class `BsonValueExtensions`
  - `ValidateDemoAsync()`:60, `DemoConflictAsync()`:63, `UpsertDemoAsync()`:83, `FindProfileAsync()`:129, `UpdateProfileAsync()`:133, `AsNullableString()`:189

**domain**

- `src/Knock.Identity.Api/Modules/Usuarios/Domain/UsuariosDomain.cs` — class `ProfileStates`, record `UserProfile`, record `ProfileUpdate`, enum `ProfileUpdateOutcome`
  - `UserProfile()`:13, `ToString()`:26, `ProfileUpdate()`:30, `ToString()`:32

**dtos**

- `src/Knock.Identity.Api/Modules/Usuarios/Dtos/ProfileDtos.cs` — record `UpdateProfileRequest`, record `ProfileResponse`

**services**

- `src/Knock.Identity.Api/Modules/Usuarios/Services/DemoClientSeeder.cs` — class `DemoClientSeeder`
  - `DemoClientSeeder()`:10, `SeedAsync()`:14
- `src/Knock.Identity.Api/Modules/Usuarios/Services/DemoSeedStartup.cs` — class `DemoSeedStartup`
  - `ShouldRun()`:5, `StartAsync()`:8, `StopAsync()`:17
- `src/Knock.Identity.Api/Modules/Usuarios/Services/MarketColoniaCatalog.cs` — class `MarketColoniaCatalog`, record `ExistsResponse`, class `MarketCatalogOptions`
  - `ExistsAsync()`:16, `HasValidBaseUrl()`:65
- `src/Knock.Identity.Api/Modules/Usuarios/Services/ProfileRules.cs` — class `ProfileOptions`, class `ProfileRules`
  - `ProfileRules()`:19, `Validate()`:36
- `src/Knock.Identity.Api/Modules/Usuarios/Services/ProfileService.cs` — class `ProfileService`
  - `ProfileService()`:12, `GetAsync()`:23, `UpdateAsync()`:36
- `src/Knock.Identity.Api/Modules/Usuarios/Services/UsuariosApi.cs` — class `UsuariosApi`
  - `FindPublicUserAsync()`:8
