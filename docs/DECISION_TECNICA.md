# Knock — Decisión técnica y justificación

> **Estado:** v2 alineada con la Propuesta de Proyecto v2.0 · octubre 2026
> **Reemplaza** las decisiones #18 (plataforma), #19 (panel) y #20 (chat) de `DECISIONES.md`. La #4 (verificación) queda como la cerró el equipo en la propuesta v2.0: fotos cifradas con llave propia y borradas al decidir o a los 7 días.
> **Repositorios:** `knock-backend` (API + scripts de base de datos + infraestructura) y `knock-frontend` (app Flutter + panel Angular). Este documento es idéntico en ambos.
> **Rector de implementación:** `.specify/memory/constitution.md`. Este documento explica el **porqué**; la constitución fija el **qué no se negocia**.

---

## 1. Resumen en una tabla

| Pieza | Decisión | En una línea |
|---|---|---|
| Arquitectura | **Monolito modular** con rebanadas verticales por HU | Un solo desplegable, módulos con fronteras estrictas, listo para separarse |
| Backend | **ASP.NET Core 10 con Controllers** | Mismo stack y convenciones que los repos de referencia del profesor |
| API hacia clientes | **REST + OpenAPI**, versionada (`/api/v1/...`) | Contrato explícito, cliente Dart generado, se prueba en Scalar |
| API interna (futuro) | **gRPC** solo si un módulo se separa en servicio | Hoy no hay comunicación entre servicios |
| Tiempo real | **SignalR + Redis** (backplane) | Chat y avisos en vivo; nativo de .NET |
| Notificaciones con app cerrada | **Firebase Cloud Messaging** | Ya se usa Firebase para el SMS |
| Proveedores externos → Knock | **Webhooks firmados** | Es como trabajan KYC y pagos |
| Base de datos | **PostgreSQL 16 + PostGIS** (EF Core + Npgsql + NetTopologySuite) | Relacional para estados; geoespacial para «técnicos cerca» |
| Procesos diferidos | **Outbox + mensajes programados** (MassTransit 8 o Wolverine) | Oleadas a 15 min, vencimiento a 48 h, borrados programados, saga |
| Caché y estado compartido | **Redis** | Blacklist de tokens, intentos, caché de lecturas, backplane |
| Login | **Firebase Phone Auth + JWT propios** con refresh rotativo | El servidor valida a Firebase y emite su sesión |
| App | **Flutter** con **Clean Architecture de 3 capas** | iOS + Android; misma arquitectura que el repo móvil del profesor |
| Panel interno | **Angular + Angular Material** | Framework completo para apps administrativas: formularios, tablas, rutas por permiso y TypeScript tipado |
| Repositorios | **knock-backend** y **knock-frontend** | Cada equipo trabaja en su stack; el contrato OpenAPI es la frontera |
| Nube | **Azure** (con equivalencias AWS documentadas) | Coherente con .NET y Azure OpenAI |
| IA en el producto | **Azure OpenAI** + **ML.NET** | La IA propone; una persona o una regla decide |
| Forma de desarrollar | **Spec-Driven Development con agentes de IA** | Constitución + specs + skills + agentes revisores |
| DevSecOps | Pipeline de seguridad en cada PR | SAST, dependencias, secretos, contenedores, DAST |

---

## 2. Arquitectura: monolito modular

### Decisión
Un solo backend desplegable, dividido en módulos con fronteras estrictas. Cada módulo es dueño de su esquema de base de datos y expone una interfaz C#; ningún módulo lee las tablas de otro.

| Módulo | Responsabilidad |
|---|---|
| `Identity` | Validar token de Firebase, sesión propia, cambio de número |
| `Catalog` | Técnicos, oficios (como datos), servicios, colonias, búsqueda geográfica |
| `Requests` | Solicitudes, oleadas, cotizaciones, citas (máquina de estados) |
| `Messaging` | Chat en la app, presencia |
| `Reputation` | Reseñas, niveles, orden «Recomendadas» |
| `Trust` | Verificación, reportes por gravedad, moderación |
| `Notifications` | Avisos, FCM, plantillas |
| `Assistant` | IA generativa (asistente, resumen, borrador de cotización) |

### Alternativas evaluadas

| Opción | Por qué no |
|---|---|
| Microservicios desde el inicio | Multiplica red, despliegues, consistencia distribuida y depuración. Con 4 personas el costo de coordinación supera cualquier beneficio. Los agentes de IA rinden peor con el contexto repartido entre repos |
| Monolito sin módulos | Rápido al inicio, pero imposible de separar después y difícil de asignar por persona |
| Backend como servicio (Supabase/Firestore) | La lógica de negocio (oleadas, estados, reportes) quedaría dispersa en reglas y funciones; choca con el requisito de .NET |

