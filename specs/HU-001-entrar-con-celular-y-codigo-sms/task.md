# HU-001 — tareas

> Borrador. Se implementa en Sprint 2, **después de que la spec esté Aprobada**; las tareas que tocan la base esperan a TT-BD-06 (`plan.md` §0). Las tareas hijas A y B (claves de Jira «por abrir», las abre el líder) y la capa Mobile no son de este PR. Puntos propuestos (no firmados): HU-001 = 5, A = 1, B = 2 (total 8).

## Previas (tareas hijas, otro issue y otro PR)
- **A — «`sub` v8 con SHA-256» (1 punto, por abrir).** Va **primero**, sin MongoDB; debe fusionar antes de cualquier dato guardado. Caso reubicado A-01 (spec §2). El método de prueba conserva el sufijo `_TC_TT_API_02_10` (D8).
- **B — «Contrato vivo» (2 puntos, por abrir).** `tests/ContractTest`, `docs/openapi.v1.json`, `docs/endpoints.md`; depende de #61 (`scripts/export_openapi.py`, no existe hasta que fusione). Caso reubicado B-01.

## T1 — Contrato del canje (código y reglas; los documentos vivos son de B)
- **Estado:** pendiente de aprobación de la spec
- **Verificar:** TC-HU-001-21 · TC-HU-001-24
- **Pendiente:** cuerpo `{ firebaseIdToken, deviceId, aceptaAviso }` obligatorios en cada canje (`deviceId` lo genera la app y no se autentica, D2); respuestas 200/201/401/403/409/422/429, `Cache-Control: no-store`; excepciones a los principios 11 y 15 anotadas; regenerar `docs/openapi.v1.json` y `docs/endpoints.md` con el script de #61 (sin editar a mano).

## T2 — Protección del celular y huella del refresh
- **Estado:** pendiente
- **Verificar:** TC-HU-001-12 · TC-HU-001-13 · TC-HU-001-14 · TC-HU-001-15 · TC-HU-001-16
- **Pendiente:** `PhoneNumberPolicy`, `PhoneProtector` (AES-256-GCM, HMAC-SHA256, llaves separadas y versión), opciones de llaves con validación de arranque (en todo entorno), `RefreshTokenFingerprint`; `FirebaseIdentity` con celular y `ToString` redactado. Avanza en paralelo a TT-BD-06.

## T3 — Dominio, servicio y validación
- **Estado:** pendiente
- **Verificar:** TC-HU-001-03 · TC-HU-001-04 · TC-HU-001-08 · TC-HU-001-11 · TC-HU-001-20 · TC-HU-001-21
- **Pendiente:** `User` y `UserState`, constantes de versión del aviso y de los términos (fecha AAAA-MM-DD, valor real al implementar, D7), flujo del canje con dobles, validador FluentValidation (422, D5; actualizar TC-TT-API-02-11), `code` en `DomainException` y su manejador solo para `account_suspended` y `account_conflict` (D4).

## T4 — Base de datos `identity` (espera a TT-BD-06)
- **Estado:** pendiente (bloqueada por TT-BD-06, `plan.md` §0)
- **Verificar:** TC-HU-001-10 · TC-HU-001-13 · TC-HU-001-14 · TC-HU-001-17 · TC-HU-001-22 · TC-HU-001-23
- **Pendiente:** colecciones `usuarios`, `consentimientos`, `auditoria` y `refresh_tokens` con sus índices (TTL por `venceEn` solo en `refresh_tokens`, D11), inicializador idempotente, repositorios solo en `Data/`, transacción de alta, `cache: no`; `Mongo:Databases:Identity` = `identity`.

## T5 — Controller e integración
- **Estado:** pendiente
- **Verificar:** TC-HU-001-03 · TC-HU-001-04 · TC-HU-001-07 · TC-HU-001-08 · TC-HU-001-09 · TC-HU-001-10 · TC-HU-001-11 · TC-HU-001-16 · TC-HU-001-17
- **Pendiente:** `AuthController` con 200/201/403/409; pruebas con emulador de Firebase y `documentdb-local`; adaptación de `KnockApiFactory` (llaves, Mongo, ventana corta); CA9 sobre el limitador existente (sin código nuevo).

## T6 — Seguridad negativa
- **Estado:** pendiente
- **Verificar:** TC-HU-001-12 · TC-HU-001-15 · TC-HU-001-18 · TC-HU-001-19 · TC-HU-001-20 · TC-HU-001-21
- **Pendiente:** logs y respuestas sin celular, `uid`, `sub`, tokens ni huella; sin enumeración; llaves fuera del repositorio.

## T7 — Gates, documentos y evidencia
- **Estado:** pendiente
- **Verificar:** TC-HU-001-24 · TC-HU-001-25
- **Pendiente:** `docs/rate-limit-matrix.md`, evidencia de paquetes (`dotnet list`), gitleaks, build y formato; firma real contra `knock-0` con número de prueba; `Memoria.md` (lo escribe el líder).

## T8 — Capa Mobile (knock-frontend)
- **Estado:** otro repositorio
- **Verificar:** TC-HU-001-01 · TC-HU-001-02 · TC-HU-001-05 · TC-HU-001-06
- **Pendiente:** pantallas de celular y código, `verifyPhoneNumber`, cuenta regresiva de 60 s, catálogo de mensajes, cuenta suspendida, `flutter_secure_storage`; consume el contrato de §4.
