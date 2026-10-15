# Backlog del proyecto

Fichas de cada historia de usuario (HU) y tarea técnica (TT), generadas desde el backlog con `scripts/export_backlog_md.py`. **El estado vive en Jira**; el contrato aprobado de cada una es su spec en `specs/`. Las fichas no se editan a mano.


## Sprint 0 (12/10 – 16/10/2026)

| ID | Título | Jira | Responsable | Pts | Épica |
|---|---|---|---|---|---|
| [TT-API-01](TT-API-01.md) | Proyecto ASP.NET Core 10 con controllers, módulos vacíos, ProblemDetails, versionado /api/v1, health y Scalar | KNOCK-20 | José Guillermo Rangel Hernández | 3 | EP-13 |
| [TT-API-02](TT-API-02.md) | Spike: validar el token de Firebase de un número de prueba y emitir la sesión propia (JWT) | KNOCK-21 | José Guillermo Rangel Hernández | 2 | EP-13 |
| [TT-AZ-01](TT-AZ-01.md) | Cuenta de Azure, grupo de recursos, presupuesto y alertas de costo | KNOCK-27 | José Julián Hernández Hernández | 1 | EP-13 |
| [TT-BD-01](TT-BD-01.md) | PostgreSQL + PostGIS en Docker Compose y conexión con EF Core (Npgsql + NetTopologySuite) y migraciones | KNOCK-22 | Ángel Alexis Blas Castillo | 2 | EP-13 |
| [TT-BD-02](TT-BD-02.md) | Catálogo base: 3 oficios como datos y colonias de la ciudad piloto (SEPOMEX + polígonos); consulta "colonia de un punto" | KNOCK-23 | Ángel Alexis Blas Castillo | 3 | EP-13 |
| [TT-CI-01](TT-CI-01.md) | GitHub Actions en cada PR: job branch-name (nombre y combinación origen → destino), commits, formato, build y pruebas de la API y la app | KNOCK-26 | José Julián Hernández Hernández | 2 | EP-13 |
| [TT-FB-01](TT-FB-01.md) | Proyecto de Firebase: autenticación por teléfono con números de prueba, FCM y apps Android/iOS registradas | KNOCK-28 | José Julián Hernández Hernández | 1 | EP-13 |
| [TT-GES-01](TT-GES-01.md) | Cargar épicas, HU y tareas en Jira y crear los sprints semanales | KNOCK-14 | José Julián Hernández Hernández | 1 | EP-13 |
| [TT-GES-02](TT-GES-02.md) | Retrospectiva de la semana de prueba y decisión de roles (mantener, cambiar o parejas) | KNOCK-33 | José Julián Hernández Hernández | 1 | EP-13 |
| [TT-MOB-01](TT-MOB-01.md) | Proyecto Flutter con Clean Architecture (core/features), Riverpod, go_router y las 5 pestañas de la maqueta | KNOCK-24 | Erick Trejo Reséndiz | 3 | EP-13 |
| [TT-MOB-02](TT-MOB-02.md) | Tema claro y oscuro con la paleta de la maqueta y archivos de traducción (ARB es/en) | KNOCK-25 | Erick Trejo Reséndiz | 2 | EP-13 |
| [TT-PRU-01](TT-PRU-01.md) | Ejercicio fuera de rol (Erick): endpoint GET /api/v1/oficios con su prueba | KNOCK-29 | Erick Trejo Reséndiz | 1 | EP-13 |
| [TT-PRU-02](TT-PRU-02.md) | Ejercicio fuera de rol (Guillermo): pantalla Flutter que lista los oficios | KNOCK-30 | José Guillermo Rangel Hernández | 1 | EP-13 |
| [TT-PRU-03](TT-PRU-03.md) | Ejercicio fuera de rol (Ángel): proyecto Angular del panel con una pantalla de inicio | KNOCK-31 | Ángel Alexis Blas Castillo | 1 | EP-13 |
| [TT-PRU-04](TT-PRU-04.md) | Demo integrada del viernes: entrar con número de prueba y ver tu colonia en la app | KNOCK-32 | Todo el equipo | 2 | EP-13 |
| [TT-REPO-01](TT-REPO-01.md) | Crear el monorepo knock en GitHub (app/, panel/, api/, docs/, deploy/) e invitar al equipo y a los docentes | KNOCK-15 | José Julián Hernández Hernández | 1 | EP-13 |
| [TT-REPO-02](TT-REPO-02.md) | Ramas main, release, develop y sprint-00 (como ITACR); CONTRIBUTING.md con el flujo; plantillas de PR e issues; CODEOWNERS | KNOCK-16 | José Julián Hernández Hernández | 1 | EP-13 |
| [TT-REPO-03](TT-REPO-03.md) | Conectar GitHub con Jira (clave KNOCK) para ligar ramas, commits y PR a cada issue | KNOCK-17 | José Julián Hernández Hernández | 1 | EP-13 |
| [TT-SDD-01](TT-SDD-01.md) | Constitución, AGENTS.md y skills base para los agentes de IA | KNOCK-19 | José Julián Hernández Hernández | 2 | EP-13 |
| [TT-STD-01](TT-STD-01.md) | Reglas de codificación: .editorconfig, analizadores de .NET, flutter_lints, Conventional Commits y convención de ramas | KNOCK-18 | José Julián Hernández Hernández | 2 | EP-13 |

