# Spec 036 — Resumen de trabajo del técnico (`GET /api/v1/tecnicos/me/trabajo`)

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** HU-020 (backlog, «Back (API): `GET /api/v1/tecnicos/me/trabajo` (agregado por secciones)»); fija el hueco de backend 028 §15 y de frontend 019 §13. **Clave de Jira:** KNOCK-67.
> **Servicio:** Mercado (técnicos, solicitudes, cotizaciones y visitas), con la verificación y el nivel leídos de `tecnicos` · **Módulo:** Técnicos · **Colecciones:** ninguna nueva (lee `tecnicos`, `cotizaciones`, `visitas`, `solicitudes`)
> **Sprint:** 5 (propuesto) · **Responsable:** Guillermo (módulo de Mercado)
> **Consumidora:** frontend 019 «Pestaña Trabajo» (R-02, R-07) y frontend 025 (etiqueta por `motivoRechazo`)

## 1. Objetivo

Que la pestaña Trabajo del técnico reciba en una sola respuesta sus visitas próximas, sus cotizaciones enviadas con su estado, sus trabajos terminados (paginados) y su nivel. La bandeja de solicitudes nuevas y el descarte no son de esta spec: los fija la spec 028. Tampoco se escribe aquí el interruptor «Disponible hoy» (HU-019, sin spec): esta spec solo lo lee.

## 2. Historias y alcance

**2.1 Historias**
- **HU-020 (agregado por secciones)** — Como técnico, quiero ver en Trabajo mis visitas, mis cotizaciones enviadas y mis trabajos terminados, para saber qué tengo que hacer. *Terminada cuando:* la respuesta cumple R-02 a R-06 y las pruebas de CA-01 a CA-09 pasan.

**2.2 Dentro**
- El `GET` agregado, sus cuatro secciones, la paginación de «Terminadas» y las reglas de privacidad de cada sección.
- El `motivoRechazo` de cada cotización enviada (valores que define la spec 021).

**2.3 Fuera**
- La bandeja de solicitudes nuevas y el descarte (spec 028).
- Escribir `disponibleHoy` (HU-019, sin spec).
- La calificación de los trabajos: el campo existe y va en `null` cuando no hay reseñas (decisión de revisión, confirmar). Cuando exista una spec de reseñas, ella define el promedio.
- La vista de la visita (spec 022) y la confirmación de llegada.

## 3. Requisitos

- **R-01:** Exige JWT. Si la persona no es técnico activo, responde `403 not_a_technician`. El técnico se resuelve por el `sub` del token en `tecnicos`, como en la spec 020 R-11.
- **R-02:** La respuesta tiene cuatro secciones, cada una de una consulta propia:
  - `proximasVisitas`: visitas del técnico en estado `confirmada` o `en_curso`, ordenadas por `dia` y `hora` ascendentes, con un máximo de 20.
  - `cotizacionesEnviadas`: las 20 cotizaciones más recientes del técnico, de cualquier estado, ordenadas por `enviadaEn` descendente.
  - `terminadas`: visitas `terminada` del técnico, ordenadas por `terminadaEn` descendente y `_id` descendente como desempate, con paginación por cursor (`limit` de 1 a 20, 20 por omisión; `nextCursor` es `null` en la última página).
  - `resumen`: `trabajosTerminados` (conteo) y `calificacion` (`null` cuando no hay reseñas, §2.3).
- **R-03:** `proximasVisitas[].lugar.direccion` aparece solo si la visita está `confirmada` o `en_curso` (la dirección se revela cuando el técnico ya fue elegido, spec 022 R-05). `cliente` lleva solo `colonia`. Ninguna sección lleva nombre, teléfono ni `sub` del cliente.
- **R-04:** Cada cotización enviada lleva `motivoRechazo` cuando su estado es `rechazada` o `retirada`, con los valores que define la spec 021 (*decisión de revisión, confirmar*: esta spec no inventa valores nuevos; si la 021 no los define, se añaden allí).
- **R-05:** `disponibleHoy` se lee del campo de `tecnicos` y, si no existe, vale `false`. Esta spec no lo escribe.
- **R-06:** `nivelVerificacion` de `tecnicos` (spec 021 §6) es la fuente de verdad de `verificacion.estado`: `ninguno` se convierte en `sin_verificar`, y `basica` o `completa` en `verificada`. Si falta o tiene un valor no reconocido, se usa `sin_verificar`. `nivel` de reputación se devuelve vacío o nulo hasta que la spec 051 lo defina, incluso si hay datos opcionales no reconocidos. Los errores reales de comunicación o consulta a la base no son datos faltantes: se mapean a `unavailable` según R-07. Esta spec no escribe estos datos.
- **R-07:** Si cualquier parte del resumen falla (la base no responde o falla una de las cuatro consultas), la respuesta entera es `503 unavailable`. Nunca se devuelve una respuesta parcial (*decisión de revisión, confirmar*: la app trata la pestaña como una sola carga; una falla deja toda la pestaña en error, y así el contrato queda simple).
- **R-08:** La respuesta lleva `Cache-Control: private, no-store` (spec 010, R-04): contiene datos personales y cambia con cada solicitud.
- **R-09:** Ninguna sección lleva el código de la visita, el nombre ni el teléfono del cliente, ni la dirección fuera de R-03.
- **R-10:** El límite de lectura del gateway (spec 009) aplica; con exceso, `429` con `Retry-After`.

