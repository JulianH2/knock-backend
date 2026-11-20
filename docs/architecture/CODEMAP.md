# Mapa de código

> Generado por `scripts/codemap.py` el 2026-11-20T17:39:35Z. No editar a mano.
> **Antes de crear una clase, método o archivo, busca aquí o corre `python scripts/codemap.py --query "<qué necesitas>"`.**

## Resumen

| Módulo | Capas | Archivos | Tipos | Métodos públicos |
|---|---|---|---|---|
| `Account` | -, controllers, data, domain, dtos, services, validators | 28 | 63 | 60 |
| `Addresses` | -, controllers, data, domain, dtos, services, validators | 12 | 25 | 46 |
| `Assistant` | data, services | 3 | 13 | 8 |
| `Catalog` | -, controllers, data, domain, dtos, services, validators | 49 | 87 | 113 |
| `Chat` | -, data, hubs, services | 6 | 18 | 26 |
| `Conversaciones` | domain | 1 | 2 | 1 |
| `HomeAndCar` | -, controllers, data, domain, dtos, services, validators | 8 | 32 | 29 |
| `Knock.Contracts` | - | 9 | 16 | 7 |
| `Knock.Gateway` | -, hosting | 15 | 40 | 51 |
| `Knock.Identity.Api` | -, common, hosting | 33 | 67 | 95 |
| `Knock.Market.Api` | -, common, hosting | 34 | 68 | 100 |
| `Knock.Messaging.Api` | -, hosting | 15 | 38 | 48 |
| `Media` | -, data, domain, dtos, services, validators | 23 | 40 | 66 |
| `Requests` | -, controllers, data, domain, dtos, services, validators | 63 | 135 | 126 |
| `Safety` | -, controllers, data, domain, dtos, services, validators | 15 | 38 | 33 |
| `Sesiones` | -, controllers, data, domain, dtos, services, validators | 26 | 47 | 54 |
| `Tecnicos` | -, controllers, data, domain, dtos, services, validators | 26 | 67 | 68 |
| `Trust` | -, controllers, data, domain, dtos, services, validators | 7 | 17 | 16 |
| `Usuarios` | -, controllers, data, domain, dtos, services | 16 | 29 | 37 |

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
- `src/Knock.Gateway/Hosting/RateLimitingExtensions.cs:53 RedisRequestCounter.HitAsync` · `src/Knock.Identity.Api/Hosting/RedisRequestCounter.cs:16 RedisRequestCounter.HitAsync` · `src/Knock.Market.Api/Hosting/RedisRequestCounter.cs:16 RedisRequestCounter.HitAsync`
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
- `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:56 AuthenticationExtensions.AddInternalControllers` · `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:57 AuthenticationExtensions.AddInternalControllers`
- `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:80 InternalControllerFeatureProvider.IsInternalController` · `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:81 InternalControllerFeatureProvider.IsInternalController`
- `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:91 AuthenticationStartupCheck.StartAsync` · `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:92 AuthenticationStartupCheck.StartAsync`
- `src/Knock.Identity.Api/Hosting/CorrelationIdMiddleware.cs:31 CorrelationIdMiddleware` · `src/Knock.Market.Api/Hosting/CorrelationIdMiddleware.cs:30 CorrelationIdMiddleware`
- `src/Knock.Identity.Api/Hosting/CorrelationIdMiddleware.cs:33 CorrelationIdMiddleware.InvokeAsync` · `src/Knock.Market.Api/Hosting/CorrelationIdMiddleware.cs:32 CorrelationIdMiddleware.InvokeAsync`
- `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs:25 CorsMiddleware.InvokeAsync` · `src/Knock.Market.Api/Hosting/CorsMiddleware.cs:24 CorsMiddleware.InvokeAsync`
- `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs:51 CorsMiddleware.AnswerPreflightAsync` · `src/Knock.Market.Api/Hosting/CorsMiddleware.cs:50 CorsMiddleware.AnswerPreflightAsync`
- `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs:69 CorsMiddleware.IsGranted` · `src/Knock.Market.Api/Hosting/CorsMiddleware.cs:68 CorsMiddleware.IsGranted`
- `src/Knock.Identity.Api/Hosting/CorsMiddleware.cs:96 CorsMiddleware.AddVaryOrigin` · `src/Knock.Market.Api/Hosting/CorsMiddleware.cs:95 CorsMiddleware.AddVaryOrigin`
- `src/Knock.Identity.Api/Hosting/DomainExceptionHandler.cs:15 DomainExceptionHandler.TryHandleAsync` · `src/Knock.Market.Api/Hosting/DomainExceptionHandler.cs:14 DomainExceptionHandler.TryHandleAsync`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:22 JsonCompressionMiddleware` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:20 JsonCompressionMiddleware`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:29 JsonCompressionMiddleware.InvokeAsync` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:27 JsonCompressionMiddleware.InvokeAsync`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:107 JsonCompressionMiddleware.WriteBufferedAsync` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:105 JsonCompressionMiddleware.WriteBufferedAsync`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:154 JsonCompressionMiddleware.AddVaryAcceptEncoding` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:152 JsonCompressionMiddleware.AddVaryAcceptEncoding`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:181 CappedBufferStream.WriteAsync` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:179 CappedBufferStream.WriteAsync`
- `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:200 CappedBufferStream.WriteCore` · `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:198 CappedBufferStream.WriteCore`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:76 KnockMongoOptionsValidator.Validate` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:74 KnockMongoOptionsValidator.Validate` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:76 KnockMongoOptionsValidator.Validate`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:149 MongoExtensions.AddKnockMongo` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:147 MongoExtensions.AddKnockMongo` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:149 MongoExtensions.AddKnockMongo`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:171 MongoExtensions.AddKnockMongoDatabase` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:169 MongoExtensions.AddKnockMongoDatabase` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:171 MongoExtensions.AddKnockMongoDatabase`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:185 MongoExtensions.CreateSettings` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:183 MongoExtensions.CreateSettings` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:185 MongoExtensions.CreateSettings`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:196 MongoExtensions.Bind` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:194 MongoExtensions.Bind` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:196 MongoExtensions.Bind`
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:229 MongoReadinessHealthCheck.CheckHealthAsync` · `src/Knock.Market.Api/Hosting/MongoExtensions.cs:227 MongoReadinessHealthCheck.CheckHealthAsync` · `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:229 MongoReadinessHealthCheck.CheckHealthAsync`
- `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:105 RateLimitingExtensions.GlobalPartition` · `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:106 RateLimitingExtensions.GlobalPartition`
- `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:140 RateLimitingExtensions.IsAuthPath` · `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:141 RateLimitingExtensions.IsAuthPath`
- `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:153 RateLimitingExtensions.ClientKey` · `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:154 RateLimitingExtensions.ClientKey` · `src/Knock.Messaging.Api/Hosting/RateLimitingExtensions.cs:99 RateLimitingExtensions.ClientKey`
- `src/Knock.Identity.Api/Hosting/RedisRequestCounter.cs:8 RedisRequestCounter` · `src/Knock.Market.Api/Hosting/RedisRequestCounter.cs:8 RedisRequestCounter`
- `src/Knock.Identity.Api/Hosting/SecurityExtensions.cs:13 SecurityExtensions.AddKnockSecurity` · `src/Knock.Market.Api/Hosting/SecurityExtensions.cs:12 SecurityExtensions.AddKnockSecurity`
- `src/Knock.Identity.Api/Hosting/SecurityHeadersMiddleware.cs:12 SecurityHeadersMiddleware` · `src/Knock.Market.Api/Hosting/SecurityHeadersMiddleware.cs:11 SecurityHeadersMiddleware`
- `src/Knock.Identity.Api/Hosting/SecurityHeadersMiddleware.cs:25 SecurityHeadersMiddleware.Apply` · `src/Knock.Market.Api/Hosting/SecurityHeadersMiddleware.cs:24 SecurityHeadersMiddleware.Apply`
- `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:50 SecuritySettingsValidator.Errors` · `src/Knock.Market.Api/Hosting/SecuritySettings.cs:49 SecuritySettingsValidator.Errors`
- `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:84 SecuritySettingsValidator.IsValidOrigin` · `src/Knock.Market.Api/Hosting/SecuritySettings.cs:83 SecuritySettingsValidator.IsValidOrigin`
- `src/Knock.Identity.Api/Hosting/SharedRateLimiter.cs:40 SharedRateLimiter.AcquireAsyncCore` · `src/Knock.Market.Api/Hosting/SharedRateLimiter.cs:40 SharedRateLimiter.AcquireAsyncCore`
- `src/Knock.Identity.Api/Hosting/ValidationExtensions.cs:70 ValidationFilter.ToCamelCasePath` · `src/Knock.Market.Api/Hosting/ValidationExtensions.cs:65 ValidationFilter.ToCamelCasePath`
- `src/Knock.Identity.Api/Hosting/ValidationExtensions.cs:74 ValidationFilter.Collect` · `src/Knock.Market.Api/Hosting/ValidationExtensions.cs:69 ValidationFilter.Collect`
- `src/Knock.Identity.Api/Modules/Usuarios/Controllers/V1/MeController.cs:81 MeController.Matches` · `src/Knock.Market.Api/Modules/Catalog/Controllers/V1/OficiosController.cs:52 OficiosController.Matches`
- `src/Knock.Identity.Api/Modules/Usuarios/Data/ProfileCacheOptions.cs:25 ProfileCacheOptionsValidator.Validate` · `src/Knock.Market.Api/Modules/Catalog/Data/CatalogCacheOptions.cs:25 CatalogCacheOptionsValidator.Validate`

## Tipos con el mismo nombre en varios lugares

- `ApiVersioningExtensions`: `src/Knock.Identity.Api/Hosting/ApiVersioningExtensions.cs:6`, `src/Knock.Market.Api/Hosting/ApiVersioningExtensions.cs:5`
- `AuthenticationExtensions`: `src/Knock.Gateway/Hosting/AuthenticationExtensions.cs:42`, `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:10`, `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:10`, `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs:28`
- `AuthenticationStartupCheck`: `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:89`, `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:90`, `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs:138`
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
- `FieldCipher`: `src/Knock.Identity.Api/Common/Crypto/FieldCipher.cs:27`, `src/Knock.Market.Api/Common/Crypto/FieldCipher.cs:25`
- `FieldEncryptionOptions`: `src/Knock.Identity.Api/Common/Crypto/FieldCipher.cs:13`, `src/Knock.Market.Api/Common/Crypto/FieldCipher.cs:12`
- `ForwardedHeadersExtensions`: `src/Knock.Identity.Api/Hosting/ForwardedHeadersExtensions.cs:7`, `src/Knock.Market.Api/Hosting/ForwardedHeadersExtensions.cs:6`, `src/Knock.Messaging.Api/Hosting/ForwardedHeadersExtensions.cs:7`
- `HealthExtensions`: `src/Knock.Gateway/Hosting/HealthExtensions.cs:10`, `src/Knock.Identity.Api/Hosting/HealthExtensions.cs:10`, `src/Knock.Market.Api/Hosting/HealthExtensions.cs:5`, `src/Knock.Messaging.Api/Hosting/HealthExtensions.cs:10`
- `HstsSettings`: `src/Knock.Gateway/Hosting/HstsExtensions.cs:10`, `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:7`, `src/Knock.Market.Api/Hosting/SecuritySettings.cs:6`
- `HttpsSettings`: `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:15`, `src/Knock.Market.Api/Hosting/SecuritySettings.cs:14`
- `IRevokedSessions`: `src/Knock.Gateway/Hosting/RevokedSessions.cs:10`, `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs:12`
- `IgnoreIncomingTraceContextPropagator`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:194`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:194`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:190`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:194`
- `InternalControllerFeatureProvider`: `src/Knock.Identity.Api/Hosting/AuthenticationExtensions.cs:72`, `src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs:73`
- `JsonCompressionMiddleware`: `src/Knock.Identity.Api/Hosting/JsonCompressionMiddleware.cs:22`, `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs:20`
- `JwtSettings`: `src/Knock.Identity.Api/Common/Security/JwtSettings.cs:7`, `src/Knock.Market.Api/Common/Security/JwtSettings.cs:6`, `src/Knock.Messaging.Api/Hosting/JwtSettings.cs:8`
- `JwtSettingsFactory`: `src/Knock.Identity.Api/Common/Security/JwtSettingsFactory.cs:11`, `src/Knock.Market.Api/Common/Security/JwtSettingsFactory.cs:10`, `src/Knock.Messaging.Api/Hosting/JwtSettings.cs:14`
- `KnockApplicationExtensions`: `src/Knock.Identity.Api/Hosting/KnockApplicationExtensions.cs:7`, `src/Knock.Market.Api/Hosting/KnockApplicationExtensions.cs:6`
- `KnockMongoOptions`: `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:16`, `src/Knock.Market.Api/Hosting/MongoExtensions.cs:14`, `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:16`
- `KnockMongoOptionsValidator`: `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:71`, `src/Knock.Market.Api/Hosting/MongoExtensions.cs:69`, `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:71`
- `KnockRedisOptions`: `src/Knock.Gateway/Hosting/RedisExtensions.cs:15`, `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:15`, `src/Knock.Market.Api/Hosting/RedisExtensions.cs:13`, `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:14`
- `KnockRedisOptionsValidator`: `src/Knock.Gateway/Hosting/RedisExtensions.cs:39`, `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:39`, `src/Knock.Market.Api/Hosting/RedisExtensions.cs:37`, `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:38`
- `LogRedactionProcessor`: `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:329`, `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:329`, `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:328`, `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:329`
- `LoggingExtensions`: `src/Knock.Gateway/Hosting/LoggingExtensions.cs:13`, `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:13`, `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:12`, `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:13`
- `ModulesExtensions`: `src/Knock.Identity.Api/Hosting/ModulesExtensions.cs:11`, `src/Knock.Market.Api/Hosting/ModulesExtensions.cs:9`
- `MongoExtensions`: `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:138`, `src/Knock.Market.Api/Hosting/MongoExtensions.cs:136`, `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:138`
- `MongoReadinessHealthCheck`: `src/Knock.Identity.Api/Hosting/MongoExtensions.cs:227`, `src/Knock.Market.Api/Hosting/MongoExtensions.cs:225`, `src/Knock.Messaging.Api/Hosting/MongoExtensions.cs:227`
- `ObservabilityExtensions`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:51`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:51`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:50`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:51`
- `ObservabilityOptions`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:22`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:22`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:21`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:22`
- `OpenApiExtensions`: `src/Knock.Identity.Api/Hosting/OpenApiExtensions.cs:7`, `src/Knock.Market.Api/Hosting/OpenApiExtensions.cs:5`
- `OpenTelemetryLogSink`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:215`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:215`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:211`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:215`
- `PrivateNoStoreAttribute`: `src/Knock.Identity.Api/Modules/Addresses/Controllers/PrivateNoStoreAttribute.cs:10`, `src/Knock.Market.Api/Hosting/PrivateNoStoreAttribute.cs:11`
- `ProblemDetailsExtensions`: `src/Knock.Identity.Api/Hosting/ProblemDetailsExtensions.cs:6`, `src/Knock.Market.Api/Hosting/ProblemDetailsExtensions.cs:5`
- `ProblemResponses`: `src/Knock.Gateway/Hosting/ProblemResponses.cs:7`, `src/Knock.Identity.Api/Hosting/RequestBodyLimitMiddleware.cs:8`, `src/Knock.Market.Api/Hosting/RequestBodyLimitMiddleware.cs:9`, `src/Knock.Messaging.Api/Hosting/ProblemResponses.cs:7`
- `RateLimitPolicySettings`: `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:12`, `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:13`, `src/Knock.Messaging.Api/Hosting/RateLimitingExtensions.cs:12`
- `RateLimitSettings`: `src/Knock.Gateway/Hosting/RateLimitingExtensions.cs:21`, `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:22`, `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:23`, `src/Knock.Messaging.Api/Hosting/RateLimitingExtensions.cs:22`
- `RateLimitingExtensions`: `src/Knock.Gateway/Hosting/RateLimitingExtensions.cs:66`, `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs:34`, `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs:35`, `src/Knock.Messaging.Api/Hosting/RateLimitingExtensions.cs:32`
- `RedactingJsonFormatter`: `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:358`, `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:358`, `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:357`, `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:358`
- `RedisExtensions`: `src/Knock.Gateway/Hosting/RedisExtensions.cs:93`, `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:93`, `src/Knock.Market.Api/Hosting/RedisExtensions.cs:91`, `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:92`
- `RedisReadinessHealthCheck`: `src/Knock.Gateway/Hosting/RedisExtensions.cs:160`, `src/Knock.Identity.Api/Hosting/RedisExtensions.cs:160`, `src/Knock.Market.Api/Hosting/RedisExtensions.cs:158`, `src/Knock.Messaging.Api/Hosting/RedisExtensions.cs:159`
- `RedisRequestCounter`: `src/Knock.Gateway/Hosting/RateLimitingExtensions.cs:45`, `src/Knock.Identity.Api/Hosting/RedisRequestCounter.cs:8`, `src/Knock.Market.Api/Hosting/RedisRequestCounter.cs:8`
- `RedisRevokedSessions`: `src/Knock.Gateway/Hosting/RevokedSessions.cs:16`, `src/Knock.Messaging.Api/Hosting/AuthenticationExtensions.cs:18`
- `RequestBodyLimitMiddleware`: `src/Knock.Identity.Api/Hosting/RequestBodyLimitMiddleware.cs:37`, `src/Knock.Market.Api/Hosting/RequestBodyLimitMiddleware.cs:34`
- `ScopeRequestPathEnricher`: `src/Knock.Gateway/Hosting/LoggingExtensions.cs:141`, `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs:141`, `src/Knock.Market.Api/Hosting/LoggingExtensions.cs:140`, `src/Knock.Messaging.Api/Hosting/LoggingExtensions.cs:141`
- `SecurityExtensions`: `src/Knock.Identity.Api/Hosting/SecurityExtensions.cs:7`, `src/Knock.Market.Api/Hosting/SecurityExtensions.cs:6`
- `SecurityHeadersMiddleware`: `src/Knock.Gateway/Hosting/SecurityHeadersMiddleware.cs:13`, `src/Knock.Identity.Api/Hosting/SecurityHeadersMiddleware.cs:12`, `src/Knock.Market.Api/Hosting/SecurityHeadersMiddleware.cs:11`, `src/Knock.Messaging.Api/Hosting/SecurityHeadersMiddleware.cs:11`
- `SecuritySettings`: `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:29`, `src/Knock.Market.Api/Hosting/SecuritySettings.cs:28`
- `SecuritySettingsValidator`: `src/Knock.Identity.Api/Hosting/SecuritySettings.cs:41`, `src/Knock.Market.Api/Hosting/SecuritySettings.cs:40`
- `SerilogLogState`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:255`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:255`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:251`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:255`
- `SharedRateLimiter`: `src/Knock.Identity.Api/Hosting/SharedRateLimiter.cs:10`, `src/Knock.Market.Api/Hosting/SharedRateLimiter.cs:10`
- `TelemetryExceptionRecorder`: `src/Knock.Gateway/Hosting/ObservabilityExtensions.cs:175`, `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs:175`, `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs:171`, `src/Knock.Messaging.Api/Hosting/ObservabilityExtensions.cs:175`
- `TelemetryRedactionProcessor`: `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:293`, `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:293`, `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:292`, `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:293`
- `TelemetryRedactor`: `src/Knock.Gateway/Hosting/TelemetryRedactor.cs:25`, `src/Knock.Identity.Api/Hosting/TelemetryRedactor.cs:25`, `src/Knock.Market.Api/Hosting/TelemetryRedactor.cs:24`, `src/Knock.Messaging.Api/Hosting/TelemetryRedactor.cs:25`
- `UnavailableContentModerator`: `src/Knock.Identity.Api/Modules/Safety/Services/UnavailableContentModerator.cs:9`, `src/Knock.Market.Api/Modules/Requests/Services/RequestPorts.cs:29`
- `ValidationExtensions`: `src/Knock.Identity.Api/Hosting/ValidationExtensions.cs:9`, `src/Knock.Market.Api/Hosting/ValidationExtensions.cs:9`
- `ValidationFilter`: `src/Knock.Identity.Api/Hosting/ValidationExtensions.cs:25`, `src/Knock.Market.Api/Hosting/ValidationExtensions.cs:24`