## Sprint 1 (19/10 – 23/10/2026)

| ID | Título | Jira | Responsable | Pts | Épica |
|---|---|---|---|---|---|
| [HU-001](HU-001.md) | Entrar con mi celular y código SMS (registro e inicio son el mismo flujo) | KNOCK-43 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-01 Acceso y cuenta |
| [HU-002](HU-002.md) | Completar mi perfil la primera vez (nombre y foto opcional) | KNOCK-44 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-01 Acceso y cuenta |
| [HU-003](HU-003.md) | Mantener mi sesión segura (renovación del token) | KNOCK-45 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-01 Acceso y cuenta |
| [HU-004](HU-004.md) | Cerrar sesión | KNOCK-46 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 1 | EP-01 Acceso y cuenta |
| [HU-009](HU-009.md) | Indicar mi zona por ubicación o código postal (lista de espera fuera de la ciudad piloto) | KNOCK-47 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-02 Zona y cobertura |
| [HU-017](HU-017.md) | Registrarme como técnico (oficios, "Otro oficio", taller o domicilio, radio) | KNOCK-48 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-09 Lado del técnico |
| [HU-052](HU-052.md) | Elegir idioma y tema en Configuración | KNOCK-49 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 1 | EP-10 Mi cuenta y avisos |
| [TT-API-03](TT-API-03.md) | Middlewares: errores a ProblemDetails, Serilog con CorrelationId y límites de peticiones | KNOCK-37 | José Guillermo Rangel Hernández | 2 | EP-13 |
| [TT-API-05](TT-API-05.md) | Archivos: Blob privado, enlaces de 5 minutos, compresión y quitar metadatos de ubicación (EXIF) | KNOCK-42 | Ángel Alexis Blas Castillo | 3 | EP-13 |
| [TT-AZ-02](TT-AZ-02.md) | Infraestructura como código (Bicep): Container Apps, PostgreSQL Flexible con PostGIS, Redis, Blob Storage y Key Vault | KNOCK-34 | José Julián Hernández Hernández | 3 | EP-13 |
| [TT-BD-03](TT-BD-03.md) | Semillas ficticias: clientes, técnicos y solicitudes de prueba | KNOCK-40 | Ángel Alexis Blas Castillo | 1 | EP-13 |
| [TT-CD-01](TT-CD-01.md) | Despliegue continuo a Staging: push a release → imagen → Container Apps, migraciones de EF Core y smoke test; app a la pista interna de Play | KNOCK-35 | José Julián Hernández Hernández | 3 | EP-13 |
| [TT-MOB-03](TT-MOB-03.md) | Cliente de Dart generado desde OpenAPI e interceptor de sesión (dio) | KNOCK-38 | Erick Trejo Reséndiz | 2 | EP-13 |
| [TT-MOB-04](TT-MOB-04.md) | Spike: Google Maps en Flutter con pines y agrupación | KNOCK-39 | Erick Trejo Reséndiz | 2 | EP-13 |
| [TT-QA-01](TT-QA-01.md) | Plantillas de pruebas: xUnit + Testcontainers, NetArchTest (fronteras de módulos), flutter_test y Stryker | KNOCK-41 | José Julián Hernández Hernández | 3 | EP-13 |
| [TT-SEC-01](TT-SEC-01.md) | Secretos en Key Vault con identidad administrada; gitleaks en CI | KNOCK-36 | José Julián Hernández Hernández | 2 | EP-13 |

