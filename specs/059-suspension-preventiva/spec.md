# Spec 059 — Suspensión preventiva en casos críticos (efecto de suspender una cuenta)

> **Estado:** Borrador · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Jira:** KNOCK-97 (HU-047 · Suspensión preventiva en casos críticos · Épica EP-08 Confianza y seguridad · Sprint 5 · 3 puntos · Capas Back + BD) · **Responsable:** José Guillermo Rangel Hernández
> **Fuentes:** `docs/backlog/HU-047.md`; `docs/referencia/DECISIONES.md` §5 (política de reportes y suspensión); specs 015 R-07, 016 R-02 y R-04 a R-08, 020 R-23, 027 R-07, 033 R-02 y R-06, 037 R-11 y R-13, 039, 043 (§6, T-08), 050, 053 R-20 (REP-05) y 054 R-15.
> **Servicios:** Identidad (`Knock.Identity.Api`, módulos `Safety` y `Sesiones`) y Mercado (`Knock.Market.Api`, consumidor del evento) · **Colecciones:** `usuarios` (campo nuevo), `sesiones`, `reportes`, `panel_auditoria`, `outbox`
> **Skills:** `security-core`, `events-outbox`, `auth-firebase-jwt`, `vertical-slice-api`
> **Nota de numeración:** 059 = «número libre más alto (056) + 3», para no chocar con las specs en borrador de KNOCK-87 y KNOCK-162 que se redactan en paralelo; puede renumerarse antes de fusionar (§15, duda 11).

## 1. Objetivo
Que una cuenta contra la que un moderador decidió un reporte de gravedad `critica` quede suspendida de inmediato y de verdad: estado `suspendida`, todas sus sesiones muertas (no solo la próxima renovación), sus citas próximas avisadas a los clientes y la decisión auditada, sin que un reporte falso o repetido pueda abusar del mecanismo. Esta spec define **el efecto de suspender** (y de levantar la suspensión), la apelación sobre ese efecto y la regla gradual por conteo de advertencias. **No** define quién decide ni la pantalla del panel: eso es la spec 053 (REP-05) con los permisos y el doble factor de la spec 033.

## 2. Historia y alcance
**2.1 Historia**
- **HU-047 (KNOCK-97):** Como cliente, quiero que un técnico reportado por un caso crítico quede suspendido de inmediato, para estar protegido mientras se revisa.

Descripción de la HU: solo para gravedad crítica (violencia, acoso, robo, suplantación, daños graves); se revocan sus sesiones y sus citas próximas se avisan a los clientes con la opción de reportar o elegir otra cotización.

**2.2 Dentro (este repositorio)**
- La operación interna de Identidad «suspender cuenta» y su inversa «reactivar cuenta», con su transacción, auditoría y eventos.
- La revocación inmediata de las sesiones de la app y del panel de la persona suspendida (specs 016 y 033).
- El evento `CuentaSuspendida` hacia Mercado: el técnico deja de ser elegible y buscable (specs 020 R-23 y 027 R-07) y sus clientes con cita próxima reciben aviso.
- La regla de la HU para reportados que son clientes (no se suspenden).
- La apelación sobre la suspensión (en lo que la spec 043 ya define) y la regla gradual por conteo de advertencias (mecanismo; los valores son una duda abierta).
- Pruebas de que el inicio de sesión (015 R-07) y la renovación (016 R-02) rechazan a la cuenta realmente suspendida.

**2.3 Fuera**
- Decidir un reporte, asignar la gravedad, la cola y la pantalla del panel: spec 053 (HU-069) y 033. Esta spec depende de ellas; no las reimplementa.
- Crear reportes y la apelación como ruta: spec 043. Aquí solo se define qué hace la suspensión con ellas.
- Baja definitiva, suspensión temporal de 7 o 30 días elegida a mano y sanción por reportes falsos (DECISIONES §5, paso 4): no están en la HU ni en la 053 (duda 6).
- Pantallas y textos del aviso de suspensión en la app (frontend) y el tipo de aviso concreto (duda 5).
- Cancelar o reasignar visitas confirmadas y qué significa «elegir otra cotización» (duda 5): la HU solo pide el aviso.
- Cambios a las specs 015, 016, 020, 027, 033, 037, 043, 053 o 054: las enmiendas que esta spec necesita se listan en §8 y se piden aparte.

