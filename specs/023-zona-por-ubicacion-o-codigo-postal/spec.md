# Spec 023 — Zona por ubicación o código postal, con lista de espera

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Claves de trabajo (verificadas en Jira el 2026-11-09):** KNOCK-47 «HU-009 · Indicar mi zona por ubicación o código postal (lista de espera fuera de la ciudad piloto)»; consume KNOCK-141 «TT-API-07 · Endpoint de colonia por punto» (Listo) y el catálogo de KNOCK-156 «TT-BD-06 · Migración del catálogo a MongoDB» (Listo)
> **Responsable sugerido:** Alexis (backend) · Erick consume el contrato (frontend 017) · **Ola:** B · **Sprint:** 4 · **Prioridad:** P2
> **Servicio:** Mercado · **Módulo:** Catalog · **Colecciones:** `colonias` (existente), `listas_espera` (nueva)
> **Reemplaza a:** el borrador aprobado `specs/_legacy/HU-009-zona-por-ubicacion-o-codigo-postal/spec.md`, del que conserva las rutas y las decisiones (e) y (f); la spec numerada es la que se implementa.

## 1. Objetivo
Que una persona que se registra indique su zona con su ubicación o con su código postal, vea solo la colonia y el código postal (nunca sus coordenadas), y que quien esté fuera de la ciudad piloto pueda apuntarse a una lista de espera para que Knock le avise cuando abra su zona.

## 2. Historia y alcance
**2.1 Historia**
- **HU-009** — Como persona que se registra, quiero indicar mi zona con mi ubicación o mi código postal, para ver expertos cerca de mí sin dar mi dirección.

**2.2 Dentro**
- Listar las colonias de un código postal, con la marca de cobertura y el nombre de la ciudad piloto.
- Registrar a la persona en la lista de espera de un código postal fuera de cobertura, una sola vez por persona y código.
- Respetar el catálogo SEPOMEX ya sembrado (13 colonias de Tula de Allende, Hidalgo) y su caché de 24 h.
- Garantizar por diseño y por prueba que las coordenadas no se guardan, no se registran y no se devuelven.

**2.3 Fuera**
- La colonia de un punto: es `GET /api/v1/colonias/en-punto` (KNOCK-141) y esta spec la consume, no la redefine.
- Guardar la colonia y el código postal en el perfil: lo hace `PUT /api/v1/me` de Identidad (spec 017), que valida la colonia contra Mercado con `IColoniaCatalog`.
- La dirección exacta del servicio (HU-010) y el cambio de zona después del registro (HU-049).
- Avisar a la lista de espera cuando se abra la cobertura: solo se guarda el registro.
- Geocodificación con servicios externos (Google, Mapbox, HERE, Nominatim): la colonia sale del catálogo propio.
- El campo `piloto` de la colonia: en el catálogo significa «tiene polígono», no «está en cobertura»; no se expone ni se usa como filtro.

