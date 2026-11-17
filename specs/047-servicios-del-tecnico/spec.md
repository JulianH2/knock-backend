# Spec 047 — Servicios y precios de cada técnico

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints SRV-01, SRV-02, SRV-03, SRV-04.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Mercado (`Knock.Market.Api`) · **Módulo:** `TechnicianServices` · **Colecciones:** `servicios_tecnico`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 037)

## 1. Objetivo
Definir y especificar los endpoints SRV-01, SRV-02, SRV-03, SRV-04 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como técnico, quiero definir los servicios que ofrezco y mis precios de referencia, para recibir solicitudes que sí puedo atender.

## 2. Historia y alcance
**2.1 Historia**
- Como técnico, quiero definir los servicios que ofrezco y mis precios de referencia, para recibir solicitudes que sí puedo atender.

**2.2 Dentro**
- `GET /api/v1/tecnicos/{id}/servicios` (SRV-01): Servicios y precios de referencia de un técnico.
- `POST /api/v1/tecnicos/me/servicios` (SRV-02): Agrega un servicio.
- `PUT /api/v1/tecnicos/me/servicios/{id}` (SRV-03): Edita un servicio.
- `DELETE /api/v1/tecnicos/me/servicios/{id}` (SRV-04): Quita un servicio.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 037.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `GET /api/v1/tecnicos/{id}/servicios` (leer todos): Servicios y precios de referencia de un técnico. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-02:** Entrada de `GET /api/v1/tecnicos/{id}/servicios`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-03:** Estados de `GET /api/v1/tecnicos/{id}/servicios`: 404 `technician_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-04:** `GET /api/v1/tecnicos/{id}/servicios` se cachea (Sí, 1 minuto); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-05:** `POST /api/v1/tecnicos/me/servicios` (crear): Agrega un servicio. Lo puede llamar: cuenta con perfil de técnico. Responde 201 cuando sale bien.
- **R-06:** Entrada de `POST /api/v1/tecnicos/me/servicios`: Cuerpo: nombre, detalle, precioReferencia, oficio. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `nombre`: Máximo 60 caracteres; `detalle`: Máximo 150 caracteres; `precioReferencia`: Mayor que 0; `oficio`: Debe existir en oficios.
- **R-07:** `PUT /api/v1/tecnicos/me/servicios/{id}` (actualizar): Edita un servicio. Lo puede llamar: técnico (dueño). Responde 200 cuando sale bien.
- **R-08:** Entrada de `PUT /api/v1/tecnicos/me/servicios/{id}`: Ruta: id. Cuerpo: campos. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-09:** Estados de `PUT /api/v1/tecnicos/me/servicios/{id}`: 404 `service_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-10:** `DELETE /api/v1/tecnicos/me/servicios/{id}` (borrar): Quita un servicio. Lo puede llamar: técnico (dueño). Responde 204 cuando sale bien.
- **R-11:** Entrada de `DELETE /api/v1/tecnicos/me/servicios/{id}`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-12:** Estados de `DELETE /api/v1/tecnicos/me/servicios/{id}`: 404 `service_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-13:** **Datos del servicio (SRV-02 y SRV-03).** `nombre` de 1 a 60 caracteres, `detalle` de hasta 150, `precioReferencia` entero positivo en pesos (sin tope de negocio; el límite es el del tipo `int32`) y `oficio` activo del catálogo y entre los oficios del técnico, para mantener la coherencia con la elegibilidad de la spec 028. En la edición (`PUT`) cambian solo los campos enviados.
- **R-14:** **Límite por técnico.** Cada técnico puede tener hasta 50 servicios (tope de cordura contra abuso); el 51.º responde 409 `service_limit_reached`.
- **R-15:** **Propiedad (SRV-03 y SRV-04).** Un técnico solo edita y elimina sus propios servicios; uno ajeno responde 404 `service_not_found`, sin revelar que existe. Al eliminar, el servicio se borra y deja de verse de inmediato.
- **R-16:** **Lectura (SRV-01).** Cualquier persona con sesión ve los servicios y los precios de referencia de un técnico visible; no hay paginación (máximo 30); caché de 1 minuto que se invalida al crear, editar o eliminar. Un técnico oculto responde 404 `technician_not_found`.
- **R-17:** **El precio es de referencia.** El `precioReferencia` solo orienta al cliente y a la referencia de mercado: no obliga al técnico y no sustituye a la cotización de cada solicitud.
- **R-18:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-19:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-20:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-21:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/tecnicos/{id}/servicios` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/tecnicos/{id}/servicios`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/tecnicos/{id}/servicios`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-04:** Dado el caso «el técnico no existe o está oculto», cuando llama `GET /api/v1/tecnicos/{id}/servicios`, entonces responde 404 `technician_not_found` con ProblemDetails y no cambia el estado.
- **CA-05:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-06:** Dado el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/tecnicos/me/servicios` con datos válidos, entonces responde 201 con lo descrito en §5.
- **CA-07:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/tecnicos/me/servicios`, entonces responde 401 y no ejecuta la operación.
- **CA-08:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/tecnicos/me/servicios`, entonces responde 404 sin revelar que el recurso existe y no cambia nada.
- **CA-09:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/tecnicos/me/servicios`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-10:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/tecnicos/me/servicios`, entonces responde 400 y no guarda nada.
- **CA-11:** Dado el rol «técnico (dueño)», cuando llama `PUT /api/v1/tecnicos/me/servicios/{id}` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-12:** Dada una petición sin token o con token vencido, cuando llama `PUT /api/v1/tecnicos/me/servicios/{id}`, entonces responde 401 y no ejecuta la operación.
- **CA-13:** Dada una persona que no cumple el rol «técnico (dueño)», cuando llama `PUT /api/v1/tecnicos/me/servicios/{id}`, entonces responde 403 y no cambia nada.
- **CA-14:** Dado un valor inválido según las reglas de §6, cuando llama `PUT /api/v1/tecnicos/me/servicios/{id}`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-15:** Dado un cuerpo que no se puede leer, cuando llama `PUT /api/v1/tecnicos/me/servicios/{id}`, entonces responde 400 y no guarda nada.
- **CA-16:** Dado el caso «el servicio no existe o no es del técnico», cuando llama `PUT /api/v1/tecnicos/me/servicios/{id}`, entonces responde 404 `service_not_found` con ProblemDetails y no cambia el estado.
- **CA-17:** Dado el rol «técnico (dueño)», cuando llama `DELETE /api/v1/tecnicos/me/servicios/{id}` con datos válidos, entonces responde 204 con lo descrito en §5.
- **CA-18:** Dada una petición sin token o con token vencido, cuando llama `DELETE /api/v1/tecnicos/me/servicios/{id}`, entonces responde 401 y no ejecuta la operación.
- **CA-19:** Dada una persona que no cumple el rol «técnico (dueño)», cuando llama `DELETE /api/v1/tecnicos/me/servicios/{id}`, entonces responde 403 y no cambia nada.
- **CA-20:** Dado un valor inválido según las reglas de §6, cuando llama `DELETE /api/v1/tecnicos/me/servicios/{id}`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-21:** Dado el caso «el servicio no existe o no es del técnico», cuando llama `DELETE /api/v1/tecnicos/me/servicios/{id}`, entonces responde 404 `service_not_found` con ProblemDetails y no cambia el estado.
- **CA-22:** Dado un técnico, cuando agrega un servicio con datos válidos, entonces queda creado; con un precio de 0 o negativo, un nombre de más de 60 caracteres o un oficio que no es suyo responde 422.
- **CA-23:** Dado un técnico con 50 servicios, cuando agrega uno más, entonces responde 409 `service_limit_reached` y no lo guarda.
- **CA-24:** Dado el servicio de otro técnico, cuando se intenta editar o borrar, entonces responde 404 `service_not_found` y no cambia nada.
- **CA-25:** Dado un técnico con servicios, cuando una persona con sesión los consulta, entonces los recibe ordenados por nombre; tras editar uno, la siguiente consulta ya muestra el cambio.
- **CA-26:** Dada una cotización de una solicitud, cuando el técnico envía un precio distinto del precio de referencia de su servicio, entonces se acepta mientras cumpla las reglas de la spec 020.
- **CA-27:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-28:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-29:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-30:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec047Tests.Criterio01_CA_047_01` |
| CA-02 | `Spec047Tests.Criterio02_CA_047_02` |
| CA-03 | `Spec047Tests.Criterio03_CA_047_03` |
| CA-04 | `Spec047Tests.Criterio04_CA_047_04` |
| CA-05 | `Spec047Tests.Criterio05_CA_047_05` |
| CA-06 | `Spec047Tests.Criterio06_CA_047_06` |
| CA-07 | `Spec047Tests.Criterio07_CA_047_07` |
| CA-08 | `Spec047Tests.Criterio08_CA_047_08` |
| CA-09 | `Spec047Tests.Criterio09_CA_047_09` |
| CA-10 | `Spec047Tests.Criterio10_CA_047_10` |
| CA-11 | `Spec047Tests.Criterio11_CA_047_11` |
| CA-12 | `Spec047Tests.Criterio12_CA_047_12` |
| CA-13 | `Spec047Tests.Criterio13_CA_047_13` |
| CA-14 | `Spec047Tests.Criterio14_CA_047_14` |
| CA-15 | `Spec047Tests.Criterio15_CA_047_15` |
| CA-16 | `Spec047Tests.Criterio16_CA_047_16` |
| CA-17 | `Spec047Tests.Criterio17_CA_047_17` |
| CA-18 | `Spec047Tests.Criterio18_CA_047_18` |
| CA-19 | `Spec047Tests.Criterio19_CA_047_19` |
| CA-20 | `Spec047Tests.Criterio20_CA_047_20` |
| CA-21 | `Spec047Tests.Criterio21_CA_047_21` |
| CA-22 | `Spec047Tests.Criterio22_CA_047_22` |
| CA-23 | `Spec047Tests.Criterio23_CA_047_23` |
| CA-24 | `Spec047Tests.Criterio24_CA_047_24` |
| CA-25 | `Spec047Tests.Criterio25_CA_047_25` |
| CA-26 | `Spec047Tests.Criterio26_CA_047_26` |
| CA-27 | `Spec047Tests.Criterio27_CA_047_27` |
| CA-28 | `Spec047Tests.Criterio28_CA_047_28` |
| CA-29 | `Spec047Tests.Criterio29_CA_047_29` |
| CA-30 | `Spec047Tests.Criterio30_CA_047_30` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| SRV-01 | `GET /api/v1/tecnicos/{id}/servicios` | persona con sesión (cliente o técnico) | Ruta: id | 200; 401; 404 |
| SRV-02 | `POST /api/v1/tecnicos/me/servicios` | cuenta con perfil de técnico | Cuerpo: nombre, detalle, precioReferencia, oficio | 201; 400; 401; 422 |
| SRV-03 | `PUT /api/v1/tecnicos/me/servicios/{id}` | técnico (dueño) | Ruta: id. Cuerpo: campos | 200; 400; 401; 403; 404 |
| SRV-04 | `DELETE /api/v1/tecnicos/me/servicios/{id}` | técnico (dueño) | Ruta: id | 204; 401; 403; 404 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| SRV-01 | Filtro por oficio | No (pocos) | Sí, 1 minuto |
| SRV-02 | No aplica | No | No |
| SRV-03 | No aplica | No | No |
| SRV-04 | No aplica | No | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| SRV-01 | 404 | `technician_not_found` | el técnico no existe o está oculto |
| SRV-03 | 404 | `service_not_found` | el servicio no existe o no es del técnico |
| SRV-04 | 404 | `service_not_found` | el servicio no existe o no es del técnico |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `servicios_tecnico`.** Lista de lo que cada técnico ofrece, con un precio de referencia. Se ve en su perfil (solo con sesión) y en Explorar con la leyenda desde y el precio. Es solo una referencia: el precio real lo da la cotización

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| tecnicoId | ObjectId | Sí | Debe existir en tecnicos | Compuesto |
| nombre | Texto | Sí | Máximo 60 caracteres | Texto |
| detalle | Texto | No | Máximo 150 caracteres | No |
| precioReferencia | Entero | Sí | Mayor que 0 | No |
| oficio | Texto | Sí | Debe existir en oficios | Compuesto |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Compuesto | tecnicoId + oficio | Servicios de un técnico y filtros por oficio |
| Texto (español) | nombre | Autocompletado de servicios |

