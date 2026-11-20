# Spec 057 — Borrado de fotos de verificación

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Jira:** KNOCK-87 (HU-068 · Borrar fotos de verificación al decidir o a los 7 días, alerta a las 24 h) · **Épica:** EP-12 Panel interno · **Módulo:** `Trust` (Identidad) · **Capas:** Back + BD · **Puntos:** 3
> **Responsable:** José Julián Hernández Hernández (según la ficha) · **Sprint:** el que decida el Tech Lead (la ficha dice Sprint 4)
> **Servicio:** Identidad (`Knock.Identity.Api`) · **Colecciones:** `verificaciones` (spec 025, campos nuevos), `tareas_programadas` y `outbox` (spec 054), `panel_auditoria` (spec 033)
> **Fuentes:** `docs/backlog/HU-068.md`; spec 025 (§2.3, R-11, R-16, R-17, T-03, T-04, T-06); spec 039 (USR-10, R-25); spec 053 (VER-04, VER-05, R-22); spec 054 (R-08, R-09, R-10); constitución §7; `docs/DECISION_TECNICA.md` (riesgo «Datos biométricos»); SEGURIDAD_Y_PRIVACIDAD §3 y §4 y DECISIONES #4 (citadas por la ficha; no están en este repositorio)
> **Skills:** spec-first-writing, security-core, events-outbox, database-scripts

## 1. Objetivo
Que las fotos de la INE y la selfie de una verificación no existan más tiempo del necesario: se destruyen cuando el verificador decide, y a los 7 días si nadie las revisó (con una alerta a las 24 h), de forma que las fotos y cualquier copia queden ilegibles y solo se conserven el resultado, la fecha, quién decidió y el HMAC de la CURP.

A este repositorio le toca todo: el servicio de purga, los dos manejadores de tarea (`AvisarAlVerificador` y `BorrarFotosDeVerificacion`, tipos que ya fija la spec 054 R-08), los campos nuevos de `verificaciones`, los eventos y la auditoría. No hay pantalla ni ruta pública nueva (la ficha lo declara: «Mobile: sin pantalla nueva»).

**Estado del código al redactar este borrador:** el módulo `Trust` de Identidad (spec 025) y el panel de revisión (spec 053) no están implementados, así que el almacén cifrado de fotos (T-03 y T-04 de la spec 025) tampoco existe. Esta spec define la purga contra el contrato de la 025 y por eso no se puede implementar antes que T-01, T-03, T-04 y T-06 de ella (ver §8 y `plan.md`).

## 2. Historia y casos de prueba
**HU-068** — Como técnico que se verificó, quiero que las fotos de mi INE y mi selfie se borren en cuanto las revisan, para no dejar mis datos biométricos guardados más de lo necesario.

