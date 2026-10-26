# TT-API-12 — `sub` v8 con SHA-256: reemplazar la derivación GUID v5 / SHA-1 antes de guardar datos

> **Jira:** KNOCK-187 · **Área:** API · Identidad · **Servicio:** Identidad y confianza (`Knock.Identity.Api`, ADR-001; hoy módulo Identity en `src/Knock.Api/`) · **Responsable:** José Guillermo Rangel Hernández · **Sprint:** S2 · **Puntos:** 1 · **Prioridad:** Técnica · **Capas:** Back
> **Repositorio:** knock-backend · **Otra parte en:** — · **Rama:** `feature/KNOCK-187-sub-v8-sha256` → PR a `sprint-01`
> **Ficha:** no existe `docs/backlog/TT-API-12.md`; la tarea nace de la decisión del líder del 2026-10-24 (D1 y D10) sobre HU-001 (KNOCK-43).
> **Depende de:** TT-API-02 (KNOCK-21, fusionado, Firmada). **No depende** de TT-BD-06 ni de MongoDB: no hay persistencia. **Debe fusionar primero**: HU-001 (KNOCK-43) depende de esta tarea.
> **Fuentes:** spec HU-001 (§2 «Caso reubicado a TT-API-12» A-01, §3 «Tareas relacionadas», §8 decisiones 1, D1 y D8) · spec TT-API-02 (casos 07, 09 y 10; Firmada, no se edita) · `docs/evidencia/KNOCK-21-spike-firebase.md` («`sub` derivable», «SHA-1 propio») · RFC 9562 §5.8 (UUID v8) · constitución 2.2.0 (principios 3 y 8; §5; §6) · `src/Knock.Api/Modules/Identity/Services/{UserIdDerivation,FirebaseSessionService,SessionTokenIssuer}.cs`
> **Skills:** story-workflow, spec-first-writing, auth-firebase-jwt, security-core, testing-strategy-backend, reuse-before-create
> **Estado:** Aprobada

## 1. Objetivo
Que el `sub` del JWT siga siendo un identificador **determinista del `uid` de Firebase**, pero como **UUID v8 con SHA-256** y no como GUID v5 con SHA-1, **antes de que se guarde ningún dato** (decisión 1 del líder, 2026-10-24). Porqué: un `sub` guardado no se puede cambiar sin cambiar la identidad de la cuenta; el SHA-1 obligaba a mantener una implementación escrita a mano para esquivar los analizadores (CA5350, S4790); y SHA-256 es la primitiva estándar del framework. El `ObjectId` de `usuarios` es aparte (HU-001) y no se toca aquí.

**Terminada cuando:** `UserIdDerivation.Derive` devuelve el UUID v8 definido en §4 (vectores del caso 01), ya no existe SHA-1 en `src/`, `uid` vacío sigue lanzando `ArgumentException`, las pruebas de TT-API-02 que fijaban v5 están actualizadas y el resto de TT-API-02 sigue en verde, sin cambios de contrato HTTP.

**Qué ya existe y qué cambia** (leído en el código; `python scripts/codemap.py --query` encuentra `UserIdDerivation` y su único consumidor):

| Pieza | Hoy | En esta tarea |
|---|---|---|
| `Modules/Identity/Services/UserIdDerivation` | `Derive(string uid) : Guid`: GUID v5 (RFC 9562 §5.5) con un SHA-1 (RFC 3174) escrito a mano; `Namespace` fijo `bcfea58b-95dd-4c0e-80d1-baf839f2e36e`; `ArgumentException` si el `uid` es vacío o en blanco. | **Se reemplaza el algoritmo:** UUID v8 con SHA-256 (`System.Security.Cryptography.SHA256`). Se **borra** `Sha1` y su documentación. Se conservan el nombre, la firma, el `Namespace` y la excepción. |
| `FirebaseSessionService.ExchangeAsync` | Llama `UserIdDerivation.Derive(identity.Uid)` y emite la sesión. | **Sin cambios** (misma firma). |
| `SessionTokenIssuer.Issue(Guid)` | Pone `sub` = `userId.ToString("D")`. | **Sin cambios.** |
| `tests/UnitTest/Identity/UserIdDerivationTests` | Vectores v5 calculados con `uuid.uuid5` de Python; prueba de «versión 5». | **Se actualiza** a vectores v8 y «versión 8» (casos 01 y 03), conservando el sufijo `_TC_TT_API_02_10` (D8 de HU-001). |
| Otras pruebas de TT-API-02 que tocan el `sub` | `SessionTokenIssuerTests` (07), `FirebaseSessionTests` (09), `MeEndpointTests` (06), `SensitiveDataLeakTests` (13). Comparan el `sub` entre sí o con `Derive`; no fijan un valor v5. | **Sin edición** (regresión, caso 07); solo se agregan métodos nuevos (casos 05 y 06). |