## Sprint 2 (26/10 – 30/10/2026)

| ID | Título | Jira | Responsable | Pts | Épica |
|---|---|---|---|---|---|
| [HU-005](HU-005.md) | Explorar sin cuenta y continuar donde me quedé al entrar | KNOCK-53 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-01 Acceso y cuenta |
| [HU-010](HU-010.md) | Guardar mi dirección con la primera solicitud a domicilio (cifrada) | KNOCK-54 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-02 Zona y cobertura |
| [HU-011](HU-011.md) | Ver expertos cercanos en el mapa (pines, panel y filtros) | KNOCK-55 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-03 Encontrar expertos |
| [HU-013](HU-013.md) | Ver el perfil de un experto | KNOCK-56 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-03 Encontrar expertos |
| [HU-018](HU-018.md) | Editar mi perfil público (servicios con precio, horarios, foto) | KNOCK-57 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-09 Lado del técnico |
| [HU-019](HU-019.md) | Activar "Disponible hoy" | KNOCK-58 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 1 | EP-09 Lado del técnico |
| [HU-024](HU-024.md) | Pedir una cotización (categoría, descripción, 1 a 5 fotos, día y franja) | KNOCK-59 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-04 Solicitudes |
| [HU-050](HU-050.md) | Recibir notificaciones push | KNOCK-60 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-10 Mi cuenta y avisos |
| [TT-API-04](TT-API-04.md) | Outbox transaccional y mensajes programados (MassTransit 8 o Wolverine) | KNOCK-50 | José Guillermo Rangel Hernández | 3 | EP-13 |
| [TT-OBS-01](TT-OBS-01.md) | Observabilidad: Application Insights en la API y Sentry en la app | KNOCK-51 | José Julián Hernández Hernández | 2 | EP-13 |
| [TT-TIE-01](TT-TIE-01.md) | Cuentas de Google Play y Apple Developer; primer build firmado en la pista interna | KNOCK-52 | Erick Trejo Reséndiz | 2 | EP-13 |

## Sprint 3 (02/11 – 06/11/2026)

