# Spec 056 — Autenticación entre servicios (TT-ARQ-05)

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Jira:** KNOCK-162 (TT-ARQ-05 · Autenticación entre servicios) · **Épica:** Arquitectura (TT-ARQ) · **Módulo:** gateway, Identidad, Mercado y Mensajería · **Responsable:** Guillermo (G, según `docs/PLAN_ADAPTACION.md`) · **Sprint:** S3 · **Puntos:** 2 · **Capas:** Back (la parte de Terraform y Key Vault la decide el líder, ver §8)
> **Otra parte en:** — (no hay cambio de contrato público ni de la app)
> **Fuentes:** `docs/PLAN_ADAPTACION.md` (línea 129) · `docs/DECISION_TECNICA.md` (API interna y autenticación) · `.specify/memory/constitution.md` (microservicios con una puerta de entrada; servicios internos sin dirección pública) · spec 009 (§9, nota del 2026-11-12) · spec 037 (R-11) · spec 041 (R-17) · spec 054 (R-15, D-04) · spec 055 (biblioteca técnica) · `docs/decisiones/ADR-001-microservicios.md`
> **Skills:** spec-first-writing, story-workflow, security-core, auth-firebase-jwt, testing-strategy-backend, traceability

## 1. Objetivo
Dejar **una regla general y comprobable** para toda llamada interna entre servicios (gateway, Identidad, Mercado y Mensajería): quien llama se prueba con una credencial de servicio propia, distinta de la sesión de la persona, y cada servicio solo acepta lo que su emisor tiene permitido.

**Situación de hoy (verificada en el código y las specs, 2026-11-19):**
- Spec 009 (§9): la única llamada interna reenvía el `Authorization` del usuario y `jwt-signing-key` es **una sola clave compartida** por el gateway, Identidad y Mercado (`deploy/apps/main.tf`, `Jwt__SigningKey`). Quien comprometa una copia puede firmar tokens de usuario. La spec 009 dejó la regla general «en una spec propia»: esta.
- Spec 026 (R-06, R-08, R-09): Mensajería comprueba la pertenencia a una visita llamando a `GET /api/v1/visitas/{id}` de Mercado **con el JWT del usuario** que se conecta, no con una credencial de servicio. Es una ruta pública de Mercado, no una ruta interna, y esta spec **no cambia ese flujo** (ver R-11 y §8 P-07).
- Spec 037 (R-11) define, para `POST /api/v1/interno/avisos` de Mensajería, un token HS256 con clave HMAC **por emisor** (`avisos-key-market`, `avisos-key-identity`), `iss`, `aud`, `exp` de 5 minutos como máximo, `jti` y el claim `permisos`. Mensajería aún no lo implementa.
- Spec 041 (R-17) y `src/Knock.Market.Api/Hosting/InternalServiceAuthentication.cs` ya implementan ese esquema para `GET /api/v1/interno/historial` de Mercado (Identidad firma con `interno-key-identity`). Es la única verificación de servicio que existe en el código.
- Las specs usan dos nombres de secreto (`avisos-key-*` en la 037 e `interno-key-identity` en la 041 y la 054 D-04) y no existe una regla común que diga cómo se nombran, rotan o prueban.

