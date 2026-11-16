# Spec 010 — Caché en tres niveles y paginación

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`

## 1. Objetivo
Definir cómo el backend responde rápido: caché en la capa de datos y en las respuestas, paginación por cursor y las demás técnicas de rendimiento que se establecieron como requerimientos.

## 2. Alcance
**Dentro**
- Caché en la capa de acceso a datos con Redis (cache-aside) e invalidación al escribir.
- Caché de respuestas con `ETag` y respuesta 304.
- Qué se cachea, qué no y con qué vigencia inicial.
- Paginación por cursor, proyecciones y compresión.
- Serializadores de JSON generados en compilación.

**Fuera**
- Caché de la app móvil (frontend).
- Pruebas de carga completas.

## 3. Requisitos
- **R-01:** Toda lectura pasa por un repositorio con caché; solo va a MongoDB si no está en Redis.
- **R-02:** Toda escritura borra de la caché las claves que afecta antes de responder.
- **R-03:** Las respuestas de lectura llevan `ETag` débil y el servidor contesta 304 si no cambió.
- **R-04:** Los catálogos se cachean unas 24 horas, los perfiles un minuto y las búsquedas unos segundos; son valores iniciales que se ajustan con pruebas de carga.
- **R-05:** No se cachean las listas personales que cambian al crear algo (solicitudes, cotizaciones y mensajes).
- **R-06:** Las respuestas personales llevan `Cache-Control: private, no-cache`.
- **R-07:** Todo listado se pagina por cursor, 20 por página y máximo 50.
- **R-08:** Las consultas traen solo los campos necesarios.
- **R-09:** Las respuestas JSON de más de 1 KB se comprimen, salvo las de autenticación y las que llevan tokens.

## 4. Criterios de aceptación
- **CA-01:** Dada una lectura repetida, cuando el dato no cambió, entonces la segunda no consulta MongoDB.
- **CA-02:** Dada una escritura, cuando termina, entonces la siguiente lectura devuelve el dato nuevo.
- **CA-03:** Dado un `If-None-Match` vigente, cuando se pide, entonces responde 304 sin cuerpo.
- **CA-04:** Dada una respuesta de autenticación, cuando se inspecciona, entonces no está comprimida.
- **CA-05:** Dado un listado de más de 50 elementos, cuando se pide, entonces devuelve a lo más 50 y un cursor.

## 5. Contrato
Fija el uso de `ETag`/`If-None-Match` y 304, el encabezado `Cache-Control` por tipo de respuesta y los parámetros `cursor` y `limit` de los listados.

## 6. Amenazas y controles
| Amenaza | Control |
|---|---|
| Un usuario ve datos de otro por una clave de caché compartida | Claves con el identificador de la persona en lo personal y `Cache-Control: private` |
| Datos viejos tras una escritura | Invalidación obligatoria al escribir, con prueba que la comprueba |
| Un secreto en la respuesta comprimida se infiere (BREACH) | No se comprimen las respuestas de autenticación ni las que llevan tokens |

## 7. Plan de pruebas
| Tipo | ¿Aplica? | Motivo |
|---|---|---|
| Unitarias | Sí | Claves de caché y vigencias |
| Integración | Sí | Redis y MongoDB reales: acierto, fallo e invalidación |
| Contrato | Sí | `ETag`, 304 y paginación en el OpenAPI |
| Rendimiento | Sí | Tiempos con y sin caché bajo carga |
| Seguridad negativa | Sí | Aislamiento de caché entre personas |
| Arquitectura | No | Se cubre en la spec 003 |

## 8. Decisiones
- **Caché en la capa de datos y no en la memoria de cada copia:** con dos copias, una caché en memoria daría respuestas distintas.
- **Paginación por cursor y no por número de página:** contar desde el principio se vuelve más lento mientras más profundo se pagina.
- **No cachear lo personal que cambia al crear algo:** una lista cacheada mostraría datos viejos justo después de crear.

## 9. Decisiones de la revisión, riesgos y dudas
Decisiones tomadas en la revisión del 2026-11-04. Quien aprueba la spec las acepta; si alguna no convence, se cambia aquí antes de aprobar.

- **`Idempotency-Key` (decidido): no se implementa en esta etapa.** Ningún endpoint de las specs 015 a 018 lo necesita; se define en la spec del primer endpoint que cree recursos con riesgo de duplicado (por ejemplo, crear una solicitud). El frontend deja de consumirlo (spec 010 del frontend).
- **Riesgo:** las vigencias iniciales de la caché pueden no ser las óptimas; se ajustan con las pruebas de carga.
- **Excepción contractual de paginación (T-05 cerrada por alcance, aprobada por el líder):** `GET /api/v1/oficios` conserva su respuesta como arreglo sin paginación, conforme a la decisión D9 (catálogo acotado; excepción documentada a la constitución §11 en `docs/endpoints.md` y en el OpenAPI), y `GET /api/v1/me/sesiones` conserva su listado acotado al máximo de 10 sesiones por cuenta (spec 018, R-02). Esta excepción no introduce cambios incompatibles en v1. R-07 y §5 siguen vigentes para los listados no acotados: todo listado futuro que no esté acotado por su propia spec deberá aplicar `CursorPage<T>` con límite predeterminado 20 y máximo 50, conforme a su especificación. Por lo tanto, **CA-05 se declara no aplicable a los endpoints actuales**: ninguno de los listados v1 vigentes puede superar 50 elementos (el catálogo acotado y el máximo de 10 sesiones), y no se implementó paginación por cursor en los endpoints existentes.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
