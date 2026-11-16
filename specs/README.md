# Specs — knock-backend

Cada carpeta tiene `spec.md`, `plan.md` y `task.md`. Estados: Borrador → Aprobada → Implementada.

| N.º | Tema | Estado | Carpeta |
|---|---|---|---|
| 001 | Arquitectura de microservicios y puerta de entrada | Aprobada | [`001-arquitectura-de-microservicios-y-puerta-de-entrada`](001-arquitectura-de-microservicios-y-puerta-de-entrada/spec.md) |
| 002 | Estructura interna de cada servicio y estilo de la API | Aprobada | [`002-estructura-interna-de-cada-servicio-y-estilo-de-la-api`](002-estructura-interna-de-cada-servicio-y-estilo-de-la-api/spec.md) |
| 003 | Bases de datos: MongoDB y Redis | Aprobada | [`003-bases-de-datos-mongodb-y-redis`](003-bases-de-datos-mongodb-y-redis/spec.md) |
| 004 | Estructura del repositorio y flujo de trabajo | Aprobada | [`004-estructura-del-repositorio-y-flujo-de-trabajo`](004-estructura-del-repositorio-y-flujo-de-trabajo/spec.md) |
| 005 | Infraestructura en Azure con Terraform | Aprobada | [`005-infraestructura-en-azure-con-terraform`](005-infraestructura-en-azure-con-terraform/spec.md) |
| 006 | Escalabilidad, balanceo y salud de los servicios | Aprobada | [`006-escalabilidad-balanceo-y-salud-de-los-servicios`](006-escalabilidad-balanceo-y-salud-de-los-servicios/spec.md) |
| 007 | Integración continua y despliegue | Aprobada | [`007-integracion-continua-y-despliegue`](007-integracion-continua-y-despliegue/spec.md) |
| 008 | Calidad, pruebas y estándares | Aprobada | [`008-calidad-pruebas-y-estandares`](008-calidad-pruebas-y-estandares/spec.md) |
| 009 | Seguridad base y configuración | Aprobada | [`009-seguridad-base-y-configuracion`](009-seguridad-base-y-configuracion/spec.md) |
| 010 | Caché en tres niveles y paginación | Aprobada | [`010-cache-en-tres-niveles-y-paginacion`](010-cache-en-tres-niveles-y-paginacion/spec.md) |
| 011 | Observabilidad y registro | Aprobada | [`011-observabilidad-y-registro`](011-observabilidad-y-registro/spec.md) |
| 012 | Versionamiento por puntos | Aprobada | [`012-versionamiento-por-puntos`](012-versionamiento-por-puntos/spec.md) |
| 013 | Almacenamiento de archivos, entrega con caché y moderación | Aprobada | [`013-almacenamiento-de-archivos-y-moderacion`](013-almacenamiento-de-archivos-y-moderacion/spec.md) |
| 014 | Entrega del proyecto y reproducibilidad | Aprobada | [`014-entrega-del-proyecto-y-reproducibilidad`](014-entrega-del-proyecto-y-reproducibilidad/spec.md) |
| 015 | Registro e inicio de sesión (SES-01) | Aprobada | [`015-registro-e-inicio-de-sesion`](015-registro-e-inicio-de-sesion/spec.md) |
| 016 | Renovación y cierre de sesión (SES-02, SES-03) | Aprobada | [`016-renovacion-y-cierre-de-sesion`](016-renovacion-y-cierre-de-sesion/spec.md) |
| 017 | Perfil de la persona (USR-01, USR-02) | Aprobada | [`017-perfil-de-la-persona`](017-perfil-de-la-persona/spec.md) |
| 018 | Sesiones por dispositivo (SES-04, SES-05) | Aprobada | [`018-sesiones-por-dispositivo`](018-sesiones-por-dispositivo/spec.md) |
| 019 | MCP del proyecto | Aprobada | [`019-mcp-del-proyecto`](019-mcp-del-proyecto/spec.md) |
| BUG-001 | El job `dast` del CI nunca se ejecuta (KNOCK-238) | Borrador | [`BUG-001-job-dast-no-se-ejecuta`](BUG-001-job-dast-no-se-ejecuta/spec.md) |
| BUG-005 | Market sin redacción de registros de consola (KNOCK-255) | Borrador | [`BUG-005-market-sin-redaccion-de-registros`](BUG-005-market-sin-redaccion-de-registros/spec.md) |
| 020 | Solicitudes y cotizaciones (pedir y enviar) | Borrador | [`020-solicitudes-y-cotizaciones`](020-solicitudes-y-cotizaciones/spec.md) |
| 021 | Comparar y elegir cotización | Borrador | [`021-comparar-y-elegir-cotizacion`](021-comparar-y-elegir-cotizacion/spec.md) |
| 022 | Visita segura con código de 4 dígitos | Borrador | [`022-visita-segura-con-codigo`](022-visita-segura-con-codigo/spec.md) |
| 023 | Zona por ubicación o código postal, con lista de espera | Borrador | [`023-zona-por-ubicacion-o-codigo-postal`](023-zona-por-ubicacion-o-codigo-postal/spec.md) |
| 024 | Cliente de IA con Azure OpenAI y plantillas locales de respaldo | Borrador | [`024-cliente-de-ia-azure-openai-y-plantillas`](024-cliente-de-ia-azure-openai-y-plantillas/spec.md) |
| 025 | Verificación de identidad con consentimiento | Borrador | [`025-verificacion-de-identidad-con-consentimiento`](025-verificacion-de-identidad-con-consentimiento/spec.md) |
| 026 | Mensajería en tiempo real con SignalR y Redis | Borrador | [`026-mensajeria-en-tiempo-real-con-signalr-y-redis`](026-mensajeria-en-tiempo-real-con-signalr-y-redis/spec.md) |
| 027 | Buscar un experto por texto | Borrador | [`027-buscar-un-experto-por-texto`](027-buscar-un-experto-por-texto/spec.md) |
| 028 | Repartir la solicitud por oleadas | Borrador | [`028-repartir-la-solicitud-por-oleadas`](028-repartir-la-solicitud-por-oleadas/spec.md) |
| 029 | Cancelar y vencer solicitudes | Borrador | [`029-cancelar-y-vencer-solicitudes`](029-cancelar-y-vencer-solicitudes/spec.md) |
| 030 | Chatear antes de elegir y mis conversaciones | Borrador | [`030-chatear-antes-de-elegir-y-mis-conversaciones`](030-chatear-antes-de-elegir-y-mis-conversaciones/spec.md) |
| 031 | Moderación de contenido con Azure | Borrador | [`031-moderacion-de-contenido-con-azure`](031-moderacion-de-contenido-con-azure/spec.md) |
| 032 | CDN de imágenes: entrega con caché y enlaces firmados | Borrador | [`032-cdn-de-imagenes-con-cache-y-enlaces-firmados`](032-cdn-de-imagenes-con-cache-y-enlaces-firmados/spec.md) |
| 033 | Panel interno con permisos y doble factor (backend) | Borrador | [`033-panel-interno-con-permisos-y-doble-factor`](033-panel-interno-con-permisos-y-doble-factor/spec.md) |
| 034 | Escalado a dos réplicas y contrato vivo | Borrador | [`034-escalado-a-dos-replicas-y-contrato-vivo`](034-escalado-a-dos-replicas-y-contrato-vivo/spec.md) |
| 035 | Redacción de registros con parámetros de consulta y encabezados reenviados | Borrador | [`035-redaccion-de-registros-y-encabezados-reenviados`](035-redaccion-de-registros-y-encabezados-reenviados/spec.md) |
| 036 | Resumen de trabajo del técnico (`GET /api/v1/tecnicos/me/trabajo`) | Borrador | [`036-resumen-de-trabajo-del-tecnico`](036-resumen-de-trabajo-del-tecnico/spec.md) |
| 037 | Registro de dispositivos y despachador de avisos push | Borrador | [`037-registro-de-dispositivos-y-despachador-de-avisos`](037-registro-de-dispositivos-y-despachador-de-avisos/spec.md) |
| BUG-002 | Falta Strict-Transport-Security en el gateway (KNOCK-239) | Implementada | [`BUG-002-falta-strict-transport-security-en-el-gateway`](BUG-002-falta-strict-transport-security-en-el-gateway/spec.md) |
| BUG-003 | El gateway y Mensajería exponen `Server: Kestrel` (KNOCK-240) | Implementada | [`BUG-003-el-gateway-expone-el-encabezado-server`](BUG-003-el-gateway-expone-el-encabezado-server/spec.md) |
| BUG-004 | Mensajería sin JWT, encabezados ni límite propios (KNOCK-241) | Implementada | [`BUG-004-mensajeria-sin-jwt-encabezados-ni-limite`](BUG-004-mensajeria-sin-jwt-encabezados-ni-limite/spec.md) |