| ID | Título | Jira | Responsable | Pts | Épica |
|---|---|---|---|---|---|
| [HU-012](HU-012.md) | Buscar un experto por texto | KNOCK-66 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-03 Encontrar expertos |
| [HU-020](HU-020.md) | Pestaña Trabajo (próximas visitas, solicitudes nuevas, enviadas y terminadas) | KNOCK-67 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-09 Lado del técnico |
| [HU-025](HU-025.md) | Repartir la solicitud por oleadas (hasta 5 técnicos, ampliar a los 15 min) | KNOCK-68 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-04 Solicitudes |
| [HU-026](HU-026.md) | Ver mi actividad (lista única con estados y último mensaje) | KNOCK-69 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-04 Solicitudes |
| [HU-027](HU-027.md) | Cancelar una solicitud | KNOCK-70 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 1 | EP-04 Solicitudes |
| [HU-028](HU-028.md) | Vencer las solicitudes a las 48 h | KNOCK-71 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-04 Solicitudes |
| [HU-029](HU-029.md) | Chatear antes de elegir (número oculto, aviso de no pagar por adelantado) | KNOCK-72 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-05 Chat y cotizaciones |
| [HU-030](HU-030.md) | Ver mis conversaciones (globo con contador) | KNOCK-73 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-05 Chat y cotizaciones |
| [HU-031](HU-031.md) | Enviar una cotización (precio fijo o rango, día y hora, nota) | KNOCK-74 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-05 Chat y cotizaciones |
| [HU-066](HU-066.md) | Entrar al panel interno con permisos y doble factor | KNOCK-75 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-12 Panel interno |
| [TT-CD-03](TT-CD-03.md) | release-please en main (versiones de api, app y panel), CHANGELOG, tag vX.Y.Z y deploy.yml a producción | KNOCK-61 | José Julián Hernández Hernández | 2 | EP-13 |
| [TT-IA-01](TT-IA-01.md) | Cliente de Azure OpenAI con filtro de datos personales, límites de costo y respaldo manual | KNOCK-63 | José Julián Hernández Hernández | 3 | EP-13 |
| [TT-RT-01](TT-RT-01.md) | SignalR con Redis como backplane | KNOCK-62 | José Guillermo Rangel Hernández | 3 | EP-13 |
| [TT-SEC-02](TT-SEC-02.md) | DevSecOps en CI: CodeQL, dependencias vulnerables y escaneo de contenedores | KNOCK-64 | José Julián Hernández Hernández | 2 | EP-13 |
| [TT-TIE-02](TT-TIE-02.md) | Reclutar testers y abrir la prueba cerrada de Google Play | KNOCK-65 | José Julián Hernández Hernández | 1 | EP-13 |

## Sprint 4 (09/11 – 13/11/2026)

| ID | Título | Jira | Responsable | Pts | Épica |
|---|---|---|---|---|---|
| [HU-021](HU-021.md) | Verificar mi identidad (opcional, consentimiento biométrico) | KNOCK-79 | José Julián Hernández Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-09 Lado del técnico |
| [HU-022](HU-022.md) | Ver mi estado de verificación y reintentar | KNOCK-80 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-09 Lado del técnico |
| [HU-032](HU-032.md) | Comparar cotizaciones (recomendadas, precio, calificación, más pronto) | KNOCK-81 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-05 Chat y cotizaciones |
| [HU-033](HU-033.md) | Elegir una cotización (cita confirmada; se revelan teléfono y dirección) | KNOCK-82 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-05 Chat y cotizaciones |
| [HU-036](HU-036.md) | Ver mi visita con el código de 4 dígitos o la cita en el taller | KNOCK-83 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-06 Visita segura y trabajo |
| [HU-037](HU-037.md) | Avisar a alguien de confianza | KNOCK-84 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-06 Visita segura y trabajo |
| [HU-038](HU-038.md) | Confirmar mi llegada con el código ("Ya llegué") | KNOCK-85 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-06 Visita segura y trabajo |
| [HU-067](HU-067.md) | Revisar verificaciones (registro de quién ve las fotos) | KNOCK-86 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-12 Panel interno |
| [HU-068](HU-068.md) | Borrar fotos de verificación al decidir o a los 7 días (alerta a las 24 h) | KNOCK-87 | José Julián Hernández Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-12 Panel interno |
| [TT-CD-02](TT-CD-02.md) | Producción con 2 instancias, Front Door + WAF y reversión probada | KNOCK-76 | José Julián Hernández Hernández | 3 | EP-13 |
| [TT-ML-01](TT-ML-01.md) | Datos de entrenamiento (sintéticos al inicio) y reentrenamiento semanal de modelos ML.NET | KNOCK-77 | Ángel Alexis Blas Castillo | 3 | EP-13 |
| [TT-SEC-03](TT-SEC-03.md) | Modelo de amenazas (STRIDE) y escaneo ZAP contra Staging | KNOCK-78 | José Julián Hernández Hernández | 2 | EP-13 |

## Sprint 5 (16/11 – 20/11/2026)