## 3. Requisitos
**Disparo y destinatarios**
- **R-01:** La suspensión preventiva solo se dispara desde la decisión de un moderador sobre un reporte (`POST /api/v1/admin/reportes/{id}/decision`, REP-05, spec 053 R-20) con `gravedad = critica`. Esa ruta llama a la operación interna `Suspender(usuarioId, reporteId)` del módulo `Safety`. No existe ruta pública ni del panel que suspenda por otra vía. Ni la creación de un reporte (REP-01) ni la cantidad de reportes suspenden por sí solas (duda 1: la HU dice «suspensión automática»).
- **R-02:** `usuarioId` sale **siempre** del `reportadoId` del reporte guardado en el servidor, nunca del cuerpo, la ruta ni la consulta. Un identificador de persona enviado en el cuerpo se ignora.
- **R-03:** Si la persona reportada es una cuenta de **cliente** (`esTecnico = false`), la decisión `critica` no la suspende: la cuenta sigue `activa`, sus sesiones no se tocan y el reporte conserva su prioridad alta en la cola del panel (HU-047 CA4). Si la cuenta es técnica (`esTecnico = true`) se suspende. Quién es «técnico» lo decide `usuarios.esTecnico` al momento de la decisión (duda 2: la 053 R-20 escribe «suspensión preventiva de la cuenta» sin esta distinción).

**Transacción de suspender**
- **R-04:** `Suspender` corre en **una sola transacción** de Identidad (spec 003, patrón de bandeja de salida) que: (a) cambia `usuarios.estado` de `activa` a `suspendida` con una operación condicionada al estado vigente; (b) escribe `usuarios.suspension = { tipo: "preventiva", reporteId, desde }`; (c) marca `revocada = true` todas las `sesiones` de la cuenta; (d) marca revocadas las sesiones del panel de ese `sub` si existe cuenta del panel (spec 033); (e) escribe en `outbox` el evento `CuentaSuspendida` y un `sesion_revocada { sid }` por cada sesión de la app que estaba abierta (spec 037 R-13); (f) escribe en `panel_auditoria` el evento `cuenta_suspendida` con `resultado`, `sub` del moderador y `recursoId` = `reporteId`, sin datos de la persona (spec 033 R-23). Si algo falla no queda nada y la decisión responde 503; si es la auditoría, 503 `audit_unavailable` como la 053 R-22.
- **R-05:** `Suspender` es **idempotente por estado**: sobre una cuenta ya `suspendida` no cambia nada, no agrega eventos, no revoca de nuevo ni repite avisos; el reporte adicional se registra en el propio reporte (spec 043). Dos decisiones simultáneas sobre la misma cuenta producen un solo cambio y un solo `CuentaSuspendida`.
- **R-06:** Una cuenta en `eliminacion_programada` (spec 039) también se puede suspender; la suspensión no cancela ni adelanta la eliminación (duda 8).
- **R-07:** El moderador no puede suspender su propia cuenta: si `reportadoId` coincide con el `sub` del moderador, la operación se rechaza sin cambiar nada (código de negocio en inglés y snake_case que se fija al implementar y entra al catálogo).

**Sesiones realmente cerradas**
- **R-08:** Después del commit de R-04, por cada `sid` revocado se escribe en Redis `sesion-cancelada:{sid}` con tiempo de vida de 15 minutos (spec 016 R-06), de modo que el gateway responde 401 al JWT vigente de inmediato (spec 016 R-07). Si Redis falla, la suspensión **no se revierte**: el servicio reintenta, registra una alerta sin datos personales y el JWT ya emitido puede servir como máximo 15 minutos (riesgo ya aceptado en la spec 016); la renovación y el inicio de sesión siguen cerrados por MongoDB (R-09). Es el sentido de «su sesión termina en máximo 15 min» (HU-047 CA2).
- **R-09:** Con la cuenta `suspendida`, `POST /api/v1/auth/firebase` responde 403 `account_suspended` sin tokens (spec 015 R-07) y `POST /api/v1/auth/refresh` responde 401 uniforme (spec 016 R-02, R-08, R-10). Esta spec no cambia esas rutas; las pruebas de CA-02 y CA-07 comprueban que siguen cerrando una suspensión real, incluida una renovación **en vuelo** (la rotación condicionada de la spec 016 R-04 no puede dejar una sesión viva después del commit).
- **R-10:** Mercado deja de tratar al técnico como elegible: al recibir `CuentaSuspendida` marca su perfil de técnico con `estado = suspendido` (campo que ya leen la spec 020 R-23 y la 027 R-07), de modo que no ve ni cotiza solicitudes (404 uniforme) y no aparece en búsquedas. La proyección es idempotente por `eventoId`. Lo ya cotizado por ese técnico no se borra (duda 5).

**Aviso a clientes con cita**
- **R-11:** Al recibir `CuentaSuspendida`, Mercado busca las visitas **próximas** de ese técnico (`confirmada` y aún no iniciada, spec 050) y deja, en la misma transacción del consumidor, un evento de aviso por cliente afectado en su `outbox` hacia Mensajería (spec 037 R-11, con la clave de Mercado). El aviso lleva solo identificadores (destinatario, rol `cliente`, `visitaId`), nunca el motivo, el reporte ni quién reportó. Un cliente con dos citas afectadas recibe un aviso por cita. El **tipo de aviso** no existe en el catálogo cerrado de la spec 037 (duda 5). La app ofrece «reportar» o «elegir otra cotización» (frontend; qué endpoint respalda lo segundo no está definido).
- **R-12:** Las visitas no se cancelan automáticamente por la suspensión (la HU solo pide avisar); queda abierto si el técnico suspendido puede completar una visita en curso (duda 5).

