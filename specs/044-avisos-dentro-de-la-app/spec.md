# Spec 044 — Avisos dentro de la app y preferencias de avisos

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints AVI-01, AVI-02, AVI-03, AVI-04, PRE-01.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Mensajería (`Knock.Messaging.Api`) · **Módulo:** `Notices` · **Colecciones:** `avisos`, `preferencias_avisos`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 034)

## 1. Objetivo
Definir y especificar los endpoints AVI-01, AVI-02, AVI-03, AVI-04, PRE-01 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como persona, quiero ver mis avisos dentro de la app y marcarlos como leídos, para enterarme de mis solicitudes sin depender solo del teléfono.

## 2. Historia y alcance
**2.1 Historia**
- Como persona, quiero ver mis avisos dentro de la app y marcarlos como leídos, para enterarme de mis solicitudes sin depender solo del teléfono.

**2.2 Dentro**
- `GET /api/v1/avisos` (AVI-01): Lista los avisos de la persona.
- `GET /api/v1/avisos/no-leidos/contador` (AVI-02): Cantidad de avisos sin leer, para el puntito de la campana.
- `POST /api/v1/avisos/{id}/leido` (AVI-03): Marca un aviso como leído.
- `POST /api/v1/avisos/leidos` (AVI-04): Marca todos como leídos.
- `GET /api/v1/me/preferencias-avisos` (PRE-01): Devuelve las preferencias.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 034.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `GET /api/v1/avisos` (leer todos / filtro): Lista los avisos de la persona. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-02:** Entrada de `GET /api/v1/avisos`: Query: cursor, limite, leido, comoRol. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo. Reglas: `comoRol`: Solo esos valores; `leido`: Por defecto falso.
- **R-03:** `GET /api/v1/avisos` pagina por cursor (Por cursor, 20 (máx. 50)): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Filtro por leído y por rol; orden por fecha descendente.
- **R-04:** `GET /api/v1/avisos/no-leidos/contador` (leer uno): Cantidad de avisos sin leer, para el puntito de la campana. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-05:** `POST /api/v1/avisos/{id}/leido` (actualizar (acción)): Marca un aviso como leído. Lo puede llamar: persona con sesión que sea dueña del recurso. Responde 204 cuando sale bien.
- **R-06:** Entrada de `POST /api/v1/avisos/{id}/leido`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-07:** Estados de `POST /api/v1/avisos/{id}/leido`: 404 `notice_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-08:** `POST /api/v1/avisos/leidos` (actualizar (acción)): Marca todos como leídos. Lo puede llamar: persona con sesión (cliente o técnico). Responde 204 cuando sale bien.
- **R-09:** `GET /api/v1/me/preferencias-avisos` (leer uno): Devuelve las preferencias. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-10:** **Lista de avisos (AVI-01).** Solo los de la persona, por cursor (20 por defecto, máximo 50), del más reciente al más antiguo; filtros `leido` (booleano) y `comoRol` (`cliente` o `tecnico`); un valor fuera de la lista responde 422. Cada aviso trae `tipo`, `titulo`, `texto`, `destino`, `leido` y `creadoEn`, sin códigos de visita, direcciones ni datos sensibles.
- **R-11:** **Contador (AVI-02).** Devuelve la cantidad de avisos sin leer de la persona (máximo mostrado 99, con `99+` en la app); no se cachea porque cambia con cada aviso; la consulta usa el índice de `usuarioId` y `leido`.
- **R-12:** **Marcar leídos (AVI-03 y AVI-04).** Marcar uno que ya está leído responde 204 igual (idempotente); marcar todos solo afecta a la persona; un aviso ajeno responde 404 `notice_not_found`, sin revelar que existe.
- **R-13:** **Preferencias (PRE-01).** Devuelve un interruptor por tipo de aviso; si la persona nunca cambió nada, todos están activos salvo `promociones`; los avisos de seguridad siempre están activos y no se pueden apagar. El cambio de preferencias (PUT) lo define la spec 037.
- **R-14:** **Conservación.** Los avisos se conservan 30 días y luego los borra el índice de vencimiento (`venceEn`); el texto no incluye información sensible y solo se crea desde el despachador de la spec 037.
- **R-15:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-16:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-17:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-18:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/avisos` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/avisos`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/avisos`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-04:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-05:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/avisos/no-leidos/contador` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-06:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/avisos/no-leidos/contador`, entonces responde 401 y no ejecuta la operación.
- **CA-07:** Dado el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/avisos/{id}/leido` con datos válidos, entonces responde 204 con lo descrito en §5.
- **CA-08:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/avisos/{id}/leido`, entonces responde 401 y no ejecuta la operación.
- **CA-09:** Dada una persona que no cumple el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/avisos/{id}/leido`, entonces responde 403 y no cambia nada.
- **CA-10:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/avisos/{id}/leido`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-11:** Dado el caso «el aviso no existe o no es de la persona», cuando llama `POST /api/v1/avisos/{id}/leido`, entonces responde 404 `notice_not_found` con ProblemDetails y no cambia el estado.
- **CA-12:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/avisos/leidos` con datos válidos, entonces responde 204 con lo descrito en §5.
- **CA-13:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/avisos/leidos`, entonces responde 401 y no ejecuta la operación.
- **CA-14:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/me/preferencias-avisos` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-15:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/me/preferencias-avisos`, entonces responde 401 y no ejecuta la operación.
- **CA-16:** Dada una persona con avisos como cliente y como técnico, cuando filtra por `comoRol`, entonces ve solo los de ese rol, paginados y del más reciente al más antiguo; un `comoRol` inválido responde 422.
- **CA-17:** Dada una persona con avisos sin leer, cuando consulta el contador, entonces recibe la cantidad exacta hasta 99 y baja al leer uno.
- **CA-18:** Dado el aviso de otra persona, cuando se intenta marcar como leído, entonces responde 404; y marcar todos no cambia los avisos de otras personas.
- **CA-19:** Dada una persona que nunca cambió preferencias, cuando las consulta, entonces recibe todos los tipos activos salvo promociones y los de seguridad marcados como no apagables.
- **CA-20:** Dado un aviso con más de 30 días, cuando corre el índice de vencimiento, entonces se borra; y ninguna ruta de esta spec crea avisos.
- **CA-21:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-22:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-23:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-24:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec044Tests.Criterio01_CA_044_01` |
| CA-02 | `Spec044Tests.Criterio02_CA_044_02` |
| CA-03 | `Spec044Tests.Criterio03_CA_044_03` |
| CA-04 | `Spec044Tests.Criterio04_CA_044_04` |
| CA-05 | `Spec044Tests.Criterio05_CA_044_05` |
| CA-06 | `Spec044Tests.Criterio06_CA_044_06` |
| CA-07 | `Spec044Tests.Criterio07_CA_044_07` |
| CA-08 | `Spec044Tests.Criterio08_CA_044_08` |
| CA-09 | `Spec044Tests.Criterio09_CA_044_09` |
| CA-10 | `Spec044Tests.Criterio10_CA_044_10` |
| CA-11 | `Spec044Tests.Criterio11_CA_044_11` |
| CA-12 | `Spec044Tests.Criterio12_CA_044_12` |
| CA-13 | `Spec044Tests.Criterio13_CA_044_13` |
| CA-14 | `Spec044Tests.Criterio14_CA_044_14` |
| CA-15 | `Spec044Tests.Criterio15_CA_044_15` |
| CA-16 | `Spec044Tests.Criterio16_CA_044_16` |
| CA-17 | `Spec044Tests.Criterio17_CA_044_17` |
| CA-18 | `Spec044Tests.Criterio18_CA_044_18` |
| CA-19 | `Spec044Tests.Criterio19_CA_044_19` |
| CA-20 | `Spec044Tests.Criterio20_CA_044_20` |
| CA-21 | `Spec044Tests.Criterio21_CA_044_21` |
| CA-22 | `Spec044Tests.Criterio22_CA_044_22` |
| CA-23 | `Spec044Tests.Criterio23_CA_044_23` |
| CA-24 | `Spec044Tests.Criterio24_CA_044_24` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| AVI-01 | `GET /api/v1/avisos` | persona con sesión (cliente o técnico) | Query: cursor, limite, leido, comoRol | 200; 401 |
| AVI-02 | `GET /api/v1/avisos/no-leidos/contador` | persona con sesión (cliente o técnico) | Ninguno | 200; 401 |
| AVI-03 | `POST /api/v1/avisos/{id}/leido` | persona con sesión que sea dueña del recurso | Ruta: id | 204; 401; 403; 404 |
| AVI-04 | `POST /api/v1/avisos/leidos` | persona con sesión (cliente o técnico) | Ninguno | 204; 401 |
| PRE-01 | `GET /api/v1/me/preferencias-avisos` | persona con sesión (cliente o técnico) | Ninguno | 200; 401 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| AVI-01 | Filtro por leído y por rol; orden por fecha descendente | Por cursor, 20 (máx. 50) | No |
| AVI-02 | No aplica | No | No |
| AVI-03 | No aplica | No | No |
| AVI-04 | No aplica | No | No |
| PRE-01 | No aplica | No | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| AVI-03 | 404 | `notice_not_found` | el aviso no existe o no es de la persona |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `avisos`.** Son los avisos que la persona ve en la pantalla Avisos: cotizaciones nuevas, visitas, solicitudes aceptadas, opiniones y resultados de verificación. Si es técnico, se ven juntos con los encabezados Como cliente y Como técnico. Cada aviso importante además sale como notificación al teléfono

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| usuarioId | ObjectId | Sí | Debe existir en usuarios | Compuesto |
| comoRol | Texto | Sí | Solo esos valores | No |
| tipo | Texto | Sí | Lista cerrada | No |
| titulo | Texto | Sí | Máximo 80 caracteres | No |
| texto | Texto | Sí | Máximo 200 caracteres. Sin datos sensibles | No |
| destino | Documento | No |  | No |
| leido | Booleano | Sí | Por defecto falso | Compuesto |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | Compuesto |
| venceEn | Fecha y hora UTC | Sí | La base lo borra al vencer | TTL |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Compuesto | usuarioId + leido + creadoEn (descendente) | Lista de avisos y contador de no leídos |
| TTL | venceEn | Que la base borre sola los avisos viejos |

**Colección `preferencias_avisos`.** Guarda qué tipos de aviso quiere recibir cada persona, tanto dentro de la app como en el teléfono. Se cambia en Configuración, en Qué avisos recibir

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| usuarioId | ObjectId | Sí | Un solo documento por persona | Único |
| tipos | Documento | Sí | Los avisos de seguridad siempre llegan | No |
| actualizadaEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Único | usuarioId | Un documento por persona y búsqueda directa |

## 7. Arquitectura y componentes
- Proyecto `Knock.Messaging.Api`, módulo `Notices` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 026 de este repositorio.
- Spec 037 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 034 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Despachador de avisos de la spec 037, único que crea avisos (hoy no implementado)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-09, CA-21 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-09, CA-21 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-03, CA-10 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-06, CA-08, CA-13, CA-15 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-23 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-22 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-24 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_044_NN` en el nombre (para `python scripts/sdd_lint.py --trace 044`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-03, CA-06, CA-08 a CA-10, CA-13, CA-15, CA-21, CA-23) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Campana:** la persona recibe una cotización → la campana muestra 1 → abre la lista, toca el aviso y llega a las cotizaciones.
- **Marcar todo:** la persona acumula avisos → toca «Marcar todos como leídos» → el contador queda en 0.
- **Filtrar por rol:** una persona que es cliente y técnico ve solo los avisos del rol que eligió.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 044` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** Conservación de 30 días para los avisos: los avisos de la cadena de solicitudes pierden vigencia en horas; 30 días cubre el historial sin acumular datos. Es el valor del índice de vencimiento, no una configuración.
- **D-05:** El contador muestra hasta 99 y la app muestra «99+» después.
- **D-06:** Los avisos de seguridad no se pueden apagar y las promociones nacen apagadas, como pide el Excel.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Retención: antes quedaba como duda; ahora se fija en 30 días con su justificación.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencia: el despachador de avisos de la spec 037 es el único que crea avisos.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
