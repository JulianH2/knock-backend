# Spec 042 — Técnicos de confianza

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints TCO-01, TCO-02, TCO-03.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Mercado (`Knock.Market.Api`) · **Módulo:** `Trust` · **Colecciones:** `tecnicos_confianza`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 032)

## 1. Objetivo
Definir y especificar los endpoints TCO-01, TCO-02, TCO-03 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como cliente, quiero guardar a los técnicos que me atendieron bien, para volver a llamarlos.

## 2. Historia y alcance
**2.1 Historia**
- Como cliente, quiero guardar a los técnicos que me atendieron bien, para volver a llamarlos.

**2.2 Dentro**
- `GET /api/v1/me/tecnicos-confianza` (TCO-01): Lista los técnicos guardados.
- `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}` (TCO-02): Guarda a un técnico de confianza (requiere cuenta).
- `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}` (TCO-03): Quita un técnico de la lista.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 032.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `GET /api/v1/me/tecnicos-confianza` (leer todos): Lista los técnicos guardados. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-02:** `GET /api/v1/me/tecnicos-confianza` pagina por cursor (Por cursor, 20): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Orden por fecha descendente.
- **R-03:** `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}` (crear): Guarda a un técnico de confianza (requiere cuenta). Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-04:** Entrada de `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}`: Ruta: tecnicoId. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `tecnicoId`: Debe existir en tecnicos.
- **R-05:** Estados de `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}`: 201; 404 `technician_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-06:** `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}` (borrar): Quita un técnico de la lista. Lo puede llamar: persona con sesión (cliente o técnico). Responde 204 cuando sale bien.
- **R-07:** Entrada de `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}`: Ruta: tecnicoId. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `tecnicoId`: Debe existir en tecnicos.
- **R-08:** Estados de `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}`: 404 `trusted_technician_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-09:** **Los contactos de confianza no se guardan en el servidor.** La persona elige a quién avisar con el selector del sistema o escribiendo el número (spec 022) y el servidor solo arma el texto: no existe colección de contactos, no se almacenan teléfonos de terceros y los endpoints CON-01 a CON-03 del Excel no se implementan.
- **R-10:** **Técnicos de confianza (TCO-01 a TCO-03).** Guardar a un técnico requiere cuenta y un técnico visible; `PUT` es idempotente: 201 la primera vez y 200 al repetir, sin duplicar. La lista se pagina por cursor (20 por defecto, máximo 50) con el más reciente primero. Quitar un técnico que no está en la lista responde 404.
- **R-11:** **Técnico no disponible.** Un técnico que se oculta o se bloquea sale de la lista de la persona al consultarla (no se borra el registro) y no se le pueden mandar solicitudes directas.
- **R-12:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-13:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-14:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-15:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/me/tecnicos-confianza` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/me/tecnicos-confianza`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-04:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-05:** Dada una petición sin token o con token vencido, cuando llama `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}`, entonces responde 401 y no ejecuta la operación.
- **CA-06:** Dado un valor inválido según las reglas de §6, cuando llama `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-07:** Dado el caso «recurso no disponible», cuando llama `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}`, entonces responde 201 con ProblemDetails y no cambia el estado.
- **CA-08:** Dado el caso «el técnico no existe o está oculto», cuando llama `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}`, entonces responde 404 `technician_not_found` con ProblemDetails y no cambia el estado.
- **CA-09:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}` con datos válidos, entonces responde 204 con lo descrito en §5.
- **CA-10:** Dada una petición sin token o con token vencido, cuando llama `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}`, entonces responde 401 y no ejecuta la operación.
- **CA-11:** Dado un valor inválido según las reglas de §6, cuando llama `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-12:** Dado el caso «ese técnico no está en la lista de la persona», cuando llama `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}`, entonces responde 404 `trusted_technician_not_found` con ProblemDetails y no cambia el estado.
- **CA-13:** Dado el aviso de confianza de la spec 022, cuando se inspeccionan las bases de datos, entonces no existe ninguna colección ni campo con números de contactos de terceros.
- **CA-14:** Dada una persona, cuando guarda al mismo técnico dos veces, entonces la primera responde 201 y la segunda 200 y queda un solo registro; y quitar uno que no tiene guardado responde 404.
- **CA-15:** Dado un técnico oculto, cuando su cliente consulta su lista, entonces ya no aparece; al volver a ser visible, reaparece.
- **CA-16:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-17:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-18:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-19:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec042Tests.Criterio01_CA_042_01` |
| CA-02 | `Spec042Tests.Criterio02_CA_042_02` |
| CA-03 | `Spec042Tests.Criterio03_CA_042_03` |
| CA-04 | `Spec042Tests.Criterio04_CA_042_04` |
| CA-05 | `Spec042Tests.Criterio05_CA_042_05` |
| CA-06 | `Spec042Tests.Criterio06_CA_042_06` |
| CA-07 | `Spec042Tests.Criterio07_CA_042_07` |
| CA-08 | `Spec042Tests.Criterio08_CA_042_08` |
| CA-09 | `Spec042Tests.Criterio09_CA_042_09` |
| CA-10 | `Spec042Tests.Criterio10_CA_042_10` |
| CA-11 | `Spec042Tests.Criterio11_CA_042_11` |
| CA-12 | `Spec042Tests.Criterio12_CA_042_12` |
| CA-13 | `Spec042Tests.Criterio13_CA_042_13` |
| CA-14 | `Spec042Tests.Criterio14_CA_042_14` |
| CA-15 | `Spec042Tests.Criterio15_CA_042_15` |
| CA-16 | `Spec042Tests.Criterio16_CA_042_16` |
| CA-17 | `Spec042Tests.Criterio17_CA_042_17` |
| CA-18 | `Spec042Tests.Criterio18_CA_042_18` |
| CA-19 | `Spec042Tests.Criterio19_CA_042_19` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| TCO-01 | `GET /api/v1/me/tecnicos-confianza` | persona con sesión (cliente o técnico) | Ninguno | 200; 401 |
| TCO-02 | `PUT /api/v1/me/tecnicos-confianza/{tecnicoId}` | persona con sesión (cliente o técnico) | Ruta: tecnicoId | 200; 201; 401; 404 |
| TCO-03 | `DELETE /api/v1/me/tecnicos-confianza/{tecnicoId}` | persona con sesión (cliente o técnico) | Ruta: tecnicoId | 204; 401; 404 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| TCO-01 | Orden por fecha descendente | Por cursor, 20 | No |
| TCO-02 | No aplica | No | No |
| TCO-03 | No aplica | No | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| TCO-02 | 404 | `technician_not_found` | el técnico no existe o está oculto |
| TCO-03 | 404 | `trusted_technician_not_found` | ese técnico no está en la lista de la persona |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `tecnicos_confianza`.** Lista de técnicos que el cliente guardó para volver a contratarlos con un toque (Volver a contratar). Es una relación simple de uno a muchos: no hay recorridos de varios saltos, por eso no se usa una base de grafos

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| clienteId | ObjectId | Sí | Debe existir en usuarios | Compuesto único |
| tecnicoId | ObjectId | Sí | Debe existir en tecnicos | Compuesto único |
| guardadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Único compuesto | clienteId + tecnicoId | No guardar dos veces y listar los guardados de un cliente |