Los casos TC-HU-068-01 a 05 son los de la ficha (CA1 a CA5); del 06 al 14 los agrega esta spec. Los nombres de prueba usan el sufijo `_TC_HU_068_KK`. Todas las pruebas de plazo usan `TimeProvider` falso (spec 054 R-12).

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-HU-068-01 | Dado un verificador que decide una verificación `en_revision` (`aprobada` o `rechazada`), cuando se guarda la decisión, entonces la llave envuelta se destruye en la misma escritura que el estado, los blobs se borran, las fotos quedan `borrada` y ni descifrarlas ni pedir un enlace de descarga es posible. | `tests/IntegrationTest/Trust/Spec057PurgaTests.cs` → `Decision_Destruye_Llave_Y_Fotos_TC_HU_068_01` |
| TC-HU-068-02 | Dada una verificación `en_revision` que lleva 24 h sin decisión, cuando corre la tarea `AvisarAlVerificador`, entonces se emite la alerta una sola vez (marca `alertadaEn` y evento `VerificacionSinRevisarAlertada`) y la cola del panel la señala; si ya se decidió antes de las 24 h, la tarea termina `hecha` sin alerta. El envío por correo depende de §8-1. | `tests/IntegrationTest/Trust/Spec057AlertaTests.cs` → `Alerta_A_Las_24_Horas_Una_Sola_Vez_TC_HU_068_02` |
| TC-HU-068-03 | Dada una verificación `en_revision` que cumple 7 días sin decisión, cuando corre `BorrarFotosDeVerificacion`, entonces se destruye la llave, se borran los blobs, las fotos quedan `borrada`, el estado vuelve a `sin_verificar` con `motivo: fotos_borradas`, `puedeReintentar: true`, y se emite `FotosDeVerificacionBorradas` con `razon: vencida` para el aviso de reenvío. | `tests/IntegrationTest/Trust/Spec057PurgaTests.cs` → `Vence_A_Los_Siete_Dias_Y_Vuelve_A_Sin_Verificar_TC_HU_068_03` |
| TC-HU-068-04 | Dadas dos instancias que corren el ejecutor sobre la misma base con varias verificaciones vencidas, cuando se ejecuta, entonces cada verificación se purga una sola vez (un solo evento, un solo registro de auditoría). | `tests/IntegrationTest/Trust/Spec057ConcurrenciaTests.cs` → `Dos_Instancias_Purgan_Una_Sola_Vez_TC_HU_068_04` |
| TC-HU-068-05 | Dada una verificación purgada (por decisión, vencimiento o baja), cuando termina, entonces hay un registro de auditoría con `evento`, `razon`, `recursoId`, `sub` de quien decidió o `sistema`, y sin contenido del documento, CURP, HMAC, rutas de blob ni llave. | `tests/IntegrationTest/Trust/Spec057AuditoriaTests.cs` → `Auditoria_Sin_Contenido_Del_Documento_TC_HU_068_05` |
| TC-HU-068-06 | Dada una verificación ya purgada, cuando se repite la purga (reintento de tarea, tarea y decisión a la vez, o doble entrega), entonces no falla, no cambia el resultado y no duplica evento ni auditoría. | `tests/UnitTest/Trust/Spec057PurgaIdempotenteTests.cs` → `Repetir_La_Purga_No_Cambia_Nada_TC_HU_068_06` |
| TC-HU-068-07 | Dada una decisión y un vencimiento que llegan casi a la vez, cuando ambos intentan escribir, entonces gana la primera escritura y la otra termina sin efecto; no queda una verificación `sin_verificar` con decisión ni una `verificada` sin fotos purgadas. | `tests/IntegrationTest/Trust/Spec057ConcurrenciaTests.cs` → `Decision_Y_Vencimiento_Simultaneos_TC_HU_068_07` |
| TC-HU-068-08 | Dada una verificación purgada, cuando se inspecciona el documento y su rastro, entonces solo conserva resultado, fechas, `decididoPor`, `curpHmac` y `purgadaEn`; no existen `llaveEnvuelta`, `llaveKekVersion` ni `blob` con valor, y la sección «fotos» solo trae tipo y estado `borrada`. | `tests/DatabaseTest/Trust/Spec057InvariantesTests.cs` → `Documento_Purgado_Solo_Conserva_El_Resultado_TC_HU_068_08` |
| TC-HU-068-09 | Dado un blob de cuarentena que quedó de una captura o un enlace de 5 minutos ya emitido, cuando se purga, entonces cuarentena y contenedor definitivo quedan sin blobs de esa verificación, el enlace emitido responde 404 y el panel (spec 053 VER-04) responde 410 `photos_destroyed`. | `tests/IntegrationTest/Trust/Spec057CopiasTests.cs` → `Purga_Alcanza_Cuarentena_Definitivo_Y_Enlaces_TC_HU_068_09` |
| TC-HU-068-10 | Dado un fallo al borrar un blob (Blob no disponible), cuando corre la tarea, entonces la llave ya está destruida (las fotos son ilegibles), la tarea se reintenta con las esperas de la spec 054 R-07, y al quinto fallo queda `fallida` con alerta sin datos personales. | `tests/IntegrationTest/Trust/Spec057FallosTests.cs` → `Fallo_Al_Borrar_Blob_Reintenta_Y_Ya_Es_Ilegible_TC_HU_068_10` |
| TC-HU-068-11 | Dada la ejecución de `EliminarCuenta` (spec 039 R-25) de una persona con fotos de verificación, cuando se ejecuta, entonces se usa la misma purga (`razon: baja_de_cuenta`) y no queda foto ni llave; si la eliminación se cancela durante la gracia, no se purga nada. | `tests/IntegrationTest/Trust/Spec057BajaDeCuentaTests.cs` → `Baja_De_Cuenta_Purga_Las_Fotos_TC_HU_068_11` |
| TC-HU-068-12 | Dado el gateway y las rutas de Identidad, cuando se inspeccionan, entonces ninguna ruta pública dispara, adelanta ni consulta la purga o las tareas; y dado un `GET /me/verificacion` de la persona, entonces nunca devuelve fotos, rutas ni llave, antes ni después de la purga. | `tests/SecurityTest/Trust/Spec057AccesoTests.cs` → `Sin_Rutas_Publicas_Ni_Fotos_En_El_Estado_TC_HU_068_12` |
| TC-HU-068-13 | Dado el envío a revisión (spec 025 R-13), cuando se confirma, entonces en la misma transacción quedan programadas `AvisarAlVerificador` (`enviadoEn + 24 h`) y `BorrarFotosDeVerificacion` (`enviadoEn + 7 días`) con `claveNegocio = verificacionId`; programarlas dos veces no duplica; y al decidir o purgar se cierran como `hecha` sin efecto. | `tests/IntegrationTest/Trust/Spec057ProgramacionTests.cs` → `Envio_Programa_Alerta_Y_Borrado_Sin_Duplicar_TC_HU_068_13` |
| TC-HU-068-14 | Dados los registros, eventos, tareas y la auditoría de cualquier purga, cuando se inspeccionan, entonces solo traen identificadores (verificación, persona, tarea) y razón; nunca CURP, HMAC, rutas de blob, llave, enlaces ni contenido. | `tests/SecurityTest/Trust/Spec057RegistrosTests.cs` → `Sin_Datos_Sensibles_En_Registros_Eventos_Y_Tareas_TC_HU_068_14` |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `PurgaDeVerificacion` (servicio del módulo `Trust` de Identidad): única operación que destruye la llave, borra los blobs y deja el documento con el estado final; reutilizable por la decisión (spec 053), por los manejadores de tarea y por `EliminarCuenta` (spec 039).
  - Manejadores `AvisarAlVerificador` (24 h) y `BorrarFotosDeVerificacion` (7 días), registrados en el ejecutor de Identidad de la spec 054, idempotentes (spec 054 R-09).
  - Programación de ambas tareas al enviar la verificación (spec 025 R-13) y cierre de ambas al decidir o purgar.
  - Campos nuevos de `verificaciones`: `alertadaEn`, `purgadaEn`, `razonPurga`; script de `db/` idempotente.
  - Eventos `VerificacionSinRevisarAlertada` y `FotosDeVerificacionBorradas` en el buzón (`outbox`) y registro de auditoría de cada purga.
  - Uso de la purga desde el manejador de `EliminarCuenta` de la spec 039 (el borrado por baja de cuenta que la spec 025 §2.3 difiere a esta historia).
  - Pruebas de los 14 casos, incluida la concurrencia con dos instancias y tiempo simulado.