## 4. Contrato API

| Método y ruta | Quién | Parámetros | Respuesta | Errores |
|---|---|---|---|---|
| `GET /api/v1/tecnicos/me/trabajo` | técnico activo | `cursorTerminadas?`, `limit?` (1–20) | `200` (ver forma abajo) | `400` (`invalid_cursor`), `401`, `403 not_a_technician`, `422` (`limit` fuera de rango), `429`, `503 unavailable` |

**Forma de la respuesta**
- `disponibleHoy: boolean`, `verificacion: { estado }`, `nivel: { clave, nombre } | null` (`null` hasta que lo defina la spec 051)
- `proximasVisitas[]`: `{ visitaId, solicitudId, dia, hora, modalidad, cliente: { colonia }, lugar: { tipo, direccion? }, precio, tope, estado, puedeLlegar }` (mismos nombres que la vista del técnico de la spec 022)
- `cotizacionesEnviadas[]`: `{ cotizacionId, solicitudId, oficioNombre, tipoPrecio, precio, precioMin?, precioMax?, tope, estado, motivoRechazo?, enviadaEn }`
- `terminadas`: `{ items: [{ solicitudId, oficioNombre, terminadaEn, precio, calificacion }], nextCursor }`
- `resumen`: `{ trabajosTerminados, calificacion }` (`calificacion: null` cuando no hay reseñas)

## 5. Seguridad

| Amenaza | Control | Prueba |
|---|---|---|
| Un cliente lee el resumen de trabajo de un técnico | `403 not_a_technician` por rol y `sub` | CA-02 |
| Fuga de datos de contacto o de la dirección antes de elegir | Forma fija por sección y R-03 | CA-01, CA-06 |
| Caché compartida de datos personales | `Cache-Control: private, no-store` | CA-05 |
| Cursor manipulado | `invalid_cursor` (400) | Integración |

## 6. Criterios de aceptación

- **CA-01:** Dado un técnico con una visita `confirmada` y otra `terminada`, cuando pide su resumen, entonces la primera está en `proximasVisitas` con dirección y la segunda en `terminadas`. (R-02, R-03)
- **CA-02:** Dada una persona que no es técnico activo (cliente o técnico dado de baja), cuando pide el resumen, entonces responde `403 not_a_technician`. (R-01)
- **CA-03:** Dado un técnico con 25 trabajos terminados, cuando pide la lista, entonces recibe 20 y `nextCursor`, y con el cursor recibe los 5 restantes sin repetir. (R-02)
- **CA-04:** Dada una cotización rechazada porque el cliente eligió a otro técnico, cuando el técnico pide su resumen, entonces esa cotización lleva `motivoRechazo`. (R-04)
- **CA-05:** Dada la base sin responder o una de las cuatro consultas fallando, cuando se pide el resumen, entonces responde `503 unavailable` sin secciones parciales y con `Cache-Control: private, no-store`. (R-07, R-08)
- **CA-06:** Dada cualquier respuesta, cuando se revisa, entonces no contiene nombre, teléfono ni `sub` del cliente, ni la dirección de una visita no aceptada. (R-03, R-09)
- **CA-07:** Dado un técnico sin el campo `disponibleHoy`, cuando pide el resumen, entonces `disponibleHoy` es `false` y el resumen no escribe ese campo. (R-05)
- **CA-08:** Dado un técnico, cuando pide el resumen, entonces `verificacion.estado` se deriva de `nivelVerificacion` y `nivel` queda vacío o nulo según R-06. (R-06)
- **CA-09:** Dado un técnico que excede el límite de lectura del gateway, cuando pide el resumen, entonces responde `429` con `Retry-After`. (R-10)

## 7. Pruebas