Además de esa llamada de Identidad (que sí usa credencial de servicio), las llamadas que reenvían el `Authorization` del usuario son las del gateway hacia los servicios y la de Mensajería hacia Mercado (spec 026); ambas siguen así (§8 P-07, decidido el 2026-11-19).

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
- **R-10 (replay y rotación):** según las decisiones de §8 (2026-11-19): la repetición se acota solo con la vida de 5 minutos y el `jti` único, **sin guardar el `jti` en Redis**, y una segunda presentación dentro de la vida se acepta de forma idempotente (P-03); cada emisor tiene a la vez dos claves (vigente y anterior), la anterior se acepta durante una ventana de 24 horas y la rotación es manual cada 90 días (P-02).
- **R-11 (llamadas que reenvían el JWT del usuario):** las llamadas del gateway hacia los servicios y de Mensajería hacia Mercado (spec 026 R-06, `GET /api/v1/visitas/{id}`) siguen reenviando el JWT del usuario hacia rutas **públicas**, sin credencial de servicio adicional (§8 P-07, decidido el 2026-11-19). Esta spec no cambia ese flujo. No contradice R-01 ni CA-04, que prohíben el JWT de usuario solo en rutas `/api/v1/interno/*`. La ruta pública receptora sigue verificando el JWT del usuario y la autorización por recurso (spec 022); el JWT reenviado solo viaja a la dirección configurada de Mercado (spec 026 R-08) y no se guarda ni se registra (spec 026 R-09).

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
- **CA-10:** Dada una misma credencial válida presentada dos veces dentro de su vida, cuando llega al receptor, entonces ambas presentaciones se aceptan y la operación es idempotente (sin efecto duplicado); no se guarda el `jti` en Redis ni en ningún almacenamiento; y presentada después de su vencimiento responde 401. (R-10, §8 P-03)
- **CA-11:** Dado un emisor con dos claves (vigente y anterior), cuando el receptor recibe una credencial firmada con la anterior dentro de las 24 horas de la rotación, entonces la acepta; pasada esa ventana la rechaza con 401; y una firmada con la vigente se acepta siempre. (R-10, §8 P-02)
- **CA-12:** Dada una conexión de Mensajería que pide unirse a una visita, cuando consulta a Mercado `GET /api/v1/visitas/{id}`, entonces reenvía el JWT del usuario (no una credencial de servicio) y la ruta pública de Mercado lo acepta sin que esta spec cambie el flujo de la spec 026; y dada una credencial de servicio enviada a esa ruta pública, entonces responde 401 (CA-04). (R-11, §8 P-07)

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
| CA-10 | Segunda presentación dentro de la vida se acepta de forma idempotente, sin `jti` guardado; vencida → 401 | `tests/UnitTest/Hosting/ServiceTokenReplayTests.cs` → `SecondPresentation_IsIdempotent_CA_056_10` |
| CA-11 | Clave anterior aceptada 24 h, luego rechazada; la vigente siempre | `tests/UnitTest/Hosting/ServiceTokenKeyRotationTests.cs` → `PreviousKey_IsAcceptedFor24Hours_CA_056_11` |
| CA-12 | Mensajería→Mercado reenvía el JWT del usuario a una ruta pública; la credencial de servicio allí → 401 | `tests/IntegrationTest/Messaging/UserTokenForwardingTests.cs` → `MarketCall_ForwardsUserToken_CA_056_12` · y regresión de la spec 026 (CA-05, CA-15) |

> Convención: las specs numeradas usan casos `CA-NN` y la prueba lleva `CA_056_NN` en el nombre (`python scripts/sdd_lint.py --trace 056`). Equivalen a los `TC-<ID>-KK` de la plantilla; no se crea un segundo esquema de numeración. Las rutas de prueba son las planeadas (aún no existen) y se confirman al implementar.

## 3. Alcance / No-alcance
- **In (este repositorio, una vez Aprobada la spec):**
  - Definir y probar la regla común de credencial de servicio (R-01 a R-09) y, según §8 P-01, reutilizar o extraer la parte técnica reutilizable de `InternalServiceAuthentication` (hoy solo en Mercado) a un lugar compartido que permita la constitución (spec 055).
  - Que el receptor de `POST /api/v1/interno/avisos` (Mensajería, spec 037) y el de `GET /api/v1/interno/historial` (Mercado, spec 041) cumplan la regla común sin cambiar su contrato.
  - Pruebas de seguridad negativa, de arquitectura y de integración de §2.
  - Dejar explícito y probado (CA-12) que la llamada de Mensajería hacia Mercado de la spec 026 sigue reenviando el JWT del usuario y no pasa por la credencial de servicio.
  - Registrar cada decisión de §8 cuando el líder la tome y actualizar `docs/endpoints.md` si cambia el esquema de alguna ruta interna.
