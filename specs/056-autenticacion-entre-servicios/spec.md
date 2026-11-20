# Spec 056 — Autenticación entre servicios (TT-ARQ-05)

> **Estado:** Borrador · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Jira:** KNOCK-162 (TT-ARQ-05 · Autenticación entre servicios) · **Épica:** Arquitectura (TT-ARQ) · **Módulo:** gateway, Identidad, Mercado y Mensajería · **Responsable:** Guillermo (G, según `docs/PLAN_ADAPTACION.md`) · **Sprint:** S3 · **Puntos:** 2 · **Capas:** Back (la parte de Terraform y Key Vault la decide el líder, ver §8)
> **Otra parte en:** — (no hay cambio de contrato público ni de la app)
> **Fuentes:** `docs/PLAN_ADAPTACION.md` (línea 129) · `docs/DECISION_TECNICA.md` (API interna y autenticación) · `.specify/memory/constitution.md` (microservicios con una puerta de entrada; servicios internos sin dirección pública) · spec 009 (§9, nota del 2026-11-12) · spec 037 (R-11) · spec 041 (R-17) · spec 054 (R-15, D-04) · spec 055 (biblioteca técnica) · `docs/decisiones/ADR-001-microservicios.md`
> **Skills:** spec-first-writing, story-workflow, security-core, auth-firebase-jwt, testing-strategy-backend, traceability

## 1. Objetivo
Dejar **una regla general y comprobable** para toda llamada interna entre servicios (gateway, Identidad, Mercado y Mensajería): quien llama se prueba con una credencial de servicio propia, distinta de la sesión de la persona, y cada servicio solo acepta lo que su emisor tiene permitido.

**Situación de hoy (verificada en el código y las specs, 2026-11-19):**
- Spec 009 (§9): la única llamada interna reenvía el `Authorization` del usuario y `jwt-signing-key` es **una sola clave compartida** por el gateway, Identidad y Mercado (`deploy/apps/main.tf`, `Jwt__SigningKey`). Quien comprometa una copia puede firmar tokens de usuario. La spec 009 dejó la regla general «en una spec propia»: esta.
- Spec 037 (R-11) define, para `POST /api/v1/interno/avisos` de Mensajería, un token HS256 con clave HMAC **por emisor** (`avisos-key-market`, `avisos-key-identity`), `iss`, `aud`, `exp` de 5 minutos como máximo, `jti` y el claim `permisos`. Mensajería aún no lo implementa.
- Spec 041 (R-17) y `src/Knock.Market.Api/Hosting/InternalServiceAuthentication.cs` ya implementan ese esquema para `GET /api/v1/interno/historial` de Mercado (Identidad firma con `interno-key-identity`). Es la única verificación de servicio que existe en el código.
- Las specs usan dos nombres de secreto (`avisos-key-*` en la 037 e `interno-key-identity` en la 041 y la 054 D-04) y no existe una regla común que diga cómo se nombran, rotan o prueban.

Esta spec **no sustituye** el esquema de la 037 y la 041: lo toma como base, lo generaliza y fija las propiedades que todo servicio debe cumplir. **Debe convivir con las claves por emisor de la 037 R-11**: no las reemplaza, no las fusiona en una sola y no reutiliza `jwt-signing-key`.

## 2. Historia y casos de prueba
**TT-ARQ-05** — Como equipo de plataforma, quiero que cada llamada interna entre servicios demuestre quién la hace y qué tiene permitido, para que un servicio comprometido o una petición de fuera de la red no pueda hacerse pasar por otro ni escalar privilegios.