**Persona suspendida y apelación**
- **R-13:** La persona suspendida no tiene sesión, así que no recibe push (sus dispositivos se borran con `sesion_revocada`); al intentar entrar recibe 403 `account_suspended` (R-09). El texto que ve la app lo define el frontend; nunca incluye quién la reportó ni el texto del reporte (spec 043 R-22).
- **R-14:** La apelación de la decisión que causó la suspensión es la de la spec 043 (REP-03, una sola vez, a otra persona del panel). **Mientras la apelación está pendiente (`en_apelacion`) la suspensión se mantiene.** Hoy REP-03 exige sesión de la persona afectada y una persona suspendida no puede tener sesión: la ruta por la que apela no está definida (duda 3) y esta spec no la inventa.

**Reactivar y regla gradual**
- **R-15:** La operación interna `Reactivar(usuarioId, reporteId)` (la llama quien resuelva la apelación o desestime el reporte, duda 4) cambia `suspendida` a `activa` con una operación condicionada, borra `usuarios.suspension` (la auditoría se conserva), emite `CuentaReactivada` (Mercado restaura el perfil del técnico, idempotente por `eventoId`) y escribe `cuenta_reactivada` en `panel_auditoria`, todo en una transacción. **No restaura sesiones**: la persona vuelve a iniciar sesión. Sobre una cuenta que no está `suspendida` se rechaza sin cambios (código de negocio que se fija al implementar). Exige el mismo permiso y doble factor que la decisión.
- **R-16:** **Regla gradual.** Al registrarse una decisión de advertencia (`media`, spec 053 R-20) sobre una cuenta, se cuentan las advertencias **decididas por un moderador y vigentes** (las desestimadas o revertidas por apelación no cuentan, y un reporte sin decidir nunca cuenta); si el conteo alcanza `Suspension:UmbralAdvertencias`, se aplica la misma transacción de R-04 con `suspension.tipo = "gradual"`. El umbral y la vigencia de una advertencia **no tienen valor en esta spec** (duda 6): si la configuración falta, la regla gradual queda apagada (no hay valor por omisión) y la validación de configuración al arrancar rechaza un umbral menor que 1.
- **R-17:** La cantidad de reportes **abiertos** o de personas que reportan nunca suspende ni cuenta para R-16: solo cuentan decisiones de moderación (control contra reportes en masa, §9).

**Transversales**
- **R-18:** Los eventos y avisos de esta spec llevan solo identificadores y tipo, nunca teléfono, dirección, nombre ni texto de reporte; los registros llevan tipo de evento, identificadores opacos y resultado (spec 011). Los errores usan ProblemDetails con `correlationId` y `traceId`.
- **R-19:** Los eventos se entregan al menos una vez por la bandeja de salida (spec 054); los consumidores son idempotentes por `eventoId`. No se usa `Idempotency-Key` (spec 020): la repetición se controla con la regla de estado de R-05 y R-15.
- **R-20:** No se agregan rutas públicas ni del panel; REP-05 conserva su límite de peticiones y su permiso `reports:resolve` con doble verificación (spec 033). No se agregan paquetes.

## 4. Criterios de aceptación
La columna «Caso HU» de §4.1 vincula cada criterio con el caso TC de la ficha HU-047; el rastreo con `sdd_lint.py --trace 059` usa los criterios `CA-NN` de las specs numeradas.