- **Out:**
  - Cambiar el flujo de la spec 026 (Mensajería comprueba la pertenencia en Mercado con el JWT del usuario) y el reenvío del JWT del usuario por el gateway (§8 P-07).
  - Cambiar el contrato de `POST /api/v1/interno/avisos` (spec 037) o de `GET /api/v1/interno/historial` (spec 041).
  - Alta, creación y permisos por secreto en Key Vault y Terraform (`deploy/**`): lo decide y ejecuta el líder en un PR aparte de `deploy/**` (§8 P-04). Esta spec solo dice qué se necesita. **Dato para el líder:** las dos claves `avisos-key-market` y `avisos-key-identity` de la spec 037 tampoco existen aún en Key Vault; hoy solo está `interno-key-identity` (`deploy/apps/main.tf`).
  - Operar Azure, producción o bases reales.
  - Reemplazar el JWT de usuario o cambiar `jwt-signing-key` y los flujos de las specs 009, 015 y 016. Repartir `jwt-signing-key` entre gateway, Identidad y Mercado se decide aparte (§8 P-05).
  - mTLS, malla de servicios o gRPC (`DECISION_TECNICA.md` los difiere hasta que se separe un módulo en servicio); solo se evalúan como alternativa en §8 P-01.
  - Tráfico de personas, límites de peticiones, CORS y cabeceras (spec 009).

## 4. Contratos API y eventos
- **Endpoints nuevos:** ninguno. **Eventos nuevos:** ninguno.
- **Rutas internas afectadas (sin cambio de contrato):** `POST /api/v1/interno/avisos` (Mensajería, spec 037 R-11) y `GET /api/v1/interno/historial` (Mercado, spec 041 R-17). Ninguna aparece en el OpenAPI público ni en el gateway.
- **Llamada que no cambia:** Mensajería → `GET /api/v1/visitas/{id}` de Mercado (spec 026 R-06) es una ruta pública y se llama con el JWT del usuario; no usa la credencial de servicio.
- **Credencial de servicio (propiedades que fija esta spec; la mecánica exacta está en §8 P-01):** encabezado `Authorization: Bearer <credencial de servicio>`; token firmado con claims `iss` (emisor), `aud` (servicio destino), `exp`, `iat`, `jti` y `permisos`; vida de 5 minutos como máximo; clave por emisor; algoritmo fijo.
- **Errores (sin cambio de forma):** ProblemDetails; `401` sin credencial válida; `403` con código `service_not_allowed` sin permiso del emisor para la ruta.
- **Documentos vivos:** `docs/endpoints.md` solo si cambia el esquema declarado de una ruta interna; `docs/openapi.v1.json` no cambia (las rutas son internas).

## 5. Modelo de datos
- Ninguna colección nueva por defecto. P-03 se decidió el 2026-11-19: no se guarda el `jti` en Redis ni en ningún almacenamiento; no hay estado nuevo.

## 6. Seguridad, privacidad y amenazas
Análisis proporcional: cambio de autenticación y permisos entre servicios; sin datos personales nuevos.

