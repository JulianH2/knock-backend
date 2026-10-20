# Plan de adaptación de Knock a los lineamientos del curso — checklist

Fecha: 2026-10-20 · Responsable: José Julián (líder) · Estado: **vigente; se va marcando conforme se cumple cada punto.** Los códigos de tarea marcados con † en las matrices son tareas planificadas que se crean en Jira (Olas 1 a 3).

Cómo usarlo: ir de arriba abajo. Cada punto tiene casilla `[ ]`. Los puntos marcados **DECIDIR** esperan una decisión de José Julián. Los puntos marcados **(confirmar)** son datos que deben validarse (spike o portal del proveedor) antes de darse por ciertos.

Fechas clave: hoy 20-oct (clase) · Sprint 1 termina 23-oct · Sprint 2 26–30 oct · Sprint 3 2–6 nov · Sprint 4 9–13 nov · Sprint 5 17–20 nov · Sprint 6 23–27 nov · **congelar el 1-dic** · **evaluación 2-dic** (después es remedial).

---

## 0. Decisiones ya tomadas (registrar por escrito, sección A)

- [x] Arquitectura: **microservicios**, cada uno con su propia base de datos, más una puerta de entrada (gateway).
- [x] Servicios (4 + gateway): **Identidad y confianza** (Identity, Trust) · **Mensajería y notificaciones** (Messaging, Notifications) · **Mercado** (Catalog, Requests, Reputation) · **Asistente** (Assistant: IA generativa y ML).
- [x] Base de datos: **MongoDB en Azure DocumentDB** (antes Cosmos DB for MongoDB vCore), free tier; respaldo: MongoDB Atlas. El spike confirma disponibilidad en la suscripción de estudiante y el comportamiento **(confirmar)**.
- [x] IaC: **Terraform** (reemplaza a Bicep; TT-AZ-02 todavía no empezó).
- [x] Nube: **Azure**, con **Azure for Students** para el costo.
- [x] Firebase: Auth por teléfono + FCM, con plan **Blaze** (tarjeta vinculada, presupuesto y alertas, política de regiones de SMS solo +52).
- [x] Redis: complemento opcional, no base principal.
- [x] Google Play: dos cuentas personales creadas después del 13-nov-2023 → prueba cerrada con 12+ testers durante 14 días; 20 testers de la universidad.
- [x] Roles: Erick = frontend (app Flutter y panel); Guillermo y Alexis = backend (Alexis conserva base de datos y ML); José Julián = líder, apoyo a todos y **revisor/aprobador final**; Erick = revisor de los PR de backend.
- [x] Asignación **por carga** para el panel de administración y la landing (ver sección B).

---

## A. Documentación y decisiones (PR en `knock-backend` y `knock-frontend`)

- [ ] A1. Decisión de arquitectura: microservicios, fronteras de los 4 servicios, gateway, comunicación (REST/gRPC) y eventos → `docs/DECISION_TECNICA.md` (reemplaza la sección «monolito modular») y `Memoria.md`.
- [ ] A2. Decisión de base de datos: MongoDB en Azure DocumentDB, por qué, qué se descarta (PostgreSQL+PostGIS, Firestore, Atlas como respaldo), costos y límites **(confirmar)**.
- [ ] A3. Decisión de IaC: Terraform en lugar de Bicep, estado remoto, `terraform destroy` como paso del pipeline.
- [ ] A4. Decisión de Firebase y Azure: por qué se acepta Firebase Auth/FCM con backend en Azure (FCM es inevitable en Android; Auth se consume como cliente), plan Blaze y controles de gasto.
- [ ] A5. Decisión de roles y carga (reemplaza la de KNOCK-33): nuevos roles, tabla «Quién revisa a quién» y regla de asignación por carga.
- [ ] A6. **Matriz de versionamiento** (formato de versión por servicio, app y landing; regla de incremento; cuándo se aplica) → documento nuevo; antes de cualquier despliegue.
- [ ] A7. **Matriz de cumplimiento** de los 19 lineamientos del profe (requisito → tarea → spec → prueba → evidencia). Se actualiza cada sprint.
- [ ] A8. Diagrama de servicios y contratos entre ellos (reemplazar/ampliar `docs/architecture/modules.mmd`).
- [ ] A9. Política de costos: qué corre 24/7, qué escala a cero, cuándo hay 2 réplicas fijas (ventana de evaluación 25-nov a 5-dic) y presupuesto de Azure for Students.
- [ ] A10. Aviso de privacidad y manejo de datos (texto fuente para la landing y la app).
- [ ] A11. Preparar el texto de cambios para los archivos **protegidos** (`AGENTS.md`, `constitution.md`): quitan «monolito», PostgreSQL y EF Core. **Los edita José Julián**; las skills y `sync_agents.py --check` se actualizan después.
- [ ] A12. `README` de cada repo y guía de desarrollo local (compose con los servicios y MongoDB).