- **CA-01** (HU-047 CA1): Dado un reporte contra un técnico activo con tres sesiones y un moderador con `reports:resolve` y doble verificación, cuando decide `critica`, entonces el técnico queda `suspendida` con `suspension.reporteId`, sus tres sesiones quedan `revocadas`, hay un `CuentaSuspendida` y tres `sesion_revocada` en `outbox`, y un registro `cuenta_suspendida` en `panel_auditoria`.
- **CA-02** (HU-047 CA2): Dado un técnico suspendido con un JWT todavía vigente, cuando llama una ruta protegida, entonces el gateway responde 401 por `sesion-cancelada:{sid}`; cuando intenta renovar, 401 uniforme; cuando intenta iniciar sesión, 403 `account_suspended`; y en Mercado no ve ni cotiza solicitudes (404) ni aparece en búsquedas.
- **CA-03** (HU-047 CA3): Dado un técnico con dos citas próximas de dos clientes y otra de uno de ellos, cuando se suspende, entonces cada cliente recibe un aviso por cita afectada, sin motivo, reporte ni datos de quien reportó, y los clientes sin cita con ese técnico no reciben nada.
- **CA-04** (HU-047 CA4): Dada una decisión `critica` sobre una cuenta que no es técnica, cuando se aplica, entonces la cuenta sigue `activa`, sus sesiones siguen vigentes, no hay `CuentaSuspendida` y el reporte conserva su prioridad alta.
- **CA-05:** Dada una falla al escribir `outbox` o `panel_auditoria` durante la suspensión, cuando se decide, entonces responde 503 (`audit_unavailable` si fue la auditoría) y la cuenta sigue `activa` con sus sesiones sin revocar y sin eventos.
- **CA-06:** Dada una cuenta ya suspendida, cuando llega otra decisión `critica` (o la misma repetida), entonces no hay segundo `CuentaSuspendida`, ni segundos avisos, ni nueva revocación; y con dos decisiones simultáneas sobre la misma cuenta hay un solo cambio y un solo evento.
- **CA-07:** Dada una renovación de sesión que corre a la vez que la suspensión, cuando ambas terminan, entonces no queda ninguna sesión sin revocar ni se entrega un token de renovación válido después del commit.
- **CA-08:** Dado un técnico suspendido que también tiene cuenta del panel con una sesión abierta, cuando se suspende, entonces la sesión del panel queda revocada y no puede renovarse.
- **CA-09:** Dado un moderador sin `reports:resolve`, sin doble verificación o sin token, cuando intenta decidir un reporte `critica`, entonces responde 403 o 401 y la cuenta reportada no cambia en nada.
- **CA-10:** Dado un cuerpo de decisión que trae el identificador de otra persona, cuando se decide, entonces se suspende solo el `reportadoId` del reporte guardado y el identificador del cuerpo se ignora.
- **CA-11:** Dado un moderador que es la persona reportada, cuando decide `critica` sobre su propio reporte, entonces la operación se rechaza y su cuenta y sus sesiones no cambian.
- **CA-12:** Dados veinte reportes abiertos y sin decidir contra un técnico por veinte personas distintas, cuando se abren más o pasa el tiempo, entonces el técnico sigue `activa` y ningún conteo lo suspende.
- **CA-13:** Dado `Suspension:UmbralAdvertencias = 3` en la prueba, cuando se deciden dos advertencias vigentes y una desestimada, entonces no se suspende; al decidir la tercera vigente, entonces se suspende con `suspension.tipo = "gradual"` por la misma transacción que CA-01; sin la configuración la regla no se aplica, y la validación de arranque rechaza un umbral menor que 1.
- **CA-14:** Dada una cuenta suspendida y una resolución que la reactiva, cuando se aplica `Reactivar`, entonces queda `activa`, se emite `CuentaReactivada`, Mercado restaura el perfil del técnico, hay un registro `cuenta_reactivada`, y las sesiones anteriores siguen revocadas (hay que iniciar sesión de nuevo).
- **CA-15:** Dada una cuenta no suspendida, cuando se llama `Reactivar`, entonces se rechaza sin cambiar nada ni emitir evento.
- **CA-16:** Dado el mismo evento `CuentaSuspendida` o `CuentaReactivada` entregado dos veces a Mercado, cuando lo procesa, entonces el perfil y los avisos quedan como con una sola entrega.
- **CA-17:** Dada una suspensión completa, cuando se inspeccionan eventos, avisos, auditoría, registros y respuestas, entonces solo hay identificadores y tipos: no hay teléfono, dirección, nombre, texto del reporte ni la identidad de quien reportó, y la persona suspendida no puede conocerla por ninguna respuesta.
- **CA-18:** Dado Redis caído al cancelar los `sid`, cuando se suspende, entonces la cuenta queda `suspendida`, se registra la alerta, se reintenta la cancelación y la renovación y el inicio de sesión ya responden como en CA-02.
- **CA-19:** Dado un reporte `critica` ya suspendido con una apelación presentada (`en_apelacion`), cuando se consulta la cuenta, entonces sigue `suspendida` hasta que se aplique `Reactivar`.

**4.1 Criterio → prueba prevista**