| Amenaza (STRIDE) | Control | Caso que lo prueba |
|---|---|---|
| **Suplantación:** un proceso de la red o un servicio comprometido llama a otro sin ser quien dice | Credencial de servicio firmada, `iss` y `aud`, falla cerrada sin clave | CA-01, CA-02, CA-06 |
| **Suplantación entre emisores:** un servicio firma como otro | Clave aislada por emisor; la verificación liga `iss` con su clave | CA-05 |
| **Suplantación con el JWT de usuario:** usar un token de persona en una ruta interna (o al revés) | Esquemas separados, sin reutilizar `jwt-signing-key` | CA-04 |
| **Repetición (replay):** capturar una credencial y reenviarla dentro de su vida | Vida de 5 minutos como máximo y `jti` único; sin almacén de `jti` (P-03); el riesgo residual se acepta porque la segunda presentación es idempotente | CA-02, CA-10 |
| **Rotación de claves:** una clave vieja o comprometida sigue válida, o una rotación corta las llamadas | Claves por emisor aisladas; dos claves por emisor, la anterior solo 24 horas; rotación manual cada 90 días (P-02) | CA-11 |
| **Fuga o mal uso del JWT del usuario reenviado a Mercado (Mensajería→Mercado, spec 026):** el token viaja a otro servicio | Se reenvía solo a la dirección configurada de Mercado (`Services:Market:BaseUrl`), vive solo en memoria de la conexión, no se registra ni se guarda (spec 026 R-08, R-09); Mercado verifica el JWT y la pertenencia por recurso (spec 022); la credencial de servicio no vale en esa ruta pública | CA-12, CA-04 |
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
| Integración | Sí | Receptores reales con credenciales válidas e inválidas; gateway que no enruta `/interno`; Mensajería→Mercado con el JWT del usuario (Testcontainers) |
| Seguridad negativa | Sí | Todos los casos de §6: sin credencial, `iss` y `aud`, algoritmo, JWT de usuario, emisor cruzado, sin permiso |
| Base de datos | No | No se agrega ni cambia ninguna colección (salvo lo que decida P-03) |
| Contrato | Sí (acotado) | Las rutas internas no están en el OpenAPI público; se verifica que no se agreguen |
| Mutación | Sí (acotada) | Meta del módulo: reglas de validación de la credencial (vida, `iss`, `aud`, `permisos`) |
| Fuzz, carga y caos | No | Sin superficie pública nueva; el costo de validar localmente es despreciable |

**Gates:** ver `AGENTS.md §5` (Definición de terminado).