## B. Flujo de equipo y reglas

- [ ] B1. Actualizar `docs/FLUJO_EQUIPO.md`: nuevos roles y tabla de revisión. Propuesta: PR de backend → revisa Erick (comentario) y el otro backend; PR de frontend → revisa José Julián o un backend; aprobación y merge final: José Julián.
- [ ] B2. **Regla de asignación por carga (propuesta, ver opinión en la respuesta):**
  - Las tareas «libres» llevan la etiqueta `resp-libre` y se toman al inicio del sprint, en la planeación, mirando la tabla de carga.
  - Quien toma una tarea se la asigna en Jira **antes de empezar**.
  - Límite de trabajo en curso por persona (máx. 2 tareas En curso).
  - A mitad de sprint, lo que siga libre lo asigna el líder.
  - **No** van al pool las tareas con fecha dura (landing y URL de privacidad para Play): llevan dueño y fecha desde ya.
- [ ] B3. Actualizar `CODEOWNERS` (revisores por servicio) y `CONTRIBUTING.md` (ramas `<tipo>/KNOCK-N-desc` se mantienen).
- [ ] B4. Plantilla de PR: agregar el campo «Servicio afectado».
- [ ] B5. Registrar en Jira los nuevos responsables y etiquetas (`resp-*`, `servicio-*`, `resp-libre`).
- [ ] B6. Daily scrum: definir hora, 3 preguntas y dónde se anota (propuesta `docs/daily/SPRINT-01.md`). **DECIDIR.**

## C. Repos, CI/CD, seguridad y herramientas (adaptar)

- [ ] C1. Estructura del repo `knock-backend`: `src/Knock.Gateway`, `src/Knock.Identity.Api`, `src/Knock.Messaging.Api`, `src/Knock.Market.Api`, `src/Knock.Assistant.Api`, biblioteca compartida `Knock.Contracts` (solo contratos y tipos, sin lógica de negocio).
- [ ] C2. Un `Dockerfile` por servicio y compose local para todos.
- [ ] C3. `ci-pr.yml`: detección de cambios por servicio, build y pruebas por servicio, cobertura por servicio (umbrales 60%/50% **sin bajar**). Quitar sqlfluff y los pasos de PostgreSQL.
- [ ] C4. Pruebas de integración con Testcontainers para MongoDB (reemplaza PostgreSQL+PostGIS) **(confirmar compatibilidad con DocumentDB)**.
- [ ] C5. `dependabot.yml`: quitar Npgsql, EF Core y NetTopologySuite; agregar driver de MongoDB, Terraform y Docker por servicio.
- [ ] C6. Escáneres: Checkov y Trivy sobre Terraform y cada imagen; Semgrep (`knock_guard`) y `check_exceptions.py` revisados por los cambios de EF Core; revisar EXC-001/002/003 (alcance y vencimiento).
- [ ] C7. `codemap.py` y `sdd_lint.py`: módulos → servicios; plantillas de spec con campo «Servicio».
- [ ] C8. Secretos y claves: Key Vault para cadenas de conexión de MongoDB, claves de Firebase y claves entre servicios. Nada en el repo (los repos son públicos).
- [ ] C9. Autenticación entre servicios (gateway valida JWT; servicios internos confían solo en el gateway o validan el JWT).
- [ ] C10. Observabilidad: Application Insights con trazas distribuidas entre servicios.
- [ ] C11. Dar acceso y presupuesto: suscripción Azure for Students, roles mínimos (TT-AZ-01) y alerta de costo.
- [ ] C12. Firebase: Blaze, política de regiones de SMS (+52), presupuesto y alertas, App Check **(confirmar)**; números de prueba solo para el CI.
- [ ] C13. Google Play: cuenta, AAB firmado, pista cerrada, 20 testers inscritos y sin bajas durante 14 días; URL de privacidad (landing) antes de pedir producción.
- [ ] C14. GitHub: repos en una sola cuenta (hoy `JulianH2`; **DECIDIR** si se transfieren a la cuenta del pack de estudiantes; deben quedar los dos en la misma).