| Criterio | Caso HU | Prueba prevista |
|---|---|---|
| CA-01 | TC-HU-047-01 | `IntegrationTest/Spec059SuspensionTests.Decision_Critica_Suspende_Tecnico_Y_Revoca_Sesiones_CA_059_01` |
| CA-02 | TC-HU-047-02 | `IntegrationTest/Spec059SuspensionTests.Suspendido_No_Entra_Ni_Renueva_Ni_Usa_Jwt_CA_059_02` y `IntegrationTest/Spec059MarketTests.Suspendido_No_Ve_Ni_Cotiza_Ni_Aparece_CA_059_02` |
| CA-03 | TC-HU-047-03 | `IntegrationTest/Spec059MarketTests.Avisa_A_Clientes_Con_Cita_Proxima_CA_059_03` |
| CA-04 | TC-HU-047-04 | `IntegrationTest/Spec059SuspensionTests.Reportado_Cliente_No_Se_Suspende_CA_059_04` |
| CA-05 | — | `IntegrationTest/Spec059SuspensionTests.Falla_De_Outbox_O_Auditoria_No_Cambia_Nada_CA_059_05` |
| CA-06 | — | `IntegrationTest/Spec059SuspensionTests.Repetida_Y_Simultanea_No_Duplica_CA_059_06` |
| CA-07 | — | `IntegrationTest/Spec059SuspensionTests.Renovacion_En_Vuelo_No_Deja_Sesion_Viva_CA_059_07` |
| CA-08 | — | `IntegrationTest/Spec059SuspensionTests.Revoca_Sesion_Del_Panel_CA_059_08` |
| CA-09 | — | `SecurityTest/Spec059SeguridadTests.Sin_Permiso_O_Sin_Doble_Factor_No_Suspende_CA_059_09` |
| CA-10 | — | `SecurityTest/Spec059SeguridadTests.Identificador_Ajeno_En_Cuerpo_Se_Ignora_CA_059_10` |
| CA-11 | — | `SecurityTest/Spec059SeguridadTests.Moderador_No_Se_Suspende_A_Si_Mismo_CA_059_11` |
| CA-12 | — | `IntegrationTest/Spec059SuspensionTests.Reportes_Sin_Decidir_No_Suspenden_CA_059_12` |
| CA-13 | — | `UnitTest/Spec059GradualRuleTests.Umbral_De_Advertencias_Vigentes_CA_059_13` y `IntegrationTest/Spec059SuspensionTests.Gradual_Suspende_Al_Alcanzar_Umbral_CA_059_13` |
| CA-14 | — | `IntegrationTest/Spec059SuspensionTests.Reactivar_Restaura_Sin_Sesiones_CA_059_14` |
| CA-15 | — | `UnitTest/Spec059SuspensionDomainTests.Reactivar_No_Suspendida_Se_Rechaza_CA_059_15` |
| CA-16 | — | `IntegrationTest/Spec059MarketTests.Evento_Repetido_Es_Idempotente_CA_059_16` |
| CA-17 | — | `SecurityTest/Spec059PrivacidadTests.Sin_Datos_Personales_Ni_Reportante_CA_059_17` |
| CA-18 | — | `IntegrationTest/Spec059SuspensionTests.Redis_Caido_No_Revierte_Y_Reintenta_CA_059_18` |
| CA-19 | — | `IntegrationTest/Spec059SuspensionTests.Apelacion_Pendiente_Mantiene_Suspension_CA_059_19` |

## 5. Contrato
**No hay rutas públicas nuevas.** Cambia el comportamiento de rutas existentes y se agregan dos eventos internos y dos operaciones internas.

| Elemento | Forma | Dueña |
|---|---|---|
| REP-05 `POST /api/v1/admin/reportes/{id}/decision` | Con `gravedad = critica` sobre un técnico, además de lo que ya define la 053, aplica `Suspender` (R-01 a R-07). Respuestas ya definidas (200; 400; 401; 403; 404) más 503 `audit_unavailable` | spec 053 (esta spec define el efecto que invoca) |
| `POST /api/v1/auth/firebase` | 403 `account_suspended` (sin cambio) | spec 015 |
| `POST /api/v1/auth/refresh` | 401 uniforme (sin cambio) | spec 016 |
| Operación interna `Suspender(usuarioId, reporteId)` | Módulo `Safety`, expuesta a Identidad como interfaz de módulo; no es una ruta HTTP | esta spec |
| Operación interna `Reactivar(usuarioId, reporteId)` | Igual | esta spec |
| Evento `CuentaSuspendida` | `{ eventoId, usuarioId, reporteId, desde }`, solo identificadores. Productor: Identidad. Consumidor: Mercado (`POST /api/v1/interno/eventos`, firma con `interno-key-identity`, spec 054 R-15) | esta spec; el catálogo de `tipo` de `outbox` de la 054 debe ampliarse (§8) |
| Evento `CuentaReactivada` | `{ eventoId, usuarioId, desde }`. Mismo canal | igual |
| `sesion_revocada { sid }` | Una por sesión de la app revocada (sin cambio de forma) | spec 037 R-13 |
| Aviso a clientes con cita | `{ eventoId, tipo, destinatarioId, rol: "cliente", recurso: visitaId }` de Mercado a Mensajería (spec 037 R-11). **El `tipo` queda por definir (duda 5)** | spec 037 (su catálogo se amplía) |
| Auditoría | `panel_auditoria`: `cuenta_suspendida`, `cuenta_reactivada` | spec 033 R-23 |

**Códigos de error de negocio nuevos** (se fijan al implementar, en inglés y snake_case, y entran al catálogo): rechazo de suspender la propia cuenta (R-07) y rechazo de reactivar una cuenta que no está suspendida (R-15). `docs/events.md` documenta los dos eventos con productor y consumidor.