Reglas (la mecánica que dependa de una decisión abierta está en §8):
- **R-01 (credencial de servicio propia):** toda ruta interna exige una credencial de servicio **distinta** del JWT de usuario. Nunca se acepta el JWT de usuario en una ruta interna ni la credencial de servicio en una ruta pública. No se reutiliza `jwt-signing-key`.
- **R-02 (emisor y destinatario):** la credencial dice quién la emite (`iss`) y para qué servicio es (`aud`); el receptor rechaza otro `iss` o `aud`.
- **R-03 (permisos por emisor):** la credencial lleva los permisos del emisor y la ruta exige el suyo. Sin permiso: 403 `service_not_allowed`. Sin credencial válida: 401.
- **R-04 (vida corta):** la credencial vive 5 minutos como máximo, con `jti` e `iat`; una sin límite o con vida mayor se rechaza. `ClockSkew` en cero, algoritmos fijos y firma obligatoria.
- **R-05 (clave aislada por emisor):** cada emisor firma con su propia clave y cada servicio lee solo la suya (para firmar) y las que necesita para verificar. Comprometer la clave de un servicio no permite emitir como otro.
- **R-06 (falla cerrada):** sin clave válida configurada, el servicio rechaza toda llamada interna; nunca acepta por omisión.
- **R-07 (ruta no alcanzable de fuera):** ninguna ruta interna (`/api/v1/interno/*`) está en la lista cerrada de rutas del gateway ni tiene ingress público.
- **R-08 (sin secretos en registros ni repositorio):** claves y credenciales no aparecen en el repositorio, la configuración del código, los registros ni las respuestas.
- **R-09 (convivencia):** el contrato y las pruebas de las specs 037 (R-11) y 041 (R-17) no cambian de comportamiento por esta spec.
- **R-10 (replay y rotación):** la protección contra repetición de una credencial y la rotación de claves quedan sujetas a las decisiones de §8 (P-02 y P-03); esta spec no las fija inventándolas.

**Criterios de aceptación (casos):**
- **CA-01:** Dada una ruta interna, cuando se llama sin credencial, con una firmada con otra clave o con un algoritmo distinto de HS256, entonces responde 401 y no ejecuta la operación. (R-01, R-04)
- **CA-02:** Dada una credencial de servicio, cuando el `iss` o el `aud` no corresponden, o está vencida, o su vida (`exp` menos `iat`) excede 5 minutos, o no trae `jti`, entonces responde 401. (R-02, R-04)
- **CA-03:** Dada una credencial válida sin el permiso que pide la ruta, cuando llama, entonces responde 403 con código `service_not_allowed` y la operación no se ejecuta. (R-03)
- **CA-04:** Dado un JWT de usuario válido (firmado con `jwt-signing-key`), cuando se envía a una ruta interna, entonces responde 401; y dada una credencial de servicio válida, cuando se envía a una ruta pública, entonces responde 401. (R-01)
- **CA-05:** Dada una credencial que declara el `iss` de un emisor pero está firmada con la clave de otro emisor, cuando llega a un receptor que verifica a ambos, entonces responde 401. (R-05)
- **CA-06:** Dado un servicio sin clave configurada o con una clave mal escrita o menor a 32 bytes, cuando recibe una llamada interna, entonces la rechaza con 401 y ningún registro contiene el valor de la clave. (R-06, R-08)
- **CA-07:** Dadas las rutas `/api/v1/interno/*`, cuando se piden a través del gateway, entonces no se enrutan; y la configuración del gateway no contiene ninguna ruta interna. (R-07)
- **CA-08:** Dada una credencial o una clave de prueba, cuando se revisan el repositorio, la configuración y los registros de una llamada rechazada o aceptada, entonces ningún valor secreto aparece. (R-08)
- **CA-09:** Dadas las pruebas de las specs 037 (R-11) y 041 (R-17), cuando se corren con el cambio, entonces siguen en verde sin editarse. (R-09)
- **CA-10:** Dada una misma credencial presentada dos veces dentro de su vida, cuando la regla de §8 P-03 esté decidida, entonces el comportamiento acordado (aceptar de forma idempotente o rechazar la segunda) queda probado. **Pendiente de decisión.** (R-10)
- **CA-11:** Dada una rotación de clave, cuando la regla de §8 P-02 esté decidida, entonces una credencial firmada con la clave anterior se acepta o se rechaza según lo acordado. **Pendiente de decisión.** (R-10)