### Justificación
- Es el **mismo patrón** de los repos de referencia (`WebAPIDevSecOpsScalling`): un desplegable que escala horizontalmente quitando el estado de memoria (Redis), con varias instancias detrás de un balanceador y lo pesado en colas.
- Los agentes de IA trabajan mejor en **un repositorio con fronteras claras y pruebas** que en varios servicios.
- Escala de sobra para el piloto (Pachuca, colonia por colonia) y para varias ciudades.

### Camino de crecimiento (no se construye hoy)

| Orden | Qué se separa | Disparador medible |
|---|---|---|
| 1 | `Messaging` (chat y tiempo real) | Conexiones simultáneas que saturan las instancias de la API |
| 2 | `Notifications` | Picos de envío que retrasan otras operaciones |
| 3 | `Assistant` (llamadas a IA) | Latencia o costo de IA afectando la API |
| 4 | Búsqueda geográfica con réplicas de lectura | El mapa domina la carga de la base |

Al separar un módulo, su interfaz C# se reimplementa como cliente **gRPC**; el resto del código no cambia. La API pública sigue siendo REST.

---

## 3. Estilo de API

### Decisión
- **App y panel → backend:** REST + OpenAPI 3.1, versionado por URL, errores en **ProblemDetails (RFC 9457)**.
- **Cliente de Dart generado** desde el OpenAPI; nadie escribe a mano llamadas HTTP en la app.
- **Tiempo real:** SignalR. **App cerrada:** FCM. **Proveedores:** webhooks con firma verificada.

### Comparación completa

| Estilo | A favor para Knock | En contra para Knock | Veredicto |
|---|---|---|---|
| **REST + OpenAPI** | Nativo en .NET, cliente Dart generado, archivos triviales, caché HTTP, Scalar para probar y demostrar | Varias llamadas por pantalla | ✅ API principal |
| **gRPC** | Contrato Protobuf estricto, código generado, rápido, streaming | Panel web necesita gRPC-Web + proxy; archivos incómodos; no se prueba con navegador ni curl; sin caché HTTP | 🟡 Solo entre servicios, si se separan |
| **GraphQL** | Pantallas complejas en una llamada | N+1, límites de costo, caché difícil, archivos no estándar, suscripciones complejas; brilla con muchos clientes distintos y Knock tiene uno | ❌ |
| **Connect RPC** | gRPC sin proxy | Soporte inmaduro en .NET | ❌ |
| **OData** | Filtros gratis en ASP.NET | Pesado, cliente Dart pobre, expone el modelo | ❌ |
| **JSON-RPC** | Simple | Sin contratos ni herramientas estándar | ❌ |
| **tRPC** | Tipos de punta a punta | Solo TypeScript | ❌ No aplica |
| **SOAP** | Contrato estricto | Obsoleto para móviles | ❌ |
| **WebSockets / SignalR** | Chat y presencia | No sirve como API general | ✅ Solo tiempo real |
| **SSE** | Avisos simples | Un solo sentido; no cubre el chat | 🟡 |
| **Webhooks** | Estándar de proveedores | Solo servidor a servidor | ✅ Proveedores |

### Por qué no empezar con gRPC
- **Por dentro:** en un monolito los módulos se llaman como métodos de C# en la misma memoria. gRPC convertiría llamadas locales en llamadas de red sin ganar nada.
- **Hacia la app:** se pagarían todos los días el proxy de gRPC-Web, la subida de fotos y la depuración, para ganar un rendimiento que solo se nota con miles de llamadas por segundo entre servicios.
- **Por qué se quita GraphQL:** los repos de referencia usan REST con catálogo de endpoints, contratos JSON y pruebas de contrato; GraphQL iría a contracorriente sin beneficio para un solo cliente.

### ¿Cambia con otro lenguaje de backend?
No en lo esencial: los motivos dependen del cliente (Flutter) y de no tener servicios internos.

| Backend | Efecto |
|---|---|
| Python (FastAPI) | REST aún más atractivo (OpenAPI desde los tipos); mejor ecosistema de ML |
| Dart (Serverpod) | Aparece una RPC propia con cliente generado; ecosistema pequeño (PostGIS, Azure) |
| TypeScript | Connect RPC se vuelve viable |
| **.NET (elegido)** | REST afuera, gRPC adentro si crece; alineado con los repos de referencia |

---

## 4. Backend: ASP.NET Core 10 con Controllers

- **Controllers en vez de Minimal APIs:** la diferencia de rendimiento es irrelevante para Knock; los repos de referencia, sus convenciones, pruebas de arquitectura y skills usan Controllers.
- Pipeline de middlewares fijo (ver constitución §4), `Asp.Versioning`, FluentValidation, Serilog con CorrelationId, health `/health` y `/health/ready`, Scalar solo en desarrollo.
- Sin `try/catch` en controllers: un middleware convierte excepciones en ProblemDetails.