- **Out:**
  - La pantalla, el endpoint de revisión y de decisión del panel (`/admin/verificaciones`, spec 053 / HU-067) y el registro de quién abrió las fotos; esta spec solo recibe la decisión ya tomada.
  - El almacén cifrado de fotos y la subida (spec 025 T-03 y T-04): se asume como prerrequisito, no se reimplementa.
  - La entrega del aviso al teléfono (la spec 037 difiere `VerificacionDecidida`); aquí solo se emite el evento.
  - Enviar correo: no hay proveedor de correo en el repositorio (ver §8-1).
  - Cambiar los 7 días o las 24 h (los fijan la ficha y la spec 054 R-08) y la retención de respaldos de MongoDB (spec 005, §8-4).
  - Fotos de perfil, solicitudes y trabajos (spec 013), y exportaciones (spec 039).
  - Cualquier cambio en knock-frontend.

## 4. Contratos API y eventos
**Endpoints:** no hay endpoints nuevos ni cambios en OpenAPI. La purga se ejecuta desde dentro (decisión, tarea, baja de cuenta). El estado público sigue siendo `GET /api/v1/me/verificacion` (spec 025 R-14): tras una purga por vencimiento responde `estado: "sin_verificar"`, `motivo: "fotos_borradas"`, `puedeReintentar: true`; tras una decisión, el estado de la decisión con las fotos `borrada`.

**Tareas programadas (catálogo de la spec 054 R-08, sin tipos nuevos):**

| Tipo | Plazo | `claveNegocio` | Efecto |
|---|---|---|---|
| `AvisarAlVerificador` | `enviadoEn + 24 h` | `verificacionId` | Si sigue `en_revision` y no hay `alertadaEn`: marca `alertadaEn` y emite el evento de alerta. Si no, `hecha` sin efecto. |
| `BorrarFotosDeVerificacion` | `enviadoEn + 7 días` (`expiraEn`) | `verificacionId` | Si sigue `en_revision`: purga con `razon: vencida` y vuelve a `sin_verificar`. Si no, `hecha` sin efecto. |

(La ficha los llama `AlertarVerificacion` y `PurgarVerificacion`; esta spec usa los nombres que ya fija la spec 054.)

**Eventos** (buzón de Identidad, `_id` determinista `{tipo}:{verificacionId}:{version}`, carga solo con identificadores, spec 054 R-02):

| Evento | Productor | Consumidor | Carga |
|---|---|---|---|
| `VerificacionSinRevisarAlertada` | `AvisarAlVerificador` | cola del panel y canal del verificador (spec 053; consumo por definir, §8-1) | `verificacionId`, `enviadoEn` |
| `FotosDeVerificacionBorradas` | `PurgaDeVerificacion` | Mensajería (aviso de reenvío; la spec 037 aún no lo cubre) | `verificacionId`, `usuarioId`, `razon` (`decidida`, `vencida`, `baja_de_cuenta`) |

Se agregan a `docs/events.md` al implementar.

**Auditoría:** una inserción en `panel_auditoria` (spec 033 R-23) por purga: `evento: verificacion_purgada`, `resultado`, `razon`, `recursoId`, `sub` del verificador o `sistema`, `correlationId`. Si la purga se dispara desde la decisión del panel y la auditoría no se puede escribir, aplica la spec 053 R-22 (503 `audit_unavailable`, no se ejecuta). Si la dispara una tarea, la falla de auditoría reintenta la tarea (ver §8-6).

## 5. Modelo de datos
Cambios sobre `verificaciones` (spec 025 §6), base `identity`:

| Campo | Tipo | Nota |
|---|---|---|
| `alertadaEn` | date? | UTC; la pone `AvisarAlVerificador`; impide repetir la alerta |
| `purgadaEn` | date? | UTC; la pone la purga; es la marca de «ya no hay fotos ni llave» (campo `purgada_en` de la ficha) |
| `razonPurga` | string? | `decidida`, `vencida`, `baja_de_cuenta` |