## Índice por módulo

### Account

**-**

- `src/Knock.Identity.Api/Modules/Account/AccountModuleExtensions.cs` — class `AccountModuleExtensions`
  - `AddAccountModule()`:12

**controllers**

- `src/Knock.Identity.Api/Modules/Account/Controllers/V1/AccessRecoveryController.cs` — class `AccessRecoveryController`
  - `StartRecovery()`:34
- `src/Knock.Identity.Api/Modules/Account/Controllers/V1/AccountControllerBase.cs` — class `AccountControllerBase`
  - `TryGetCaller()`:13, `UnauthorizedProblem()`:21
- `src/Knock.Identity.Api/Modules/Account/Controllers/V1/MeCambioNumeroController.cs` — class `MeCambioNumeroController`
  - `CheckAvailability()`:32, `Confirm()`:55
- `src/Knock.Identity.Api/Modules/Account/Controllers/V1/MeConsentimientosController.cs` — class `MeConsentimientosController`
  - `List()`:25, `Set()`:46
- `src/Knock.Identity.Api/Modules/Account/Controllers/V1/MeEliminacionController.cs` — class `MeEliminacionController`
  - `Schedule()`:39, `Cancel()`:65
- `src/Knock.Identity.Api/Modules/Account/Controllers/V1/MeExportacionesController.cs` — class `MeExportacionesController`
  - `RequestExport()`:29
- `src/Knock.Identity.Api/Modules/Account/Controllers/V1/MePendientesController.cs` — class `MePendientesController`
  - `List()`:22

**data**

- `src/Knock.Identity.Api/Modules/Account/Data/AccountStore.cs` — class `AccountStore`, class `Fields`
  - `EnsureIndexesAsync()`:54, `FindBySubAsync()`:77, `PhoneHashExistsAsync()`:82, `FindOtherOpenSessionIdsAsync()`:86, `ChangePhoneAsync()`:95, `FindByPhoneHashAsync()`:149, `CreateRecoveryCaseIfNoneOpenAsync()`:153, `TryStartExportAsync()`:180, `ClearExportAsync()`:193, `FindConsentsAsync()`:199, `SetOptionalConsentAsync()`:206, `FindSessionStartAsync()`:252, `ScheduleDeletionAsync()`:265, `CancelDeletionAsync()`:278
- `src/Knock.Identity.Api/Modules/Account/Data/IAccountStore.cs` — interface `IAccountStore`
- `src/Knock.Identity.Api/Modules/Account/Data/RedisSessionCanceller.cs` — class `RedisSessionCanceller`
  - `CancelAsync()`:10

**domain**

- `src/Knock.Identity.Api/Modules/Account/Domain/AccountDomain.cs` — class `AccountStates`, class `AccountErrorCodes`, record `AccountSnapshot`, class `ConsentTypes`, record `ConsentEntry`, record `StoredConsent`, class `ConsentCatalog`, class `OptionalConsentOptions`, enum `PhoneChangeOutcome`, enum `ExportStartOutcome`, class `RecoveryStates`, class `RecoveryTypes`, record `RecoveryCase`, record `AccountPending`
  - `AccountSnapshot()`:29, `ToString()`:32, `IsOptional()`:48, `Build()`:60, `IsValid()`:120, `RecoveryCase()`:124, `ToString()`:127

**dtos**

- `src/Knock.Identity.Api/Modules/Account/Dtos/AccountDtos.cs` — record `PhoneAvailabilityRequest`, record `ConfirmPhoneChangeRequest`, record `AccessRecoveryRequest`, record `UpdateConsentRequest`, record `ConsentResponse`, record `PendingGroupResponse`, record `PendingResponse`, record `DeleteAccountRequest`, record `DeletionScheduledResponse`

**services**