## 7. Arquitectura y componentes
- Proyecto `Knock.Market.Api`, módulo `TechnicianServices` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 046 de este repositorio.
- Spec 020 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 037 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Perfil del técnico y catálogo de oficios activos (spec 046, hoy no implementada)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-08, CA-13, CA-19, CA-27 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-08, CA-13, CA-19, CA-27 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-03, CA-09 a CA-10, CA-14 a CA-15, CA-20 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-07, CA-12, CA-18 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-29 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-28 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-30 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_047_NN` en el nombre (para `python scripts/sdd_lint.py --trace 047`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-03, CA-07 a CA-10, CA-12 a CA-15, CA-18 a CA-20, CA-27, CA-29) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Agregar:** el técnico abre «Mis servicios» → toca «Agregar» → captura nombre, detalle, precio y oficio → el servicio aparece en su perfil.
- **Ver:** el cliente abre el perfil de un técnico con sesión → ve sus servicios con precios de referencia.
- **Límite:** un técnico con 30 servicios intenta agregar otro → la app le dice que llegó al límite.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 047` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** Sin tope de negocio al precio de referencia: es solo una guía y las cotizaciones reales ya se validan en la spec 020; un tope arbitrario bloquearía trabajos grandes legítimos.
- **D-05:** Máximo 50 servicios por técnico: tope de cordura.
- **D-06:** El servicio debe ser de un oficio que el técnico ofrece, para no mostrar servicios por los que nunca recibirá solicitudes.
- **D-07:** Eliminar un servicio lo borra: las cotizaciones guardan su propio precio.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Precio: antes máximo de 100 000 pesos; ahora sin tope de negocio.
- **C-02:** Servicios por técnico: antes 30; ahora 50 como tope de cordura.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencia: oficios y elegibilidad de las specs 028 y 046.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
