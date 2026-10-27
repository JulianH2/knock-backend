# plan.md — HU-001 — Entrar con mi celular y código SMS

> Spec Aprobada. Las decisiones 1 a 9 del líder (2026-10-24) están en spec §8; las 11 dudas de la primera redacción quedaron resueltas como D1 a D11 (spec §8); el `sub` v8 salió a TT-API-12 (KNOCK-187) y el contrato vivo es TT-CI-02 (KNOCK-137) (D10); falta la aprobación de la spec.

## 0. Cuándo y en qué orden se implementa
**Implementación en Sprint 2.** Esta tarea es solo la spec.

1. **TT-API-12 (KNOCK-187) «`sub` v8 con SHA-256» (1 punto, ya abierta): primero y antes de cualquier dato guardado** (decisión 1, D10). Sin base de datos ni MongoDB: `UserIdDerivation` (UUID v8 con SHA-256, se borra el SHA-1 propio), su prueba (caso A-01, spec §2 «Casos reubicados») y las pruebas de TT-API-02 que usaban el vector v5. Debe fusionar antes de que exista la primera cuenta persistida; HU-001 depende de ella.
2. **TT-CI-02 (KNOCK-137) «Contrato vivo» (2 puntos, a cargo del líder, Sprint 2):** crea `tests/ContractTest`, `docs/openapi.v1.json` y `docs/endpoints.md`; usa `scripts/export_openapi.py`, que ya existe en `sprint-01` (PR #61 fusionado). **Dependencia de HU-001:** se apoya en ese proyecto y solo agrega la prueba de contrato del canje; el caso de contrato se redacta en TT-CI-02. La prueba del canje cierra cuando HU-001 implemente 201, 403, 409 y 422 (HU-001 regenera el contrato con sus cambios).
3. **HU-001 (5 puntos) depende de TT-API-12 y espera a TT-BD-06** (KNOCK-156, #53) para todo lo que toca la base: `AddKnockMongo`, `Mongo:Databases:Identity` y la fixture de `documentdb-local`. **Ruta crítica:** HU-001 depende de TT-BD-06; lo que no toca la base (cifrado, opciones de llaves, validador, huella, política de celular) avanza en paralelo. Si no ha fusionado, ver spec §3 «Dependencia de TT-BD-06»: se adelanta lo que se prueba con dobles y **no** se crea un cliente de Mongo propio.
4. El resto sigue el orden de `AGENTS.md §1`: contrato (spec §4) → colecciones e índices → dominio y servicio + unitarias → controller + integración y seguridad negativa → regenerar `docs/openapi.v1.json` con el script de #61 → gates → `Memoria.md` → informe.

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
  - **No se crean aquí** `tests/ContractTest/`, `docs/openapi.v1.json` ni `docs/endpoints.md`: son de TT-CI-02 (KNOCK-137); HU-001 agrega `tests/ContractTest/Identity/AuthContractTests` (prueba del canje) sobre ese proyecto.
  - Pruebas: `tests/UnitTest/{Identity,Architecture}/`, `tests/IntegrationTest/Identity/`, `tests/SecurityTest/Identity/`, `tests/DatabaseTest/Identity/`, `tests/ContractTest/Identity/` (spec §2).
- **Modificar (declarado por decisión 1 y por la lista de casos):**
  - `Modules/Identity/Services/UserIdDerivation.cs` (UUID v8/SHA-256; se **borra** el SHA-1 propio) y su prueba `tests/UnitTest/Identity/UserIdDerivationTests` (vector nuevo): **es de TT-API-12**, no de esta historia; HU-001 la consume.
  - `FirebaseSessionService`, `FirebaseIdTokenVerifier`, `SessionTokenIssuer`, `AuthController` (200/201/403/409), `SessionDtos` (cuerpo y respuesta nuevos) y, según D4 (`code` solo para `account_suspended` y `account_conflict`) y D5 (422), `DomainException` + `DomainExceptionHandler` (`code`) y la validación del cuerpo (de `DataAnnotations` a FluentValidation).
  - Pruebas de TT-API-02 que usan el canje o el vector (se editan **solo** en lo que cambia el contrato, con la autorización del líder; D8): `UserIdDerivationTests` (v5 → v8; TT-API-12, con el método conservando `_TC_TT_API_02_10`), `FirebaseSessionTests` (primer canje 201, TC-TT-API-02-01/06/09; ahora con la base), `FirebaseRequestValidationTests` (TC-TT-API-02-11: 400 → 422 (D5); el 413 no cambia), `SensitiveDataLeakTests` (TC-TT-API-02-13; el celular y la base entran), `SessionTokenIssuerTests` (TC-TT-API-02-07, solo si cambia la firma de `Issue`). Los métodos conservan su sufijo `_TC_TT_API_02_KK` para no romper `sdd_lint --trace TT-API-02`.
  - `KnockApiFactory`: llaves de pruebas ficticias de `Identity:PhoneProtection:*`, cadena y bases del contenedor de Mongo, y la ventana corta del limitador para el caso 09.
  - `appsettings.json`: solo `Mongo:Databases:Identity` y las versiones vigentes de llave (sin secretos). `Knock.slnx` y, si hace falta, `Knock.Api.csproj` y `packages.lock.json` (ningún paquete nuevo previsto).
  - `docs/rate-limit-matrix.md`: anotar que CA9 se limita por IP (desviación) y que el canje cuenta en la política `/auth`.
- **No tocar:** `.github/`, `scripts/` (incluido `scripts/export_openapi.py`, ya fusionado en #61, D6), `.agents/`, configuración de analizadores, `constitution.md`, `ModuleSurfaceTests`, `ModuleBoundaryTests`, `docs/openapi.v1.json` a mano, `Hosting/RateLimitingExtensions.cs`, el catálogo y su base, specs ajenas (incluida la de TT-API-02, Firmada), `AddKnockMongo` (es de TT-BD-06), módulos de negocio.
- **Configuración y secretos:** las llaves AES y HMAC por variable de entorno o user-secrets (Key Vault después, TT-SEC-01); nunca en el repositorio. La cadena `ConnectionStrings:KnockMongo` es de TT-BD-06.

## 2. Riesgos y efectos secundarios
- **Orden del `sub`:** si alguna cuenta se guarda antes del cambio v5 → v8, su `sub` queda mal y no se puede corregir sin romper la identidad. Por eso el cambio va primero (§0) y se verifica en TT-API-12 (caso A-01).
- **Dependencia de TT-BD-06 (#53, Borrador):** cambia de nombre o de forma `AddKnockMongo` o la fixture y se rompen la integración y la base de datos. Antes de implementar se confirma contra lo fusionado. Si no ha fusionado: spec §3.
- **Pruebas de TT-API-02 que cambian:** son las únicas pruebas ya fusionadas que se editan; cada edición se limita a lo que cambia el contrato y se lista en el PR. Las suites que llaman al canje pasan a necesitar `documentdb-local` (Docker) y llaves de pruebas: `KnockApiFactory` las entrega. Las suites que **no** llegan a la base (token inválido, cuerpo inválido) no deberían necesitarla; el arranque, sin embargo, exige la configuración de Mongo y de llaves (TC-TT-BD-06-14, TC-HU-001-15): la fábrica la provee siempre.
- **Cifrado y llaves:** llave perdida = celulares ilegibles; llave filtrada = celulares descifrables si también se filtra la base (por eso llaves separadas y fuera del repositorio). La rotación de la llave HMAC exige re-hash masivo (límite anotado). Sin llaves la API no arranca (en ningún entorno); hay que documentarlo en el README de ejecución local y en `Memoria.md`.
- **`FirebaseIdentity` con celular:** un `record` imprime todas sus propiedades en `ToString`; si alguien lo registra, el celular sale en el log. Se redacta y lo cubre el caso 18.
- **Carrera de creación:** dos canjes simultáneos del mismo celular. Los índices únicos deciden; el perdedor de la transacción reintenta como cuenta existente. Las transacciones de DocumentDB devuelven `WriteConflict` transitorio: se usa `WithTransaction` (reintento del driver) y el caso 10 lo prueba con 8 canjes en paralelo.
- **Enlace del cuerpo:** un `string` no anulable crea un `[Required]` implícito y el 400 automático de `[ApiController]` llega antes del `ValidationFilter` y puede repetir valores (hallazgo de #54, TT-API-07 caso 15). Con la D5 aprobada, los campos se enlazan como anulables con validador; el caso 21 comprueba que ningún valor se repite.
- **CA9 por IP:** quien comparte IP (NAT) comparte el límite; el «mismo dispositivo» no se limita aquí. Desviación aceptada y anotada (decisión 7).
- **Contrato vivo (TT-CI-02, KNOCK-137):** `docs/openapi.v1.json` y `tests/ContractTest` aún no existen (los crea TT-CI-02); `scripts/export_openapi.py` ya existe en `sprint-01` (#61, D6). Sin esos tres, esta historia no puede regenerar el contrato y el CI que lo espera (`ci-pr.yml`) no lo ejecuta aún. HU-009 y TT-API-03 dependen de TT-CI-02.
- **`/auth/*` a 10 por minuto por IP:** el grupo o el profesor entrando desde la misma red el día de la evaluación pueden bloquearse entre sí; el valor es configuración y se sube en la ventana de evaluación. HU-009 depende de este contrato.
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
| TC-HU-001-10 | `IntegrationTest/Identity/FirebaseLoginConcurrencyTests` · `DatabaseTest/Identity/IdentityIndexTests` | integración y base de datos |
| TC-HU-001-11 | `UnitTest/Identity/FirebaseSessionServiceTests` · `IntegrationTest/Identity/FirebaseLoginTests` | unitaria e integración |
| TC-HU-001-12 | `UnitTest/Identity/PhoneNumberPolicyTests` · `SecurityTest/Identity/PhoneNumberRejectionTests` | unitaria y seguridad negativa |
| TC-HU-001-13 | `UnitTest/Identity/PhoneProtectorTests` · `DatabaseTest/Identity/UserStorageTests` | unitaria y base de datos |
| TC-HU-001-14 | `UnitTest/Identity/PhoneProtectorTests` · `DatabaseTest/Identity/UserLookupTests` | unitaria y base de datos |
| TC-HU-001-15 | `UnitTest/Identity/PhoneProtectionOptionsTests` · `SecurityTest/Identity/PhoneProtectionSecretsTests` | unitaria y seguridad negativa |
| TC-HU-001-16 | `UnitTest/Identity/RefreshTokenFingerprintTests` · `IntegrationTest/Identity/RefreshFingerprintTests` | unitaria e integración |
| TC-HU-001-17 | `IntegrationTest/Identity/FirebaseRegistrationAtomicityTests` | integración |
| TC-HU-001-18 | `SecurityTest/Identity/LoginSensitiveLogTests` | seguridad negativa |
| TC-HU-001-19 | `SecurityTest/Identity/LoginResponsePrivacyTests` | seguridad negativa |
| TC-HU-001-20 | `SecurityTest/Identity/AccountEnumerationTests` | seguridad negativa |
| TC-HU-001-21 | `UnitTest/Identity/FirebaseExchangeRequestValidatorTests` · `SecurityTest/Identity/FirebaseRequestValidationTests` | unitaria y seguridad negativa |
| TC-HU-001-22 | `DatabaseTest/Identity/IdentityIndexTests` · `DatabaseTest/Identity/IdentityCollectionNamingTests` | base de datos |
| TC-HU-001-23 | `UnitTest/Architecture/IdentityAccessTests` | arquitectura |
| TC-HU-001-24 | `docs/openapi.v1.json` y `docs/endpoints.md` regenerados, `docs/rate-limit-matrix.md`, `dotnet list package`, `gitleaks` | comando (evidencia en el PR) |
| TC-HU-001-25 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

## 4. Secuencia
1. **Aprobación de la spec** (D1 a D11 ya resueltas; HU-001 = 5 puntos confirmados; falta fusionar TT-API-12 y TT-BD-06). Confirmar que #53 (TT-BD-06) fusionó o decidir con el líder qué se adelanta (spec §3).
2. **TT-API-12** (`sub` v8; caso A-01): derivación, prueba con vector independiente y ajuste de las pruebas de TT-API-02 que lo usan. Fusiona antes de cualquier dato; HU-001 la espera.
3. **Contrato** (spec §4) y pruebas que fallan primero: cuerpo y respuestas del canje (200, 201, 401, 403, 409, 422, 429) y prueba de contrato del canje sobre el `tests/ContractTest` de TT-CI-02.
4. **Protección del celular** (casos 12 a 16): `PhoneNumberPolicy`, `PhoneProtector`, `PhoneProtectionOptions` con validación de arranque, `RefreshTokenFingerprint`; unitarias.
5. **Dominio y servicio** con dobles (casos 03, 04, 08, 11 y 21): `User`, `UserState`, flujo del canje, validador, `code` de dominio (D3 a D5).
6. **Datos** (casos 10, 13, 14, 17 y 22; tras TT-BD-06): repositorios en `Data/`, `IdentityIndexInitializer`, transacción de alta, registro en `IdentityModuleExtensions`.
7. **Controller** (200/201/403/409, `Cache-Control: no-store`) e integración con el emulador y `documentdb-local` (casos 03, 04, 07, 08, 09, 10, 11, 16 y 17); seguridad negativa (18 a 21); arquitectura (23).
8. Regenerar `docs/openapi.v1.json` y `docs/endpoints.md` con el script de #61 (D6) y agregar la prueba de contrato del canje sobre el proyecto `tests/ContractTest` de TT-CI-02; `docs/rate-limit-matrix.md`.
9. Gates, `Memoria.md`, informe y evidencia en el PR (casos 24 y 25; firma real contra `knock-0` con número de prueba).

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
python scripts/export_openapi.py                            # ya existe en sprint-01 (#61, D6)
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace HU-001
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