| ID | Título | Jira | Responsable | Pts | Épica |
|---|---|---|---|---|---|
| [HU-014](HU-014.md) | Explorar (servicios populares, trabajos reales, cerca de ti, mejor calificados) | KNOCK-90 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-03 Encontrar expertos |
| [HU-015](HU-015.md) | Guardar técnicos de confianza y volver a contratar | KNOCK-91 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-03 Encontrar expertos |
| [HU-039](HU-039.md) | Terminar el trabajo con fotos y lo que hice | KNOCK-92 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-06 Visita segura y trabajo |
| [HU-042](HU-042.md) | Calificar el servicio (general y por partes, hasta 30 días) | KNOCK-93 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-07 Reseñas y reputación |
| [HU-043](HU-043.md) | Ver la reputación del técnico (promedios por parte, distribución, niveles) | KNOCK-94 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-07 Reseñas y reputación |
| [HU-045](HU-045.md) | Reportar un problema por gravedad (incluye "no se terminó" hasta 7 días) | KNOCK-95 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-08 Confianza y seguridad |
| [HU-046](HU-046.md) | Bloquear a un usuario (desde el chat, con "Deshacer") | KNOCK-96 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-08 Confianza y seguridad |
| [HU-047](HU-047.md) | Suspensión preventiva en casos críticos | KNOCK-97 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-08 Confianza y seguridad |
| [HU-049](HU-049.md) | Mis datos (nombre, foto, colonia, dirección) | KNOCK-98 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-10 Mi cuenta y avisos |
| [HU-057](HU-057.md) | Asistente Knock "No sé cuál" (texto y foto) | KNOCK-99 | José Julián Hernández Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-11 IA y aprendizaje automático |
| [TT-QA-02](TT-QA-02.md) | Prueba de carga con k6 y escalamiento con 2 instancias | KNOCK-88 | Ángel Alexis Blas Castillo | 2 | EP-13 |
| [TT-QA-03](TT-QA-03.md) | Prueba de usabilidad 1 con 5 personas sin capacitación | KNOCK-89 | José Julián Hernández Hernández | 2 | EP-13 |

## Sprint 6 (23/11 – 27/11/2026)

| ID | Título | Jira | Responsable | Pts | Épica |
|---|---|---|---|---|---|
| [HU-006](HU-006.md) | Eliminar mi cuenta (ver lo pendiente, código SMS, 7 días de gracia) | KNOCK-104 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-01 Acceso y cuenta |
| [HU-051](HU-051.md) | Bandeja de avisos y qué avisos recibir | KNOCK-105 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-10 Mi cuenta y avisos |
| [HU-058](HU-058.md) | Resumen de la conversación y bitácora del técnico | KNOCK-106 | José Julián Hernández Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-11 IA y aprendizaje automático |
| [HU-059](HU-059.md) | Cotización con IA (borrador editable) | KNOCK-107 | José Julián Hernández Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-11 IA y aprendizaje automático |
| [HU-060](HU-060.md) | Orden "Recomendadas" por cumplimiento | KNOCK-108 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-11 IA y aprendizaje automático |
| [HU-061](HU-061.md) | Rango de precio de referencia | KNOCK-109 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-11 IA y aprendizaje automático |
| [HU-069](HU-069.md) | Atender reportes por gravedad | KNOCK-110 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-12 Panel interno |
| [HU-070](HU-070.md) | Ver métricas (incluye oficios pedidos y lista de espera) | KNOCK-111 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-12 Panel interno |
| [TT-BD-04](TT-BD-04.md) | Prueba de restauración de respaldo | KNOCK-103 | Ángel Alexis Blas Castillo | 1 | EP-13 |
| [TT-QA-04](TT-QA-04.md) | Regresión completa y corrección de defectos | KNOCK-100 | Todo el equipo | 5 | EP-13 |
| [TT-TIE-03](TT-TIE-03.md) | Fichas de tienda, capturas, declaración de datos y página web de eliminación de cuenta | KNOCK-101 | Erick Trejo Reséndiz | 2 | EP-13 |
| [TT-TIE-04](TT-TIE-04.md) | Envío a revisión de Google Play y App Store (23 – 24 nov) | KNOCK-102 | Erick Trejo Reséndiz | 2 | EP-13 |

