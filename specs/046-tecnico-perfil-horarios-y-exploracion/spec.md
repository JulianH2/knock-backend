# Spec 046 — Técnico: perfil, horarios, disponibilidad, mapa y explorar

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints TEC-01, TEC-02, TEC-03, TEC-04, TEC-05, TEC-06, TEC-07, TEC-08, TEC-09, TEC-10, TEC-11, TEC-13, TEC-14.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Mercado (`Knock.Market.Api`) · **Módulo:** `Technicians` · **Colecciones:** `tecnicos`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 036)

## 1. Objetivo
Definir y especificar los endpoints TEC-01, TEC-02, TEC-03, TEC-04, TEC-05, TEC-06, TEC-07, TEC-08, TEC-09, TEC-10, TEC-11, TEC-13, TEC-14 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como técnico, quiero crear mi perfil, definir mis horarios y mi disponibilidad, y como cliente quiero explorar técnicos en el mapa, para encontrar a la persona adecuada cerca de mí.

## 2. Historia y alcance
**2.1 Historia**
- Como técnico, quiero crear mi perfil, definir mis horarios y mi disponibilidad, y como cliente quiero explorar técnicos en el mapa, para encontrar a la persona adecuada cerca de mí.

**2.2 Dentro**
- `POST /api/v1/tecnicos` (TEC-01): Registra a la persona como técnico (nombre y apellido, oficios, taller o domicilio, radio, perfil opcional y términos).
- `GET /api/v1/tecnicos` (TEC-02): Busca técnicos por filtros: oficio, colonia, distancia o radio, Disponible hoy y solo verificados (el texto libre es `/busqueda`, spec 027).
- `GET /api/v1/tecnicos/autocompletar` (TEC-03): Sugerencias de oficios, servicios y negocios mientras se escribe (por ejemplo, boiler, frenos).
- `GET /api/v1/mapa/tecnicos` (TEC-04): Técnicos de la zona visible del mapa, ya agrupados, para no mandarlos todos.
- `GET /api/v1/tecnicos/{id}` (TEC-05): Perfil del técnico. Sin sesión devuelve solo el resumen (foto, verificación, calificación, experiencia, descripción corta); con sesión suma servicios y precios, colonias….
- `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia` (TEC-06): Dice si el técnico atiende la colonia de la persona.
- `GET /api/v1/tecnicos/{id}/fotos-trabajos` (TEC-07): Fotos de trabajos terminados que el cliente permitió publicar.
- `PUT /api/v1/tecnicos/me` (TEC-08): Edita el perfil público: negocio, años, oficios, forma de atención, radio, descripción, foto.
- `PUT /api/v1/tecnicos/me/disponibilidad` (TEC-09): Cambia el interruptor Disponible hoy: guarda `disponibleHasta` (hoy a las 23:59:59 hora de la ciudad) y devuelve `disponibleHoy` y `disponibleHasta`.
- `GET /api/v1/tecnicos/me/horarios` (TEC-10): Horarios del técnico.
- `PUT /api/v1/tecnicos/me/horarios` (TEC-11): Cambia abierto o cerrado por día.
- `GET /api/v1/tecnicos/me/reputacion` (TEC-13): Calificación promedio, distribución por estrellas y nivel.
- `GET /api/v1/explorar` (TEC-14): Bloques de la pestaña Explorar: servicios populares, trabajos reales, cerca de ti, mejor calificados e ideas, con el filtro elegido.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 036.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `POST /api/v1/tecnicos` (crear): Registra a la persona como técnico (nombre y apellido, oficios, taller o domicilio, radio, perfil opcional y términos). Lo puede llamar: persona con sesión (cliente o técnico). Responde 201 cuando sale bien.
- **R-02:** Entrada de `POST /api/v1/tecnicos`: Cuerpo: oficios, atencion, direccionTaller o radioKm, perfil opcional, aceptaTerminos. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `oficios`: Al menos uno; `atencion`: Solo esos valores; `direccionTaller`: Obligatoria si atiende en taller (mínimo 8 caracteres). No se muestra si atiende a domici…; `radioKm`: 3, 5, 8 o 12.
- **R-03:** Estados de `POST /api/v1/tecnicos`: 409 `already_a_technician` (ya es técnico). Los códigos de negocio van en inglés y en snake_case.
- **R-04:** `GET /api/v1/tecnicos` (leer todos / filtro / búsqueda): Busca técnicos por filtros: oficio, colonia, distancia o radio, Disponible hoy y solo verificados (el texto libre es `/busqueda`, spec 027). Lo puede llamar: cualquier persona para el resumen y una persona con sesión para el detalle. Responde 200 cuando sale bien.
- **R-05:** Entrada de `GET /api/v1/tecnicos`: Query: oficio, colonia, lat, lng, radioKm, disponibleHoy, soloVerificados, orden, cursor, limite. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo. Reglas: `radioKm`: 3, 5, 8 o 12; `disponibleHoy`: Necesario para recibir solicitudes.
- **R-06:** `GET /api/v1/tecnicos` pagina por cursor (Por cursor, 20 (máx. 50)): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Orden: distancia, calificacion o recomendadas.
- **R-07:** `GET /api/v1/tecnicos` se cachea (Sí, unos segundos); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-08:** `GET /api/v1/tecnicos/autocompletar` (autocompletado): Sugerencias de oficios, servicios y negocios mientras se escribe (por ejemplo, boiler, frenos). Lo puede llamar: cualquier persona, sin sesión. Responde 200 cuando sale bien.
- **R-09:** Entrada de `GET /api/v1/tecnicos/autocompletar`: Query: q (mínimo 2 caracteres). Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-10:** `GET /api/v1/tecnicos/autocompletar` se cachea (Sí, unos segundos); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-11:** `GET /api/v1/mapa/tecnicos` (búsqueda específica): Técnicos de la zona visible del mapa, ya agrupados, para no mandarlos todos. Lo puede llamar: cualquier persona, sin sesión. Responde 200 cuando sale bien.
- **R-12:** Entrada de `GET /api/v1/mapa/tecnicos`: Query: norte, sur, este, oeste, oficio, zoom. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-13:** `GET /api/v1/mapa/tecnicos` se cachea (Sí, unos segundos); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-14:** `GET /api/v1/tecnicos/{id}` (leer uno): Perfil del técnico. Sin sesión devuelve solo el resumen (foto, verificación, calificación, experiencia, descripción corta); con sesión suma servicios y precios, colonias, fotos y horarios. Lo puede llamar: cualquier persona para el resumen y una persona con sesión para el detalle. Responde 200 cuando sale bien.
- **R-15:** Entrada de `GET /api/v1/tecnicos/{id}`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-16:** Estados de `GET /api/v1/tecnicos/{id}`: 404 `technician_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-17:** `GET /api/v1/tecnicos/{id}` se cachea (Sí, 1 minuto); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-18:** `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia` (búsqueda específica): Dice si el técnico atiende la colonia de la persona. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-19:** Entrada de `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia`: Ruta: id. Query: colonia. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-20:** Estados de `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia`: 404 `technician_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-21:** `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia` se cachea (Sí, 1 minuto); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-22:** `GET /api/v1/tecnicos/{id}/fotos-trabajos` (leer todos): Fotos de trabajos terminados que el cliente permitió publicar. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-23:** Entrada de `GET /api/v1/tecnicos/{id}/fotos-trabajos`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-24:** Estados de `GET /api/v1/tecnicos/{id}/fotos-trabajos`: 404 `technician_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-25:** `GET /api/v1/tecnicos/{id}/fotos-trabajos` pagina por cursor (Por cursor, 20): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Orden por fecha descendente.
- **R-26:** `GET /api/v1/tecnicos/{id}/fotos-trabajos` se cachea (Sí, 1 minuto); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-27:** `PUT /api/v1/tecnicos/me` (actualizar): Edita el perfil público: negocio, años, oficios, forma de atención, radio, descripción, foto. Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-28:** Entrada de `PUT /api/v1/tecnicos/me`: Cuerpo: campos a cambiar. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-29:** `PUT /api/v1/tecnicos/me/disponibilidad` (actualizar): Cambia el interruptor Disponible hoy: guarda `disponibleHasta` (hoy a las 23:59:59 hora de la ciudad) y devuelve `disponibleHoy` y `disponibleHasta`. Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-30:** Entrada de `PUT /api/v1/tecnicos/me/disponibilidad`: Cuerpo: disponibleHoy. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `disponibleHoy`: Necesario para recibir solicitudes.
- **R-31:** `GET /api/v1/tecnicos/me/horarios` (leer uno): Horarios del técnico. Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-32:** `PUT /api/v1/tecnicos/me/horarios` (actualizar): Cambia abierto o cerrado por día. Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-33:** Entrada de `PUT /api/v1/tecnicos/me/horarios`: Cuerpo: horarios. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-34:** `GET /api/v1/tecnicos/me/reputacion` (leer uno): Calificación promedio, distribución por estrellas y nivel. Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-35:** `GET /api/v1/tecnicos/me/reputacion` se cachea (Sí, 1 minuto); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-36:** `GET /api/v1/explorar` (búsqueda específica): Bloques de la pestaña Explorar: servicios populares, trabajos reales, cerca de ti, mejor calificados e ideas, con el filtro elegido. Lo puede llamar: cualquier persona para el resumen y una persona con sesión para el detalle. Responde 200 cuando sale bien.
- **R-37:** Entrada de `GET /api/v1/explorar`: Query: filtro (todo|auto|agua|luz), colonia. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-38:** `GET /api/v1/explorar` se cachea (Sí, 1 minuto); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-39:** **Registro (TEC-01).** Crea el perfil del técnico con `estado = activo`, el que usa la elegibilidad de la spec 028; **la verificación de identidad es opcional** (spec 025) y solo agrega la insignia. `oficios` es una lista de uno o más oficios activos del catálogo, sin repetir y sin máximo propio; `atencion` solo `taller`, `domicilio` o `ambos`; `direccionTaller` obligatoria con taller (mínimo 8 caracteres) y nunca visible con atención a domicilio; `radioKm` de 3, 5, 8 o 12; `aceptaTerminos` verdadero. La persona es la del `sub` del token y solo puede tener un perfil.
- **R-40:** **Búsqueda y mapa (TEC-02 y TEC-04).** `lat`, `lng` y los límites del mapa **no se guardan, no se registran, no se devuelven ni forman parte de la clave de caché**: se convierten en colonia o en celdas (spec 023, R-12). Solo aparecen técnicos `activo` y no ocultos (spec 027). TEC-02 reutiliza el almacén y los índices de la spec 027 y atiende **filtros estructurados** (oficio, colonia, radio, `disponibleHoy`, `soloVerificados`, `orden`); **no recibe `q`**: la búsqueda por texto libre es `/busqueda` de la 027. `orden` es `distancia`, `calificacion` o `recomendadas`; el mapa devuelve hasta 200 elementos agrupados por celda según `zoom` (de 1 a 20; fuera de rango, 422).
- **R-41:** **Autocompletar (TEC-03).** `q` de 2 a 40 caracteres (letras, números y espacios); hasta 8 sugerencias; es público y por eso tiene límite por IP; el texto buscado no se registra junto con la persona.
- **R-42:** **Perfil (TEC-05).** Sin sesión devuelve solo el resumen (foto, verificación, calificación, experiencia y descripción corta); con sesión suma servicios, precios, colonias, fotos y horarios. Nunca devuelve teléfono ni dirección; un técnico oculto responde 404 `technician_not_found`.
- **R-43:** **Llega a mi colonia (TEC-06).** Responde `{ atiende: bool }` comparando la colonia consultada con las que atiende el técnico; una `colonia` que no está en el catálogo responde 422 `errors.colonia`.
- **R-44:** **Fotos de trabajos (TEC-07).** Solo fotos de trabajos terminados con permiso del cliente (spec 050, VIS-11) y que pasaron la moderación (spec 031); se entregan con las direcciones de imágenes públicas de la spec 032 y se paginan por cursor, 20 por defecto.
- **R-45:** **Editar perfil (TEC-08).** Cambian solo los campos enviados; rigen las reglas de TEC-01 más `nombreNegocio` de hasta 60 caracteres, `descripcion` de hasta 300 y `anios` de 0 a 60; `fotoUrl` se acepta solo si viene de la subida del servidor (spec 032) y pasa moderación; al guardar se invalida la caché del perfil.
- **R-46:** **Disponibilidad (TEC-09).** `disponibleHoy = true` fija `disponibleHasta` a las 23:59:59 de hoy en la hora de la ciudad (`America/Mexico_City`) y `false` lo fija en este momento; así se apaga sola a la medianoche (HU-019, spec 028). La respuesta devuelve `disponibleHoy` y `disponibleHasta`. No exige verificación de identidad.
- **R-47:** **Horarios (TEC-10 y TEC-11).** Siete días, cada uno `abierto` o cerrado con una o dos franjas `HH:mm` de 24 horas en la hora de la ciudad, con inicio anterior al fin y sin traslapes; `PUT` reemplaza la lista completa y una franja inválida responde 422.
- **R-48:** **Reputación (TEC-13).** Calificación promedio con un decimal, distribución por estrellas de 1 a 5 y nivel, calculados con las opiniones (spec 051); se cachea 1 minuto por técnico.
- **R-49:** **Explorar (TEC-14).** `filtro` es `todo`, `auto`, `agua` o `luz` y `colonia` es opcional; cada bloque (servicios populares, trabajos reales, cerca de ti, mejor calificados e ideas) tiene como máximo 10 elementos; sin sesión devuelve solo los bloques públicos; se cachea 1 minuto por filtro y colonia.
- **R-50:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-51:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-52:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-53:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/tecnicos` con datos válidos, entonces responde 201 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/tecnicos`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/tecnicos`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-04:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/tecnicos`, entonces responde 400 y no guarda nada.
- **CA-05:** Dado el caso «la persona ya tiene perfil de técnico», cuando llama `POST /api/v1/tecnicos`, entonces responde 409 `already_a_technician` con ProblemDetails y no cambia el estado.
- **CA-06:** Dado el rol «cualquier persona para el resumen y una persona con sesión para el detalle», cuando llama `GET /api/v1/tecnicos` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-07:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/tecnicos`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-08:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-09:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-10:** Dado el rol «cualquier persona, sin sesión», cuando llama `GET /api/v1/tecnicos/autocompletar` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-11:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/tecnicos/autocompletar`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-12:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-13:** Dado el rol «cualquier persona, sin sesión», cuando llama `GET /api/v1/mapa/tecnicos` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-14:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/mapa/tecnicos`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-15:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-16:** Dado el rol «cualquier persona para el resumen y una persona con sesión para el detalle», cuando llama `GET /api/v1/tecnicos/{id}` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-17:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/tecnicos/{id}`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-18:** Dado el caso «el técnico no existe o está oculto», cuando llama `GET /api/v1/tecnicos/{id}`, entonces responde 404 `technician_not_found` con ProblemDetails y no cambia el estado.
- **CA-19:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-20:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-21:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia`, entonces responde 401 y no ejecuta la operación.
- **CA-22:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-23:** Dado el caso «el técnico no existe o está oculto», cuando llama `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia`, entonces responde 404 `technician_not_found` con ProblemDetails y no cambia el estado.
- **CA-24:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-25:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/tecnicos/{id}/fotos-trabajos` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-26:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/tecnicos/{id}/fotos-trabajos`, entonces responde 401 y no ejecuta la operación.
- **CA-27:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/tecnicos/{id}/fotos-trabajos`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-28:** Dado el caso «el técnico no existe o está oculto», cuando llama `GET /api/v1/tecnicos/{id}/fotos-trabajos`, entonces responde 404 `technician_not_found` con ProblemDetails y no cambia el estado.
- **CA-29:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-30:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-31:** Dado el rol «cuenta con perfil de técnico», cuando llama `PUT /api/v1/tecnicos/me` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-32:** Dada una petición sin token o con token vencido, cuando llama `PUT /api/v1/tecnicos/me`, entonces responde 401 y no ejecuta la operación.
- **CA-33:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `PUT /api/v1/tecnicos/me`, entonces responde 404 sin revelar que el recurso existe y no cambia nada.
- **CA-34:** Dado un valor inválido según las reglas de §6, cuando llama `PUT /api/v1/tecnicos/me`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-35:** Dado un cuerpo que no se puede leer, cuando llama `PUT /api/v1/tecnicos/me`, entonces responde 400 y no guarda nada.
- **CA-36:** Dado el rol «cuenta con perfil de técnico», cuando llama `PUT /api/v1/tecnicos/me/disponibilidad` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-37:** Dada una petición sin token o con token vencido, cuando llama `PUT /api/v1/tecnicos/me/disponibilidad`, entonces responde 401 y no ejecuta la operación.
- **CA-38:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `PUT /api/v1/tecnicos/me/disponibilidad`, entonces responde 403 y no cambia nada.
- **CA-39:** Dado un valor inválido según las reglas de §6, cuando llama `PUT /api/v1/tecnicos/me/disponibilidad`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-40:** Dado el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/tecnicos/me/horarios` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-41:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/tecnicos/me/horarios`, entonces responde 401 y no ejecuta la operación.
- **CA-42:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/tecnicos/me/horarios`, entonces responde 404 sin revelar que el recurso existe y no cambia nada.
- **CA-43:** Dado el rol «cuenta con perfil de técnico», cuando llama `PUT /api/v1/tecnicos/me/horarios` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-44:** Dada una petición sin token o con token vencido, cuando llama `PUT /api/v1/tecnicos/me/horarios`, entonces responde 401 y no ejecuta la operación.
- **CA-45:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `PUT /api/v1/tecnicos/me/horarios`, entonces responde 404 sin revelar que el recurso existe y no cambia nada.
- **CA-46:** Dado un valor inválido según las reglas de §6, cuando llama `PUT /api/v1/tecnicos/me/horarios`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-47:** Dado un cuerpo que no se puede leer, cuando llama `PUT /api/v1/tecnicos/me/horarios`, entonces responde 400 y no guarda nada.
- **CA-48:** Dado el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/tecnicos/me/reputacion` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-49:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/tecnicos/me/reputacion`, entonces responde 401 y no ejecuta la operación.
- **CA-50:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/tecnicos/me/reputacion`, entonces responde 404 sin revelar que el recurso existe y no cambia nada.
- **CA-51:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-52:** Dado el rol «cualquier persona para el resumen y una persona con sesión para el detalle», cuando llama `GET /api/v1/explorar` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-53:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/explorar`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-54:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-55:** Dada una persona sin perfil, cuando se registra con datos válidos, entonces se crea el perfil `activo` sin exigir verificación; con un radio fuera de 3, 5, 8 o 12, un oficio repetido o inactivo o sin términos responde 422; y si ya es técnico responde 409 `already_a_technician`.
- **CA-56:** Dada una búsqueda con coordenadas, cuando se inspeccionan registros, caché y respuesta, entonces no aparecen coordenadas; un técnico suspendido u oculto no sale en la lista ni en el mapa; y un parámetro `q` en TEC-02 se ignora.
- **CA-57:** Dado un `q` de 1 o de más de 40 caracteres o con símbolos, cuando se consulta, entonces responde 422; con uno válido recibe hasta 8 sugerencias.
- **CA-58:** Dado un perfil visible, cuando lo consulta alguien sin sesión, entonces recibe solo el resumen; con sesión, el detalle; y en ambos casos sin teléfono ni dirección.
- **CA-59:** Dado un técnico y una colonia del catálogo, cuando se consulta, entonces responde si la atiende; con una colonia inexistente responde 422.
- **CA-60:** Dado un técnico con fotos con y sin permiso, cuando se consultan, entonces solo salen las permitidas y moderadas, paginadas.
- **CA-61:** Dado un técnico, cuando cambia solo su descripción, entonces el resto del perfil no cambia y la caché del perfil se invalida; con una `fotoUrl` ajena responde 422.
- **CA-62:** Dado un técnico, cuando activa «Disponible hoy», entonces `disponibleHasta` queda a las 23:59:59 de hoy y deja de ser elegible al pasar esa hora; al apagarlo, deja de serlo de inmediato.
- **CA-63:** Dado un horario con franjas traslapadas o con inicio posterior al fin, cuando se guarda, entonces responde 422 y no cambia el horario anterior.
- **CA-64:** Dado un técnico con opiniones, cuando consulta su reputación, entonces recibe promedio, distribución y nivel coherentes con sus opiniones.
- **CA-65:** Dado un filtro válido, cuando se consulta Explorar, entonces cada bloque trae como máximo 10 elementos; con un filtro fuera de la lista responde 422.
- **CA-66:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-67:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-68:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-69:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec046Tests.Criterio01_CA_046_01` |
| CA-02 | `Spec046Tests.Criterio02_CA_046_02` |
| CA-03 | `Spec046Tests.Criterio03_CA_046_03` |
| CA-04 | `Spec046Tests.Criterio04_CA_046_04` |
| CA-05 | `Spec046Tests.Criterio05_CA_046_05` |
| CA-06 | `Spec046Tests.Criterio06_CA_046_06` |
| CA-07 | `Spec046Tests.Criterio07_CA_046_07` |
| CA-08 | `Spec046Tests.Criterio08_CA_046_08` |
| CA-09 | `Spec046Tests.Criterio09_CA_046_09` |
| CA-10 | `Spec046Tests.Criterio10_CA_046_10` |
| CA-11 | `Spec046Tests.Criterio11_CA_046_11` |
| CA-12 | `Spec046Tests.Criterio12_CA_046_12` |
| CA-13 | `Spec046Tests.Criterio13_CA_046_13` |
| CA-14 | `Spec046Tests.Criterio14_CA_046_14` |
| CA-15 | `Spec046Tests.Criterio15_CA_046_15` |
| CA-16 | `Spec046Tests.Criterio16_CA_046_16` |
| CA-17 | `Spec046Tests.Criterio17_CA_046_17` |
| CA-18 | `Spec046Tests.Criterio18_CA_046_18` |
| CA-19 | `Spec046Tests.Criterio19_CA_046_19` |
| CA-20 | `Spec046Tests.Criterio20_CA_046_20` |
| CA-21 | `Spec046Tests.Criterio21_CA_046_21` |
| CA-22 | `Spec046Tests.Criterio22_CA_046_22` |
| CA-23 | `Spec046Tests.Criterio23_CA_046_23` |
| CA-24 | `Spec046Tests.Criterio24_CA_046_24` |
| CA-25 | `Spec046Tests.Criterio25_CA_046_25` |
| CA-26 | `Spec046Tests.Criterio26_CA_046_26` |
| CA-27 | `Spec046Tests.Criterio27_CA_046_27` |
| CA-28 | `Spec046Tests.Criterio28_CA_046_28` |
| CA-29 | `Spec046Tests.Criterio29_CA_046_29` |
| CA-30 | `Spec046Tests.Criterio30_CA_046_30` |
| CA-31 | `Spec046Tests.Criterio31_CA_046_31` |
| CA-32 | `Spec046Tests.Criterio32_CA_046_32` |
| CA-33 | `Spec046Tests.Criterio33_CA_046_33` |
| CA-34 | `Spec046Tests.Criterio34_CA_046_34` |
| CA-35 | `Spec046Tests.Criterio35_CA_046_35` |
| CA-36 | `Spec046Tests.Criterio36_CA_046_36` |
| CA-37 | `Spec046Tests.Criterio37_CA_046_37` |
| CA-38 | `Spec046Tests.Criterio38_CA_046_38` |
| CA-39 | `Spec046Tests.Criterio39_CA_046_39` |
| CA-40 | `Spec046Tests.Criterio40_CA_046_40` |
| CA-41 | `Spec046Tests.Criterio41_CA_046_41` |
| CA-42 | `Spec046Tests.Criterio42_CA_046_42` |
| CA-43 | `Spec046Tests.Criterio43_CA_046_43` |
| CA-44 | `Spec046Tests.Criterio44_CA_046_44` |
| CA-45 | `Spec046Tests.Criterio45_CA_046_45` |
| CA-46 | `Spec046Tests.Criterio46_CA_046_46` |
| CA-47 | `Spec046Tests.Criterio47_CA_046_47` |
| CA-48 | `Spec046Tests.Criterio48_CA_046_48` |
| CA-49 | `Spec046Tests.Criterio49_CA_046_49` |
| CA-50 | `Spec046Tests.Criterio50_CA_046_50` |
| CA-51 | `Spec046Tests.Criterio51_CA_046_51` |
| CA-52 | `Spec046Tests.Criterio52_CA_046_52` |
| CA-53 | `Spec046Tests.Criterio53_CA_046_53` |
| CA-54 | `Spec046Tests.Criterio54_CA_046_54` |
| CA-55 | `Spec046Tests.Criterio55_CA_046_55` |
| CA-56 | `Spec046Tests.Criterio56_CA_046_56` |
| CA-57 | `Spec046Tests.Criterio57_CA_046_57` |
| CA-58 | `Spec046Tests.Criterio58_CA_046_58` |
| CA-59 | `Spec046Tests.Criterio59_CA_046_59` |
| CA-60 | `Spec046Tests.Criterio60_CA_046_60` |
| CA-61 | `Spec046Tests.Criterio61_CA_046_61` |
| CA-62 | `Spec046Tests.Criterio62_CA_046_62` |
| CA-63 | `Spec046Tests.Criterio63_CA_046_63` |
| CA-64 | `Spec046Tests.Criterio64_CA_046_64` |
| CA-65 | `Spec046Tests.Criterio65_CA_046_65` |
| CA-66 | `Spec046Tests.Criterio66_CA_046_66` |
| CA-67 | `Spec046Tests.Criterio67_CA_046_67` |
| CA-68 | `Spec046Tests.Criterio68_CA_046_68` |
| CA-69 | `Spec046Tests.Criterio69_CA_046_69` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| TEC-01 | `POST /api/v1/tecnicos` | persona con sesión (cliente o técnico) | Cuerpo: oficios, atencion, direccionTaller o radioKm, perfil opcional, aceptaTerminos | 201; 400; 401; 409 (ya es técnico); 422 |
| TEC-02 | `GET /api/v1/tecnicos` | cualquier persona para el resumen y una persona con sesión para el detalle | Query: oficio, colonia, lat, lng, radioKm, disponibleHoy, soloVerificados, orden, cursor, limite | 200; 422 |
| TEC-03 | `GET /api/v1/tecnicos/autocompletar` | cualquier persona, sin sesión | Query: q (mínimo 2 caracteres) | 200; 422 |
| TEC-04 | `GET /api/v1/mapa/tecnicos` | cualquier persona, sin sesión | Query: norte, sur, este, oeste, oficio, zoom | 200; 422 |
| TEC-05 | `GET /api/v1/tecnicos/{id}` | cualquier persona para el resumen y una persona con sesión para el detalle | Ruta: id | 200; 404 |
| TEC-06 | `GET /api/v1/tecnicos/{id}/llega-a-mi-colonia` | persona con sesión (cliente o técnico) | Ruta: id. Query: colonia | 200; 401; 404 |
| TEC-07 | `GET /api/v1/tecnicos/{id}/fotos-trabajos` | persona con sesión (cliente o técnico) | Ruta: id | 200; 401; 404 |
| TEC-08 | `PUT /api/v1/tecnicos/me` | cuenta con perfil de técnico | Cuerpo: campos a cambiar | 200; 400; 401; 422 |
| TEC-09 | `PUT /api/v1/tecnicos/me/disponibilidad` | cuenta con perfil de técnico | Cuerpo: disponibleHoy | 200; 401; 403 |
| TEC-10 | `GET /api/v1/tecnicos/me/horarios` | cuenta con perfil de técnico | Ninguno | 200; 401 |
| TEC-11 | `PUT /api/v1/tecnicos/me/horarios` | cuenta con perfil de técnico | Cuerpo: horarios | 200; 400; 401 |
| TEC-13 | `GET /api/v1/tecnicos/me/reputacion` | cuenta con perfil de técnico | Ninguno | 200; 401 |
| TEC-14 | `GET /api/v1/explorar` | cualquier persona para el resumen y una persona con sesión para el detalle | Query: filtro (todo/auto/agua/luz), colonia | 200 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| TEC-01 | No aplica | No | No |
| TEC-02 | Orden: distancia, calificacion o recomendadas | Por cursor, 20 (máx. 50) | Sí, unos segundos |
| TEC-03 | Máximo 8 sugerencias | No | Sí, unos segundos |
| TEC-04 | Filtro por oficio; agrupados por zona | Hasta 200 por solicitud | Sí, unos segundos |
| TEC-05 | No aplica | No | Sí, 1 minuto |
| TEC-06 | No aplica | No | Sí, 1 minuto |
| TEC-07 | Orden por fecha descendente | Por cursor, 20 | Sí, 1 minuto |
| TEC-08 | No aplica | No | No (invalida caché del perfil) |
| TEC-09 | No aplica | No | No |
| TEC-10 | No aplica | No | No |
| TEC-11 | No aplica | No | No |
| TEC-13 | No aplica | No | Sí, 1 minuto |
| TEC-14 | Filtro por categoría | No (cada bloque limitado) | Sí, 1 minuto |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| TEC-01 | 409 | `already_a_technician` | la persona ya tiene perfil de técnico |
| TEC-05 | 404 | `technician_not_found` | el técnico no existe o está oculto |
| TEC-06 | 404 | `technician_not_found` | el técnico no existe o está oculto |
| TEC-07 | 404 | `technician_not_found` | el técnico no existe o está oculto |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `tecnicos`.** Guarda el perfil de cada técnico: lo que ven los clientes (presentación, oficios, horarios, nivel, verificación) y lo que usa el sistema para repartir solicitudes (disponibilidad, radio y ubicación aproximada). Es la colección más consultada por la búsqueda y el mapa. Un técnico es la misma cuenta que el cliente: este documento se enlaza con usuarios. El perfil devuelve solo un resumen a quien no…

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| usuarioId | ObjectId | Sí | Única; debe existir en usuarios | Único |
| nombreNegocio | Texto | No | Máximo 60 caracteres | Texto |
| oficios | Lista de textos | Sí | Al menos uno | Compuesto |
| otroOficio | Texto | No | Si es el único oficio, no recibe solicitudes | No |
| atencion | Texto | Sí | Solo esos valores | Compuesto |
| direccionTaller | Texto cifrado | No | Obligatoria si atiende en taller (mínimo 8 caracteres). No se muestra si atiende a domicilio | No |
| radioKm | Entero | No | 3, 5, 8 o 12 | No |
| ubicacion | Punto (GeoJSON) | Sí | Aproximada, no exacta | 2dsphere |
| descripcion | Texto | No | Máximo 300 caracteres | Texto |
| anios | Entero | No | De 0 a 60 | No |
| horarios | Documento | No |  | No |
| disponibleHoy | Booleano | Sí | Necesario para recibir solicitudes | Compuesto |
| verificacion | Texto | Sí | Copia del resultado de Identidad | Compuesto |
| reputacion | Documento | Sí | Calculado por el servicio | Sí (promedio) |
| fotoUrl | Texto | No | Moderada; reducida | No |
| ocultoHasta | Fecha y hora UTC | No | Lo asigna la moderación | No |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| 2dsphere | ubicacion | Buscar técnicos cerca de un punto (mapa y lista) y por radio |
| Compuesto | oficios + disponibleHoy + verificacion | Filtros de la búsqueda y reparto de solicitudes por oleadas |
| Único | usuarioId | Un perfil de técnico por cuenta |
| Texto (español) | nombreNegocio + descripcion + servicios | Autocompletado y búsqueda por texto |
| Normal | reputacion.promedio | Orden por calificación |

**Disponibilidad.** `tecnicos.disponibleHasta` (fecha UTC, spec 028) sustituye al booleano `disponibleHoy` del Excel: `disponibleHoy` es un valor derivado (`disponibleHasta > ahora`) que no se guarda.

## 7. Arquitectura y componentes
- Proyecto `Knock.Market.Api`, módulo `Technicians` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 023 de este repositorio.
- Spec 025 de este repositorio.
- Spec 027 de este repositorio.
- Spec 036 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 036 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Almacén de búsqueda de la spec 027 y elegibilidad de la 028 (hoy no implementados)
  - `disponibleHasta` (HU-019): hoy solo existe en las semillas

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-33, CA-38, CA-42, CA-45, CA-50, CA-66 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-33, CA-38, CA-42, CA-45, CA-50, CA-66 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-03 a CA-04, CA-07, CA-11, CA-14, CA-17, CA-22, CA-27, CA-34 a CA-35, CA-39, CA-46 a CA-47, CA-53 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-21, CA-26, CA-32, CA-37, CA-41, CA-44, CA-49 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-68 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-67 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-69 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_046_NN` en el nombre (para `python scripts/sdd_lint.py --trace 046`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-04, CA-07, CA-11, CA-14, CA-17, CA-21 a CA-22, CA-26 a CA-27, CA-32 a CA-35, CA-37 a CA-39, CA-41 a CA-42, CA-44 a CA-47, CA-49 a CA-50, CA-53, CA-66, CA-68) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Registro:** una persona toca «Quiero ser técnico» → llena oficios, forma de atención, radio y términos → queda activo y se le ofrece verificar su identidad para obtener la insignia.
- **Explorar:** el cliente abre Explorar con su colonia → ve bloques de técnicos y trabajos reales → entra al perfil, donde con sesión ve servicios, precios y horarios.
- **Disponible hoy:** el técnico activa el interruptor → aparece en búsquedas y mapa hasta la medianoche.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 046` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** El perfil nace `activo` y la verificación es opcional, como dice el Excel: exigirla frenaría el arranque del marketplace y la 028 ya define la elegibilidad sin ella.
- **D-05:** La disponibilidad se guarda como `disponibleHasta` (apagado a medianoche) porque así la lee la spec 028; `disponibleHoy` es el valor derivado que usa el cliente.
- **D-06:** TEC-02 solo filtra y ordena; el texto libre es de `/busqueda`: así no hay dos buscadores con las mismas reglas.
- **D-07:** Sin máximo de oficios: cada técnico elige entre los oficios activos del catálogo, que ya es corto.
- **D-08:** El mapa agrupa por celdas según el zoom para devolver como máximo 200 elementos, como pide el Excel.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Estado y verificación: antes `pendiente_de_verificacion` y exigir verificación para estar disponible; ahora `activo` y verificación opcional (Excel y spec 028).
- **C-02:** Disponibilidad: antes un interruptor que no se apagaba solo; ahora `disponibleHasta` con apagado a medianoche (spec 028).
- **C-03:** Búsqueda: antes `/tecnicos` aceptaba `q`; ahora el texto libre queda en `/busqueda` (spec 027).
- **C-04:** Oficios: antes máximo 5 sin respaldo; ahora sin máximo propio.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencias: almacén de búsqueda de la spec 027, elegibilidad de la 028 y la insignia de verificación de la 025. TEC-12 (resumen) ya lo cubre la spec 036 con `/tecnicos/me/trabajo`.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