## D. Tareas técnicas existentes: qué pasa con cada una

Acciones: **Mantener**, **Modificar**, **Reemplazar** (queda como terminada, la sustituye otra), **Mover** (cambia de sprint). Ninguna se borra de Jira.

| Tarea | Acción | Qué cambia |
|---|---|---|
| TT-GES-01, TT-REPO-01/02/03, TT-STD-01, TT-SDD-01, TT-PRU-03, TT-GES-02, TT-MOB-01 | Mantener | Terminadas |
| TT-API-01 | Modificar (nota) | Terminada como monolito; base para el esqueleto de servicios (TT-ARQ-01) |
| TT-BD-01 | Reemplazar | Terminada con PostgreSQL; la sustituye TT-BD-06 |
| TT-BD-02 | Reemplazar | Catálogo con PostGIS; lo rehace TT-BD-06 en MongoDB (consulta por polígono) |
| TT-CI-01 | Modificar | CI por servicio (C3) |
| TT-REPO-05 | Modificar | CODEOWNERS con los nuevos dueños (B3) |
| TT-REPO-06 | Modificar | Dependabot sin Npgsql/EF Core, con Terraform y Docker (C5) |
| TT-AZ-01 | Mantener | Pasa a Azure for Students; roles y alerta pendientes |
| TT-FB-01 | Modificar | Agregar Blaze, regiones de SMS y App Check (TT-FB-02) |
| TT-API-02 | Mantener | Va al servicio Identidad; fusionar #26 antes de separar servicios |
| TT-MOB-02 | Mantener | Tema y traducciones; sin cambios |
| TT-PRU-01 | Modificar | Endpoint de oficios vive en el servicio Mercado y usa MongoDB; #31 se fusiona antes de la migración o se ajusta después |
| TT-PRU-02, TT-PRU-04 | Mantener | La demo (paso 2) depende de TT-API-07 y de la migración |
| TT-AZ-02 | Reemplazar/Mover | Bicep → **Terraform** por servicio; sin PostgreSQL Flexible; con DocumentDB; Sprint 2 |
| TT-CD-01 | Modificar/Mover | Despliegue por servicio; sin migraciones de EF Core; Sprint 2 |
| TT-SEC-01 | Modificar | Key Vault para las nuevas cadenas; ajustar solape con gitleaks |
| TT-API-03 | Modificar | Middlewares compartidos y en el gateway |
| TT-MOB-03 | Modificar | Cliente Dart generado desde el OpenAPI del gateway |
| TT-MOB-04 | Mantener | Spike de Google Maps |
| TT-BD-03 | Modificar/Mover | Semillas en JSON para MongoDB; Sprint 2 |
| TT-QA-01 | Modificar/Mover | Plantillas de prueba con MongoDB y de contrato; Sprint 2 |
| TT-API-05 | Modificar | Agregar CDN y gancho de moderación |
| TT-CI-02, TT-API-06 | Modificar | Contrato y encabezados de seguridad por servicio/gateway |
| TT-CI-03 | Mantener | Emulador de Firebase para el servicio Identidad |
| TT-CI-04 | Mantener | ARB al día |
| TT-API-07 | Modificar | Colonia por punto con consulta por polígono de MongoDB |
| TT-API-04 | Modificar | Outbox entre servicios con transacciones de MongoDB |
| TT-OBS-01 | Modificar | Trazas distribuidas |
| TT-TIE-01, TT-TIE-02 | **Mover a S1** | Cuenta, primer AAB y 20 testers ya |
| TT-CD-03 | Modificar | release-please por servicio; sigue la matriz de versionamiento (A6) |
| TT-RT-01 | Modificar | SignalR en el servicio Mensajería; Redis opcional |
| TT-IA-01 | Modificar | Azure OpenAI en el servicio Asistente |
| TT-SEC-02, TT-SEC-03 | Mantener | Ajustar al alcance por servicio |
| TT-CD-02 | Modificar | Producción con 2 réplicas solo en la ventana de evaluación; revisar si Front Door+WAF cabe en el costo cero |
| TT-ML-01 | Mantener | En el servicio Asistente |
| TT-QA-02 | Mantener | Carga con k6 sobre 2 réplicas |
| TT-QA-03, TT-QA-04, TT-QA-05 | Mantener | Pero terminan antes del 1-dic |
| TT-TIE-03 | Modificar | Ficha de Google Play y declaración de datos; **quitar Apple** |
| TT-TIE-04 | Modificar | «Producción en Google Play y prueba de descarga»; **quitar App Store** |
| TT-BD-04 | Modificar | Restauración de DocumentDB (sin respaldo automático: `mongodump` programado) |
| TT-DOC-01, TT-DOC-02 | Mover | Terminan el 1-dic; DOC-02 incluye `.rar` y script de la base |