## Sprint 7 (30/11 – 04/12/2026)

| ID | Título | Jira | Responsable | Pts | Épica |
|---|---|---|---|---|---|
| [TT-DOC-01](TT-DOC-01.md) | Manuales: cliente, técnico, panel interno y manual técnico | KNOCK-113 | Todo el equipo | 3 | EP-13 |
| [TT-DOC-02](TT-DOC-02.md) | Documento final y evidencias de pruebas | KNOCK-114 | José Julián Hernández Hernández | 2 | EP-13 |
| [TT-QA-05](TT-QA-05.md) | Prueba de usabilidad 2 con cuestionario SUS | KNOCK-112 | José Julián Hernández Hernández | 1 | EP-13 |

## Siguiente versión (prioridad media)

| ID | Título | Jira | Responsable | Pts | Épica |
|---|---|---|---|---|---|
| [HU-007](HU-007.md) | Ver y cerrar sesiones en otros dispositivos | KNOCK-115 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-01 Acceso y cuenta |
| [HU-008](HU-008.md) | Cambiar de número y recuperar el acceso | KNOCK-116 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-01 Acceso y cuenta |
| [HU-016](HU-016.md) | Ideas para tu casa y tu auto | KNOCK-117 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 1 | EP-03 Encontrar expertos |
| [HU-023](HU-023.md) | Capa "Mi agenda" en el mapa | KNOCK-118 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-09 Lado del técnico |
| [HU-034](HU-034.md) | Reprogramar la cita | KNOCK-119 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-05 Chat y cotizaciones |
| [HU-035](HU-035.md) | Editar o retirar mi cotización | KNOCK-120 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-05 Chat y cotizaciones |
| [HU-040](HU-040.md) | Recordatorios de la visita | KNOCK-121 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-06 Visita segura y trabajo |
| [HU-041](HU-041.md) | Marcar que no se presentó y elegir otra cotización | KNOCK-122 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-06 Visita segura y trabajo |
| [HU-044](HU-044.md) | Responder a una opinión | KNOCK-123 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 1 | EP-07 Reseñas y reputación |
| [HU-048](HU-048.md) | Apelar una sanción | KNOCK-124 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-08 Confianza y seguridad |
| [HU-053](HU-053.md) | Usar la app completa en inglés | KNOCK-125 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-10 Mi cuenta y avisos |
| [HU-054](HU-054.md) | Mi casa y mi auto | KNOCK-126 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-10 Mi cuenta y avisos |
| [HU-055](HU-055.md) | Privacidad: descargar y corregir mis datos, consentimientos opcionales | KNOCK-127 | José Guillermo Rangel Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-10 Mi cuenta y avisos |
| [HU-056](HU-056.md) | Centro de ayuda y hablar con soporte | KNOCK-128 | Erick Trejo Reséndiz  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-10 Mi cuenta y avisos |
| [HU-062](HU-062.md) | Detección de anomalías | KNOCK-129 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-11 IA y aprendizaje automático |
| [HU-063](HU-063.md) | Explicar la cotización en lenguaje sencillo | KNOCK-130 | José Julián Hernández Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-11 IA y aprendizaje automático |
| [HU-064](HU-064.md) | Resumen de opiniones | KNOCK-131 | José Julián Hernández Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-11 IA y aprendizaje automático |
| [HU-065](HU-065.md) | Constancia del servicio | KNOCK-132 | José Julián Hernández Hernández  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 5 | EP-11 IA y aprendizaje automático |
| [HU-071](HU-071.md) | Moderar opiniones | KNOCK-133 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 2 | EP-12 Panel interno |
| [HU-072](HU-072.md) | Gestionar categorías y zonas | KNOCK-134 | Ángel Alexis Blas Castillo  ·  subtareas por capa: [BD] [Back] [Mobile] [Pruebas] | 3 | EP-12 Panel interno |
