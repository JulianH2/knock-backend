# Spec 040 — Direcciones del cliente

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints DIR-01, DIR-02, DIR-03, DIR-04.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Identidad (`Knock.Identity.Api`) · **Módulo:** `Addresses` · **Colecciones:** `direcciones`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 030)

## 1. Objetivo
Definir y especificar los endpoints DIR-01, DIR-02, DIR-03, DIR-04 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como cliente, quiero guardar y editar las direcciones donde pido servicios, para no escribirlas cada vez.

## 2. Historia y alcance
**2.1 Historia**
- Como cliente, quiero guardar y editar las direcciones donde pido servicios, para no escribirlas cada vez.

**2.2 Dentro**
- `GET /api/v1/me/direcciones` (DIR-01): Lista las direcciones del cliente.
- `POST /api/v1/me/direcciones` (DIR-02): Guarda una dirección nueva.
- `PUT /api/v1/me/direcciones/{id}` (DIR-03): Edita una dirección.
- `DELETE /api/v1/me/direcciones/{id}` (DIR-04): Elimina una dirección.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 030.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `GET /api/v1/me/direcciones` (leer todos): Lista las direcciones del cliente. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-02:** `POST /api/v1/me/direcciones` (crear): Guarda una dirección nueva. Lo puede llamar: persona con sesión (cliente o técnico). Responde 201 cuando sale bien.
- **R-03:** Entrada de `POST /api/v1/me/direcciones`: Cuerpo: calleNumero, colonia, referencias, principal. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `calleNumero`: Mínimo 8 caracteres. Cifrado a nivel de campo; `colonia`: Debe existir en colonias; `referencias`: Máximo 200 caracteres. Cifrado; `principal`: Solo una principal por persona.
- **R-04:** `PUT /api/v1/me/direcciones/{id}` (actualizar): Edita una dirección. Lo puede llamar: persona con sesión que sea dueña del recurso. Responde 200 cuando sale bien.
- **R-05:** Entrada de `PUT /api/v1/me/direcciones/{id}`: Ruta: id. Cuerpo: campos a cambiar. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-06:** Estados de `PUT /api/v1/me/direcciones/{id}`: 404. El código de negocio de cada conflicto va en inglés y en snake_case, y se define al implementar.
- **R-07:** `DELETE /api/v1/me/direcciones/{id}` (borrar): Elimina una dirección. Lo puede llamar: persona con sesión que sea dueña del recurso. Responde 204 cuando sale bien.
- **R-08:** Entrada de `DELETE /api/v1/me/direcciones/{id}`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-09:** Estados de `DELETE /api/v1/me/direcciones/{id}`: 404. El código de negocio de cada conflicto va en inglés y en snake_case, y se define al implementar.
- **R-10:** **Datos de la dirección (DIR-02 y DIR-03).** `calleNumero` de al menos 8 caracteres y cifrada a nivel de campo; `colonia` debe existir en el catálogo (spec 017); `referencias` opcional de hasta 200 caracteres y cifrada; `principal` booleano. En la edición cambian solo los campos enviados.
- **R-11:** **Dirección principal.** Cada persona tiene como máximo una principal: la primera dirección queda principal y marcar otra como principal desmarca la anterior. Al eliminar la principal, pasa a serlo la más reciente que quede.
- **R-12:** **Límite por persona.** Cada persona puede guardar hasta 10 direcciones (límite de cordura contra abuso de almacenamiento); la undécima responde 409 `address_limit_reached`.
- **R-13:** **Privacidad de la dirección.** La dirección exacta solo la ve su dueña; el técnico la recibe únicamente al ser elegido (specs 021 y 022); no se escribe en registros, no se cachea y las respuestas llevan `Cache-Control: private, no-store`.
- **R-14:** **Propiedad (DIR-03 y DIR-04).** Una dirección ajena responde 404, sin revelar que existe; borrar la dirección usada en una cita ya confirmada no cambia la cita (la cita guarda su propia copia).
- **R-15:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-16:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-17:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-18:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/me/direcciones` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/me/direcciones`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/me/direcciones` con datos válidos, entonces responde 201 con lo descrito en §5.
- **CA-04:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/me/direcciones`, entonces responde 401 y no ejecuta la operación.
- **CA-05:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/me/direcciones`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-06:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/me/direcciones`, entonces responde 400 y no guarda nada.
- **CA-07:** Dado el rol «persona con sesión que sea dueña del recurso», cuando llama `PUT /api/v1/me/direcciones/{id}` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-08:** Dada una petición sin token o con token vencido, cuando llama `PUT /api/v1/me/direcciones/{id}`, entonces responde 401 y no ejecuta la operación.
- **CA-09:** Dada una persona que no cumple el rol «persona con sesión que sea dueña del recurso», cuando llama `PUT /api/v1/me/direcciones/{id}`, entonces responde 403 y no cambia nada.
- **CA-10:** Dado un valor inválido según las reglas de §6, cuando llama `PUT /api/v1/me/direcciones/{id}`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-11:** Dado un cuerpo que no se puede leer, cuando llama `PUT /api/v1/me/direcciones/{id}`, entonces responde 400 y no guarda nada.
- **CA-12:** Dado el caso «recurso no disponible», cuando llama `PUT /api/v1/me/direcciones/{id}`, entonces responde 404 con ProblemDetails y no cambia el estado.
- **CA-13:** Dado el rol «persona con sesión que sea dueña del recurso», cuando llama `DELETE /api/v1/me/direcciones/{id}` con datos válidos, entonces responde 204 con lo descrito en §5.
- **CA-14:** Dada una petición sin token o con token vencido, cuando llama `DELETE /api/v1/me/direcciones/{id}`, entonces responde 401 y no ejecuta la operación.
- **CA-15:** Dada una persona que no cumple el rol «persona con sesión que sea dueña del recurso», cuando llama `DELETE /api/v1/me/direcciones/{id}`, entonces responde 403 y no cambia nada.
- **CA-16:** Dado un valor inválido según las reglas de §6, cuando llama `DELETE /api/v1/me/direcciones/{id}`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-17:** Dado el caso «recurso no disponible», cuando llama `DELETE /api/v1/me/direcciones/{id}`, entonces responde 404 con ProblemDetails y no cambia el estado.
- **CA-18:** Dada una dirección con calle de menos de 8 caracteres, una colonia inexistente o referencias de más de 200, cuando se guarda, entonces responde 422 y no se guarda.
- **CA-19:** Dada una persona con dos direcciones, cuando marca la segunda como principal, entonces la primera deja de serlo; al eliminar la principal, la otra pasa a serlo.
- **CA-20:** Dada una persona con 10 direcciones, cuando guarda otra, entonces responde 409 `address_limit_reached` y no la guarda.
- **CA-21:** Dada una dirección guardada, cuando se inspeccionan registros, caché y respuestas, entonces no aparece en registros ni en caché y la respuesta no es cacheable.
- **CA-22:** Dada la dirección de otra persona, cuando se intenta editar o borrar, entonces responde 404; y borrar una dirección usada en una cita confirmada no altera esa cita.
- **CA-23:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-24:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-25:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-26:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec040Tests.Criterio01_CA_040_01` |
| CA-02 | `Spec040Tests.Criterio02_CA_040_02` |
| CA-03 | `Spec040Tests.Criterio03_CA_040_03` |
| CA-04 | `Spec040Tests.Criterio04_CA_040_04` |
| CA-05 | `Spec040Tests.Criterio05_CA_040_05` |
| CA-06 | `Spec040Tests.Criterio06_CA_040_06` |
| CA-07 | `Spec040Tests.Criterio07_CA_040_07` |
| CA-08 | `Spec040Tests.Criterio08_CA_040_08` |
| CA-09 | `Spec040Tests.Criterio09_CA_040_09` |
| CA-10 | `Spec040Tests.Criterio10_CA_040_10` |
| CA-11 | `Spec040Tests.Criterio11_CA_040_11` |
| CA-12 | `Spec040Tests.Criterio12_CA_040_12` |
| CA-13 | `Spec040Tests.Criterio13_CA_040_13` |
| CA-14 | `Spec040Tests.Criterio14_CA_040_14` |
| CA-15 | `Spec040Tests.Criterio15_CA_040_15` |
| CA-16 | `Spec040Tests.Criterio16_CA_040_16` |
| CA-17 | `Spec040Tests.Criterio17_CA_040_17` |
| CA-18 | `Spec040Tests.Criterio18_CA_040_18` |
| CA-19 | `Spec040Tests.Criterio19_CA_040_19` |
| CA-20 | `Spec040Tests.Criterio20_CA_040_20` |
| CA-21 | `Spec040Tests.Criterio21_CA_040_21` |
| CA-22 | `Spec040Tests.Criterio22_CA_040_22` |
| CA-23 | `Spec040Tests.Criterio23_CA_040_23` |
| CA-24 | `Spec040Tests.Criterio24_CA_040_24` |
| CA-25 | `Spec040Tests.Criterio25_CA_040_25` |
| CA-26 | `Spec040Tests.Criterio26_CA_040_26` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| DIR-01 | `GET /api/v1/me/direcciones` | persona con sesión (cliente o técnico) | Ninguno | 200; 401 |
| DIR-02 | `POST /api/v1/me/direcciones` | persona con sesión (cliente o técnico) | Cuerpo: calleNumero, colonia, referencias, principal | 201; 400; 401; 422 |
| DIR-03 | `PUT /api/v1/me/direcciones/{id}` | persona con sesión que sea dueña del recurso | Ruta: id. Cuerpo: campos a cambiar | 200; 400; 401; 403; 404 |
| DIR-04 | `DELETE /api/v1/me/direcciones/{id}` | persona con sesión que sea dueña del recurso | Ruta: id | 204; 401; 403; 404 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| DIR-01 | No aplica | No (pocas) | No |
| DIR-02 | No aplica | No | No |
| DIR-03 | No aplica | No | No |
| DIR-04 | No aplica | No | No |

**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `direcciones`.** Guarda las direcciones donde el cliente pide servicios a domicilio. La dirección exacta es un dato sensible: se cifra a nivel de campo y solo se entrega al técnico elegido, después de que el cliente acepta su cotización. Mientras el técnico cotiza solo ve la colonia

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| usuarioId | ObjectId | Sí | Debe existir en usuarios | Sí |
| calleNumero | Texto cifrado | Sí | Mínimo 8 caracteres. Cifrado a nivel de campo | No |
| colonia | Texto | Sí | Debe existir en colonias | No |
| referencias | Texto cifrado | No | Máximo 200 caracteres. Cifrado | No |
| principal | Booleano | Sí | Solo una principal por persona | No |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Normal | usuarioId | Listar las direcciones de una persona |

## 7. Arquitectura y componentes
- Proyecto `Knock.Identity.Api`, módulo `Addresses` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 017 de este repositorio.
- Spec 021 de este repositorio.
- Spec 022 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 030 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Llaves de cifrado de campo en Key Vault (spec 025) y la copia de la dirección en la visita de la spec 022 (hoy no implementadas)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-09, CA-15, CA-23 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-09, CA-15, CA-23 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-05 a CA-06, CA-10 a CA-11, CA-16 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-04, CA-08, CA-14 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-25 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-24 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-26 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_040_NN` en el nombre (para `python scripts/sdd_lint.py --trace 040`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02, CA-04 a CA-06, CA-08 a CA-11, CA-14 a CA-16, CA-23, CA-25) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Primera dirección:** el cliente pide su primer servicio → captura calle, colonia y referencias → queda como principal.
- **Cambiar la principal:** el cliente abre sus direcciones → marca otra como principal → la anterior deja de serlo.
- **Límite:** con 5 direcciones, la app le pide borrar una antes de agregar otra.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 040` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** Máximo 10 direcciones por persona: el Excel no lo fija; se pone un tope de cordura contra abuso, sin sistema de configuración.
- **D-05:** La cita confirmada conserva su propia copia de la dirección (`lugar` de la spec 022), por eso borrar la original no la afecta.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Límite de direcciones: antes 5 sin justificación; ahora 10 como tope de cordura, porque una persona puede tener casa, trabajo, familiares y otros domicilios.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencias: catálogo de colonias de la spec 017 y la copia de la dirección en la visita de la spec 022.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