**Decisiones del Tech Lead (JulianH2, comentario del PR #180, 2026-11-19)** — se anotan como las dio; la spec sigue en Borrador:
- **P-01 (2026-11-19):** se confirma HS256 por emisor como mecanismo del sistema.
- **P-01b (2026-11-19):** la validación y emisión de la credencial va al catálogo de `Knock.Hosting`. La enmienda a la spec 055 se hace en un PR aparte, **antes del código de la 056**.
- **P-02 (2026-11-19):** dos claves por emisor a la vez (vigente y anterior), ventana de 24 horas, rotación manual cada 90 días. Reescribe CA-11.
- **P-03 (2026-11-19):** basta la vida de 5 minutos con `jti` único; no se guarda el `jti` en Redis; la segunda presentación se acepta de forma idempotente. Reescribe CA-10.
- **CA-10 y CA-11 (2026-11-19):** son casos normales con pruebas `CA_056_10` y `CA_056_11`.
- **P-04 (2026-11-19):** los secretos de Key Vault (`avisos-key-market`, `avisos-key-identity`, `interno-key-identity`) y sus permisos por servicio van en un PR aparte de `deploy/**`, que hace el Tech Lead.
- **P-05 (2026-11-19):** la clave `jwt-signing-key` compartida va en una tarea aparte, de menor prioridad; no se mezcla en la 056.
- **P-06 (2026-11-19):** se dejan los nombres actuales (`avisos-key-*` e `interno-key-identity`); `interno-key-*` se usa solo en rutas internas nuevas.
- **P-07 (2026-11-19, ampliada):** se mantiene el reenvío del JWT de usuario en el gateway y en Mensajería hacia Mercado (spec 026), sin credencial de servicio extra. Ver R-11 y CA-12.
- **P-08 (2026-11-19):** se elimina como duda aparte; quedó resuelta con P-02 y P-03.
- **Estado «Firmada» (2026-11-19):** se mantiene como pide `sdd_lint`; unificarlo con «Implementada» es otra tarea.

**Decisiones pendientes del Tech Lead (sí/no).** Quedan cuatro; ninguna bloquea la aprobación de la spec, pero D-01 y D-02 deben estar resueltas antes de la tarea T-07 (rotación). Cada una trae contexto, opciones y la recomendación del autor; responder «sí» a una recomendación la deja decidida tal como está escrita.

- **D-01 — ¿La rotación manual de 90 días la ejecuta el Tech Lead con un procedimiento escrito en `docs/`?**
  - Contexto: P-02 fijó periodicidad (90 días), dos claves y ventana de 24 horas, pero no quién rota ni los pasos. Los secretos viven en Key Vault y su alta la hace el Tech Lead (P-04).
  - Opciones: (a) el Tech Lead, con un procedimiento de cinco pasos (crear la clave nueva, pasar la vigente a «anterior», desplegar con la nueva, esperar 24 horas, retirar la anterior); (b) un turno rotativo del equipo con el mismo procedimiento; (c) automatizarla (contradice «manual» de P-02).
  - **Recomendación: (a), sí.** Quien administra Key Vault ya tiene el permiso; el procedimiento se escribe en `docs/` junto con el PR de `deploy/**` de P-04. Si la respuesta es «no», indicar quién.
- **D-02 — ¿La clave «anterior» de cada emisor se llama como la vigente con el sufijo `-previous` y se lee de una configuración `<Clave>:Previous`?**
  - Contexto: P-06 dejó `avisos-key-market`, `avisos-key-identity` e `interno-key-identity`; falta el nombre de la clave anterior y su lectura. Se define en el mismo PR de `deploy/**` (P-04).
  - Opciones: (a) sufijo `-previous` (por ejemplo `interno-key-identity-previous`), opcional: si no existe, solo vale la vigente; (b) usar versiones de un mismo secreto de Key Vault (la anterior es la versión previa); (c) un secreto con dos valores.
  - **Recomendación: (a), sí.** Es explícito, se prueba con un nombre fijo (CA_056_11) y permite retirar la anterior borrando un secreto sin tocar la vigente. (b) depende de listar versiones en tiempo de ejecución y complica la prueba.
- **D-03 — ¿Basta con que cada receptor ya sea idempotente, con una prueba por ruta interna, para cumplir CA-10?**
  - Contexto: la 037 deduplica por `eventoId` y `GET /api/v1/interno/historial` (041) solo lee; P-03 acepta la segunda presentación de la misma credencial porque no hay efecto duplicado.
  - Opciones: (a) sí: CA_056_10 prueba la validación (dos presentaciones aceptan, vencida rechaza) y se agrega una prueba de integración por ruta interna existente que repite la misma llamada y comprueba un solo efecto; (b) basta la prueba de validación y se confía en las suites de la 037 y la 041.
  - **Recomendación: (a), sí.** Son dos rutas y la prueba es barata; (b) deja sin comprobar que la repetición de la credencial no duplique el aviso.
- **D-04 — ¿Se descarta en esta spec la ruta interna de pertenencia en Mercado (mejora de la spec 026)?**
  - Contexto: hoy Mensajería comprueba la pertenencia con `GET /api/v1/visitas/{id}` y el JWT del usuario (P-07 lo mantiene). Una ruta interna usaría credencial de servicio, quitaría el reenvío del JWT de usuario entre servicios y exigiría enmendar la 026 y su propia decisión.
  - Opciones: (a) sí, se descarta y se deja como mejora futura con ticket propio; (b) no, se incluye en la 056 (amplía alcance y puntos; la spec vale 2 puntos).
  - **Recomendación: (a), sí.** P-07 ya decidió mantener el reenvío; incluirla cambia el alcance y obliga a una enmienda de la 026. Si la respuesta es «no», se abre una spec aparte.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-20
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