## E. Tareas técnicas nuevas (puntos y sprint provisionales)

Dueño «Libre» = pool por carga. «J» José Julián, «G» Guillermo, «A» Alexis, «E» Erick.

**Arquitectura y servicios**
- [ ] TT-ARQ-01 Esqueleto de microservicios (solución, 4 servicios + gateway, Dockerfiles, compose) · J+G · 8 · S2
- [ ] TT-ARQ-02 Gateway YARP: enrutamiento, JWT, límite de tasa, HTTPS · G · 3 · S2
- [ ] TT-ARQ-03 Contratos y eventos entre servicios (biblioteca de contratos) · A · 3 · S3
- [ ] TT-ARQ-04 CI por servicio (filtros por ruta) · J · 3 · S2
- [ ] TT-ARQ-05 Autenticación entre servicios · G · 2 · S3
- [ ] TT-QA-06 Pruebas de contrato entre servicios · A · 2 · S3

**Datos**
- [ ] TT-BD-05 Spike DocumentDB: disponibilidad, consulta por polígono, transacción, texto, conexiones y respaldo · A · 2 · S1
- [ ] TT-BD-06 Migración del catálogo a MongoDB (oficios, colonias, consulta por punto) · A · 8 · S2
- [ ] TT-BD-07 Adaptar CI y pruebas a MongoDB (Testcontainers) · J · 3 · S2
- [ ] TT-BD-08 Caché y rendimiento de la base (índices) · A · 2 · S3
- [ ] TT-BD-09 Script de índices y semillas para la entrega · A · 1 · S6

**Nube y costo**
- [ ] TT-AZ-03 Destruir la infraestructura con `terraform destroy` desde el pipeline · J · 2 · S3
- [ ] TT-AZ-04 Revisión de costo cero y créditos de Azure for Students · J · 1 · S1
- [ ] TT-AZ-05 Estado remoto de Terraform · J · 1 · S2
- [ ] TT-FB-02 Blaze, política de regiones de SMS, alertas y App Check · J · 1 · S1

**Rendimiento y contenido**
- [ ] TT-API-08 Caché en el backend · G · 3 · S3
- [ ] TT-API-09 Paginación, serializadores y compresión de respuestas · G · 3 · S3
- [ ] TT-IA-02 Moderación de texto, imágenes y videos antes de guardar · J · 3 · S4
- [ ] TT-API-10 CDN para imágenes (fotos de verificación siguen privadas) · J · 2 · S4

**Móvil, landing y Play**
- [ ] TT-MOB-05 Caché en la app y SQLite local con sincronización · E · 3 · S3
- [ ] TT-MOB-06 Aviso de privacidad y permisos en la app · E · 2 · S3
- [ ] TT-WEB-01 Landing page (redirección a Play y avisos), hosting gratuito · **dueño y fecha ya**: propuesta quien tenga menos carga, límite 27-oct · 3 · S1–S2
- [ ] TT-WEB-02 Páginas de aviso de privacidad y manejo de datos · J (contenido) · 2 · S2
- [ ] TT-TIE-05 Demo en Google Play actualizado cada sprint · E · 5 (1 por sprint) · S2–S6
- [ ] TT-TIE-06 Solicitud de acceso a producción · E · 1 · S3