## 6. Modelo de datos
- **`usuarios`** (Identidad): subdocumento opcional `suspension` = `{ tipo ("preventiva" | "gradual"), reporteId (ObjectId), desde (fecha UTC asignada por el servidor) }`, presente solo mientras `estado = suspendida`. `estado` ya admite `suspendida` (spec 015 R-07). El script de `db/` documenta el campo; no agrega índices (no hay consulta por `suspension`). La ficha menciona `version_seguridad`: no se agrega porque la cancelación por `sid` (spec 016) ya invalida los JWT; ver duda 9.
- **`sesiones`**: sin cambios de forma; `revocada = true` dentro de la transacción de R-04.
- **`panel_auditoria`**: dos valores nuevos de `evento` (`cuenta_suspendida`, `cuenta_reactivada`); sin datos de la persona.
- **`outbox`** (Identidad y Mercado): valores nuevos de `tipo` (`CuentaSuspendida`, `CuentaReactivada` y el aviso de la duda 5); cargas solo con identificadores (spec 054 R-02).
- **Perfil del técnico en Mercado** (colección `tecnicos`, que aún no tiene spec dueña, spec 027 §15): `estado` pasa a `suspendido` y vuelve a `activo`. Esta spec solo escribe `estado`, que ya leen las specs 020 y 027.
- **Sin índice nuevo:** la regla gradual (R-16) cuenta decisiones con el índice `reportes.reportadoId` de la spec 043 §6.

## 7. Arquitectura y componentes
- Módulo `Safety` de `Knock.Identity.Api` (spec 043): servicio de dominio `AccountSuspension` con `Suspender` y `Reactivar`, que usa `Sesiones` (specs 015 y 016) y la auditoría del panel (spec 033) por interfaces de módulo, no por sus colecciones. La transición de estado vive en el dominio (`usuarios` ya define `Suspended`).
- REP-05 (spec 053) llama a `Suspender`; el controller de la 053 no escribe en `usuarios`.
- Mercado: consumidor en `/api/v1/interno/eventos` (spec 054 R-15), proyección del estado del técnico y generación de avisos a clientes dentro de su transacción y su `outbox`.
- Redis: escritura de `sesion-cancelada:{sid}` fuera de la transacción de MongoDB, con reintento.
- Ninguna lectura cruzada de colecciones entre servicios (spec 001 R-03): Mercado no lee `usuarios` ni `reportes`.

## 8. Dependencias
- **Spec 053 (REP-05): hoy no implementada.** Esta spec define el efecto; el disparo llega con la 053.
- **Spec 033 (panel, permisos y doble factor):** permiso `reports:resolve` con doble verificación y `panel_auditoria`. Dependencia explícita: no se redefine.
- **Spec 043:** reportes, apelación única y el conteo «de reportes y advertencias de una cuenta para la suspensión gradual» (su tarea T-08 sigue pendiente).
- **Specs 015, 016 (R-02, R-05 a R-08) y 039:** estado `suspendida`, sesiones y `sesion_revocada`; la 015 y la 016 aún deben emitir `sesion_revocada` (Memoria).
- **Spec 054:** su catálogo cerrado de `tipo` de `outbox` debe ampliarse con `CuentaSuspendida` y `CuentaReactivada` (enmienda a pedir).
- **Spec 037:** su catálogo cerrado de avisos requiere un tipo nuevo para «cita afectada por suspensión» (enmienda a pedir; duda 5).
- **Specs 020, 027 y 050:** elegibilidad del técnico y visitas próximas; la colección `tecnicos` y las visitas de Mercado todavía no están completas.
- **Prerrequisito de la 043:** mientras la moderación de la spec 031 no esté en Identidad, crear un reporte responde 503 (Memoria): sin reportes no hay suspensión que probar de extremo a extremo.