- `src/Knock.Identity.Api/Modules/Account/Services/AccessRecoveryService.cs` — record `AccessRecoveryInput`, record `AccessRecoveryOutcome`, record `Accepted`, record `InvalidToken`, record `RateLimited`, class `AccessRecoveryService`
  - `AccessRecoveryInput()`:8, `ToString()`:11, `AccessRecoveryService()`:36, `RequestAsync()`:44
- `src/Knock.Identity.Api/Modules/Account/Services/AccountDeletionService.cs` — record `DeleteAccountInput`, record `DeletionOutcome`, record `Scheduled`, record `ReauthRequired`, record `ReauthRejected`, class `AccountDeletionService`
  - `DeleteAccountInput()`:10, `ToString()`:13, `AccountDeletionService()`:39, `ScheduleAsync()`:55, `CancelAsync()`:94
- `src/Knock.Identity.Api/Modules/Account/Services/ConsentService.cs` — class `ConsentService`
  - `ConsentService()`:12, `ListAsync()`:19, `SetAsync()`:28
- `src/Knock.Identity.Api/Modules/Account/Services/DataExportService.cs` — class `DataExportService`
  - `DataExportService()`:12, `RequestAsync()`:19
- `src/Knock.Identity.Api/Modules/Account/Services/IAccountOutbox.cs` — interface `IAccountOutbox`, class `PendingAccountOutbox`
  - `EnqueueSessionRevokedAsync()`:20, `EnqueueExportAsync()`:26
- `src/Knock.Identity.Api/Modules/Account/Services/IMarketAccountClient.cs` — interface `IMarketAccountClient`, class `UnavailableMarketAccountClient`
  - `GetPendingAsync()`:23, `CancelOpenAsync()`:25
- `src/Knock.Identity.Api/Modules/Account/Services/ISessionCanceller.cs` — interface `ISessionCanceller`
- `src/Knock.Identity.Api/Modules/Account/Services/PendingService.cs` — class `PendingService`
  - `PendingService()`:9, `ListAsync()`:14
- `src/Knock.Identity.Api/Modules/Account/Services/PhoneChangeService.cs` — class `PhoneChangeService`
  - `PhoneChangeService()`:12, `EnsureAvailableAsync()`:23, `ConfirmAsync()`:38, `PhoneTaken()`:80
- `src/Knock.Identity.Api/Modules/Account/Services/RecoveryAttemptLimiter.cs` — interface `IRecoveryAttemptLimiter`, class `RecoveryAttemptLimiter`
  - `TryAcquire()`:32, `Dispose()`:46

**validators**

- `src/Knock.Identity.Api/Modules/Account/Validators/AccessRecoveryRequestValidator.cs` — class `AccessRecoveryRequestValidator`
- `src/Knock.Identity.Api/Modules/Account/Validators/ConfirmPhoneChangeRequestValidator.cs` — class `ConfirmPhoneChangeRequestValidator`
- `src/Knock.Identity.Api/Modules/Account/Validators/DeleteAccountRequestValidator.cs` — class `DeleteAccountRequestValidator`
- `src/Knock.Identity.Api/Modules/Account/Validators/PhoneAvailabilityRequestValidator.cs` — class `PhoneAvailabilityRequestValidator`
- `src/Knock.Identity.Api/Modules/Account/Validators/UpdateConsentRequestValidator.cs` — class `UpdateConsentRequestValidator`

### Addresses

**-**

- `src/Knock.Identity.Api/Modules/Addresses/AddressesModuleExtensions.cs` — class `AddressesModuleExtensions`
  - `AddAddressesModule()`:11

**controllers**

- `src/Knock.Identity.Api/Modules/Addresses/Controllers/PrivateNoStoreAttribute.cs` — class `PrivateNoStoreAttribute`
  - `OnResourceExecuting()`:12, `OnResourceExecuted()`:18
- `src/Knock.Identity.Api/Modules/Addresses/Controllers/V1/MeDireccionesController.cs` — class `MeDireccionesController`
  - `List()`:31, `Create()`:54, `Update()`:81, `Delete()`:105

**data**

- `src/Knock.Identity.Api/Modules/Addresses/Data/AddressesStore.cs` — interface `IAddressesStore`, class `AddressesStore`
  - `EnsureIndexesAsync()`:62, `FindUserIdAsync()`:67, `ListAsync()`:76, `ApplyAsync()`:84, `ToDocument()`:149, `ToStored()`:168

**domain**

- `src/Knock.Identity.Api/Modules/Addresses/Domain/AddressesDomain.cs` — record `Address`, record `StoredAddress`, record `AddressChanges`, class `AddressRejections`
  - `Address()`:9, `ToString()`:12, `StoredAddress()`:19, `ToString()`:22, `AddressChanges()`:29, `Reject()`:31, `ToString()`:33

**dtos**

- `src/Knock.Identity.Api/Modules/Addresses/Dtos/AddressDtos.cs` — record `CreateAddressRequest`, record `AddressResponse`, record `UpdateAddressRequest`, class `AddressRoute`

**services**

- `src/Knock.Identity.Api/Modules/Addresses/Services/AddressCipher.cs` — class `AddressCipher`
  - `AddressCipher()`:8, `SealStreet()`:10, `SealReferences()`:12, `Edit()`:15, `Open()`:47
- `src/Knock.Identity.Api/Modules/Addresses/Services/AddressPlanner.cs` — class `AddressPlanner`
  - `ForCreate()`:16, `ForEdit()`:39, `ForDelete()`:53
- `src/Knock.Identity.Api/Modules/Addresses/Services/AddressRules.cs` — class `AddressRules`, record `AddressInput`, record `AddressEdit`
  - `Normalize()`:23, `HasControlCharacters()`:25, `IsColoniaName()`:27, `AddressInput()`:31, `From()`:33, `ToString()`:43, `AddressEdit()`:50, `From()`:52, `ToString()`:62
- `src/Knock.Identity.Api/Modules/Addresses/Services/AddressService.cs` — class `AddressCrypto`, enum `AddressOutcome`, record `AddressResult`, class `AddressService`
  - `StreetContext()`:15, `ReferencesContext()`:17, `AddressResult()`:31, `ToString()`:33, `AddressService()`:40, `ListAsync()`:47, `CreateAsync()`:58, `UpdateAsync()`:95, `DeleteAsync()`:134

**validators**

- `src/Knock.Identity.Api/Modules/Addresses/Validators/CreateAddressRequestValidator.cs` — class `CreateAddressRequestValidator`
- `src/Knock.Identity.Api/Modules/Addresses/Validators/UpdateAddressRequestValidator.cs` — class `UpdateAddressRequestValidator`, class `AddressRouteValidator`

### Assistant

**data**

- `src/Knock.Market.Api/Modules/Assistant/Data/ConsumoIaStore.cs` — enum `ConsumptionResult`, enum `AdjustmentResult`, record `ConsumptionReservation`, record `ReservationResult`, class `ConsumoIaStore`
  - `ConsumoIaStore()`:13, `ReserveAsync()`:44, `AdjustAsync()`:68
- `src/Knock.Market.Api/Modules/Assistant/Data/UsoIaStore.cs` — record `UsageRecord`, class `UsoIaStore`
  - `UsoIaStore()`:8, `SaveAsync()`:10

**services**

- `src/Knock.Market.Api/Modules/Assistant/Services/FiltroDatosPersonales.cs` — enum `PersonalDataKind`, record struct `CensorshipEntry`, record `FilterResult`, class `FiltroDatosPersonales`, record struct `Range`, class `View`
  - `Censurar()`:59, `Name()`:78, `Build()`:263

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

### HomeAndCar

**-**

- `src/Knock.Identity.Api/Modules/HomeAndCar/HomeAndCarModuleExtensions.cs` — class `HomeAndCarModuleExtensions`
  - `AddHomeAndCarModule()`:11

**controllers**

- `src/Knock.Identity.Api/Modules/HomeAndCar/Controllers/V1/CasaYAutoController.cs` — class `CasaYAutoController`
  - `List()`:29, `Create()`:45, `Update()`:67, `Delete()`:79, `History()`:99

**data**