**Gestión y documentación**
- [ ] TT-GES-03 Matriz de versionamiento · J · 1 · S1
- [ ] TT-GES-04 Matriz de cumplimiento de lineamientos · J · 2 · S1
- [ ] TT-GES-05 Entrevistas a usuarios y evidencia · José Julián con el equipo · 3 · S1
- [ ] TT-GES-06 Decisiones por escrito (sección A) · J · 2 · S1
- [ ] TT-DOC-03 Descripciones en español de backend y móvil · J · 3 · hoy
- [ ] TT-DOC-04 Hoja de componentes de backend y móvil · G y E · 4 · hoy
- [ ] TT-DOC-05 Hoja de endpoints y CRUD (colecciones, llaves e índices por endpoint) · A · 5 · hoy (provisional hasta el spike)
- [ ] TT-DOC-06 Entrega en `.rar` y script de la base · J · 1 · S6

**Total nuevo: unos 90 puntos.** Con lo que ya había (280) supera la capacidad. Hay que recortar (sección G).

## F. Historias de usuario: criterios que cambian (no se eliminan funcionalidades)

- [ ] F1. HU-009, 010, 011, 012: consultas geoespaciales y de texto con MongoDB.
- [ ] F2. HU-024, 025, 033: dependen de transacciones de MongoDB (resultado del spike).
- [ ] F3. HU-066, 067, 068, 070, 071, 072 (panel y administración): agregaciones; ver G sobre su prioridad.
- [ ] F4. HU-002, 018, 024, 029, 039: criterio de **moderación** antes de guardar; imágenes por el CDN.
- [ ] F5. HU-003, 004, 006: sesión siempre viva, **logout también en el backend** (revocar token) y eliminación completa de cuenta.
- [ ] F6. HU-050 (push): marcarla como proceso principal con notificación.
- [ ] F7. HU-001 y HU-003: política de SMS (solo +52), reintentos y límites.
- [ ] F8. HU de la app que usen datos locales: criterio de caché y SQLite (HU-005, HU-026, HU-011).
- [ ] F9. Las 20 historias «en espera»: revisar contra la maqueta. El profe pide no eliminar funcionalidades.
- [ ] F10. Actualizar `Historias_de_Usuario_Agile_Knock.xlsx` y `Product_Backlog_Knock.docx` con los criterios; regenerar `docs/backlog/*.md`.

## G. Planeación (Excel, Jira) y recortes

- [ ] G1. Crear una copia de trabajo de los Excel y el CSV de Jira en `Plan_adaptacion/` antes de reemplazar los originales.
- [ ] G2. `Backlog_Planeacion_Knock.xlsx`: aplicar la sección D (acciones y notas de seguimiento) y la sección E (filas nuevas con clave de Jira).
- [ ] G3. `Sprint_Planning_Knock.xlsx`: mover tareas, insertar nuevas, recalcular la tabla de carga, nuevos responsables.
- [ ] G4. Jira: crear las tareas nuevas por CSV; renombrar TT-AZ-02 y las modificadas; etiquetas `servicio-*`, `resp-libre`; mover TT-TIE-01/02 a Sprint 1.
- [ ] G5. **Recortar para que quepa (DECIDIR).** Lo que el profe evalúa es: Nube, Base de datos, Backend, Móvil y Landing. **El panel interno de administración no está en esa lista.** Candidatos a reducir o dejar para la reserva: HU-066, 067, 068, 070, 071, 072 y parte de la verificación de identidad (HU-021, 022). Confirmar contra la maqueta antes de mover.
- [ ] G6. Recalcular la carga por persona con los nuevos roles (objetivo: ningún sprint por encima de la capacidad real, y el Sprint 1 reconocido como incompleto).
- [ ] G7. Sprint 1 (cierra 23-oct): lo que no se alcance se arrastra explícitamente; no abrir trabajo nuevo grande.

## H. Entregables de la clase de hoy (20-oct)

- [ ] H1. Entrevistas a usuarios con evidencia (solo José Julián; hechas con personas reales).
- [ ] H2. Descripción en español del backend, por separado (TT-DOC-03).
- [ ] H3. Descripción en español de la app móvil, por separado (TT-DOC-03).
- [ ] H4. Hoja de cálculo de componentes del backend y de la móvil (TT-DOC-04).
- [ ] H5. Hoja de cálculo de CRUD y funciones (autocompletado, filtros, búsquedas) con diseño de colecciones en lenguaje natural, llaves e índices por endpoint (TT-DOC-05; provisional hasta el spike).