## 7. Arquitectura y componentes
- Proyecto `Knock.Market.Api`, módulo `Trust` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 022 de este repositorio.
- Spec 037 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 032 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Almacén de técnicos y su visibilidad de las specs 027 y 028 (hoy no implementados)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-16 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-16 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-06, CA-11 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-05, CA-10 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-18 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-17 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-19 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_042_NN` en el nombre (para `python scripts/sdd_lint.py --trace 042`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02, CA-05 a CA-06, CA-10 a CA-11, CA-16, CA-18) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Guardar técnico:** al terminar una visita, el cliente toca «Guardar como de confianza» → el técnico aparece en su lista.
- **Privacidad:** la persona avisa a un familiar de su visita con el selector del sistema; Knock no guarda ese número.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 042` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** Se sigue la spec 022 (aprobada): los contactos los maneja la app con el selector del sistema. Se elimina CON-01 a CON-03 porque guardar teléfonos de terceros agrega riesgo de privacidad y mantenimiento sin dar más valor que el selector.
- **D-05:** Un técnico oculto o bloqueado sale de la lista al consultarla sin borrar el registro, para que reaparezca si vuelve a ser visible.
- **D-06:** Guardar a un técnico requiere cuenta y no tiene un máximo propio: la lista se pagina por cursor.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Contactos de confianza (CON-01 a CON-03): antes se guardaban en el servidor (Excel) con un máximo de 5; ahora no se implementan, porque la spec 022 ya aprobada los resuelve en la app.
- **C-02:** Máximo de contactos: se elimina junto con los contactos.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencia: «solicitud directa a un técnico de confianza» no entra en esta spec; la distribución de solicitudes es de la spec 028.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