| Caso | Criterio (resumen) | Verificación |
|---|---|---|
| CA-01 | Sin credencial, otra clave o algoritmo distinto → 401 | `tests/SecurityTest/InternalAuth/ServiceAuthenticationSecurityTests.cs` → `InvalidCredential_Returns401_CA_056_01` |
| CA-02 | `iss` o `aud` incorrectos, vencida, vida mayor a 5 min o sin `jti` → 401 | `tests/UnitTest/Hosting/ServiceTokenValidationTests.cs` → `Claims_AreValidated_CA_056_02` |
| CA-03 | Sin permiso → 403 `service_not_allowed` | `tests/SecurityTest/InternalAuth/ServiceAuthenticationSecurityTests.cs` → `MissingPermission_Returns403_CA_056_03` |
| CA-04 | JWT de usuario en ruta interna y credencial de servicio en ruta pública → 401 | `tests/SecurityTest/InternalAuth/ServiceAuthenticationSecurityTests.cs` → `SchemesAreNotInterchangeable_CA_056_04` |
| CA-05 | Un emisor no puede firmar como otro → 401 | `tests/SecurityTest/InternalAuth/ServiceAuthenticationSecurityTests.cs` → `IssuerKeyMismatch_Returns401_CA_056_05` |
| CA-06 | Sin clave válida, falla cerrada y no se registra el valor | `tests/UnitTest/Hosting/ServiceTokenKeyTests.cs` → `InvalidKey_FailsClosed_CA_056_06` |
| CA-07 | Rutas internas no enrutadas por el gateway | `tests/IntegrationTest/Gateway/InternalRoutesNotExposedTests.cs` → `InternalRoutes_AreNotRouted_CA_056_07` |
| CA-08 | Sin secretos en repositorio, configuración y registros | `tests/SecurityTest/InternalAuth/ServiceSecretsNotLoggedTests.cs` → `Secrets_DoNotAppear_CA_056_08` · y `gitleaks` (comando, salida en el PR) |
| CA-09 | Regresión de las specs 037 (R-11) y 041 (R-17) | Suites de las specs 037 y 041 sin editar · (comando) `dotnet test tests/IntegrationTest -c Release --no-build` |
| CA-10 | Replay de una credencial (según §8 P-03) | **Pendiente de decisión**: prueba por definir (`..._CA_056_10`) |
| CA-11 | Rotación de clave (según §8 P-02) | **Pendiente de decisión**: prueba por definir (`..._CA_056_11`) |

> Convención: las specs numeradas usan casos `CA-NN` y la prueba lleva `CA_056_NN` en el nombre (`python scripts/sdd_lint.py --trace 056`). Equivalen a los `TC-<ID>-KK` de la plantilla; no se crea un segundo esquema de numeración. Las rutas de prueba son las planeadas (aún no existen) y se confirman al implementar.

## 3. Alcance / No-alcance
- **In (este repositorio, una vez Aprobada la spec):**
  - Definir y probar la regla común de credencial de servicio (R-01 a R-09) y, según §8 P-01, reutilizar o extraer la parte técnica reutilizable de `InternalServiceAuthentication` (hoy solo en Mercado) a un lugar compartido que permita la constitución (spec 055).
  - Que el receptor de `POST /api/v1/interno/avisos` (Mensajería, spec 037) y el de `GET /api/v1/interno/historial` (Mercado, spec 041) cumplan la regla común sin cambiar su contrato.
  - Pruebas de seguridad negativa, de arquitectura y de integración de §2.
  - Registrar cada decisión de §8 cuando el líder la tome y actualizar `docs/endpoints.md` si cambia el esquema de alguna ruta interna.