| Tipo | ¿Aplica? | Qué cubre |
|---|---|---|
| Unitarias | Sí | Reglas de R-03 (dirección según estado), orden y desempate de `terminadas`, `motivoRechazo`, valor por omisión de `disponibleHoy` |
| Integración | Sí | MongoDB real con visitas, cotizaciones y técnicos; Redis real para el límite de R-10 |
| Seguridad negativa | Sí | Cliente, técnico inactivo, cursor manipulado, base caída |
| Contrato (CON) | Sí | `openapi-diff` contra `contracts/openapi.v1.json` y cliente de frontend 019 |
| Arquitectura | Sí | Mercado lee la verificación y el nivel solo de `tecnicos` (su propia base), nunca de la base de Identidad |
| Regresión | Sí | Suites de specs 020, 021 y 025 en verde; pestaña Trabajo (frontend 019) sin cambios de contrato |
| Mutación | No aplica | Lectura agregada sin cálculo de dinero ni de estado; las reglas de R-03 y R-04 tienen casos negativos en unitarias |
| Humo | Sí | En el entorno de evaluación: `GET /api/v1/tecnicos/me/trabajo` responde 401 sin token y 403 para una cuenta de cliente |
| E2E | Sí | Recorrido del técnico de semilla por la pestaña Trabajo (frontend 019, E2E) |
| Carga | No aplica | Volumen del piloto bajo; el límite de lectura se prueba en CA-09 |
| CI gates | Sí | Análisis, formato, pruebas, cobertura, escáneres (Semgrep, CodeQL, detección de secretos y OSV) y `oasdiff` |

## 8. Riesgos y decisiones

1. **Una sola respuesta, no tres llamadas.** La app pidió un agregado (frontend 019 D-01). Alternativa: cuatro rutas pequeñas, que multiplican el trabajo del cliente y la lógica de error. *Decisión de revisión, confirmar.*
2. **Falla completa, no parcial (R-07).** Si falla una parte, la respuesta entera es `503 unavailable`; la app muestra error en la pestaña. Alternativa: secciones con su propio estado, que exige respuestas parciales y un contrato más complejo. *Decisión de revisión, confirmar.*
3. **Calificación en `null` cuando no hay reseñas.** No hay spec de reseñas; mejor un campo vacío que inventar una fuente. *Decisión de revisión, confirmar.*
4. **Sin escritura de «Disponible hoy».** Lo define HU-019, que no tiene spec; esta spec solo lee el campo.
5. **Verificación desde `nivelVerificacion`.** `ninguno`, un campo ausente o un valor desconocido producen `sin_verificar`; `basica` y `completa` producen `verificada`.
6. **Reputación pendiente de la spec 051.** `nivel` es `null` hasta que esa spec defina su fuente y contrato; no se usan datos opcionales no reconocidos.
7. **Fallo de consulta distinto de datos ausentes.** Un error real de MongoDB produce `unavailable`, sin valores de respaldo; T-04 debe revisar `ErrorCode` antes de usar el resultado y responder 503 sin secciones parciales.

## Trazabilidad CA → prueba

Salvo CA-05 y CA-08, los nombres son propuestas pendientes, no evidencia de ejecución.

| CA | Prueba |
|---|---|
| CA-01 | `Spec036ResumenTests.Visitas_Y_Terminadas_Por_Estado_CA_036_01` |
| CA-02 | `Spec036ResumenTests.Cliente_O_Tecnico_Inactivo_CA_036_02` |
| CA-03 | `Spec036ResumenTests.Terminadas_Paginadas_CA_036_03` |
| CA-04 | `Spec036ResumenTests.Motivo_Rechazo_En_Cotizacion_CA_036_04` |
| CA-05 | `Spec036TrabajoStoreTests.Base_Caida_Unavailable_Sin_Valores_De_Respaldo_CA_036_05` (lectura; respuesta HTTP y cabecera pendientes en T-04) |
| CA-06 | `Spec036ResumenTests.Sin_Datos_De_Contacto_CA_036_06` |
| CA-07 | `Spec036ResumenTests.DisponibleHoy_Por_Omision_CA_036_07` |
| CA-08 | `Spec036TrabajoStoreTests.Verificacion_Y_Nivel_Desde_Tecnicos_CA_036_08` y `Campo_No_Texto_Y_Tecnico_Ausente_CA_036_08` (lectura; contrato HTTP pendiente en T-04) |
| CA-09 | `Spec036ResumenTests.Limite_De_Lectura_429_CA_036_09` |

## Aprobación

- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-11
- [x] Sin conflicto con la constitución