## I. Lineamientos del profe → dónde se cubre

| # | Lineamiento | Cubierto por |
|---|---|---|
| 1 | Descarga desde Google Play | TT-TIE-01/02/05/06, C13 |
| 2 | Evaluación con prompts | A7 (matriz de cumplimiento), specs y pruebas |
| 3 | Nube, base, backend, móvil y landing | TT-AZ-*, TT-BD-*, TT-ARQ-*, TT-WEB-01 |
| 4 | Móvil fiel a la maqueta, sin eliminar funciones | F9 |
| 5 | SDD y DevSecOps | Ya activos; C3, C6, C7 |
| 6 | Desarrollo con agentes de IA | Ya activo; sección «Uso de IA» en cada PR |
| 7 | Pruebas unitarias, de seguridad e integración | C3, C4, TT-QA-06 |
| 8 | CI/CD backend y móvil | C3, TT-CD-01, TT-CD-03 |
| 9 | Caché en base, backend y app | TT-BD-08, TT-API-08, TT-MOB-05 |
| 10 | Logout en backend y móvil | F5 (HU-004) |
| 11 | Matriz de versionamiento | A6, TT-GES-03 |
| 12 | MCP (opcional) | — |
| 13 | Nube: balanceo, IaC, SSL | TT-ARQ-02 (gateway), TT-AZ-02 (Terraform), TT-AZ-03 |
| 14 | Bases NoSQL | TT-BD-05/06/07, A2 |
| 15 | Backend: 2 instancias, Azure, costo cero, CDN, observabilidad, JWT, rendimiento | A9, TT-CD-02, TT-API-10, TT-OBS-01, TT-API-02, TT-API-08/09 |
| 16 | Móvil: moderación, push, privacidad, eliminar cuenta, permisos, sesión viva, SQLite, ML e IA | TT-IA-02, HU-050, TT-MOB-06, HU-006, HU-003, TT-MOB-05, TT-ML-01, TT-IA-01 |
| 17 | Landing | TT-WEB-01/02 |
| 18 | Una sola cuenta de GitHub | C14 |
| 19 | Evaluación 2-dic, `.rar` y script de la base | TT-DOC-06, TT-BD-09 |

## J. Orden de ejecución (olas)

- [ ] **Ola 0 — hoy y hasta el 23-oct:** H2–H5, TT-GES-03/04/06, TT-AZ-04, TT-FB-02, TT-BD-05 (spike), TT-TIE-01/02 (cuenta de Play, primer AAB, testers), TT-WEB-01 (landing mínima con URL de privacidad), fusionar los PR abiertos (#26, #31, #16, #40) sin migrar todavía.
- [ ] **Ola 1 — Sprint 2:** TT-ARQ-01/02/04, TT-BD-06/07, TT-AZ-02 (Terraform) y TT-AZ-05, TT-CD-01, TT-WEB-02, TT-TIE-05.
- [ ] **Ola 2 — Sprint 3:** caché, paginación, compresión, contratos entre servicios, pruebas de contrato, `terraform destroy`, aviso de privacidad y permisos en la app, producción en Play.
- [ ] **Ola 3 — Sprints 4 a 6:** moderación, CDN, producción con 2 réplicas, estabilización, entrega (`.rar` y script).
- [ ] **Congelar el 1-dic. Evaluación el 2-dic.**

## K. Riesgos a vigilar

- [ ] La migración a MongoDB y la separación en servicios caen en el mismo sprint: no empezar la separación antes de que el spike responda.
- [ ] Play: si algún tester se sale durante los 14 días, el conteo puede reiniciarse **(confirmar)**.
- [ ] Costo: con 5 contenedores, las 2 réplicas fijas solo en la ventana de evaluación.
- [ ] Azure for Students puede restringir regiones o servicios **(confirmar)**.
- [ ] Atlas M0 (respaldo) tiene 100 conexiones y 100 operaciones por segundo según la documentación oficial; no sirve para varios servicios sin pagar.
- [ ] DocumentDB gratuito: sin respaldo ni alta disponibilidad; puede pausarse **(confirmar)**.
- [ ] Capacidad: unos 90 puntos nuevos sobre un plan ya lleno; sin recortes (G5) no se llega.