## 9. Amenazas y controles
| Amenaza (STRIDE) | Control | Casos que lo prueban |
|---|---|---|
| **Suspensión abusiva con reportes falsos o en masa** (T/D: dañar a un técnico) | Solo la decisión de un moderador con permiso y doble factor dispara (R-01); la cantidad u origen de reportes abiertos nunca cuenta (R-17); la regla gradual solo cuenta decisiones vigentes (R-16); apelación única que mantiene la suspensión hasta que otra persona del panel resuelva (R-14, spec 043) | CA-09, CA-12, CA-13, CA-19 |
| **IDOR y asignación masiva** (S/E) | La cuenta suspendida sale del `reportadoId` guardado; el cuerpo no acepta identificadores de persona (R-02) | CA-10 |
| **Elevación: quien decide se protege o se sanciona a sí mismo** | Rechazo de la propia cuenta (R-07); permiso `reports:resolve` y doble factor | CA-09, CA-11 |
| **Repetición y concurrencia** (D/T) | Operación condicionada al estado e idempotente (R-05, R-15); eventos idempotentes por `eventoId` (R-19) | CA-06, CA-15, CA-16 |
| **Sesiones vivas después de suspender** (E) | Revocación en la misma transacción, `sesion_revocada`, cancelación en Redis y cierre de inicio de sesión y renovación (R-04, R-08, R-09); sesiones del panel incluidas (R-04 d) | CA-01, CA-02, CA-07, CA-08, CA-18 |
| **Fuga de identidad de quien reportó o de datos personales** (I) | Eventos, avisos, auditoría y errores solo con identificadores; nunca el reportante ni el texto (R-13, R-18) | CA-03, CA-17 |
| **Repudio de la decisión** (R) | Auditoría `cuenta_suspendida` y `cuenta_reactivada` con el `sub` del moderador, dentro de la transacción (R-04 f, R-15); si no se puede escribir, la acción no se ejecuta | CA-01, CA-05 |
| **Estado inconsistente entre servicios** (T) | Bandeja de salida en la misma transacción (spec 054 R-02), consumidores idempotentes y entrega al menos una vez | CA-05, CA-16 |
| **Avisos en cascada a clientes** (D) | Un aviso por cita afectada, límite por persona de la spec 037 R-12 y sin duplicados por `eventoId` | CA-03, CA-16 |
| **Reactivación indebida** (E) | Mismo permiso y doble factor; solo desde `suspendida`; las sesiones no se restauran (R-15) | CA-14, CA-15 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_059_NN` en el nombre (para `python scripts/sdd_lint.py --trace 059`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Transición de estado de `Suspender` y `Reactivar`, regla gradual (conteo, vigencia, umbral apagado) y validación de la configuración |
| Arquitectura | Sí | `Safety` no escribe en `sesiones` ni en `panel_auditoria` por sus colecciones; Mercado no lee colecciones de Identidad |
| Integración | Sí | Identidad y Mercado con MongoDB y Redis en contenedor (Testcontainers): transacción, `outbox`, consumidor, avisos, inicio de sesión, renovación y gateway |
| Seguridad negativa | Sí | 401, 403, identificador ajeno en el cuerpo, autosuspensión y privacidad de eventos y registros (CA-09, CA-10, CA-11, CA-17) |
| Base de datos | Sí | Documento `usuarios` con y sin `suspension`; condición de estado de la operación atómica; sin índices nuevos |
| Contrato | Sí | `docs/events.md` y OpenAPI sin cambios de rutas; los eventos nuevos se validan contra su esquema |
| Mutación | Sí | Meta del módulo según la spec 008, sobre `Safety` (dominio de suspensión y regla gradual) |
| Rendimiento | No | Sin ruta nueva; el efecto corre en una transacción pequeña |
| Caos | No | Cubierto por CA-05 y CA-18 (falla de `outbox`, auditoría y Redis) |

## 11. Escenarios
- **Caso crítico:** una clienta reporta a un técnico por acoso → el moderador revisa y decide `critica` → el técnico queda suspendido, sus sesiones mueren, los clientes con cita próxima reciben aviso y la decisión queda auditada.
- **Reporte contra un cliente:** un técnico reporta a un cliente por un caso crítico → no se suspende al cliente y el reporte queda con prioridad alta para el panel.
- **Reportes en masa:** varias cuentas reportan al mismo técnico → ninguna cantidad de reportes lo suspende; solo la decisión del moderador.
- **Apelación:** el técnico suspendido apela → mientras otra persona del panel revisa, la suspensión se mantiene → si se acepta, se reactiva y vuelve a iniciar sesión.

## 12. Definición de terminado
- Criterios CA-01 a CA-19 cubiertos por pruebas en verde y `sdd_lint --trace 059` sin faltantes.
- `AGENTS.md §5` completo: build sin advertencias, `dotnet format`, pruebas de las suites afectadas, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check` y Semgrep.
- `docs/events.md` con `CuentaSuspendida` y `CuentaReactivada`; `docs/endpoints.md` con la nota de efecto en REP-05.
- `security-reviewer` LIBERA, `traceability-clerk` sin drift y `Memoria.md` al día.

## 13. Decisiones
- La suspensión es una **operación interna de Identidad** y no una ruta: ninguna persona puede invocarla por separado y toda suspensión pasa por la autorización y la auditoría de la decisión del moderador (specs 033 y 053).
- Se reutiliza el mecanismo de sesiones existente (revocar en MongoDB, `sesion-cancelada:{sid}` en Redis y rechazo en inicio de sesión y renovación) en vez de un contador `version_seguridad` nuevo (reutilizar antes de crear).
- Mercado se entera por evento y mantiene su proyección del estado del técnico (spec 001 R-03); no lee la base de Identidad.
- Cada criterio de la ficha HU-047 tiene su `CA` (CA-01 a CA-04); los demás salen de las amenazas de §9.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se numera como 059 por la regla pedida de «el más alto libre + 3» (el más alto visto en las ramas es 056). Hay que confirmarlo (duda 11).
- **D-02:** Los casos se escriben como `CA-NN` y se mapean a los `TC-HU-047-0N` de la ficha, porque las specs numeradas se rastrean por `CA_NNN_KK` (specs 043 y 053).
- **D-03:** Se toma el permiso `reports:resolve` de la spec 033 R-02 (la lista de permisos de la 033 es cerrada).
- **D-04:** Se incluyen las sesiones del panel de la persona suspendida en la revocación (R-04 d) porque «sesiones vivas tras suspender» es una amenaza directa; la 033 R-06 solo cierra el inicio de sesión nuevo.