- **Out:**
  - Cambiar el contrato de `POST /api/v1/interno/avisos` (spec 037) o de `GET /api/v1/interno/historial` (spec 041).
  - Alta, creación y permisos por secreto en Key Vault y Terraform (`deploy/**`): lo decide y ejecuta el líder (§8 P-04). Esta spec solo dice qué se necesita.
  - Operar Azure, producción o bases reales.
  - Reemplazar el JWT de usuario o cambiar `jwt-signing-key` y los flujos de las specs 009, 015 y 016. Repartir `jwt-signing-key` entre gateway, Identidad y Mercado se decide aparte (§8 P-05).
  - mTLS, malla de servicios o gRPC (`DECISION_TECNICA.md` los difiere hasta que se separe un módulo en servicio); solo se evalúan como alternativa en §8 P-01.
  - Tráfico de personas, límites de peticiones, CORS y cabeceras (spec 009).

## 4. Contratos API y eventos
- **Endpoints nuevos:** ninguno. **Eventos nuevos:** ninguno.
- **Rutas internas afectadas (sin cambio de contrato):** `POST /api/v1/interno/avisos` (Mensajería, spec 037 R-11) y `GET /api/v1/interno/historial` (Mercado, spec 041 R-17). Ninguna aparece en el OpenAPI público ni en el gateway.
- **Credencial de servicio (propiedades que fija esta spec; la mecánica exacta está en §8 P-01):** encabezado `Authorization: Bearer <credencial de servicio>`; token firmado con claims `iss` (emisor), `aud` (servicio destino), `exp`, `iat`, `jti` y `permisos`; vida de 5 minutos como máximo; clave por emisor; algoritmo fijo.
- **Errores (sin cambio de forma):** ProblemDetails; `401` sin credencial válida; `403` con código `service_not_allowed` sin permiso del emisor para la ruta.
- **Documentos vivos:** `docs/endpoints.md` solo si cambia el esquema declarado de una ruta interna; `docs/openapi.v1.json` no cambia (las rutas son internas).

## 5. Modelo de datos
- Ninguna colección nueva por defecto. Si P-03 decide guardar los `jti` usados para evitar replay, se guardarían en Redis con TTL igual a la vida de la credencial (no en MongoDB ni en memoria del proceso); queda sujeto a la decisión del líder.

## 6. Seguridad, privacidad y amenazas
Análisis proporcional: cambio de autenticación y permisos entre servicios; sin datos personales nuevos.

| Amenaza (STRIDE) | Control | Caso que lo prueba |
|---|---|---|
| **Suplantación:** un proceso de la red o un servicio comprometido llama a otro sin ser quien dice | Credencial de servicio firmada, `iss` y `aud`, falla cerrada sin clave | CA-01, CA-02, CA-06 |
| **Suplantación entre emisores:** un servicio firma como otro | Clave aislada por emisor; la verificación liga `iss` con su clave | CA-05 |
| **Suplantación con el JWT de usuario:** usar un token de persona en una ruta interna (o al revés) | Esquemas separados, sin reutilizar `jwt-signing-key` | CA-04 |
| **Repetición (replay):** capturar una credencial y reenviarla dentro de su vida | Vida de 5 minutos como máximo y `jti` único; protección adicional **según §8 P-03** | CA-02 y CA-10 (pendiente) |
| **Rotación de claves:** una clave vieja o comprometida sigue válida, o una rotación corta las llamadas | Claves por emisor aisladas; regla de rotación **según §8 P-02** | CA-11 (pendiente) |
| **Elevación de privilegios:** un emisor llama rutas o tipos que no le tocan | Claim `permisos` y política por ruta; 403 `service_not_allowed` | CA-03 |
| **Alteración:** modificar la credencial en tránsito | Firma HS256 obligatoria, algoritmos fijos, `ClockSkew` en cero | CA-01, CA-02 |
| **Exposición de ruta interna:** llegar a `/interno` desde internet | Ingress privado y rutas fuera de la lista cerrada del gateway | CA-07 |
| **Fuga:** claves o credenciales en repositorio, registros o respuestas | Redacción de registros (specs 011 y 035), mensajes sin valores, `gitleaks` | CA-06, CA-08 |
| **Denegación:** una clave inválida tumba el servicio o la validación se vuelve cuello de botella | Verificación local sin llamada de red; una clave inválida cierra las rutas internas, no el servicio | CA-06 |
| **Repudio:** no saber qué servicio hizo una llamada | `iss` y `jti` en el registro del receptor (sin la credencial) | CA-08 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Validación de claims, vida máxima, lectura de clave y falla cerrada (xUnit, `TimeProvider` falso) |
| Arquitectura | Sí | Ningún servicio lee la clave de otro emisor ni referencia código de otro servicio; la pieza compartida vive donde la constitución lo permita (spec 055) |
| Integración | Sí | Receptores reales con credenciales válidas e inválidas; gateway que no enruta `/interno` (Testcontainers) |
| Seguridad negativa | Sí | Todos los casos de §6: sin credencial, `iss` y `aud`, algoritmo, JWT de usuario, emisor cruzado, sin permiso |
| Base de datos | No | No se agrega ni cambia ninguna colección (salvo lo que decida P-03) |
| Contrato | Sí (acotado) | Las rutas internas no están en el OpenAPI público; se verifica que no se agreguen |
| Mutación | Sí (acotada) | Meta del módulo: reglas de validación de la credencial (vida, `iss`, `aud`, `permisos`) |
| Fuzz, carga y caos | No | Sin superficie pública nueva; el costo de validar localmente es despreciable |

