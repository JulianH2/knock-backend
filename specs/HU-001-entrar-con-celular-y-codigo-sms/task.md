# HU-001 — tareas

> Borrador. Se implementa en Sprint 2, **después de que la spec esté Aprobada**; las tareas T3 en adelante que tocan la base esperan a TT-BD-06 (`plan.md` §0). Las tareas de la capa Mobile son de knock-frontend.

## T1 — `sub` v8 (primero, antes de cualquier dato guardado)
- **Estado:** pendiente de aprobación de la spec
- **Verificar:** TC-HU-001-10
- **Pendiente:** UUID v8 con SHA-256 en `UserIdDerivation` (se borra el SHA-1 propio), vector independiente, y ajuste de las pruebas de TT-API-02 que usaban el vector v5 (duda 8). No depende de TT-BD-06.

## T2 — Contrato del canje
- **Estado:** pendiente
- **Verificar:** TC-HU-001-22 · TC-HU-001-25 · TC-HU-001-26
- **Pendiente:** cuerpo `{ firebaseIdToken, deviceId, aceptaAviso }` (duda 2), respuestas 200/201/401/403/409/422/429, `Cache-Control: no-store`, proyecto `tests/ContractTest`, `docs/openapi.v1.json` y `docs/endpoints.md` (duda 6), excepciones a los principios 11 y 15 anotadas.

## T3 — Protección del celular y huella del refresh
- **Estado:** pendiente
- **Verificar:** TC-HU-001-13 · TC-HU-001-14 · TC-HU-001-15 · TC-HU-001-16 · TC-HU-001-17
- **Pendiente:** `PhoneNumberPolicy`, `PhoneProtector` (AES-256-GCM, HMAC-SHA256, llaves separadas y versión), opciones de llaves con validación de arranque (en todo entorno), `RefreshTokenFingerprint`; `FirebaseIdentity` con celular y `ToString` redactado.

## T4 — Dominio, servicio y validación
- **Estado:** pendiente
- **Verificar:** TC-HU-001-03 · TC-HU-001-04 · TC-HU-001-08 · TC-HU-001-12 · TC-HU-001-21 · TC-HU-001-22
- **Pendiente:** `User` y `UserState`, constantes de versión del aviso y de los términos (duda 7), flujo del canje con dobles (alta, existente, suspendida, conflicto, rechazos antes de la base), validador FluentValidation (duda 5), `code` estable para 403 y 409 (duda 4).

## T5 — Base de datos `identity` (espera a TT-BD-06)
- **Estado:** pendiente (bloqueada por TT-BD-06, `plan.md` §0)
- **Verificar:** TC-HU-001-11 · TC-HU-001-14 · TC-HU-001-15 · TC-HU-001-18 · TC-HU-001-23 · TC-HU-001-24
- **Pendiente:** colecciones `usuarios`, `consentimientos`, `auditoria` y `refresh_tokens` (nombres por duda 1) con sus índices, inicializador idempotente, repositorios solo en `Data/`, transacción de alta, `cache: no` declarado; `Mongo:Databases:Identity` = `identity`.

## T6 — Controller e integración
- **Estado:** pendiente
- **Verificar:** TC-HU-001-03 · TC-HU-001-04 · TC-HU-001-07 · TC-HU-001-08 · TC-HU-001-09 · TC-HU-001-11 · TC-HU-001-12 · TC-HU-001-17 · TC-HU-001-18
- **Pendiente:** `AuthController` con 200/201/403/409; pruebas con emulador de Firebase y `documentdb-local`; adaptación de `KnockApiFactory` (llaves, Mongo, ventana corta); CA9 sobre el limitador existente (sin código nuevo).

## T7 — Seguridad negativa
- **Estado:** pendiente
- **Verificar:** TC-HU-001-13 · TC-HU-001-16 · TC-HU-001-19 · TC-HU-001-20 · TC-HU-001-21 · TC-HU-001-22
- **Pendiente:** logs y respuestas sin celular, `uid`, `sub`, tokens ni huella; sin enumeración; llaves fuera del repositorio.

## T8 — Gates, documentos y evidencia
- **Estado:** pendiente
- **Verificar:** TC-HU-001-26 · TC-HU-001-27
- **Pendiente:** `docs/rate-limit-matrix.md`, evidencia de paquetes (`dotnet list`), gitleaks, ausencia de SHA-1, build y formato; firma real contra `knock-0` con número de prueba; `Memoria.md`.

## T9 — Capa Mobile (knock-frontend)
- **Estado:** otro repositorio
- **Verificar:** TC-HU-001-01 · TC-HU-001-02 · TC-HU-001-05 · TC-HU-001-06
- **Pendiente:** pantallas de celular y código, `verifyPhoneNumber`, cuenta regresiva de 60 s, catálogo de mensajes, cuenta suspendida, `flutter_secure_storage`; consume el contrato de §4.