- `src/Knock.Identity.Api/Modules/HomeAndCar/Data/HomeAndCarStore.cs` — interface `IHomeAndCarStore`, class `HomeAndCarStore`
  - `EnsureIndexesAsync()`:53, `ListAsync()`:60, `FindAsync()`:69, `AddAsync()`:72, `UpdateAsync()`:93, `DeleteAsync()`:110

**domain**

- `src/Knock.Identity.Api/Modules/HomeAndCar/Domain/HomeAndCarDomain.cs` — class `ItemTypes`, class `HomeAndCarRules`, record `HomeAndCarItem`, enum `AddItemOutcome`
  - `IsValid()`:15, `MaxYear()`:31, `NormalizeName()`:34, `HomeAndCarItem()`:42, `ToString()`:45

**dtos**

- `src/Knock.Identity.Api/Modules/HomeAndCar/Dtos/HomeAndCarDtos.cs` — record `CreateItemRequest`, record `UpdateItemRequest`, record `ItemResponse`, class `ItemRoute`, class `HistoryQuery`, record `HistoryItemResponse`

**services**

- `src/Knock.Identity.Api/Modules/HomeAndCar/Services/HomeAndCarService.cs` — class `HomeAndCarService`, record `UpdateItemOutcome`, record `HistoryOutcome`, interface `IOutcome`
  - `HomeAndCarService()`:15, `ListAsync()`:20, `CreateAsync()`:27, `UpdateAsync()`:46, `DeleteAsync()`:66, `HistoryAsync()`:78, `ToResponse()`:96
- `src/Knock.Identity.Api/Modules/HomeAndCar/Services/MarketHistoryClient.cs` — class `MarketHistoryOptions`, record `HistoryEntry`, enum `HistoryStatus`, record `HistoryResult`, interface `IMarketHistory`, class `MarketHistoryClient`, record `MarketPage`, record `MarketEntry`, record `MarketProblem`
  - `HistoryResult()`:38, `Failure()`:40, `ListAsync()`:64, `ReadKey()`:108

**validators**

- `src/Knock.Identity.Api/Modules/HomeAndCar/Validators/ItemValidators.cs` — class `ItemReasons`, class `ItemRouteValidator`, class `CreateItemRequestValidator`, class `UpdateItemRequestValidator`, class `HistoryQueryValidator`
  - `InvalidYear()`:19

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
  - `HitAsync()`:53, `AddKnockGatewayRateLimiting()`:68, `IsApiPath()`:81, `ClientKey()`:91, `IpRateLimitMiddleware()`:119, `InvokeAsync()`:123, `IsAuthPath()`:146, `UserRateLimitMiddleware()`:153, `InvokeAsync()`:155, `RejectedAsync()`:173
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

- `src/Knock.Identity.Api/Common/Crypto/FieldCipher.cs` — class `FieldEncryptionOptions`, class `FieldCipher`
  - `FieldCipher()`:27, `Encrypt()`:34, `Decrypt()`:57
- `src/Knock.Identity.Api/Common/Errors/DomainException.cs` — enum `DomainErrorKind`, class `DomainException`, class `DomainErrors`
  - `ToStatusCode()`:58
- `src/Knock.Identity.Api/Common/Errors/ErrorCodes.cs` — class `CommonErrorCodes`
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
  - `JsonCompressionMiddleware()`:22, `InvokeAsync()`:29, `ToMemoryStream()`:175, `Write()`:177, `Write()`:179, `WriteAsync()`:181, `Flush()`:251, `FlushAsync()`:259, `Read()`:262, `Seek()`:264, `SetLength()`:266, `Dispose()`:268
- `src/Knock.Identity.Api/Hosting/KnockApplicationExtensions.cs` — class `KnockApplicationExtensions`
  - `AddKnockApi()`:9, `UseKnockApi()`:31
- `src/Knock.Identity.Api/Hosting/LoggingExtensions.cs` — class `LoggingExtensions`, class `ScopeRequestPathEnricher`
  - `AddKnockLogging()`:20, `ConfigureRequestLogging()`:36, `IsQueryBearingBelowWarning()`:67, `RoutePath()`:80, `Enrich()`:145
- `src/Knock.Identity.Api/Hosting/ModelStateProblems.cs` — class `ModelStateProblems`
  - `AddKnockModelStateProblems()`:13
- `src/Knock.Identity.Api/Hosting/ModulesExtensions.cs` — class `ModulesExtensions`
  - `AddKnockModules()`:14
- `src/Knock.Identity.Api/Hosting/MongoExtensions.cs` — class `KnockMongoOptions`, class `KnockMongoOptionsValidator`, class `MongoExtensions`, class `MongoReadinessHealthCheck`
  - `Seconds()`:60, `Validate()`:76, `AddKnockMongo()`:149, `AddKnockMongoDatabase()`:171, `CreateSettings()`:185, `CheckHealthAsync()`:229
- `src/Knock.Identity.Api/Hosting/ObservabilityExtensions.cs` — class `ObservabilityOptions`, record `AzureMonitorSignalSettings`, class `AzureMonitorExportStatus`, class `ObservabilityExtensions`, class `TelemetryExceptionRecorder`, class `IgnoreIncomingTraceContextPropagator`, class `OpenTelemetryLogSink`, class `SerilogLogState`
  - `Record()`:47, `AddKnockObservability()`:71, `TryHandleAsync()`:177, `Inject()`:200, `ExtractTraceIdAndState()`:202, `ExtractBaggage()`:208, `Emit()`:224, `GetEnumerator()`:285
- `src/Knock.Identity.Api/Hosting/OpenApiExtensions.cs` — class `OpenApiExtensions`
  - `AddKnockOpenApi()`:9, `MapKnockOpenApi()`:41
- `src/Knock.Identity.Api/Hosting/ProblemDetailsExtensions.cs` — class `ProblemDetailsExtensions`
  - `AddKnockProblemDetails()`:9
- `src/Knock.Identity.Api/Hosting/RateLimitingExtensions.cs` — class `RateLimitPolicySettings`, class `RateLimitSettings`, class `RateLimitingExtensions`
  - `AddKnockRateLimiting()`:59, `ClientKey()`:153
- `src/Knock.Identity.Api/Hosting/RedisExtensions.cs` — class `KnockRedisOptions`, class `KnockRedisOptionsValidator`, class `RedisExtensions`, class `RedisReadinessHealthCheck`
  - `Validate()`:41, `AddKnockRedis()`:105, `CreateConfiguration()`:123, `CheckHealthAsync()`:162
- `src/Knock.Identity.Api/Hosting/RedisRequestCounter.cs` — class `RedisRequestCounter`
  - `RedisRequestCounter()`:8, `HitAsync()`:16
- `src/Knock.Identity.Api/Hosting/RequestBodyLimitMiddleware.cs` — class `ProblemResponses`, class `RequestBodyLimitMiddleware`
  - `WriteAsync()`:10, `RequestBodyLimitMiddleware()`:37, `InvokeAsync()`:39
- `src/Knock.Identity.Api/Hosting/SecurityExtensions.cs` — class `SecurityExtensions`
  - `AddKnockSecurity()`:13
- `src/Knock.Identity.Api/Hosting/SecurityHeadersMiddleware.cs` — class `SecurityHeadersMiddleware`
  - `SecurityHeadersMiddleware()`:12, `InvokeAsync()`:18
- `src/Knock.Identity.Api/Hosting/SecuritySettings.cs` — class `HstsSettings`, class `HttpsSettings`, class `CorsSettings`, class `SecuritySettings`, class `SecuritySettingsValidator`
  - `Validate()`:47, `Errors()`:50, `IsValidOrigin()`:84
- `src/Knock.Identity.Api/Hosting/SharedRateLimiter.cs` — class `SharedRateLimiter`, class `SharedLease`
  - `GetStatistics()`:36, `AttemptAcquireCore()`:38, `AcquireAsyncCore()`:40, `Dispose()`:65, `TryGetMetadata()`:81
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
  - `AddKnockAuthentication()`:15, `AddInternalControllers()`:57, `IsController()`:75, `StartAsync()`:92, `StopAsync()`:110
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
- `src/Knock.Market.Api/Hosting/InternalServiceAuthentication.cs` — class `InternalServiceAuthentication`
  - `AddInternalServiceScheme()`:32, `AddKnockInternalServiceAuthentication()`:35, `ReadKey()`:80