**Gates:** ver `AGENTS.md §5` (Definición de terminado).

**Límites y dudas abiertas** (este borrador no las resuelve; las decide el líder y se anotan aquí al decidirse):
- **P-01 — Mecanismo exacto.** El borrador toma como base el esquema ya implementado y especificado (HS256 por emisor, 037 R-11 y 041 R-17). ¿Se confirma para todo el sistema o se evalúan alternativas (firma asimétrica, identidad administrada de Azure con Entra ID, mTLS del ingress)? ¿Dónde vive la pieza reutilizable: `Knock.Hosting` (spec 055, cuyo catálogo hoy no la incluye) o una copia por servicio?
- **P-02 — Rotación de claves.** Periodicidad, ventana de solapamiento (aceptar dos claves a la vez), quién la ejecuta y cómo se rota sin cortar llamadas. Hoy no existe procedimiento.
- **P-03 — Replay.** ¿Basta la vida de 5 minutos con `jti`, o se guarda el `jti` usado (por ejemplo en Redis con TTL)? Los receptores de la 037 ya deduplican por `eventoId`, lo que vuelve idempotente la entrega de avisos.
- **P-04 — Key Vault y Terraform.** Nombres y permisos por secreto (la 037 usa `avisos-key-market` y `avisos-key-identity`; la 041 y `deploy/apps/main.tf` usan `interno-key-identity`), qué servicio lee cuáles y cómo se crean. Es del líder; esta spec no toca `deploy/**`.
- **P-05 — `jwt-signing-key` compartida.** Esta spec la deja intacta (Out). ¿Se abre otra spec o tarea para que el gateway solo verifique y solo Identidad firme (clave asimétrica o claves separadas)?
- **P-06 — Convención de nombres.** ¿Un solo prefijo de secreto (`interno-key-*`) para todas las rutas internas, o se mantienen los `avisos-key-*` de la 037? Cambiar un nombre ya especificado afecta a las specs 037, 039 y 054.
- **P-07 — Llamadas del gateway hacia los servicios.** Hoy el gateway reenvía el `Authorization` del usuario. ¿Basta eso (el servicio verifica el JWT de usuario) o el gateway también debe presentar una credencial de servicio? El borrador no lo cambia.
- **P-08 — Casos CA-10 y CA-11** quedan sin prueba concreta hasta decidir P-03 y P-02.

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