## 3. Requisitos
- **R-01:** `GET /api/v1/zonas/cp/{cp}` exige JWT y recibe `cp` de exactamente cinco dígitos ASCII. El valor se enlaza a un objeto de ruta con su validador registrado, nunca a un `string` suelto. Un código mal formado (no numérico, de 4 o 6 dígitos, vacío o con intento de inyección como `{"$ne":1}` o `.*`) responde 422 en ProblemDetails con `errors.cp`, sin repetir el valor enviado, y no consulta el catálogo. El byte nulo codificado (`42800%00`) lo rechaza el servidor (Kestrel) con 400 sin cuerpo antes de llegar a la aplicación; un `\0` literal o `%2500` sí llega al validador y responde 422.
- **R-02:** La respuesta correcta es `{ enCobertura, ciudadPiloto, colonias: [{ id, nombre, cp, municipio }] }`. Un código con al menos una colonia en el catálogo responde `enCobertura: true` y sus colonias ordenadas por `nombreBusqueda`; uno sin colonias responde `enCobertura: false` y `colonias: []`. En ambos casos `ciudadPiloto` viene de configuración (`Catalogo:CiudadPiloto`, «Tula de Allende»), nunca de datos de la persona ni del cliente.
- **R-03:** La cobertura se decide por la presencia del código postal en el catálogo, no por el campo `piloto`: las colonias sin polígono (`piloto: false`) son visibles por código postal y nunca por punto. La respuesta no incluye `geom`, `centro`, `nombreBusqueda`, `version`, `piloto` ni ningún campo interno.
- **R-04:** La consulta por código postal usa el índice `{ cp: 1 }` de `colonias` y una caché de 24 h con la clave `colonias:cp:{cp}`; un código sin colonias se cachea 5 minutos para no golpear la base con códigos inventados. La consulta no lleva coordenadas y por eso su clave de caché no contiene datos de la persona.
- **R-05:** `POST /api/v1/zonas/lista-espera` exige JWT y cuerpo `{ cp, municipio? }`. Responde 201 la primera vez y 200 al repetir el mismo `sub` con el mismo código postal, con el registro existente y `yaRegistrado: true`; no se duplica ni se modifica.
- **R-06:** `cp` cumple la misma regla de cinco dígitos y responde 422 `errors.cp` si no. `municipio` es opcional, de 1 a 100 caracteres, letras (incluidas acentuadas y `ñ`), espacios, punto, guion y apóstrofo; cualquier otro carácter (dígitos, `<`, `>`, `$`, `{`, `}`, `;`, comillas, caracteres de control, CR/LF) o una longitud fuera del rango responde 422 `errors.municipio` sin repetir el valor y sin guardar nada.
- **R-07:** La persona de la lista de espera es siempre la del `sub` del token; el cuerpo no acepta identificador de usuario. Un `usuarioId` enviado en el cuerpo se ignora.
- **R-08:** La lista de espera es idempotente por el índice único `{ usuarioId: 1, cp: 1 }`: la repetición responde 200 sin crear otro documento.
- **R-09:** La lista de espera admite un código postal que no esté en el catálogo, porque su propósito es justamente ese; no se valida contra `colonias`. El `municipio` es opcional y no se deduce del catálogo cuando el código no existe.
- **R-10:** El listado de colonias por código postal no se pagina: el conjunto está acotado por el catálogo (13 colonias en total y como máximo un puñado por código) y devolverlo entero evita una pantalla con cursor (excepción documentada al principio de paginación, decisión (e) del borrador anterior). Un código devuelve como máximo 50 colonias; si el catálogo crece por encima de ese tope, se pagina. Esta excepción a la paginación de la spec 010 la aprueba quien firma esta spec.
- **R-11:** `listas_espera` no se cachea: liga a una persona con su código postal y es un dato personal (constitución §2.9 y spec 010). Sus lecturas y escrituras responden `Cache-Control: private, no-store`.
- **R-12:** Ni la API ni sus registros guardan, escriben o devuelven coordenadas. El módulo no recibe `lat` ni `lon` en ninguna ruta nueva; la ubicación se convierte en colonia en `/colonias/en-punto` y se descarta allí.
- **R-13:** Los errores usan ProblemDetails con `correlationId` y `traceId`. Los registros no incluyen el código postal junto a la persona, el municipio ni las coordenadas; el registro de la petición lleva método, ruta, estado y correlación.
- **R-14:** Sin token, cualquier ruta responde 401; por encima del límite, 429. La lista de espera tiene además 5 peticiones por hora por persona y la consulta por código postal queda sujeta al límite global por IP y al de usuario del gateway.
- **R-15:** Un código postal que no existe en el catálogo responde 200 con `enCobertura: false` y la ciudad piloto: no se distingue «no existe» de «no está en cobertura», para no habilitar la enumeración del catálogo por prueba y error. Los códigos postales piloto son información pública de la ciudad: el barrido lo acotan los límites del gateway por IP y por usuario (hoy 100 y 120 por minuto) y la ruta exige sesión, pero no lo impiden; el riesgo se acepta (§15).
- **R-16:** `GET /api/v1/zonas/cp/{cp}` responde con `ETag` débil calculado sobre el cuerpo y `Cache-Control: private, no-cache` (siempre revalida; es catálogo, pero exige sesión; el 304 es barato porque la respuesta sale de Redis); con un `If-None-Match` vigente responde 304 sin cuerpo (spec 010, R-03). Las respuestas de la lista de espera llevan `Cache-Control: private, no-store`.
- **R-17:** El cuerpo de `POST /api/v1/zonas/lista-espera` se limita a 2 KB; uno mayor responde 413. Los campos desconocidos del cuerpo se ignoran. Las respuestas 429 llevan `Retry-After` (spec 009).