## 2. Historia y casos de prueba
**TT-API-12** — Tarea técnica (no es historia de usuario). Los casos 01 a 07 se verifican con pruebas de código; el 08, 09 y 10 con comandos (la salida va en el PR). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_12_KK`. El caso 01 es el **A-01 de HU-001** («Caso reubicado a TT-API-12»). Los `uid` de las pruebas son ficticios. Los vectores esperados se calcularon al redactar con `hashlib` de Python (`SHA256(namespace en orden de red ‖ UTF-8(uid))`, primeros 16 bytes, versión 8 y variante `10`); el constructor **los recalcula con una implementación independiente** antes de fijarlos en la prueba.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-12-01 | Dado el namespace fijo `bcfea58b-95dd-4c0e-80d1-baf839f2e36e` y un `uid` ficticio, cuando se deriva el `sub`, entonces coincide con el valor esperado calculado de forma independiente: `uid-prueba-0001` → `dba04c44-65fd-8390-8279-e3940c6e8663`; `uid-prueba-0002` → `e0743e2a-1841-8077-a348-2dcfdf0be576`; `wVSEsCC4m04ROIgLCrHUGoxMyjn5` → `ebaf9b54-0189-8919-ac36-385cf0c45584`; `nándú-π` (UTF-8 no ASCII) → `e7176ed4-649c-8c0b-b884-3e853874fbe2`; y en la frontera del relleno de bloque (16 + 39 y 16 + 40 bytes): 39 «z» → `888a6ce1-0249-8d75-b89f-e46b59d9bd78`, 40 «z» → `403e0242-dc63-83f3-bf68-4c80cef2b43c`, 40 «x» → `3a3c03c3-269d-82ae-b237-c83ad91982f9`. (A-01 de HU-001.) | `UnitTest/Identity/UserIdDerivationTests.KnownVector_MatchesUuidV8Sha256_TC_TT_API_12_01` (Theory; el método conserva además el sufijo `_TC_TT_API_02_10`) |
| TC-TT-API-12-02 | Dado un mismo `uid`, cuando se deriva varias veces, entonces el resultado es siempre el mismo; dados dos `uid` distintos, los `sub` son distintos; y el `Namespace` público sigue siendo exactamente `bcfea58b-95dd-4c0e-80d1-baf839f2e36e` (no cambia nunca: cambiarlo cambiaría el `sub` de todos). | `UnitTest/Identity/UserIdDerivationTests.SameUidIsStableDifferentUidsDifferAndNamespaceIsFixed_TC_TT_API_12_02` (conserva también `_TC_TT_API_02_10`) |
| TC-TT-API-12-03 | Dado cualquier `uid` ficticio, cuando se deriva, entonces el texto «D» del resultado tiene el dígito de versión `8` (posición 14) y la variante RFC (`8`, `9`, `a` o `b` en la posición 19), y los demás bits (los 122 que no son versión ni variante) coinciden con los primeros 16 bytes del SHA-256 de `namespace ‖ uid` calculado en la prueba con `SHA256.HashData` (no con la implementación bajo prueba). | `UnitTest/Identity/UserIdDerivationTests.SetsVersion8AndRfcVariantKeepingTheSha256Bits_TC_TT_API_12_03` (conserva también `_TC_TT_API_02_10`) |
| TC-TT-API-12-04 | Dado un `uid` vacío, en blanco o `null`, cuando se deriva, entonces lanza `ArgumentException` (o su derivada `ArgumentNullException`) sin incluir el valor en el mensaje; ningún `uid` válido lo lanza. | `UnitTest/Identity/UserIdDerivationTests.EmptyOrBlankUidThrowsArgumentException_TC_TT_API_12_04` (Theory; conserva también `_TC_TT_API_02_10`) |
| TC-TT-API-12-05 | Dado un canje con un ID token válido del emulador de Firebase, cuando se emite la sesión, entonces el `sub` del JWT es el UUID v8 de `Derive(uid)` (dígito de versión `8`), no es el `uid` ni lo contiene, `GET /api/v1/me` devuelve ese mismo valor como `id`, y canjear dos veces el mismo usuario da el mismo `sub` (regresión de TC-TT-API-02-06 y 09 con la derivación nueva). Sin base de datos ni cambio de contrato: la respuesta sigue siendo `{ accessToken, refreshToken }`. | `IntegrationTest/Identity/FirebaseSessionTests.IssuedSubIsTheUuidV8OfTheUid_TC_TT_API_12_05` (emulador de Firebase; usa `FirebaseEmulator`) |
| TC-TT-API-12-06 | Dado un canje correcto y uno rechazado con un `uid` y un teléfono ficticios y reconocibles, cuando se capturan los logs (`ConsoleCapture`) y se revisan las respuestas, entonces **ni el `uid` ni el `sub` aparecen en los logs**, ni el `uid` en ninguna respuesta; el `sub` solo viaja dentro del JWT de acceso y en `/me` (que lo devuelve a su dueño); y el `sub` no contiene el `uid` ni una forma trivial de él (texto, Base64, hexadecimal). | `SecurityTest/Identity/SensitiveDataLeakTests.ExchangeDoesNotLogOrReturnUidOrSub_TC_TT_API_12_06` (emulador; método nuevo en el archivo existente, sin tocar `..._TC_TT_API_02_13`) |
| TC-TT-API-12-07 | Dado el árbol de pruebas de TT-API-02 después del cambio, cuando se ejecutan sus casos 01 a 14 (unitarias, integración con emulador y seguridad), entonces **siguen en verde sin editar sus aserciones**, salvo `UserIdDerivationTests` (vector y versión: casos 01 y 03 de esta tarea); `sdd_lint.py --trace TT-API-02` no reporta drift porque los métodos de derivación conservan `_TC_TT_API_02_10`; y la firma pública `Derive(string) : Guid` y `Namespace : Guid` no cambian. | Regresión de `UnitTest/Identity/SessionTokenIssuerTests.IssuedJwt_HasHs256_15Min_AndExpectedClaims_TC_TT_API_02_07`, `IntegrationTest/Identity/{FirebaseSessionTests,MeEndpointTests}` y `SecurityTest/Identity/*` existentes (no se duplican) · `UnitTest/Identity/UserIdDerivationTests.PublicSignatureIsKept_TC_TT_API_12_07` (reflexión) |
| TC-TT-API-12-08 | Dado el cambio terminado, cuando se busca SHA-1 en `src/`, entonces **no hay coincidencias** de `Sha1`, `SHA-1`, `SHA1`, `HashAlgorithmName.SHA1`, `0x67452301` ni `0xC3D2E1F0` en archivos `.cs`; no se agregó ninguna excepción `EXC-NNN` ni supresión de CA5350 o S4790; y los analizadores no reportan warnings. | (comando) `grep -rniE "sha-?1\b|0x67452301|0xC3D2E1F0" src --include=*.cs` sin resultados y `python scripts/ci/check_exceptions.py`; salida en el PR |
| TC-TT-API-12-09 | Dado el trabajo terminado, entonces no hay paquetes nuevos (`System.Security.Cryptography.SHA256` es del framework: `dotnet list Knock.slnx package --include-transitive` sin cambios respecto de `sprint-01` y `--vulnerable` sin vulnerabilidades), gitleaks no encuentra secretos y los datos de las pruebas son solo ficticios. | (comando) `dotnet list Knock.slnx package --include-transitive --vulnerable` y `gitleaks`; salida en el PR |
| TC-TT-API-12-10 | Dada la solución `Knock.slnx`, cuando se compila en Release y se verifica el formato, entonces hay 0 errores y 0 warnings y `dotnet format` no produce cambios. | (comando) `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes`; salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio, módulo Identity):**
  - `UserIdDerivation.Derive`: UUID v8 con SHA-256 como en §4; se borra el SHA-1 escrito a mano y su documentación; se actualiza el comentario del tipo (ya no menciona v5 ni la prueba contra Python de v5).
  - `tests/UnitTest/Identity/UserIdDerivationTests`: vectores v8, versión 8 y variante, estabilidad, `uid` vacío y firma; los métodos de derivación conservan el sufijo `_TC_TT_API_02_10` además del `_TC_TT_API_12_KK`.
  - Una prueba de integración nueva (caso 05) y un método nuevo de seguridad (caso 06); ninguna otra prueba de TT-API-02 se edita.
  - Documentar el cambio en `Memoria.md` lo hace el líder.
- **Out:**
  - **MongoDB, `usuarios`, el `ObjectId`, el cifrado del celular, el consentimiento, la auditoría y el canje persistente:** HU-001 (KNOCK-43). Esta tarea no depende de TT-BD-06.
  - **Cambios de contrato HTTP u OpenAPI:** ninguno (el JWT sigue con `sub`, `jti`, `iss` y `aud`; `/me` devuelve `{ id }`). Contrato vivo: TT-CI-02 (KNOCK-137).
  - **Cambiar el `Namespace`** (no cambia nunca) o volver el `sub` dependiente de una llave (HMAC): no se decidió; el `sub` sigue derivable por quien conozca el `uid` y el namespace (riesgo aceptado en TT-API-02, §6).
  - **Editar la spec firmada de TT-API-02 o su evidencia** (`docs/evidencia/KNOCK-21-spike-firebase.md` queda como registro histórico del spike con v5): el líder no enmienda esa spec (D8 de HU-001). El caso 10 de TT-API-02 queda reemplazado en la práctica por los casos 01 a 04 de esta tarea; se anota en el PR y en `Memoria.md`.
  - Migrar `sub` ya emitidos: no hay datos guardados; los JWT de 15 min emitidos con v5 en desarrollo caducan solos y no hay cuentas.
  - Editar `.github/`, `scripts/`, `.agents/`, `constitution.md`, `ModuleSurfaceTests`, `ModuleBoundaryTests`; operar Azure, Docker o producción.

## 4. Contratos API y eventos
**Sin endpoints ni eventos nuevos y sin cambios de contrato.** Cambia solo el **valor** del `sub` (sigue siendo un UUID de 36 caracteres en texto «D»).

**Algoritmo (RFC 9562 §5.8, UUID v8 definido por Knock):**
1. `input` = los 16 bytes del `Namespace` fijo en **orden de red** (big-endian, como `Guid.TryWriteBytes(bigEndian: true)`) ‖ `UTF-8(uid)`.
2. `hash` = `SHA-256(input)` (32 bytes); se toman los **primeros 16 bytes**.
3. Versión 8: `hash[6] = (hash[6] & 0x0F) | 0x80`.
4. Variante `10`: `hash[8] = (hash[8] & 0x3F) | 0x80`.
5. `sub` = `new Guid(hash[0..16], bigEndian: true)` en texto «D» (minúsculas, 36 caracteres).

Es el mismo armado que el v5 de TT-API-02 (mismo namespace, mismo orden de bytes, mismas máscaras salvo la versión), cambiando SHA-1 por SHA-256 y la versión 5 por 8. Firma pública sin cambios: `public static Guid Derive(string uid)` y `public static readonly Guid Namespace`. `uid` vacío o en blanco: `ArgumentException` (`ArgumentException.ThrowIfNullOrWhiteSpace`).

**Eventos:** ninguno.

## 5. Modelo de datos
— Sin persistencia: no hay colecciones, índices ni migración. El `sub` se calcula en cada canje a partir del `uid` verificado. (HU-001 lo guardará en `usuarios.sub` con índice único y su propio `ObjectId`.)

## 6. Seguridad, privacidad y amenazas
Cambio de una función pura de identidad; toca `security-core`, checks 2, 7 y 13. STRIDE breve:

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Alteración (T):** que un `sub` v5 quede guardado o circulando y luego haya que cambiarlo (cambiaría la identidad de la cuenta) | Esta tarea fusiona **antes** de cualquier dato (HU-001 depende de ella); el `sub` se calcula, no se guarda aquí | TC-TT-API-12-01, 05 |
| **Alteración (T):** la derivación cambia sin querer (otro namespace, otro orden de bytes, otra codificación) y todas las cuentas futuras cambian de `sub` | `Namespace` fijo comprobado; vectores independientes (incluidos UTF-8 no ASCII y la frontera del relleno de bloque); firma pública fija | TC-TT-API-12-01, 02, 07 |
| **Suplantación (S) / fuga (I):** el `sub` revela el `uid` o permite recuperarlo | SHA-256 es unidireccional; el `sub` no contiene el `uid`; el `uid` no se registra ni se devuelve. **Riesgo aceptado (de TT-API-02):** quien conozca un `uid` y el namespace (no secreto) puede calcular el `sub`; no autoriza nada por sí solo (hace falta el JWT firmado). Truncar a 122 bits mantiene colisiones despreciables | TC-TT-API-12-06 |
| **Fuga (I):** `uid` o `sub` en logs o respuestas | Sin registros de ninguno; el `sub` solo en el JWT y en `/me` | TC-TT-API-12-06 |
| **Repudio (R) / denegación (D) / elevación (E):** | No aplican: no hay endpoint, estado ni recurso nuevos; el JWT y su validación no cambian | TC-TT-API-12-07 |
| **Cadena de suministro:** primitiva criptográfica propia o paquete nuevo | Se **elimina** el SHA-1 propio y se usa `SHA256` del framework; sin paquetes nuevos; sin supresiones ni `EXC-NNN` | TC-TT-API-12-08, 09 |

No aplican aquí: autorización por recurso, cifrado de campo, biométricos, archivos, IA y SSRF.

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Derivación (vectores independientes, estabilidad, versión y variante, `uid` vacío, firma pública): casos 01 a 04 y 07. `tests/UnitTest/Identity/UserIdDerivationTests`. |
| Integración | Sí | Caso 05 con el emulador de Firebase Auth (sin base de datos). `tests/IntegrationTest/Identity/FirebaseSessionTests`. |
| Seguridad negativa | Sí | Caso 06: `uid` y `sub` fuera de logs y respuestas. `tests/SecurityTest/Identity/SensitiveDataLeakTests`. |
| Arquitectura | Sí, existentes | `ModuleBoundaryTests` y `ModuleSurfaceTests` siguen en verde sin cambios (el tipo sigue `internal` en `Services/`). |
| Regresión | Sí | TC-TT-API-02-01 a 14 siguen en verde (caso 07); el caso 10 de TT-API-02 queda cubierto por los casos 01 a 04 de esta tarea. |
| Base de datos, contrato, eventos, E2E, UI, rendimiento | No | Sin persistencia, endpoints ni eventos; el contrato vivo es de TT-CI-02. |
| Mutación | No | Sin meta para esta función (la del módulo Identity ≥ 80 % se mide con HU-001); los vectores exactos matan mutantes de bytes, versión y variante. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit (con Architecture), Integration y Security en verde (resultado real; la integración necesita el emulador de Firebase en marcha), `sdd_lint.py --trace TT-API-12` y `--trace TT-API-02`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día (lo escribe el líder) e informe de `story-workflow` en el PR con «Uso de IA». Esta spec es un borrador: no se ha escrito ni ejecutado ninguna prueba ni línea de código.

**Decisiones previas (de HU-001 y el líder, 2026-10-24):**
1. **`sub` = UUID v8 con SHA-256**, derivado del `uid`, y se cambia antes de guardar datos (decisión 1 de HU-001). Porqué: ver §1.
2. **El `Namespace` no cambia** (ya fijado e inmutable en TT-API-02). Porqué: no hay motivo para otro y cambiarlo después de guardar cuentas cambiaría su `sub`.
3. **Spec firmada de TT-API-02:** no se edita (D8 de HU-001); los métodos conservan `_TC_TT_API_02_10` y esta tarea lo anota.
4. **Va primero y no depende de MongoDB** (D10 de HU-001); HU-001 depende de esta tarea.

**Límites conocidos:**
- El `sub` sigue siendo derivable por quien conozca el `uid` y el namespace (riesgo aceptado); si se quisiera irreversible sin conocer un secreto, haría falta HMAC con llave (decisión distinta, no tomada).
- La evidencia `docs/evidencia/KNOCK-21-spike-firebase.md` y la spec firmada de TT-API-02 siguen diciendo «GUID v5» como registro histórico.

**Dudas abiertas para el líder:** ninguna de diseño. Recomendación de proceso: anotar en el PR y en `Memoria.md` que TC-TT-API-02-10 (vector v5) queda reemplazado por TC-TT-API-12-01 a 04, ya que la spec firmada no se enmienda.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-26
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
