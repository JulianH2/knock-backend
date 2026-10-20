# Matriz de cumplimiento de los lineamientos del curso

> **Tarea:** TT-GES-04. Cada fila lleva el requisito, la tarea que lo cubre, la evidencia y el estado. **Se actualiza en cada sprint**; el docente evalúa con prompts, así que cada fila debe poder comprobarse en el repositorio.
> Fecha de la versión: 2026-10-20. Estados: ✅ cubierto · 🔶 en curso o parcial · ⬜ pendiente · ❓ por decidir.

| # | Lineamiento | Tareas | Evidencia esperada | Estado |
|---|---|---|---|---|
| 1 | Evaluación inicia descargando la app de Google Play | TT-TIE-01, 02, 05, 06 | Ficha publicada y descarga desde la tienda | ⬜ |
| 2 | Evaluación a nivel de proyecto con prompts | Esta matriz, specs y pruebas | Cada requisito apunta a un archivo | 🔶 |
| 3 | El proyecto: nube, base de datos, backend, móvil y landing | TT-AZ-*, TT-BD-*, TT-ARQ-*, TT-WEB-01 | Repos y despliegue | 🔶 |
| 4 | Móvil a imagen de la maqueta, sin eliminar funciones | Historias de usuario, `maqueta-knock` | Revisión de las historias en espera contra la maqueta | 🔶 |
| 5 | Spec-Driven Development y DevSecOps | `specs/`, `.github/workflows` | Specs aprobadas; CI con SAST, SCA, secretos, contenedor, DAST | ✅ |
| 6 | Desarrollo con agentes de IA | Sección «Uso de IA» de cada PR | PR con la sección completa | ✅ |
| 7 | Pruebas unitarias, de seguridad e integración | `tests/UnitTest`, `SecurityTest`, `IntegrationTest` | CI en verde, cobertura 60 % líneas / 50 % ramas | ✅ |
| 8 | CI/CD en backend y móvil | TT-CI-01 (hecha), TT-CD-01, TT-CD-03, TT-ARQ-04 | Workflows; despliegue a Staging y publicación a Play | 🔶 |
| 9 | Caché en base de datos, backend y app | TT-BD-08, TT-API-08, TT-MOB-05 | Índices, caché de respuestas, caché local | ⬜ |
| 10 | Logout en backend y móvil | HU-004, HU-003 | Endpoint que revoca el token + pantalla | ⬜ |
| 11 | Matriz de versionamiento previa al desarrollo | TT-GES-03 | `docs/VERSIONAMIENTO.md` | ✅ (documento) |
| 12 | MCP (opcional) | — | Jira por MCP | ✅ |
| 13 | Nube: servicios, balanceo, Terraform, HTTPS | TT-AZ-02, TT-AZ-03, TT-ARQ-02, TT-CD-01 | Módulos de Terraform; gateway con HTTPS | 🔶 |
| 14 | Bases de datos NoSQL | ADR-002, TT-BD-05, TT-BD-06, TT-BD-07 | MongoDB en Azure DocumentDB, una base por servicio | 🔶 (spike pendiente) |
| 15a | Backend escalable en 2 instancias, en Azure, costo cero | TT-CD-02, ADR-006, TT-AZ-04 | 2 réplicas en la ventana de evaluación | ⬜ |
| 15b | Endpoints eficientes | TT-API-08, TT-API-09 | Caché, paginación, compresión, índices | ⬜ |
| 15c | Imágenes en el CDN de la nube | TT-API-10 | CDN delante del almacenamiento | ⬜ |
| 15d | Observabilidad (eventos y errores) | TT-OBS-01 | Application Insights con trazas distribuidas | ⬜ |
| 15e | JWT para autenticación | TT-API-02 | Servicio de Identidad emite y valida JWT | 🔶 (PR #26) |
| 15f | Rendimiento: serializadores, caché, límite de tasa, middleware, paginación, índices, compresión | TT-API-03, 06, 08, 09 | Cada técnica con su prueba | 🔶 |
| 15g | Crear y eliminar infraestructura y despliegue como paso | TT-AZ-02, TT-AZ-03, TT-CD-01 | `terraform apply` y `destroy` en el pipeline | ⬜ |
| 16a | Despliegue desde GitHub a Google Play | TT-CD-01, TT-TIE-05 | Workflow de publicación | ⬜ |
| 16b | Moderar texto, imágenes y videos antes de guardar | TT-IA-02 | Llamada a la API de moderación y prueba de rechazo | ⬜ |
| 16c | Notificaciones push en procesos principales | HU-050, TT-FB-01 | Push de solicitud, cotización y cita | ⬜ |
| 16d | Aviso de privacidad en la app, enlazado a la landing | TT-MOB-06, TT-WEB-02 | Pantalla y páginas | ⬜ |
| 16e | Eliminar cuenta | HU-006 | Flujo completo con borrado | ⬜ |
| 16f | Permisos del móvil | TT-MOB-06 | Solicitud de ubicación, cámara y notificaciones con motivo | ⬜ |
| 16g | Sesión siempre viva salvo logout | HU-003 | Renovación del token | ⬜ |
| 16h | SQLite local con sincronización (si aplica) | TT-MOB-05 | Caché local y sincronización | ❓ |
| 16i | Al menos una función de ML y una de IA generativa en el móvil | TT-ML-01, TT-IA-01, HU-057, HU-059 | Funciones visibles en la app | ⬜ |
| 17 | Landing: redirección a la tienda y avisos de privacidad | TT-WEB-01, TT-WEB-02 | Sitio publicado con enlaces | ⬜ |
| 18 | Una sola cuenta de GitHub | Repos `knock-backend` y `knock-frontend` | Ambos bajo la misma cuenta | ✅ |
| 19 | Evaluación el 2 de diciembre; entrega en `.rar` y script de la base | TT-DOC-06, TT-BD-09 | Archivos de entrega | ⬜ |