## 4. Criterios de aceptación
- **CA-01:** Dado un código postal de la ciudad piloto presente en el catálogo, cuando la persona lo consulta, entonces responde 200 con `enCobertura: true` y solo las colonias de ese código, con `id`, `nombre`, `cp` y `municipio`.
- **CA-02:** Dado un código postal sin colonias en el catálogo, cuando lo consulta, entonces responde 200 con `enCobertura: false`, `colonias: []` y el nombre de la ciudad piloto.
- **CA-03:** Dado un código mal formado (no numérico, de 4 o 6 dígitos, vacío o con intento de inyección), cuando lo consulta, entonces responde 422 con `errors.cp`, no repite el valor y no consulta el catálogo. El `%00` codificado queda fuera: el servidor lo rechaza con 400 antes de la aplicación.
- **CA-04:** Dada una consulta válida, cuando se inspeccionan la respuesta, la caché y los registros, entonces no aparecen coordenadas ni campos internos de la colonia (`geom`, `centro`, `piloto`, `nombreBusqueda`, `version`).
- **CA-05:** Dada una consulta fuera de cobertura, cuando la persona confirma «Avísame cuando lleguen», entonces responde 201 y hay un documento en `listas_espera` con su `sub`, el código postal y, si lo envió, el municipio.
- **CA-06:** Dado el mismo `sub` con el mismo código postal, cuando repite la lista de espera, entonces responde 200 con el registro existente y `yaRegistrado: true`, y sigue habiendo un solo documento.
- **CA-07:** Dado un `sub` distinto o un código postal distinto, cuando se registra, entonces se crea otro documento; el índice único no lo impide.
- **CA-08:** Dado un `municipio` de más de 100 caracteres, vacío, solo con espacios o con caracteres no permitidos, cuando se envía, entonces responde 422 con `errors.municipio` sin repetir el valor y no guarda nada.
- **CA-09:** Dados `Tula de Allende`, `San Agustín Tlaxiaca` e `Ixmiquilpan`, cuando se envían como municipio, entonces se aceptan; sin municipio, la petición también es válida.
- **CA-10:** Dado un cuerpo con un `usuarioId` de otra persona, cuando se registra, entonces el documento guarda el `sub` del token y no el del cuerpo.
- **CA-11:** Dada una petición sin token, cuando se llama cualquier ruta, entonces responde 401; pasados 5 registros en una hora por la misma persona, 429.
- **CA-12:** Dado el registro de una consulta o de una lista de espera, cuando se inspecciona, entonces no contiene coordenadas ni el código postal asociado a la persona.
- **CA-13:** Dado un código postal consultado dos veces seguidas, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-14:** Dado un código postal inventado consultado dos veces en menos de 5 minutos, cuando llega la segunda, entonces se responde «fuera de cobertura» desde la caché negativa.
- **CA-15:** Dada una consulta por código postal ya respondida, cuando se repite con su `ETag` en `If-None-Match`, entonces responde 304 sin cuerpo y con `Cache-Control: private, no-cache`.
- **CA-16:** Dado un cuerpo de lista de espera de más de 2 KB, cuando se envía, entonces responde 413 y no se guarda nada.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec023ZonaTests.CodigoPiloto_Lista_Sus_Colonias_CA_023_01` |
| CA-02 | `Spec023ZonaTests.Codigo_Fuera_De_Cobertura_CA_023_02` |
| CA-03 | `Spec023ValidacionTests.Codigo_Mal_Formado_CA_023_03` y `Spec023ZonaTests.Rechaza_Byte_Nulo_En_El_Servidor_CA_023_03` |
| CA-04 | `Spec023PrivacidadTests.Sin_Coordenadas_Ni_Campos_Internos_CA_023_04` |
| CA-05 | `Spec023ListaEsperaTests.Registra_Lista_De_Espera_CA_023_05` |
| CA-06 | `Spec023ListaEsperaTests.Idempotente_Por_Persona_Y_Codigo_CA_023_06` |
| CA-07 | `Spec023ListaEsperaTests.Dos_Personas_O_Dos_Codigos_CA_023_07` |
| CA-08 | `Spec023ValidacionTests.Municipio_Invalido_CA_023_08` |
| CA-09 | `Spec023ValidacionTests.Municipios_Validos_CA_023_09` |
| CA-10 | `Spec023SeguridadTests.Usuario_Del_Token_CA_023_10` |
| CA-11 | `Spec023SeguridadTests.Exige_Sesion_Y_Limite_CA_023_11` |
| CA-12 | `Spec023RegistrosTests.Sin_Datos_Personales_CA_023_12` |
| CA-13 | `Spec023CacheTests.Consulta_Repetida_Usa_Cache_CA_023_13` |
| CA-14 | `Spec023CacheTests.Cache_Negativa_Corta_CA_023_14` |
| CA-15 | `Spec023CacheTests.Etag_Y_304_CA_023_15` |
| CA-16 | `Spec023SeguridadTests.Cuerpo_Demasiado_Grande_CA_023_16` |

## 5. Contrato
| Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|
| `GET /api/v1/zonas/cp/{cp}` | persona con sesión | `cp` de 5 dígitos; `If-None-Match?` | 200 `{ enCobertura, ciudadPiloto, colonias[] }` con `ETag`, 304, 401, 422 (`errors.cp`), 429 |
| `POST /api/v1/zonas/lista-espera` | persona con sesión | `{ cp, municipio? }` | 201 (registro nuevo), 200 (repetido, `yaRegistrado: true`), 401, 413, 422 (`errors.cp`, `errors.municipio`), 429 |

**Cuerpos de respuesta**
- Zona: `{ enCobertura: bool, ciudadPiloto: string, colonias: [{ id, nombre, cp, municipio }] }`.
- Lista de espera: `{ id, cp, municipio?, creadoEn, yaRegistrado }`.

**Códigos de error:** no hay códigos de negocio propios; los errores de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429.

**Límites:** 5 registros en la lista de espera por hora y por persona; 100 caracteres de municipio; cuerpo de 2 KB; caché de 24 h en Redis para el código con colonias y de 5 minutos para el vacío; `max-age` de 1 hora en el cliente. Se registran en `docs/rate-limit-matrix.md`.

**Convención de errores:** la de la spec 002 (R-08 a R-12) y la común de las specs 020 a 026 (spec 020, §5): formato inválido 422 con `errors`, sin sesión 401, cuerpo grande 413, límite 429 con `Retry-After`.

## 6. Modelo de datos
**`colonias`** (existente, de TT-BD-06; esta spec no la modifica)
| Campo | Uso aquí |
|---|---|
| `_id`, `nombre`, `cp`, `municipio` | respuesta de la consulta por código postal |
| `nombreBusqueda` | orden y proyección; nunca se devuelve |
| `piloto`, `geom`, `centro`, `version` | no se leen en esta spec |

Índices: los de TT-BD-06. Esta spec solo usa `{ cp: 1 }`.

**`listas_espera`** (nueva, base `market`)
| Campo | Tipo | Nota |
|---|---|---|
| `_id` | ObjectId | |
| `usuarioId` | string | `sub` del JWT (36 caracteres); nunca el `ObjectId` de Identidad |
| `cp` | string | 5 dígitos |
| `municipio` | string? | 1–100 caracteres, opcional |
| `creadoEn` | date | UTC |

Índices: `{ usuarioId: 1, cp: 1 }` único y `{ cp: 1, creadoEn: -1 }` para el reporte de demanda por zona.

**Invariantes:** ningún documento tiene coordenadas; la lista de espera es única por persona y código; la persona sale del token.

El script `db/` de esta spec crea la colección y sus índices de forma idempotente y no borra ni cambia las existentes.

## 7. Arquitectura y componentes
- Se amplía el módulo `Catalog` existente (`src/Knock.Market.Api/Modules/Catalog/`): no se crea un módulo nuevo, porque ya es dueño de `colonias` y de su caché.
- `ZonasController` (consulta por código postal) y `ListaEsperaController` (registro) separados por responsabilidad (S6960).
- `ZonaService` aplica la regla de cobertura y arma la respuesta; reutiliza `CatalogStore.FindNamesByPostalCodeAsync` y la proyección existente, ampliada para devolver `id`, `nombre`, `cp` y `municipio`.
- `ListaEsperaService` resuelve el `sub`, valida el cuerpo y hace un solo `updateOne` con `upsert: true` sobre `{ usuarioId, cp }`; si insertó, responde 201; si ya existía, 200. El índice único es la garantía, no la lógica.
- `ListaEsperaStore` es el único punto con acceso a `listas_espera`; usa el cliente compartido de la spec 003 y la base `Mongo:Databases:Catalog` (hoy `market`).
- La ciudad piloto sale de `CatalogOptions` (`Catalogo:CiudadPiloto`), no se escribe en el código.
- La caché usa el cliente de Redis de la spec 003; si Redis no responde, la consulta va a MongoDB y se responde igual (la caché es una optimización, no un requisito de corrección).
- Sin estado en el proceso: todo vive en MongoDB salvo la caché del catálogo.

## 8. Dependencias
- Spec 002 (estructura y estilo de API), 003 (MongoDB, Redis, outbox), 009 (JWT, límites), 010 (caché y paginación), 017 (la colonia que se guarda en el perfil).
- `GET /api/v1/colonias/en-punto` de KNOCK-141 (TT-API-07): la ubicación se resuelve ahí; esta spec no la toca.
- Catálogo SEPOMEX de Tula de Allende (TT-BD-06): 13 colonias, 9 con polígono.
- `IColoniaCatalog` de `Knock.Contracts` que consume Identidad (spec 017) ya responde «¿existe esta colonia?» con `/colonias/existe`; esta spec no lo cambia.
- La app (frontend 017) consume los dos endpoints de esta spec.

## 9. Amenazas y controles
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Saber dónde vive la persona por sus coordenadas | Esta spec no recibe ni guarda coordenadas; la ubicación se descarta en `/colonias/en-punto` | CA-04, CA-12 |
| Enumerar el catálogo probando códigos postales | 200 uniforme para «no existe» y «fuera de cobertura», límites del gateway por IP y por usuario, sesión obligatoria y caché negativa; riesgo aceptado porque el catálogo es público (R-15) | CA-02, CA-14 |
| Inyección en el código postal o en el municipio | Validación estricta de formato y caracteres, filtros tipados del driver, 422 sin repetir el valor | CA-03, CA-08 |
| Suplantar a la persona de la lista de espera (BOLA) | La persona sale del `sub`; el cuerpo no acepta identificador | CA-10 |
| Llenar la lista de espera con registros falsos | Índice único por persona y código, límite de 5 por hora | CA-06, CA-11 |
| Fuga de datos personales en registros | No se registra el código postal junto a la persona ni el municipio | CA-12 |
| Caché con datos de una persona | La clave es el código postal, que no identifica a nadie; la lista de espera no se cachea | CA-13 |
| Caché envenenada con la ciudad de otra zona | La ciudad piloto sale de configuración y la clave incluye el código | CA-02 |

## 10. Plan de pruebas
| Tipo | ¿Aplica? | Motivo |
|---|---|---|
| Unitarias | Sí | Regla de cobertura, validadores y armado de la respuesta con un doble del catálogo |
| Integración | Sí | `Testcontainers` con `documentdb-local`: consulta por código, índice único de la lista de espera y caché con Redis real |
| Seguridad negativa | Sí | Código y municipio hostiles, sin token, usuario del cuerpo y límite de la lista de espera |
| Contrato | Sí | OpenAPI de Mercado regenerado y comparado |
| Arquitectura | Sí | El controller no accede a MongoDB ni a Redis; el módulo no lee colecciones de `Requests` |
| Mutación | Sí | Regla de cobertura y condición de «insertó o ya existía» |
| Rendimiento | Sí | Consulta por código postal con índice y caché; objetivo p95 menor a 100 ms, como el borrador anterior |
| Aceptación | Sí | CA-01 a CA-16 con la tabla 4.1 |
| Base de datos | Sí | `DatabaseTest`: `listas_espera` y su índice único creados de forma idempotente |
| Regresión | Sí | Las pruebas de `GET /colonias/en-punto` (TT-API-07) y `GET /colonias/existe` (spec 017) siguen en verde sin cambios; `dotnet test Knock.slnx -c Release` completo |
| CI/CD | Sí | Job `gate` de `ci-pr.yml` (build-test, openapi-diff, sast, sca, secrets) en verde; smoke de `/health/ready` tras el despliegue |
| Análisis estático | Sí | Semgrep (job `sast`) sin hallazgos nuevos; regla de registros sin `lat`/`lon`; complejidad máxima 10 |
| Vulnerabilidades y licencias | Sí | NuGetAudit, `dotnet list package --vulnerable`, OSV-Scanner y dependency-review sin altas ni críticas; sin dependencias nuevas |
| DAST | Sí (nocturno) | ZAP de API con token de prueba, con los valores hostiles de R-01 y R-06; no bloquea el PR |

**Gates:** los de `AGENTS.md`; contrato regenerado; la prueba de integración comprueba además que ningún documento de `listas_espera` tiene campos de ubicación.

## 11. Escenarios
**Positivos**
- La persona usa su ubicación, la app consulta `/colonias/en-punto`, confirma la colonia y el perfil guarda colonia y código postal.
- La persona escribe `42800`, ve las colonias y elige una.
- La persona escribe `91000` (fuera de cobertura), ve el aviso con «Tula de Allende» y se apunta a la lista de espera.
- La misma persona vuelve a apuntarse: la app le agradece otra vez sin duplicar.

**Negativos**
- Código de cuatro dígitos, con letras o con `%00` (este último espera 400 del servidor, no 422).
- Municipio con `<script>` o con salto de línea.
- Cuerpo con `usuarioId` de otra persona.
- Sexto registro en una hora.

**Errores esperados**
- 401 sin sesión, 422 por código o municipio, 429 por límite y por el límite global del gateway.

**Casos límite**
- Código con ceros a la izquierda (`01234`).
- Código válido que no existe en SEPOMEX y código que existe sin colonias (mismo 200 fuera de cobertura).
- Colonia sin polígono (`piloto: false`) visible por código postal.
- Dos personas del mismo código postal; una persona en dos códigos.
- Redis caído: la consulta funciona y solo se pierde la caché.

## 12. Definición de terminado
- Los 16 criterios tienen prueba en verde y la tabla 4.1 coincide con los nombres reales (`python scripts/sdd_lint.py --trace 023` sin faltantes).
- Job `gate` en verde, Semgrep y análisis de dependencias sin hallazgos altos ni críticos.
- La consulta por código postal y la lista de espera están en OpenAPI, en `docs/endpoints.md` y en `docs/rate-limit-matrix.md`.
- `db/` crea `listas_espera` con su índice único de forma idempotente y la prueba de base de datos lo compara.
- La prueba de integración demuestra que no hay coordenadas en la base ni en los registros.
- `Memoria.md` con la entrada del cambio; revisión de par y sin hallazgos de seguridad abiertos.

## 13. Decisiones
- **Se conserva el contrato ya aprobado** (`/api/v1/zonas/cp/{cp}` y `/api/v1/zonas/lista-espera`) en vez de inventar rutas nuevas: el frontend 017 se escribe en paralelo y ya lo conoce; cambiarlo obligaría a dos contratos para lo mismo.
- **Un módulo existente, no uno nuevo:** `Catalog` ya es dueño de `colonias`, de su caché y de la proyección; un módulo `Zonas` duplicaría el acceso a la misma colección.
- **Cobertura por presencia del código postal:** es la regla aprobada (decisión (d) del borrador anterior) y no depende del campo `piloto`, que significa «tiene polígono» y no «está en cobertura».
- **Idempotencia natural en la lista de espera:** un índice único basta para un registro de un solo documento.
- **Sin eventos:** nada consume todavía el alta en la lista de espera; el aviso de cobertura es del panel y se especificará cuando exista.
- **Caché negativa de 5 minutos:** protege a la base de códigos inventados sin dejar «fuera de cobertura» grabado 24 h cuando se abra una zona.

## 14. Decisiones tomadas sin consulta
Se tomaron de noche, con el criterio de la constitución; quien apruebe puede cambiarlas antes de firmar.

1. **La forma de la respuesta de zona se unifica** a `{ enCobertura, ciudadPiloto, colonias }` en los dos casos. El borrador anterior devolvía `ciudadPiloto` sin `colonias` cuando estaba fuera de cobertura; que la forma cambie según el caso obliga a la app a dos modelos. Alternativa: mantener las dos formas del borrador, con más código en el cliente.
2. **La ciudad piloto sale de configuración** (`Catalogo:CiudadPiloto`) en vez de estar escrita en el código o en el catálogo. Alternativa: guardarla en cada colonia, que repetiría el dato 12 veces y lo haría difícil de cambiar.
3. **La lista de espera acepta cualquier código de cinco dígitos**, aunque no exista en SEPOMEX. Su propósito es captar demanda fuera de cobertura; exigir que exista dejaría fuera a quien escribe mal su código, pero también permitiría códigos inventados. Se acepta el segundo riesgo a cambio de no bloquear a nadie.
4. **`GET /zonas/cp/{cp}` exige sesión.** Es el mismo criterio de todos los endpoints de catálogo del repositorio; la pantalla de registro ya tiene sesión (spec 015). Alternativa: hacerlo público con límite por IP, que expone el catálogo completo sin necesidad.
5. **No se crea un endpoint para «ver mi zona guardada»:** el perfil (spec 017) ya devuelve `colonia` y `codigoPostal`.
6. **Se acepta el nombre `zonas` en la ruta aunque la colección sea `colonias`:** es la ruta aprobada; renombrarla a `colonias/por-codigo-postal` rompería el contrato que el frontend ya escribió.

**Agregadas en la revisión del 9 de noviembre**

7. **`ETag` y `private, no-cache` en la consulta por código postal (decisión del 2026-11-12).** Es catálogo y la constitución pide caché en toda lectura; `private` porque la ruta exige sesión y `no-cache` para que siempre revalide con `If-None-Match` (spec 010, R-06): deja un solo valor HTTP y evita que la app y el servidor tengan vigencias distintas. Alternativas descartadas: `max-age=3600` (tres tiempos distintos) y `no-store`, como `/colonias/en-punto`, que sí lleva coordenadas.
8. **422 para formato, como el resto de las specs 020 a 026.** Ya era el comportamiento aprobado de HU-009 y del filtro de validación; ahora es la convención común.

## 15. Riesgos y dudas abiertas
- **Pantallas sin sesión:** si la app necesita listar colonias por código postal antes de iniciar sesión (exploración de HU-005), estos endpoints tendrían que volverse públicos con límite por IP. El borrador anterior ya dejó la duda (pregunta (g)); sigue abierta.
- **La lista de espera no avisa a nadie:** el registro se guarda y ahí termina. Avisar cuando se abra la cobertura requiere un proceso y una plantilla de mensajería que no están en este sprint.
- **El código postal no se valida contra SEPOMEX:** un código inventado de cinco dígitos entra a la lista de espera. La consecuencia es ruido en el reporte de demanda, no un riesgo de seguridad.
- **Los textos de ciudad piloto están duplicados en la documentación:** `DECISIONES.md` todavía dice Pachuca en algunos puntos; esta spec usa Tula de Allende, como el catálogo y el borrador aprobado.
- **El catálogo tiene 13 colonias de una sola ciudad:** la regla de cobertura por presencia no se ha probado con varias ciudades; al abrir la segunda, habrá que decidir si la cobertura pasa a un campo explícito.
- **El límite por IP depende de KNOCK-180 (riesgo aparte):** el gateway solo ve la IP real del cliente si `ForwardedHeaders:KnownNetworks` apunta a la red de Container Apps. El Terraform la define con `container_apps_subnet_cidr`, pero KNOCK-180 (fijar esas redes en el despliegue) sigue por hacer y no se verificó el valor real. Sin eso el límite por IP contaría todo el tráfico como una sola IP.
- **Mercado no tiene host de integración:** esta spec necesita `MarketApiFactory` con `Testcontainers`, igual que 020, 021 y 022.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Implementada (tras el merge)