## 15. Decisiones pendientes del Tech Lead (sí/no)
Cada una se contesta sí o no sobre la recomendación del autor; responder «sí» la deja decidida tal como está escrita y «no» obliga a ajustar la spec antes de aprobar. Ninguna se inventó como bloqueo: se anota qué parte del trabajo depende de ella.

| N.º | Decisión (recomendación del autor: sí) | Contexto y alternativa | Si es «no» |
|---|---|---|---|
| 1 | **La suspensión se dispara por la decisión de un moderador (`critica`, spec 053 R-20), no de forma automática por el reporte.** | La HU dice «automática», pero la gravedad la asigna la moderación (043) y la 053 deriva la suspensión de esa decisión. Un disparo automático abriría la puerta a reportes falsos o en masa (§9). | Se redefine el disparo y el control de §9 (por ejemplo, automática solo por motivo de peligro, spec 043 R-20). |
| 2 | **Contra un cliente no se suspende: un reporte `critica` contra un cliente entra con prioridad alta y se enmienda la spec 053 R-20 para distinguirlo.** | La HU CA4 lo pide; la 053 dice «la cuenta» sin distinguir. | La 053 no se enmienda y se suspende también al cliente (contradice la HU). |
| 3 | **La apelación de una persona suspendida se resuelve en una spec propia (ruta pública que prueba el control del número con el código SMS); esta spec no la incluye.** | La 043 REP-03 exige sesión y la suspensión las revoca; sin canal el debido proceso (DECISIONES §5) no es alcanzable. | Se define aquí el canal (soporte manual u otro) y se agregan casos. |
| 4 | **`Reactivar` solo lo invoca la resolución de la apelación del panel (permiso `reports:resolve` y doble factor); no se agrega ruta aquí.** | No hay hoy una ruta que desestime un reporte ya decidido ni que resuelva una apelación. | Se agrega la ruta de reactivación a esta spec (más puntos). |
| 5 | **Esta spec solo avisa a los clientes con cita próxima (tipo nuevo en la spec 037); no cancela visitas, ni toca cotizaciones ni conversaciones.** | Qué pasa con una visita `confirmada` o en curso depende de las specs 021, 027 y 050. | Se define aquí la cancelación de visitas y sus efectos. |
| 6 | **La regla gradual queda apagada hasta que se fijen sus valores: sin `UmbralAdvertencias` configurado no se aplica; la spec solo deja el mecanismo.** | DECISIONES §5 habla de strikes y suspensión de 7 o 30 días sin umbrales, vigencia ni quién la levanta. | El Tech Lead da los valores (umbral, vigencia de una advertencia, duración, quién levanta) y se escriben en la spec. |
| 7 | **El contador de advertencias lo implementa la spec 043 (T-08); esta spec solo lo lee de `reportes`.** | T-08 de la 043 sigue pendiente. | Lo implementa esta spec y se mueve la tarea. |
| 8 | **Suspender una cuenta con eliminación programada (spec 039) pausa el plazo de gracia.** | Sin pausa, la eliminación podría borrar evidencia y evadir la revisión. | El plazo sigue corriendo y se conserva la evidencia por otro medio. |
| 9 | **No se agrega `version_seguridad`.** | La cancelación por `sid` en Redis ya cubre la revocación (§13). | Se agrega para invalidar JWT sin Redis. |
| 10 | **El estado de suspensión visible en el panel queda fuera de esta spec** (ticket de la 053). | La 053 no tiene ruta para ello. | Se agrega a esta spec el campo y la ruta de lectura. |
| 11 | **Se mantiene la numeración 059.** | Ya está en `sprint-05` con su fila del índice (PR #199); renumerar cambiaría las pruebas `CA_059_NN`. También confirma D-01 de §14. | Se renumera antes de fusionar. |
| 12 | **Se acepta que, si Redis se pierde, un JWT pueda servir hasta 15 minutos (riesgo ya aceptado en la spec 016) también para una suspensión crítica.** | Alternativa: que el gateway consulte un campo de la cuenta en cada petición (costo y latencia). | El gateway consulta el estado de la cuenta y se agrega un caso. |
| 13 | **Se confirman D-02, D-03 y D-04 de §14** (casos `CA-NN`, permiso `reports:resolve`, sesiones del panel incluidas en la revocación). | Son decisiones tomadas por el autor sin consulta. | Se indica cuál cambiar. |

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Implementada (tras el merge)