---

## 5. Datos: PostgreSQL + PostGIS

| Alternativa | Por qué no |
|---|---|
| SQL Server (el de los repos de referencia) | Tiene tipos geográficos, pero PostGIS es más completo (índices GiST, `ST_DWithin`, polígonos de colonias) y no requiere licencia en la nube |
| MongoDB / NoSQL | Los estados de solicitudes y citas exigen transacciones y restricciones |
| Firestore | Igual que arriba, y la lógica quedaría en el cliente |

- Un esquema por módulo, nombres en `snake_case` (convención de PostgreSQL), concurrencia optimista con `xmin`.
- La colonia se resuelve con **una tabla propia** (catálogo SEPOMEX + polígonos) y `ST_Contains`; no se pagan geocodificaciones.

---

## 6. Procesos diferidos y eventos

### Decisión
**Outbox transaccional de EF Core** con mensajes programados (**MassTransit 8 o Wolverine**, se fija en TT-API-04): el evento se guarda en la misma transacción que el cambio de estado, así no se pierde si el servidor se reinicia. Mensajes programados para los plazos.

| Proceso | Evento / mensaje programado |
|---|---|
| Ampliar oleada si hay < 2 cotizaciones | `RevisarOleada` a los 15 min |
| Vencer solicitud | `VencerSolicitud` a las 48 h |
| Borrar fotos de verificación | `PurgeVerificationMedia` a los 7 días (o al decidir) |
| Eliminar cuenta | `EjecutarEliminacion` tras 7 días de gracia |
| Cerrar ventana de reseña / reporte | a los 30 y 7 días |

Transporte: **en memoria** en desarrollo y pruebas; **Azure Service Bus** en la nube (equivale a SQS en los repos de referencia).

**Por qué un bus con outbox y no Hangfire:** MassTransit es la herramienta de los repos de referencia (saga con compensación) y, igual que Wolverine, cubre eventos y plazos con una sola pieza y sin perder mensajes.

---

## 7. Tiempo real y notificaciones

- **SignalR** para chat, «escribiendo…» y avisos en vivo, con **Redis como backplane** cuando haya más de una instancia.
- Si el destinatario no está conectado → **FCM**.
- Se reemplazan las suscripciones de GraphQL de la decisión #20.

---

## 8. Autenticación

- **Firebase Phone Auth** en la app (números de prueba en desarrollo y demo).
- El backend valida el ID token de Firebase y emite **sus propios JWT** (HS256, 15 min) + **refresh rotativo** (30 días), revocación con `blacklist:{jti}` en Redis — mismo esquema que los repos de referencia.
- Pendiente: confirmar costo y límites de SMS antes de la demo.

---

## 9. App, panel y dos repositorios

### App (knock-frontend/app)
```
lib/
  core/                 # red (interceptor de sesión), errores, tema, i18n (ARB), almacenamiento seguro
  features/<feature>/
    presentation/       # pantallas + controladores (Riverpod) → UiState
    domain/             # entidades, casos de uso, interfaces de repositorio (Dart puro)
    data/               # implementaciones de repositorio con el cliente generado
packages/knock_api/     # cliente Dart generado desde el contrato
```
Equivale capa por capa al repo `MobileDevSecOps` del profesor (Compose + ViewModel + UseCases + Ktor + Koin).

### Panel (knock-frontend/panel)
Angular con componentes standalone, signals y Angular Material; `features/<feature>/{pages,components,services,models}`; los componentes no usan `HttpClient`, llaman al servicio de su feature que usa el cliente TypeScript generado (`src/app/api`). Rutas protegidas por permiso y doble factor; la autorización real la hace la API.

### Por qué dos repositorios
| A favor | Cómo se cubre el costo |
|---|---|
| Cada stack con su CI, dependencias y versiones (api-v, app-v, panel-v) | release-please en cada repo |
| Permisos y revisores por área (CODEOWNERS) | Mismas reglas de ramas, commits y PR en ambos |
| Pipelines más rápidos: un cambio de Flutter no compila .NET | El contrato OpenAPI se publica desde el backend y llega al frontend por PR automático (`contract-publish.yml`) |
| | Constitución, skills comunes y guardián idénticos en ambos |

Una historia que toca ambos lados tiene su spec en cada repositorio (misma HU y mismos casos `TC-HU-NNN-KK`; cada uno verifica los suyos y marca «(otro repo)» los del otro) y un PR en cada uno con la misma clave KNOCK-N. **Nota:** el backlog original menciona un monorepo `knock` (TT-REPO-01); actualizar esa ficha a dos repositorios.

## 10. Nube: Azure (con equivalencia AWS)