- `src/Knock.Market.Api/Hosting/JsonCompressionMiddleware.cs` — class `JsonCompressionMiddleware`, class `CappedBufferStream`, class `MarketJsonSerializerContext`
  - `JsonCompressionMiddleware()`:20, `InvokeAsync()`:27, `ToMemoryStream()`:173, `Write()`:175, `Write()`:177, `WriteAsync()`:179, `Flush()`:249, `FlushAsync()`:257, `Read()`:260, `Seek()`:262, `SetLength()`:264, `Dispose()`:266
- `src/Knock.Market.Api/Hosting/KnockApplicationExtensions.cs` — class `KnockApplicationExtensions`
  - `AddKnockApi()`:8, `UseKnockApi()`:31
- `src/Knock.Market.Api/Hosting/LoggingExtensions.cs` — class `LoggingExtensions`, class `ScopeRequestPathEnricher`
  - `AddKnockLogging()`:19, `ConfigureRequestLogging()`:35, `IsQueryBearingBelowWarning()`:66, `RoutePath()`:79, `Enrich()`:144
- `src/Knock.Market.Api/Hosting/ModulesExtensions.cs` — class `ModulesExtensions`
  - `AddKnockModules()`:11
- `src/Knock.Market.Api/Hosting/MongoExtensions.cs` — class `KnockMongoOptions`, class `KnockMongoOptionsValidator`, class `MongoExtensions`, class `MongoReadinessHealthCheck`
  - `Seconds()`:58, `Validate()`:74, `AddKnockMongo()`:147, `AddKnockMongoDatabase()`:169, `CreateSettings()`:183, `CheckHealthAsync()`:227
- `src/Knock.Market.Api/Hosting/ObservabilityExtensions.cs` — class `ObservabilityOptions`, record `AzureMonitorSignalSettings`, class `AzureMonitorExportStatus`, class `ObservabilityExtensions`, class `TelemetryExceptionRecorder`, class `IgnoreIncomingTraceContextPropagator`, class `OpenTelemetryLogSink`, class `SerilogLogState`
  - `Record()`:46, `AddKnockObservability()`:67, `TryHandleAsync()`:173, `Inject()`:196, `ExtractTraceIdAndState()`:198, `ExtractBaggage()`:204, `Emit()`:220, `GetEnumerator()`:281
- `src/Knock.Market.Api/Hosting/OpenApiExtensions.cs` — class `OpenApiExtensions`
  - `AddKnockOpenApi()`:7, `MapKnockOpenApi()`:14
- `src/Knock.Market.Api/Hosting/PrivateNoStoreAttribute.cs` — class `PrivateNoStoreAttribute`
  - `OnActionExecuting()`:15
- `src/Knock.Market.Api/Hosting/ProblemDetailsExtensions.cs` — class `ProblemDetailsExtensions`
  - `AddKnockProblemDetails()`:8
- `src/Knock.Market.Api/Hosting/RateLimitingExtensions.cs` — class `RateLimitPolicySettings`, class `RateLimitSettings`, class `RateLimitingExtensions`
  - `AddKnockRateLimiting()`:57, `ClientKey()`:154
- `src/Knock.Market.Api/Hosting/RedisExtensions.cs` — class `KnockRedisOptions`, class `KnockRedisOptionsValidator`, class `RedisExtensions`, class `RedisReadinessHealthCheck`
  - `Validate()`:39, `AddKnockRedis()`:103, `CreateConfiguration()`:121, `CheckHealthAsync()`:160
- `src/Knock.Market.Api/Hosting/RedisRequestCounter.cs` — class `RedisRequestCounter`
  - `RedisRequestCounter()`:8, `HitAsync()`:16
- `src/Knock.Market.Api/Hosting/RequestBodyLimitMiddleware.cs` — class `ProblemResponses`, class `RequestBodyLimitMiddleware`
  - `WriteAsync()`:11, `RequestBodyLimitMiddleware()`:34, `InvokeAsync()`:36
- `src/Knock.Market.Api/Hosting/SecurityExtensions.cs` — class `SecurityExtensions`
  - `AddKnockSecurity()`:12
- `src/Knock.Market.Api/Hosting/SecurityHeadersMiddleware.cs` — class `SecurityHeadersMiddleware`
  - `SecurityHeadersMiddleware()`:11, `InvokeAsync()`:17
- `src/Knock.Market.Api/Hosting/SecuritySettings.cs` — class `HstsSettings`, class `HttpsSettings`, class `CorsSettings`, class `SecuritySettings`, class `SecuritySettingsValidator`
  - `Validate()`:46, `Errors()`:49, `IsValidOrigin()`:83
- `src/Knock.Market.Api/Hosting/SharedRateLimiter.cs` — class `SharedRateLimiter`, class `SharedLease`
  - `GetStatistics()`:36, `AttemptAcquireCore()`:38, `AcquireAsyncCore()`:40, `Dispose()`:65, `TryGetMetadata()`:81
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
- `src/Knock.Market.Api/Modules/Media/Options/ImagenesOptions.cs` — class `ImagenesOptions`, class `ImagenesOptionsValidator`
  - `Validate()`:25, `TryParseBase()`:35
- `src/Knock.Market.Api/Modules/Media/Options/ImagenesServiceCollectionExtensions.cs` — class `ImagenesServiceCollectionExtensions`
  - `AddImagenesPublicas()`:10

**data**

- `src/Knock.Market.Api/Modules/Media/Data/BlobMediaFileStore.cs` — class `BlobMediaFileStore`, record `MediaFileRecord`
  - `AddAsync()`:25, `FindAsync()`:40, `TryReplaceAsync()`:56, `FromDomain()`:106, `ToDomain()`:110
- `src/Knock.Market.Api/Modules/Media/Data/IMediaFileStore.cs` — record `StoredMediaFile`, interface `IMediaFileStore`
- `src/Knock.Market.Api/Modules/Media/Data/InMemoryMediaFileStore.cs` — class `InMemoryMediaFileStore`
  - `AddAsync()`:13, `FindAsync()`:24, `TryReplaceAsync()`:27
- `src/Knock.Market.Api/Modules/Media/Data/MediaOptions.cs` — class `MediaOptions`

**domain**

- `src/Knock.Market.Api/Modules/Media/Domain/CodigosImagen.cs` — class `CodigosImagen`
- `src/Knock.Market.Api/Modules/Media/Domain/ImagenPublica.cs` — enum `PropositoImagen`, class `PropositoImagenExtensions`, record `ImagenPublica`
  - `Parse()`:8, `ToContractValue()`:15
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
- `src/Knock.Market.Api/Modules/Media/Services/ImagenPublicaUrl.cs` — class `ImagenPublicaUrl`
  - `Build()`:31, `TryParse()`:40
- `src/Knock.Market.Api/Modules/Media/Services/InMemoryMediaBlobStore.cs` — class `InMemoryMediaBlobStore`
  - `EnsureContainersAsync()`:12, `UploadRaw()`:14, `ContainsQuarantine()`:21, `GetBlob()`:23, `ClearQuarantine()`:26, `GetUploadedAsync()`:28, `DownloadQuarantineAsync()`:33, `UploadFileAsync()`:44, `DeleteQuarantineAsync()`:53
- `src/Knock.Market.Api/Modules/Media/Services/MediaApi.cs` — class `MediaApi`
  - `IsReadyAndOwnedByAsync()`:8
- `src/Knock.Market.Api/Modules/Media/Services/MediaService.cs` — record `UploadTicket`, record `ConfirmedFile`, class `MediaService`
  - `MediaService()`:18, `CreateUploadAsync()`:25, `ConfirmAsync()`:41, `IsReadyAndOwnedByAsync()`:95
- `src/Knock.Market.Api/Modules/Media/Services/ProcesadorImagen.cs` — record `ImagenProcesada`, record `ResultadoProcesamiento`, class `ProcesadorImagen`
  - `ResultadoProcesamiento()`:11, `Ok()`:13, `Rechazada()`:15, `ProcesadorImagen()`:25, `LadoMaximo()`:38, `ProcesarAsync()`:41

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
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/ArrivalController.cs` — class `ArrivalController`
  - `Arrive()`:40
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/ChooseQuoteController.cs` — class `ChooseQuoteController`
  - `Choose()`:36
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/ConversationPermissionController.cs` — class `ConversationPermissionController`
  - `Get()`:31
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/CotizacionesController.cs` — record `QuoteRequest`, class `CotizacionesController`
  - `Send()`:35
- `src/Knock.Market.Api/Modules/Requests/Controllers/V1/InternalHistoryController.cs` — class `InternalHistoryController`
  - `List()`:34
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
- `src/Knock.Market.Api/Modules/Requests/Data/HistoryStore.cs` — record `HistoryVisit`, record `HistoryVisitPage`, class `HistoryStore`
  - `FindLinkedTradesAsync()`:34
- `src/Knock.Market.Api/Modules/Requests/Data/MongoOutbox.cs` — record `OutboxMessage`, interface `IOutbox`, class `MongoOutbox`
  - `OutboxMessage()`:10, `Create()`:12, `AddAsync()`:29
- `src/Knock.Market.Api/Modules/Requests/Data/QuoteReaders.cs` — class `MongoTechnicianDirectory`, class `MongoQuoteRequestReader`
  - `FindBySubAsync()`:16, `FindEligibleAsync()`:19, `ExistsAsync()`:60, `FindAsync()`:74
- `src/Knock.Market.Api/Modules/Requests/Data/QuoteStore.cs` — class `QuoteStore`
  - `SaveAsync()`:21, `SaveAsync()`:24
- `src/Knock.Market.Api/Modules/Requests/Data/RequestsStore.cs` — record `RequestRecord`, record `RequestRecordPage`, class `RequestsStore`
  - `InsertAsync()`:53, `FindAsync()`:67, `CountLiveQuotesAsync()`:103, `HasQuotedAsync()`:119
- `src/Knock.Market.Api/Modules/Requests/Data/VisitStore.cs` — record `VisitRecord`, record `FailedAttempt`, enum `VisitCloseReason`, class `VisitStore`
  - `VisitRecord()`:10, `FindAsync()`:65, `MarkArrivedAsync()`:75, `RegisterFailedAttemptAsync()`:99, `CloseAsync()`:133, `CountNoticeAsync()`:162

**domain**

- `src/Knock.Market.Api/Modules/Requests/Domain/ChoiceRules.cs` — class `ChoiceRules`
  - `Block()`:10
- `src/Knock.Market.Api/Modules/Requests/Domain/ChooseStatus.cs` — enum `ChooseStatus`
- `src/Knock.Market.Api/Modules/Requests/Domain/ConversationRules.cs` — class `ConversationClosures`, record struct `ConversationOpening`, class `ConversationRules`
  - `ConversationOpening()`:14, `Closed()`:18, `Evaluate()`:29
- `src/Knock.Market.Api/Modules/Requests/Domain/HistoryCursor.cs` — class `HistoryCursor`
  - `Encode()`:17, `Decode()`:19
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
- `src/Knock.Market.Api/Modules/Requests/Domain/TransicionesDeSolicitud.cs` — class `TransicionesDeSolicitud`
  - `CanClose()`:6, `CancellationError()`:9
- `src/Knock.Market.Api/Modules/Requests/Domain/VisitRules.cs` — class `VisitErrorCodes`, enum `ArrivalBlock`, record `VisitState`, class `VisitRules`
  - `WindowStart()`:48, `CanArrive()`:55, `TechnicianMaySeeAddress()`:68, `CanNotify()`:71, `CodeHiddenReason()`:76, `BlockForArrival()`:100, `SafetyRule()`:136, `TrustNotice()`:143

**dtos**

- `src/Knock.Market.Api/Modules/Requests/Dtos/CreateRequestBody.cs` — record `CreateRequestBody`
  - `CreateRequestBody()`:13
- `src/Knock.Market.Api/Modules/Requests/Dtos/InternalHistoryDtos.cs` — class `InternalHistoryQuery`, record `HistoryEntryResponse`
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
- `src/Knock.Market.Api/Modules/Requests/Services/HistoryService.cs` — class `HistoryService`
  - `HistoryService()`:14, `ListAsync()`:18
- `src/Knock.Market.Api/Modules/Requests/Services/QuoteComparisonService.cs` — class `QuoteComparisonService`
  - `QuoteComparisonService()`:13, `CompareAsync()`:19
- `src/Knock.Market.Api/Modules/Requests/Services/QuoteService.cs` — record `Technician`, record `QuoteRequestSnapshot`, interface `ITechnicianDirectory`, interface `IQuoteRequestReader`, enum `QuoteCheckResult`, interface `IQuoteWriter`, record `QuoteResponse`, interface `ICreatedQuote`, enum `NoteModerationResult`, interface `INoteModerator`, class `UnavailableNoteModerator`, class `QuoteService`
  - `ModerateAsync()`:48, `QuoteService()`:51, `SendAsync()`:55, `Covers()`:83
- `src/Knock.Market.Api/Modules/Requests/Services/RequestPorts.cs` — interface `IRequestPhotos`, class `UnavailableRequestPhotos`, class `UnavailableContentModerator`
  - `AreReadyForAsync()`:22, `ReadLinksAsync()`:24, `ReviewTextAsync()`:31, `ReviewImageAsync()`:34
- `src/Knock.Market.Api/Modules/Requests/Services/RequestsApi.cs` — class `RequestsApi`
  - `CanArrive()`:7
- `src/Knock.Market.Api/Modules/Requests/Services/SolicitudService.cs` — enum `CreateRequestStatus`, record `CreateRequestOutcome`, class `SolicitudService`
  - `SolicitudService()`:31, `CreateAsync()`:46, `ListAsync()`:116, `GetAsync()`:143, `AddressContext()`:209
- `src/Knock.Market.Api/Modules/Requests/Services/TrustNoticeService.cs` — enum `TrustNoticeStatus`, record `TrustNoticeOutcome`, class `TrustNoticeService`
  - `TrustNoticeService()`:21, `PrepareAsync()`:23
- `src/Knock.Market.Api/Modules/Requests/Services/VisitCodeFactory.cs` — record `VisitCode`, class `VisitCodeFactory`
  - `VisitCodeFactory()`:15, `Create()`:19, `Context()`:26, `ExpiresAt()`:28
- `src/Knock.Market.Api/Modules/Requests/Services/VisitService.cs` — class `VisitService`
  - `VisitService()`:15, `GetAsync()`:21, `TechnicianViewAsync()`:28, `FindAsync()`:35

**validators**

- `src/Knock.Market.Api/Modules/Requests/Validators/CreateRequestBodyValidator.cs` — class `CreateRequestBodyValidator`
- `src/Knock.Market.Api/Modules/Requests/Validators/InternalHistoryQueryValidator.cs` — class `InternalHistoryQueryValidator`
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
- `src/Knock.Identity.Api/Modules/Sesiones/Services/PhoneNumbers.cs` — class `PhoneNumbers`
  - `IsValid()`:8
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
- `src/Knock.Market.Api/Modules/Tecnicos/Controllers/V1/TrabajoController.cs` — class `TrabajoController`
  - `Get()`:26

**data**

- `src/Knock.Market.Api/Modules/Tecnicos/Data/TechnicianLookupStore.cs` — class `TechnicianLookupStore`
  - `FindCoverageAsync()`:19, `ToCoverage()`:28, `FindBusinessNamesAsync()`:45
- `src/Knock.Market.Api/Modules/Tecnicos/Data/TechnicianProfileStore.cs` — class `TechnicianProfileStore`
  - `ExistsAsync()`:22, `InsertAsync()`:26, `FindByIdAsync()`:46, `ToProfile()`:54, `ToDocument()`:78
- `src/Knock.Market.Api/Modules/Tecnicos/Data/TechnicianSelfStore.cs` — class `TechnicianSelfStore`
  - `FindOwnAsync()`:26, `ReadOwnAsync()`:37, `SetAvailableUntilAsync()`:60, `ReadScheduleAsync()`:63, `SetScheduleAsync()`:71, `UpdateAsync()`:82