- La purga deja `llaveEnvuelta` y `llaveKekVersion` en `null`, cada foto con `estado: borrada`, `blob: null` y `borradoEn`, y conserva `curpHmac`, `decididoPor`, `decididoEn`, `nivelOtorgado`, el resultado y `curpVigente` según el estado final (spec 025 R-16).
- Estado final: la decisión conserva el que decidió (`verificada` o `no_verificada`); el vencimiento y la baja dejan `sin_verificar`.
- Sin índice nuevo: la búsqueda de vencidas usa `{ estado: 1, expiraEn: 1 }` (spec 025) y la reserva de tareas, los índices de la spec 054.
- Script `db/057-identity-verificaciones-purga.js`: agrega los campos de forma idempotente (los documentos sin ellos se leen como `null`). No edita el script de la spec 025.
- **Invariante:** si `purgadaEn != null` no existe llave envuelta ni blob de esa verificación en ningún contenedor.

## 6. Seguridad, privacidad y amenazas
Dato biométrico (INE y selfie): aplican `security-core` #7, #8, #9, #15 y la constitución §7.

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Fuga o retención de biometría más allá de lo necesario | Purga al decidir y a los 7 días; la llave se destruye primero, así las fotos quedan ilegibles aunque quede un blob | TC-HU-068-01, 03, 10 |
| Borrado incompleto (blob de cuarentena, enlace emitido, fallo a medias) | Purga única que recorre cuarentena y definitivo; reintentos con espera; la llave ya destruida vuelve ilegible lo que falte; alerta si la tarea queda `fallida` | TC-HU-068-09, 10 |
| Repetición o concurrencia (tarea doble, dos instancias, decisión contra vencimiento) | Reserva atómica de la spec 054 R-10, `claveNegocio` única, transición condicionada por estado, purga idempotente | TC-HU-068-04, 06, 07, 13 |
| Repudio: negar que se borró o quién decidió | Auditoría de cada purga en `panel_auditoria`, de solo inserción | TC-HU-068-05 |
| Acceso indebido a la purga o a las fotos (persona ajena, usuario sin permiso, enumeración) | Sin rutas públicas; la purga solo se invoca desde el servicio; el estado de la persona no devuelve fotos ni rutas; la decisión exige `verification:review` con doble factor (spec 053) | TC-HU-068-12 |
| Fuga de datos en registros, eventos o auditoría | Cargas con identificadores y razón; sin CURP, HMAC, rutas, enlaces ni llave | TC-HU-068-05, 14 |
| Fotos que sobreviven a la baja de cuenta | La baja usa la misma purga | TC-HU-068-11 |
| Volver a hacer legible una foto con un respaldo antiguo (llave envuelta restaurada) | Sin control completo hoy: depende de la retención de respaldos | §8-4 (sin caso hasta decidir) |
| Denegación: la purga masiva satura Blob o MongoDB | Una verificación por tarea, reservada y reintentada con espera (spec 054) | TC-HU-068-04, 10 |

## 7. Pantallas
—. Sin pantalla nueva. El aviso de reenvío y la alerta en el panel son contenido de knock-frontend y de la spec 053, fuera de esta spec.

## 8. Pruebas, gates y límites
**Plan de pruebas** (meta de mutación del módulo `Trust`: 70 %, como la ficha, sobre las reglas de plazo, estado y purga):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Reglas de plazo y estado, idempotencia de la purga (xUnit, `TimeProvider` falso) |
| Integración | Sí | MongoDB real (Testcontainers), Azurite y doble de Key Vault: decisión, vencimiento, baja, dos instancias |
| Arquitectura | Sí | Nadie fuera de `PurgaDeVerificacion` borra blobs de verificación ni la llave; controllers sin acceso a Blob ni MongoDB |
| Seguridad negativa | Sí | Sin rutas públicas, sin datos sensibles en registros y eventos, estado sin fotos |
| Base de datos | Sí | Campos nuevos, script idempotente e invariante de documento purgado |
| Contrato | No | Sin endpoints nuevos ni cambios de OpenAPI; el diff del OpenAPI de Identidad debe quedar vacío |
| Mutación | Sí | Stryker.NET, meta 70 % (condición de 24 h y 7 días, estado de la transición) |
| Rendimiento | No | Volumen de piloto bajo |
| Accesibilidad / widget / móvil | No | Sin pantalla |