| Necesidad | Azure (elegido) | Equivalente AWS (repos de referencia) |
|---|---|---|
| API y worker | Container Apps | EC2 / ECS detrás de ALB |
| Base de datos | Database for PostgreSQL Flexible (PostGIS) | RDS PostgreSQL |
| Caché / backplane | Cache for Redis | ElastiCache |
| Colas y programados | Service Bus | SQS (+ DLQ) |
| Archivos privados | Blob Storage + SAS de corta vida | S3 + URLs firmadas |
| CDN | Front Door | CloudFront |
| IA | Azure OpenAI | Bedrock |
| Secretos | Key Vault | Secrets Manager |

Una sola región. Infraestructura como código en `deploy/` (Bicep).

---

## 11. Verificación de identidad (decisión del equipo, propuesta v2.0)

- Revisión manual por un verificador con doble factor (permiso `verification:review`) en menos de 24 h; alerta a las 24 h.
- INE y selfie en un contenedor privado y separado, **cifradas con una llave propia por verificación**, enlaces de 5 minutos y registro de cada acceso.
- Al decidir se **destruye la llave** (borrado criptográfico); si nadie revisa en **7 días**, se borran y el estado vuelve a «Sin verificar».
- Nada de datos reales en desarrollo ni en la prueba cerrada; revisión con abogado antes de un lanzamiento real.
- Un proveedor KYC automático queda fuera del MVP.

## 12. IA dentro del producto

| Sí (la IA propone) | No (decide una persona o una regla) |
|---|---|
| Asistente para describir el problema (texto y foto) | Cambiar estados de solicitudes o citas |
| Clasificar la categoría | Aprobar verificaciones |
| Resumen del chat y bitácora del técnico | Resolver reportes graves |
| Borrador de cotización | Enviar una cotización sin que el técnico la confirme |
| Rango de precio de referencia (ML.NET) | Sancionar cuentas |

Reglas: salida estructurada (JSON con esquema), sin datos personales innecesarios en el prompt (teléfono, dirección exacta), registro de lo enviado, límite de costo por usuario y respuesta de respaldo si el servicio falla.

---

## 13. Desarrollo con IA: Spec-Driven Development

El profesor pide construir con IA. La forma de hacerlo sin generar código que no funciona junto es **SDD**, el mismo método de sus repos `API_SDD` y `WebAPIDevSecOpsScallingSDD`:

1. **Constitución** (`.specify/memory/constitution.md`): lo no negociable. Manda ante cualquier conflicto.
2. **Una spec por issue** en `specs/<ID>-<slug>/` (HU-NNN, TT-ÁREA-NN o BUG-NNN: spec + plan + tareas + gates), generada del backlog con sus casos `TC-<ID>-KK`.
3. **`AGENTS.md`**: el manual operativo que lee cualquier agente.
4. **Skills** (`.agents/skills/`): conocimiento reutilizable (rebanada vertical, PostGIS, eventos, Flutter, seguridad…).
5. **Agentes con permisos distintos**: los que construyen editan; los que revisan (seguridad, trazabilidad) **no pueden editar**.
6. **Gates automáticos** en cada PR y **`Memoria.md`** como bitácora de decisiones.

**Funciona con cualquier agente:** la fuente es neutral (`AGENTS.md` + `.agents/`) y `scripts/sync_agents.py` genera instrucciones, skills, subagentes, reglas por ruta, permisos y **hooks** para Claude Code, Codex, GitHub Copilot, Cursor, Gemini CLI y opencode. Un mismo guardián (`scripts/hooks/knock_guard.py`) bloquea lo que rompe la arquitectura y no deja terminar sin verificar. Ver `docs/SDD_GUIA.md` y `docs/AGENTES_GUARDARRAILES.md`.

**Por qué acelera de verdad:** la IA multiplica la escritura de código, no la integración. La spec le dice qué construir, las pruebas le dicen cuándo terminó, y los revisores sin permiso de edición detectan lo que el constructor pasó por alto.

---

## 14. Riesgos y pendientes

| # | Riesgo / pendiente | Mitigación |
|---|---|---|
| 1 | Costo y límites de SMS de Firebase | Números de prueba; confirmar antes de la demo |
| 2 | Precio de Google Maps | Cuota gratuita + tabla propia de colonias |
| 3 | Datos biométricos | Llave por verificación, borrado a 7 días, doble factor, sin datos reales; abogado antes de lanzar |
| 4 | Paquetes Flutter para captura de INE y mapa con capas | Spike TT-MOB-04 |
| 5 | Curva de .NET, Flutter y Angular | Semana de prueba de roles, skills para agentes y revisión por pares |
| 6 | Desfase de contrato entre repositorios | PR automático del contrato, clientes generados verificados en CI, oasdiff |
| 7 | Textos legales de demostración | Revisión con abogado anotada como riesgo |

---

## Aprobación
- [ ] Equipo (Julián, Guillermo, Erick, Ángel) — fecha:
- [ ] Profesor — fecha:
