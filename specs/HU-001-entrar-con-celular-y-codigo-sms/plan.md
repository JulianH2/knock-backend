# plan.md — HU-001 — Entrar con mi celular y código SMS

> Borrador. Las decisiones 1 a 9 del líder (2026-10-24) están en spec §8; siguen abiertas las dudas 1 a 11 de §8; falta la aprobación de la spec.

## 0. Cuándo y en qué orden se implementa
**Implementación en Sprint 2.** Esta tarea es solo la spec.

1. **Primero y solo, antes de cualquier dato guardado: el `sub` v8** (decisión 1). Cambio pequeño y sin base de datos: `UserIdDerivation` (UUID v8 con SHA-256, se borra el SHA-1 propio), su prueba (caso 10) y las pruebas de TT-API-02 que usaban el vector v5. Se puede fusionar antes que TT-BD-06 y **debe** fusionar antes de que exista la primera cuenta persistida.
2. **Esperar TT-BD-06** (KNOCK-156, #53) para todo lo que toca la base: `AddKnockMongo`, `Mongo:Databases:Identity` y la fixture de `documentdb-local`. Si no ha fusionado, ver spec §3 «Dependencia de TT-BD-06»: se adelanta lo que se prueba con dobles y **no** se crea un cliente de Mongo propio.
3. El resto sigue el orden de `AGENTS.md §1`: contrato (spec §4 + OpenAPI) → colecciones e índices → dominio y servicio + unitarias → controller + integración, seguridad negativa y contrato → `scripts/export_openapi.py` (duda 6) → gates → `Memoria.md` → informe.

## 1. Mapeo de archivos
- **Servicio:** Identidad (`Knock.Identity.Api`, ADR-001; hoy `src/Knock.Api/Modules/Identity/`).
- **Reutilizar** (buscado con `python scripts/codemap.py --query` sobre sesión, refresh, cifrado, auditoría, repositorio y reloj de pruebas; sin coincidencias para cifrado, auditoría ni repositorio):
  - `Modules/Identity/Services/FirebaseIdTokenVerifier.cs` y `IFirebaseIdTokenVerifier.cs` (`FirebaseIdentity`): se extienden para devolver el celular.
  - `Services/FirebaseSessionService.cs` (flujo del canje), `Services/SessionTokenIssuer.cs` (JWT y refresh aleatorio) y `Controllers/V1/AuthController.cs`, `Dtos/SessionDtos.cs`: se extienden, no se duplican.
  - `Hosting/ValidationExtensions.cs` (`ValidationFilter`, 422 sin valores), `Hosting/ProblemDetailsExtensions.cs`, `DomainExceptionHandler.cs` y `Common/Errors/DomainException.cs` (códigos de dominio), `Hosting/RateLimitingExtensions.cs` (política `/auth/*`, **sin cambios**), `Hosting/AuthenticationExtensions.cs`, `Common/Security/JwtSettings*` (molde de la lectura de claves) y el `TimeProvider` ya registrado por `AddIdentityModule`.
  - De **TT-BD-06**: `AddKnockMongo` en `Hosting/` (cliente, `maxPoolSize`, readiness), la configuración `Mongo:Databases:*` y la fixture de Testcontainers con `documentdb-local` fijada por digest. Molde de repositorio, inicializador de índices y contador de comandos del driver: lo que entregue `Modules/Catalog/Data/` y `tests/DatabaseTest/Support/`.
  - `tests/IntegrationTest/Support/` (`KnockApiFactory`, `FirebaseEmulator`, `ConsoleCapture`, `AuthHttp`, `TestHttp`), `tests/IntegrationTest/Identity/FirebaseSessionTests`, `tests/SecurityTest/Identity/*` y `tests/UnitTest/Identity/*` como molde.
- **Crear (nombres por confirmar al implementar):**
  - `Modules/Identity/Domain/`: `User` y `UserState` (`activo`, `suspendido`), `ConsentDocuments` (constantes `PrivacyNoticeVersion` y `TermsVersion`), `AuditEvents` (`USER_REGISTERED`), `PhoneNumberPolicy` (`^\+52\d{10}$`).
  - `Modules/Identity/Data/`: `IUserRepository`, `IConsentRepository`, `IAuditRepository` (solo inserción), `IRefreshTokenRepository` y sus implementaciones sobre `IMongoCollection` (único lugar con `IMongoCollection`), la transacción de alta y `IdentityIndexInitializer` (índices idempotentes; falla el arranque si no se pueden crear).
  - `Modules/Identity/Services/`: `PhoneProtector` (AES-256-GCM + HMAC-SHA256 con versión de llave; sin `Unprotect`), `PhoneProtectionOptions` con validación al arrancar (`Identity:PhoneProtection:*`), `RefreshTokenFingerprint` (SHA-256), y un validador `FirebaseExchangeRequestValidator`.
  - `Modules/Identity/IdentityModuleExtensions.cs`: registrar repositorios, inicializador y servicios; pedir `IMongoDatabase` por la clave `Mongo:Databases:Identity`.
  - `tests/ContractTest/` (proyecto nuevo, en `Knock.slnx`), `docs/openapi.v1.json` y `docs/endpoints.md`.
  - Pruebas: `tests/UnitTest/{Identity,Architecture}/`, `tests/IntegrationTest/Identity/`, `tests/SecurityTest/Identity/`, `tests/DatabaseTest/Identity/`, `tests/ContractTest/Identity/` (spec §2).
- **Modificar (declarado por decisión 1 y por la lista de casos):**
  - `Modules/Identity/Services/UserIdDerivation.cs` (UUID v8/SHA-256; se **borra** el SHA-1 propio) y su prueba `tests/UnitTest/Identity/UserIdDerivationTests` (vector nuevo, caso 10).
  - `FirebaseSessionService`, `FirebaseIdTokenVerifier`, `SessionTokenIssuer`, `AuthController` (200/201/403/409), `SessionDtos` (cuerpo y respuesta nuevos) y, según las dudas 4 y 5, `DomainException` + `DomainExceptionHandler` (`code`) y la validación del cuerpo (de `DataAnnotations` a FluentValidation).
  - Pruebas de TT-API-02 que usan el canje o el vector (se editan **solo** en lo que cambia el contrato, con la autorización del líder; duda 8): `UserIdDerivationTests` (v5 → v8, caso 10), `FirebaseSessionTests` (primer canje 201, TC-TT-API-02-01/06/09; ahora con la base), `FirebaseRequestValidationTests` (TC-TT-API-02-11: 400 → 422 si la duda 5 se aprueba; el 413 no cambia), `SensitiveDataLeakTests` (TC-TT-API-02-13; el celular y la base entran), `SessionTokenIssuerTests` (TC-TT-API-02-07, solo si cambia la firma de `Issue`). Los métodos conservan su sufijo `_TC_TT_API_02_KK` para no romper `sdd_lint --trace TT-API-02`.
  - `KnockApiFactory`: llaves de pruebas ficticias de `Identity:PhoneProtection:*`, cadena y bases del contenedor de Mongo, y la ventana corta del limitador para el caso 09.
  - `appsettings.json`: solo `Mongo:Databases:Identity` y las versiones vigentes de llave (sin secretos). `Knock.slnx` y, si hace falta, `Knock.Api.csproj` y `packages.lock.json` (ningún paquete nuevo previsto).
  - `docs/rate-limit-matrix.md`: anotar que CA9 se limita por IP (desviación) y que el canje cuenta en la política `/auth`.
- **No tocar:** `.github/`, `scripts/` (incluido `scripts/export_openapi.py`, duda 6), `.agents/`, configuración de analizadores, `constitution.md`, `ModuleSurfaceTests`, `ModuleBoundaryTests`, `docs/openapi.v1.json` a mano, `Hosting/RateLimitingExtensions.cs`, el catálogo y su base, specs ajenas (incluida la de TT-API-02, Firmada), `AddKnockMongo` (es de TT-BD-06), módulos de negocio.
- **Configuración y secretos:** las llaves AES y HMAC por variable de entorno o user-secrets (Key Vault después, TT-SEC-01); nunca en el repositorio. La cadena `ConnectionStrings:KnockMongo` es de TT-BD-06.

## 2. Riesgos y efectos secundarios
- **Orden del `sub`:** si alguna cuenta se guarda antes del cambio v5 → v8, su `sub` queda mal y no se puede corregir sin romper la identidad. Por eso el cambio va primero (§0) y se verifica con el caso 10.
- **Dependencia de TT-BD-06 (#53, Borrador):** cambia de nombre o de forma `AddKnockMongo` o la fixture y se rompen la integración y la base de datos. Antes de implementar se confirma contra lo fusionado. Si no ha fusionado: spec §3.
- **Pruebas de TT-API-02 que cambian:** son las únicas pruebas ya fusionadas que se editan; cada edición se limita a lo que cambia el contrato y se lista en el PR. Las suites que llaman al canje pasan a necesitar `documentdb-local` (Docker) y llaves de pruebas: `KnockApiFactory` las entrega. Las suites que **no** llegan a la base (token inválido, cuerpo inválido) no deberían necesitarla; el arranque, sin embargo, exige la configuración de Mongo y de llaves (TC-TT-BD-06-14, TC-HU-001-16): la fábrica la provee siempre.
- **Cifrado y llaves:** llave perdida = celulares ilegibles; llave filtrada = celulares descifrables si también se filtra la base (por eso llaves separadas y fuera del repositorio). La rotación de la llave HMAC exige re-hash masivo (límite anotado). Sin llaves la API no arranca (en ningún entorno); hay que documentarlo en el README de ejecución local y en `Memoria.md`.
- **`FirebaseIdentity` con celular:** un `record` imprime todas sus propiedades en `ToString`; si alguien lo registra, el celular sale en el log. Se redacta y lo cubre el caso 19.
- **Carrera de creación:** dos canjes simultáneos del mismo celular. Los índices únicos deciden; el perdedor de la transacción reintenta como cuenta existente. Las transacciones de DocumentDB devuelven `WriteConflict` transitorio: se usa `WithTransaction` (reintento del driver) y el caso 11 lo prueba con 8 canjes en paralelo.
- **Enlace del cuerpo:** un `string` no anulable crea un `[Required]` implícito y el 400 automático de `[ApiController]` llega antes del `ValidationFilter` y puede repetir valores (hallazgo de #54, TT-API-07 caso 15). Con la duda 5 aprobada, los campos se enlazan como anulables con validador; el caso 22 comprueba que ningún valor se repite.
- **CA9 por IP:** quien comparte IP (NAT) comparte el límite; el «mismo dispositivo» no se limita aquí. Desviación aceptada y anotada (decisión 7).
- **Contrato vivo:** `docs/openapi.v1.json` y `tests/ContractTest` no existen y `scripts/export_openapi.py` es protegido (duda 6). Sin el script, el caso 25 queda a medias y el CI que lo espera (`ci-pr.yml`) no lo ejecuta aún. HU-009 depende de este contrato.
- **Firma real del ID token:** el emulador no firma; solo el caso 15 de TT-API-02 (a mano, contra `knock-0`) prueba la firma. Se repite con el canje completo antes de la prueba cerrada.
- **Contraste con TT-FB-01/02:** SMS reales requieren Blaze (ADR-004); hasta entonces solo números de prueba. HU-001 no controla el costo ni los intentos por número.
- **Docker, Azure y Firebase** los opera una persona; las pruebas de integración necesitan Docker (`documentdb-local`) y el emulador de Firebase (`npx firebase-tools`, Node 20, 22 o 24) en marcha, y si no están, **fallan** con mensaje claro (no se saltan).

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-HU-001-01, 02, 05, 06 | knock-frontend | (otro repo) |
| TC-HU-001-03 | `UnitTest/Identity/FirebaseSessionServiceTests` · `IntegrationTest/Identity/FirebaseLoginTests` | unitaria e integración |
| TC-HU-001-04 | `UnitTest/Identity/FirebaseSessionServiceTests` · `IntegrationTest/Identity/FirebaseLoginTests` | unitaria e integración |
| TC-HU-001-07 | `IntegrationTest/Identity/FirebaseLoginRejectionTests` | integración |
| TC-HU-001-08 | `UnitTest/Identity/FirebaseSessionServiceTests` · `IntegrationTest/Identity/FirebaseLoginTests` | unitaria e integración |
| TC-HU-001-09 | `IntegrationTest/Identity/FirebaseLoginRateLimitTests` | integración |
| TC-HU-001-10 | `UnitTest/Identity/UserIdDerivationTests` | unitaria |
| TC-HU-001-11 | `IntegrationTest/Identity/FirebaseLoginConcurrencyTests` · `DatabaseTest/Identity/IdentityIndexTests` | integración y base de datos |
| TC-HU-001-12 | `UnitTest/Identity/FirebaseSessionServiceTests` · `IntegrationTest/Identity/FirebaseLoginTests` | unitaria e integración |
| TC-HU-001-13 | `UnitTest/Identity/PhoneNumberPolicyTests` · `SecurityTest/Identity/PhoneNumberRejectionTests` | unitaria y seguridad negativa |
| TC-HU-001-14 | `UnitTest/Identity/PhoneProtectorTests` · `DatabaseTest/Identity/UserStorageTests` | unitaria y base de datos |
| TC-HU-001-15 | `UnitTest/Identity/PhoneProtectorTests` · `DatabaseTest/Identity/UserLookupTests` | unitaria y base de datos |
| TC-HU-001-16 | `UnitTest/Identity/PhoneProtectionOptionsTests` · `SecurityTest/Identity/PhoneProtectionSecretsTests` | unitaria y seguridad negativa |
| TC-HU-001-17 | `UnitTest/Identity/RefreshTokenFingerprintTests` · `IntegrationTest/Identity/RefreshFingerprintTests` | unitaria e integración |
| TC-HU-001-18 | `IntegrationTest/Identity/FirebaseRegistrationAtomicityTests` | integración |
| TC-HU-001-19 | `SecurityTest/Identity/LoginSensitiveLogTests` | seguridad negativa |
| TC-HU-001-20 | `SecurityTest/Identity/LoginResponsePrivacyTests` | seguridad negativa |
| TC-HU-001-21 | `SecurityTest/Identity/AccountEnumerationTests` | seguridad negativa |
| TC-HU-001-22 | `UnitTest/Identity/FirebaseExchangeRequestValidatorTests` · `SecurityTest/Identity/FirebaseRequestValidationTests` | unitaria y seguridad negativa |
| TC-HU-001-23 | `DatabaseTest/Identity/IdentityIndexTests` · `DatabaseTest/Identity/IdentityCollectionNamingTests` | base de datos |
| TC-HU-001-24 | `UnitTest/Architecture/IdentityAccessTests` | arquitectura |
| TC-HU-001-25 | `ContractTest/Identity/AuthContractTests` | contrato |
| TC-HU-001-26 | `docs/openapi.v1.json`, `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `dotnet list package`, `grep` de SHA-1, `gitleaks` | comando (evidencia en el PR) |
| TC-HU-001-27 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. **Aprobación de la spec** (y respuesta a las dudas 1 a 11). Confirmar que #53 (TT-BD-06) fusionó o decidir con el líder qué se adelanta (spec §3).
2. **`sub` v8** (caso 10): derivación, su prueba con vector independiente, ajuste de las pruebas de TT-API-02 que lo usan. Fusionar antes de cualquier dato.
3. **Contrato** (spec §4) y pruebas que fallan primero: cuerpo y respuestas del canje (200, 201, 401, 403, 409, 422, 429) y proyecto `tests/ContractTest`.
4. **Protección del celular** (casos 13 a 17): `PhoneNumberPolicy`, `PhoneProtector`, `PhoneProtectionOptions` con validación de arranque, `RefreshTokenFingerprint`; unitarias.
5. **Dominio y servicio** con dobles (casos 03, 04, 08, 12 y 22): `User`, `UserState`, flujo del canje, validador, `code` de dominio (dudas 3 a 5).
6. **Datos** (casos 11, 14, 15, 18 y 23; tras TT-BD-06): repositorios en `Data/`, `IdentityIndexInitializer`, transacción de alta, registro en `IdentityModuleExtensions`.
7. **Controller** (200/201/403/409, `Cache-Control: no-store`) e integración con el emulador y `documentdb-local` (casos 03, 04, 07, 08, 09, 11, 12, 17 y 18); seguridad negativa (19 a 22); arquitectura (24).
8. `docs/openapi.v1.json` y `docs/endpoints.md` con el script de la duda 6; `docs/rate-limit-matrix.md`; caso 25.
9. Gates, `Memoria.md`, informe y evidencia en el PR (casos 26 y 27; firma real contra `knock-0` con número de prueba).

## 5. Verificación (comandos exactos)
```bash
docker compose -f deploy/docker-compose.local.yml up -d     # (la fixture de pruebas usa Testcontainers con documentdb-local)
# emulador de Firebase Auth (otra terminal): npx firebase-tools@15.34.0 emulators:start --only auth --project demo-knock
# y FIREBASE_AUTH_EMULATOR_HOST=127.0.0.1:9099
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build            # incluye Architecture/
dotnet test tests/IntegrationTest -c Release --no-build     # requiere Docker y el emulador
dotnet test tests/SecurityTest tests/DatabaseTest tests/ContractTest -c Release --no-build
dotnet list Knock.slnx package --include-transitive --vulnerable
python scripts/export_openapi.py                            # cuando exista (duda 6)
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace HU-001
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
