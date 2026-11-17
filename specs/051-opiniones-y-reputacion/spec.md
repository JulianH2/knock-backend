# Spec 051 — Opiniones y reputación del técnico

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints OPI-01, OPI-02, OPI-03.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Mercado (`Knock.Market.Api`) · **Módulo:** `Reviews` · **Colecciones:** `opiniones`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 041)

## 1. Objetivo
Definir y especificar los endpoints OPI-01, OPI-02, OPI-03 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como cliente, quiero calificar un trabajo terminado, y como técnico responder a las opiniones, para que la reputación refleje el servicio real.

## 2. Historia y alcance
**2.1 Historia**
- Como cliente, quiero calificar un trabajo terminado, y como técnico responder a las opiniones, para que la reputación refleje el servicio real.

**2.2 Dentro**
- `GET /api/v1/tecnicos/{id}/opiniones` (OPI-01): Opiniones de un técnico con su distribución por estrellas.
- `POST /api/v1/visitas/{id}/opinion` (OPI-02): El cliente deja su opinión (hasta 30 días después). Recalcula la reputación del técnico.
- `POST /api/v1/opiniones/{id}/respuesta` (OPI-03): El técnico responde a una opinión.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 041.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `GET /api/v1/tecnicos/{id}/opiniones` (leer todos): Opiniones de un técnico con su distribución por estrellas. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-02:** Entrada de `GET /api/v1/tecnicos/{id}/opiniones`: Ruta: id. Query: cursor, limite. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-03:** Estados de `GET /api/v1/tecnicos/{id}/opiniones`: 404 `technician_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-04:** `GET /api/v1/tecnicos/{id}/opiniones` pagina por cursor (Por cursor, 20 (máx. 50)): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Orden por fecha descendente.
- **R-05:** `GET /api/v1/tecnicos/{id}/opiniones` se cachea (Sí, 1 minuto); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-06:** `POST /api/v1/visitas/{id}/opinion` (crear): El cliente deja su opinión (hasta 30 días después). Recalcula la reputación del técnico. Lo puede llamar: persona con sesión que sea dueña del recurso. Responde 201 cuando sale bien.
- **R-07:** Entrada de `POST /api/v1/visitas/{id}/opinion`: Ruta: id. Cuerpo: calificacion, partes, comentario, fotos, guardarTecnicoConfianza. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `calificacion`: De 1 a 5; `partes`: Cada una de 1 a 5; `comentario`: Máximo 500 caracteres. Moderado; `fotos`: Hasta 3. Moderadas.
- **R-08:** Estados de `POST /api/v1/visitas/{id}/opinion`: 409 `review_not_allowed` (ya opinó o venció el plazo). Los códigos de negocio van en inglés y en snake_case.
- **R-09:** `POST /api/v1/opiniones/{id}/respuesta` (actualizar (acción)): El técnico responde a una opinión. Lo puede llamar: técnico (calificado). Responde 200 cuando sale bien.
- **R-10:** Entrada de `POST /api/v1/opiniones/{id}/respuesta`: Ruta: id. Cuerpo: texto. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-11:** Estados de `POST /api/v1/opiniones/{id}/respuesta`: 409 `review_already_answered`. Los códigos de negocio van en inglés y en snake_case.
- **R-12:** **Quién opina y cuándo (OPI-02).** Solo el cliente dueño de una visita `terminada`, **una sola opinión por visita** y hasta 30 días después de terminada; después responde 409 `review_not_allowed`. Solo se opina de servicios hechos por Knock; la opinión sale como «Servicio verificado».
- **R-13:** **Contenido de la opinión.** `calificacion` entero de 1 a 5 (obligatoria); `partes` opcional con puntualidad, calidad, precio justo y trato, cada una de 1 a 5; `comentario` opcional de hasta 500 caracteres, moderado (spec 031); `fotos` de 0 a 3, moderadas (spec 032); `guardarTecnicoConfianza` opcional para sumar al técnico a los de confianza.
- **R-14:** **Reputación.** Al guardar una opinión se recalcula la reputación del técnico (promedio con un decimal, distribución de 1 a 5 y nivel) y se guarda ya calculada en su perfil; el cálculo es asíncrono. Niveles: `nuevo` (menos de 5 opiniones), `confiable` (5 o más y promedio de 4.0 o más) y `destacado` (20 o más y promedio de 4.7 o más). La solicitud de la visita pasa a `calificada` (spec 020).
- **R-15:** **Respuesta del técnico (OPI-03).** Solo el técnico calificado, **una vez** por opinión; `texto` de 1 a 500 caracteres (igual que el comentario), moderado; después responde 409 `review_already_answered`.
- **R-16:** **Lectura (OPI-01).** Lista las opiniones visibles de un técnico con su distribución por estrellas, de la más nueva a la más vieja, por cursor (20 por defecto, máximo 50); caché de 1 minuto. Las opiniones de cuentas eliminadas se muestran como «Usuario eliminado», sin nombre ni foto.
- **R-17:** **Moderación y reportes.** Una opinión con texto o fotos pendiente de revisión no se publica hasta pasar la moderación (spec 031); una opinión reportada se atiende por el panel (spec 053) y puede ocultarse sin borrarse.
- **R-18:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-19:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-20:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-21:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/tecnicos/{id}/opiniones` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/tecnicos/{id}/opiniones`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/tecnicos/{id}/opiniones`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-04:** Dado el caso «el técnico no existe o está oculto», cuando llama `GET /api/v1/tecnicos/{id}/opiniones`, entonces responde 404 `technician_not_found` con ProblemDetails y no cambia el estado.
- **CA-05:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-06:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-07:** Dado el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/visitas/{id}/opinion` con datos válidos, entonces responde 201 con lo descrito en §5.
- **CA-08:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/visitas/{id}/opinion`, entonces responde 401 y no ejecuta la operación.
- **CA-09:** Dada una persona que no cumple el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/visitas/{id}/opinion`, entonces responde 403 y no cambia nada.
- **CA-10:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/visitas/{id}/opinion`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-11:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/visitas/{id}/opinion`, entonces responde 400 y no guarda nada.
- **CA-12:** Dado el caso «ya opinó de esa visita, la visita no está terminada o pasaron más de 30 días», cuando llama `POST /api/v1/visitas/{id}/opinion`, entonces responde 409 `review_not_allowed` con ProblemDetails y no cambia el estado.
- **CA-13:** Dado el rol «técnico (calificado)», cuando llama `POST /api/v1/opiniones/{id}/respuesta` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-14:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/opiniones/{id}/respuesta`, entonces responde 401 y no ejecuta la operación.
- **CA-15:** Dada una persona que no cumple el rol «técnico (calificado)», cuando llama `POST /api/v1/opiniones/{id}/respuesta`, entonces responde 403 y no cambia nada.
- **CA-16:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/opiniones/{id}/respuesta`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-17:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/opiniones/{id}/respuesta`, entonces responde 400 y no guarda nada.
- **CA-18:** Dado el caso «la opinión ya tiene respuesta del técnico», cuando llama `POST /api/v1/opiniones/{id}/respuesta`, entonces responde 409 `review_already_answered` con ProblemDetails y no cambia el estado.
- **CA-19:** Dado el cliente de una visita terminada hace menos de 30 días, cuando opina, entonces se crea la opinión; una segunda opinión de la misma visita o pasados 30 días responde 409 `review_not_allowed`.
- **CA-20:** Dada una opinión con calificación de 0 o 6, un comentario de más de 500 caracteres o más de 3 fotos, cuando se envía, entonces responde 422 y no se guarda.
- **CA-21:** Dada una opinión nueva, cuando se guarda, entonces la reputación del técnico se recalcula con ella y las consultas posteriores la reflejan; con 5 opiniones y promedio de 4.2 su nivel es `confiable`; y la solicitud pasa a `calificada`.
- **CA-22:** Dada una opinión sin respuesta, cuando el técnico calificado responde, entonces queda publicada; una segunda respuesta responde 409 `review_already_answered` y la de otro técnico, 403.
- **CA-23:** Dado un técnico con opiniones, cuando se consultan, entonces salen de la más nueva a la más vieja con su distribución; las de cuentas eliminadas aparecen anonimizadas.
- **CA-24:** Dada una opinión con comentario, cuando aún no pasa la moderación, entonces no aparece en la lista pública; al aprobarse, sí.
- **CA-25:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-26:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-27:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-28:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec051Tests.Criterio01_CA_051_01` |
| CA-02 | `Spec051Tests.Criterio02_CA_051_02` |
| CA-03 | `Spec051Tests.Criterio03_CA_051_03` |
| CA-04 | `Spec051Tests.Criterio04_CA_051_04` |
| CA-05 | `Spec051Tests.Criterio05_CA_051_05` |
| CA-06 | `Spec051Tests.Criterio06_CA_051_06` |
| CA-07 | `Spec051Tests.Criterio07_CA_051_07` |
| CA-08 | `Spec051Tests.Criterio08_CA_051_08` |
| CA-09 | `Spec051Tests.Criterio09_CA_051_09` |
| CA-10 | `Spec051Tests.Criterio10_CA_051_10` |
| CA-11 | `Spec051Tests.Criterio11_CA_051_11` |
| CA-12 | `Spec051Tests.Criterio12_CA_051_12` |
| CA-13 | `Spec051Tests.Criterio13_CA_051_13` |
| CA-14 | `Spec051Tests.Criterio14_CA_051_14` |
| CA-15 | `Spec051Tests.Criterio15_CA_051_15` |
| CA-16 | `Spec051Tests.Criterio16_CA_051_16` |
| CA-17 | `Spec051Tests.Criterio17_CA_051_17` |
| CA-18 | `Spec051Tests.Criterio18_CA_051_18` |
| CA-19 | `Spec051Tests.Criterio19_CA_051_19` |
| CA-20 | `Spec051Tests.Criterio20_CA_051_20` |
| CA-21 | `Spec051Tests.Criterio21_CA_051_21` |
| CA-22 | `Spec051Tests.Criterio22_CA_051_22` |
| CA-23 | `Spec051Tests.Criterio23_CA_051_23` |
| CA-24 | `Spec051Tests.Criterio24_CA_051_24` |
| CA-25 | `Spec051Tests.Criterio25_CA_051_25` |
| CA-26 | `Spec051Tests.Criterio26_CA_051_26` |
| CA-27 | `Spec051Tests.Criterio27_CA_051_27` |
| CA-28 | `Spec051Tests.Criterio28_CA_051_28` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| OPI-01 | `GET /api/v1/tecnicos/{id}/opiniones` | persona con sesión (cliente o técnico) | Ruta: id. Query: cursor, limite | 200; 401; 404 |
| OPI-02 | `POST /api/v1/visitas/{id}/opinion` | persona con sesión que sea dueña del recurso | Ruta: id. Cuerpo: calificacion, partes, comentario, fotos, guardarTecnicoConfianza | 201; 400; 401; 403; 409 (ya opinó o venció el plazo); 422 |
| OPI-03 | `POST /api/v1/opiniones/{id}/respuesta` | técnico (calificado) | Ruta: id. Cuerpo: texto | 200; 400; 401; 403; 409 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| OPI-01 | Orden por fecha descendente | Por cursor, 20 (máx. 50) | Sí, 1 minuto |
| OPI-02 | No aplica | No | No |
| OPI-03 | No aplica | No | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| OPI-01 | 404 | `technician_not_found` | el técnico no existe o está oculto |
| OPI-02 | 409 | `review_not_allowed` | ya opinó de esa visita, la visita no está terminada o pasaron más de 30 días |
| OPI-03 | 409 | `review_already_answered` | la opinión ya tiene respuesta del técnico |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `opiniones`.** Calificación del cliente sobre un trabajo terminado. Solo se puede opinar de servicios hechos por Knock, hasta 30 días después, y sale como Servicio verificado. El técnico puede responder. De las opiniones se calcula el promedio, la distribución por estrellas y el nivel del técnico: Nuevo (0 trabajos), Confiable (1), Experto Knock (5) y Maestro Knock (15)

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| visitaId | ObjectId | Sí | Debe estar terminado. Una opinión por visita | Único |
| tecnicoId | ObjectId | Sí | Debe existir | Compuesto |
| clienteId | ObjectId | Sí | Debe ser el cliente de la visita | No |
| calificacion | Entero | Sí | De 1 a 5 | No |
| partes | Documento | No | Cada una de 1 a 5 | No |
| comentario | Texto | No | Máximo 500 caracteres. Moderado | No |
| fotos | Lista de textos | No | Hasta 3. Moderadas | No |
| respuesta | Documento | No | Una por opinión | No |
| anonimizada | Booleano | Sí | Por defecto falso | No |
| creadoEn | Fecha y hora UTC | Sí | Antes de 30 días del trabajo | Compuesto |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Único | visitaId | Una opinión por trabajo |
| Compuesto | tecnicoId + creadoEn (descendente) | Opiniones de un técnico, de la más nueva a la más vieja |

## 7. Arquitectura y componentes
- Proyecto `Knock.Market.Api`, módulo `Reviews` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 022 de este repositorio.
- Spec 031 de este repositorio.
- Spec 032 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 041 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Moderación de la spec 031 y entrega de imágenes de la 032 (hoy no implementadas)
  - Tareas de cierre del ejecutor de la 054 (hoy no implementado)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-09, CA-15, CA-25 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-09, CA-15, CA-25 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-03, CA-10 a CA-11, CA-16 a CA-17 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-08, CA-14 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-27 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-26 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-28 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_051_NN` en el nombre (para `python scripts/sdd_lint.py --trace 051`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-03, CA-08 a CA-11, CA-14 a CA-17, CA-25, CA-27) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Calificar:** el cliente recibe el aviso de que su visita terminó → califica con estrellas, escribe un comentario y decide si guarda al técnico de confianza → la reputación del técnico se actualiza.
- **Responder:** el técnico ve una opinión nueva → escribe su respuesta → queda publicada junto a la opinión.
- **Fuera de plazo:** pasados 30 días, el cliente ya no puede opinar y la app se lo explica.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 051` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** La calificación numérica cuenta desde que se envía; el comentario y las fotos se publican después de pasar la moderación (spec 031): una opinión no debe perderse por una revisión de texto.
- **D-05:** La respuesta del técnico es una sola, no se edita y tiene el mismo máximo que el comentario (500).
- **D-06:** Niveles simples y calculables (`nuevo`, `confiable`, `destacado`): una sola opinión no define a un técnico.
- **D-07:** Sin compromiso de tiempo para el recálculo: es asíncrono, como dice el Excel.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Reputación: antes con un compromiso de consistencia en menos de 1 minuto; ahora asíncrona, sin plazo.
- **C-02:** Niveles: antes sin definir; ahora tres niveles con umbrales.
- **C-03:** Respuesta del técnico: antes 300 caracteres; ahora 500, igual que el comentario.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencias: moderación de la spec 031, imágenes de la 032, estado `calificada` de la 020 y el cierre a los 30 días de la 054.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