- `src/Knock.Market.Api/Modules/Tecnicos/Data/TrabajoStore.cs` — record `TrabajoVerification`, class `TrabajoStore`
  - `ReadTechnicianAsync()`:14, `ReadUpcomingAsync()`:19, `ReadQuotesAsync()`:29, `CountFinishedAsync()`:35, `ReadRequestsAsync()`:43, `ReadVerificationAsync()`:71

**domain**

- `src/Knock.Market.Api/Modules/Tecnicos/Domain/ScheduleRules.cs` — class `ScheduleRules`
  - `EndOfCityDayUtc()`:14, `AllClosed()`:23, `TryParseTime()`:25, `IsValid()`:31, `Normalize()`:61
- `src/Knock.Market.Api/Modules/Tecnicos/Domain/TechnicianRules.cs` — class `TechnicianRules`
  - `IsVisible()`:30, `IsAvailable()`:33, `IsKnownAttention()`:35, `UsesWorkshop()`:37, `VisitsHomes()`:39
- `src/Knock.Market.Api/Modules/Tecnicos/Domain/TrabajoRules.cs` — class `TrabajoRules`
  - `CanShowAddress()`:5, `RejectionReason()`:7, `CompareFinished()`:10

**dtos**

- `src/Knock.Market.Api/Modules/Tecnicos/Dtos/MyTechnicianDtos.cs` — record `AvailabilityBody`, record `AvailabilityResponse`, record `UpdateTechnicianBody`, record `DayScheduleDto`, record `TimeRangeDto`, record `ScheduleBody`
  - `AvailabilityBody()`:4, `UpdateTechnicianBody()`:17, `ScheduleBody()`:39
- `src/Knock.Market.Api/Modules/Tecnicos/Dtos/RegisterTechnicianBody.cs` — record `RegisterTechnicianBody`, record `TechnicianProfileBody`, record `TechnicianResponse`
  - `RegisterTechnicianBody()`:13
- `src/Knock.Market.Api/Modules/Tecnicos/Dtos/TechnicianLookupDtos.cs` — class `CoverageQuery`, record `CoverageResponse`, class `AutocompleteQuery`, record `Suggestion`, record `AutocompleteResponse`
- `src/Knock.Market.Api/Modules/Tecnicos/Dtos/TechnicianProfileDtos.cs` — class `TechnicianIdRoute`, record `TechnicianProfileResponse`, record `TechnicianProfileDetail`
- `src/Knock.Market.Api/Modules/Tecnicos/Dtos/TrabajoResponse.cs` — record `TrabajoResponse`, record `TrabajoVerificationResponse`, record `TrabajoClient`, record `TrabajoPlace`, record `TrabajoVisitResponse`, record `TrabajoQuoteResponse`, record `TrabajoFinishedItem`, record `TrabajoFinishedPage`, record `TrabajoSummary`

**services**

- `src/Knock.Market.Api/Modules/Tecnicos/Services/MyTechnicianService.cs` — class `TecnicosOptions`, record `OwnTechnician`, interface `ITechnicianSelfStore`, enum `UpdateTechnicianStatus`, record `UpdateTechnicianOutcome`, class `MyTechnicianService`
  - `MyTechnicianService()`:52, `SetAvailabilityAsync()`:61, `GetScheduleAsync()`:76, `SetScheduleAsync()`:88, `UpdateAsync()`:102
- `src/Knock.Market.Api/Modules/Tecnicos/Services/TechnicianLookupService.cs` — record `TechnicianCoverage`, record `BusinessName`, interface `ITechnicianLookupReader`, enum `CoverageStatus`, record `CoverageOutcome`, class `TechnicianLookupService`
  - `TechnicianLookupService()`:47, `CoverageAsync()`:61, `AutocompleteAsync()`:104, `Normalize()`:130, `DistanceKm()`:142
- `src/Knock.Market.Api/Modules/Tecnicos/Services/TechnicianProfileService.cs` — record `TechnicianProfileRecord`, interface `ITechnicianProfileReader`, class `TechnicianProfileService`
  - `TechnicianProfileService()`:37, `CacheKey()`:44, `GetAsync()`:46, `Forget()`:73, `IsVisible()`:75, `Shorten()`:78
- `src/Knock.Market.Api/Modules/Tecnicos/Services/TechnicianRegistrationService.cs` — enum `RegisterTechnicianStatus`, record `RegisterTechnicianOutcome`, record `NewTechnician`, interface `ITechnicianProfileStore`, class `TechnicianRegistrationService`
  - `TechnicianRegistrationService()`:47, `RegisterAsync()`:54
- `src/Knock.Market.Api/Modules/Tecnicos/Services/TecnicoTrabajoService.cs` — class `TecnicoTrabajoService`
  - `TecnicoTrabajoService()`:15, `GetAsync()`:17

**validators**

- `src/Knock.Market.Api/Modules/Tecnicos/Validators/MyTechnicianValidators.cs` — class `AvailabilityBodyValidator`, class `ScheduleBodyValidator`, class `UpdateTechnicianBodyValidator`
- `src/Knock.Market.Api/Modules/Tecnicos/Validators/RegisterTechnicianBodyValidator.cs` — class `RegisterTechnicianBodyValidator`
- `src/Knock.Market.Api/Modules/Tecnicos/Validators/TechnicianIdRouteValidator.cs` — class `TechnicianIdRouteValidator`
- `src/Knock.Market.Api/Modules/Tecnicos/Validators/TechnicianLookupValidators.cs` — class `CoverageQueryValidator`, class `AutocompleteQueryValidator`

### Trust

**-**

- `src/Knock.Market.Api/Modules/Trust/TrustModuleExtensions.cs` — class `TrustModuleExtensions`
  - `AddTrustModule()`:13

**controllers**

- `src/Knock.Market.Api/Modules/Trust/Controllers/V1/TrustedTechniciansController.cs` — class `TrustedTechniciansController`
  - `List()`:37, `Save()`:50, `Remove()`:69

**data**

- `src/Knock.Market.Api/Modules/Trust/Data/TrustStore.cs` — record `TrustTechnicianView`, record `TrustSaved`, record `TrustSaveResult`, class `TrustStore`
  - `EnsureIndexesAsync()`:42, `FindVisibleTechnicianAsync()`:50, `SaveAsync()`:57, `RemoveAsync()`:82

**domain**

- `src/Knock.Market.Api/Modules/Trust/Domain/TrustCursor.cs` — class `TrustErrorCodes`, class `TrustCursor`
  - `Encode()`:24, `Decode()`:27

**dtos**

- `src/Knock.Market.Api/Modules/Trust/Dtos/TrustDtos.cs` — record `TrustedTechnician`, record `TrustedTechnicianPage`, class `TrustedListQuery`, class `TrustedTechnicianRoute`

**services**

- `src/Knock.Market.Api/Modules/Trust/Services/TrustService.cs` — enum `TrustSaveStatus`, record `TrustSaveOutcome`, class `TrustService`
  - `TrustSaveOutcome()`:17, `ToString()`:19, `TrustService()`:26, `ListAsync()`:29, `SaveAsync()`:38, `RemoveAsync()`:53

**validators**

- `src/Knock.Market.Api/Modules/Trust/Validators/TrustValidators.cs` — class `TrustedListQueryValidator`, class `TrustedTechnicianRouteValidator`

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
- `src/Knock.Identity.Api/Modules/Usuarios/Services/ProfileCacheInvalidator.cs` — class `ProfileCacheInvalidator`
  - `ProfileCacheInvalidator()`:6, `TryForgetAsync()`:9
- `src/Knock.Identity.Api/Modules/Usuarios/Services/ProfileRules.cs` — class `ProfileOptions`, class `ProfileRules`
  - `ProfileRules()`:19, `Validate()`:36
- `src/Knock.Identity.Api/Modules/Usuarios/Services/ProfileService.cs` — class `ProfileService`
  - `ProfileService()`:12, `GetAsync()`:23, `UpdateAsync()`:36
- `src/Knock.Identity.Api/Modules/Usuarios/Services/UsuariosApi.cs` — class `UsuariosApi`
  - `FindPublicUserAsync()`:8