**Gates:** `AGENTS.md §5`. `python scripts/sdd_lint.py --trace 057` no reconoce este formato de casos (ver §8-7).

**Decisiones pendientes del Tech Lead (sí/no).** Cada una se contesta sí o no sobre la recomendación del autor; responder «sí» la deja decidida tal como está escrita y «no» obliga a ajustar la spec antes de aprobar. Ninguna se inventó como bloqueo: se anota qué parte del trabajo depende de ella.

| N.º | Decisión (recomendación del autor: sí) | Contexto y alternativa | Si es «no» |
|---|---|---|---|
| D-01 | **La alerta de 24 h es solo del panel (cola de la spec 053 marcada) más el evento `VerificacionSinRevisarAlertada`; no se envía correo.** | La ficha pide «panel y correo del verificador», pero el repositorio no tiene proveedor ni canal de correo. Agregarlo es una spec y un paquete nuevos. | Se abre una spec aparte del canal de correo y esta HU emite el evento sin consumidor de correo hasta entonces. |
| D-02 | **Se sigue la spec 025 (R-11, R-16): «destruir la llave» es borrar la llave envuelta del documento; no se crea una llave por verificación en Key Vault.** | La 025 está Aprobada y guarda la llave envuelta con la llave maestra en MongoDB. Una llave por verificación en Key Vault cambiaría la 025 y el costo. | Se enmienda la 025 y se agrega la deshabilitación de una llave por verificación en Key Vault (más puntos). |
| D-03 | **Se purga en toda decisión (`aprobada` o `rechazada`) y a los 7 días.** | La ficha y la spec 053 R-18 piden destruir la llave en cualquier decisión; la 025 R-17 solo nombra `no_verificada`. Esta spec toma la lectura de la ficha y la 053. | Se purga solo al rechazar y a los 7 días, y se cambian TC-HU-068-01 y 05. |
| D-04 | **Se acepta como límite documentado que un respaldo anterior a la purga conserva la llave envuelta o el blob hasta que venza su retención, y se pide fijar una retención de respaldos de `verificaciones` y del almacenamiento de fotos no mayor a 7 días.** | «Cualquier copia queda ilegible» no se puede garantizar sin esto; no se verificó que la spec 005 fije hoy esa retención. El 7 es igual al plazo de las fotos (valor propuesto, el Tech Lead puede indicar otro). | Se indica otra retención o se acepta sin límite y se anota el riesgo en §6. |
| D-05 | **Quedan fuera de esta HU la cancelación por la persona y la limpieza de la captura abandonada (`capturaVenceEn`, 24 h).** | La 025 R-07 ya cancela la captura viva al crear una nueva y no define endpoint de cancelación. | Se agrega un caso y una tarea para purgar la captura abandonada. |
| D-06 | **Las purgas del sistema (vencimiento y baja de cuenta) se auditan en `panel_auditoria` con `sub: sistema`.** | Evita una colección nueva; la 033 la pensó para acciones del personal pero su documento admite `sub`. | Se crea una colección de auditoría de sistema y su script. |
| D-07 | **Los casos pasan a `CA-NN` y las pruebas a `CA_057_KK`** (como las specs 056, 058 y 059), con una equivalencia a `TC-HU-068-KK` en una columna. | `sdd_lint.py --trace 057` solo reconoce `CA_057_KK`; con `TC-HU-068-KK` el trazado no corre. Aún no hay código, el renombre es solo de texto. | Se mantienen `TC-HU-068-KK` y se acepta que `--trace 057` no sirva. |
| D-08 | **Se conservan los casos 11 (baja de cuenta) y 13 (programación al enviar).** | La ficha no menciona la baja de cuenta; la 025 §2.3 difiere el borrado por baja a esta HU. | Se quitan ambos casos y la baja de cuenta queda sin purga de fotos. |

**Dependencias (no son decisiones):** esta historia no se puede cerrar sin la spec 025 (T-01, T-03, T-04, T-06), la 054 (ejecutor de Identidad) y la 053 (decisión).

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-20
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
