---
version: 2.2.0
ratified: pendiente
last_amended: 2026-10-22
status: draft
source_of_truth: .specify/memory/constitution.md
mirror: ./constitution.md
applies_to: [knock-backend, knock-frontend]
methodology: SDD
---

# Constitución — Knock

> Rector SDD de **ambos repositorios** (copia idéntica en knock-backend y knock-frontend; un cambio se hace en los dos con PR propio).
> Ante conflicto con cualquier otro artefacto (specs, AGENTS.md, skills, código), **manda esta constitución**.
> Porqué de cada decisión: `docs/DECISION_TECNICA.md`. Cómo trabajar: `AGENTS.md` de cada repositorio.

## 1. Propósito y alcance

- Knock conecta a personas con técnicos de confianza (mecánica, plomería, electricidad) en la ciudad piloto, colonia por colonia.
- **knock-backend:** API ASP.NET Core en **microservicios** (Identidad y confianza, Mensajería y notificaciones y Mercado, que incluye el módulo Asistente) con un gateway, bases **MongoDB** (Azure DocumentDB), scripts de datos, infraestructura con **Terraform** y pruebas (ADR-001 a ADR-006 en `docs/decisiones/`).
- **knock-frontend:** app móvil Flutter (clientes y técnicos), panel interno Angular (moderación, verificación, métricas) y la landing page (redirección a Google Play, aviso de privacidad, eliminar cuenta y Google Analytics).
- Cada historia (HU-NNN), tarea técnica (TT-ÁREA-NN) o defecto (BUG-NNN) tiene una spec en `specs/<ID>-<slug>/` y un issue de Jira `KNOCK-<N>`. **Sin spec Aprobada no hay código ni cambio de contrato.**
- Fuera del MVP: pagos en la app, servicios remotos, verificación automática KYC, multi-región.

## 2. Principios no negociables

1. **Microservicios con gateway:** tres servicios desplegables por separado y una puerta de entrada; cada servicio es dueño de su base lógica y solo se comunica por su contrato público (REST o gRPC) o por eventos. Dentro de cada servicio los módulos conservan sus fronteras (ADR-001).
2. **Stateless:** nada de estado de negocio en memoria del proceso; lo compartido vive en MongoDB o Redis.
3. **Seguridad y privacidad primero:** validar en la frontera, denegar por defecto, mínimo privilegio, datos mínimos, sin secretos en código, logs ni errores.
4. **Contrato primero:** el OpenAPI de knock-backend es la fuente; knock-frontend lo vendoriza y genera sus clientes; nadie escribe llamadas HTTP a mano.
5. **La IA propone, la persona decide:** ningún cambio de estado, sanción o aprobación lo ejecuta un modelo; la IA nunca recibe INE, CURP, dirección ni teléfono.
6. **Reutilizar antes de crear:** se busca en el mapa de código antes de escribir algo nuevo; el CI rechaza duplicados y violaciones de arquitectura.
7. **Pruebas que prueban:** cada criterio de aceptación tiene un caso `TC-<ID>-KK` y una prueba que falla si la lógica está mal.
8. **Honestidad:** no se declara que algo pasó si no se ejecutó; las limitaciones se informan.
9. **Real-first:** servicios reales en integración (MongoDB, Redis, emulador de Firebase); fakes solo en unitarias. Además, una prueba de humo contra el DocumentDB real, porque Testcontainers con MongoDB no reproduce todo lo que hace DocumentDB.
10. **Caché en todo acceso a datos:** toda lectura pasa por el repositorio con caché de su servicio (una prueba de arquitectura con NetArchTest prohíbe usar `IMongoCollection` fuera de `Data/`). Cada colección declara en su spec `cache: <TTL en s>` o `cache: no` con motivo. Toda escritura invalida sus claves antes de responder. Valores por defecto: catálogos 86 400 s, perfiles públicos 60 s, búsquedas 10 s; solicitudes, cotizaciones, mensajes y datos personales: `no`. La app tiene su propia caché local (§12).
11. **Respuestas rápidas:** todo listado va paginado por cursor, todo campo por el que se filtra u ordena lleva índice y las respuestas se comprimen (§12).
12. **Escalable y sin estado:** durante la ventana de evaluación (§13) cada servicio de negocio y el gateway corren con `minReplicas = maxReplicas = 2` (o más) y sin reglas de escalado; nada de negocio vive en la memoria del proceso; todo el tráfico externo va por HTTPS.
13. **Logout real:** cerrar sesión cancela el token en el backend (lista de revocados en Redis) y borra la sesión y la caché en la app.
14. **Contenido moderado:** todo texto e imagen que sube una persona pasa por la API de moderación antes de guardarse; si la API no responde o no hay cuota, no se guarda y se informa (falla cerrado); no se admite video. Las fotos de INE y selfie no se envían a moderación ni a IA: las revisa una persona en el panel.
15. **Idempotencia:** toda operación que crea algo (solicitud, cotización, mensaje) exige el encabezado `Idempotency-Key`; repetirla con el mismo cuerpo devuelve la respuesta original y no crea un duplicado (§5).
16. **Costo cero:** niveles gratuitos y crédito de estudiante; el número de réplicas se fija por variable de Terraform y baja fuera de la ventana de evaluación (§12); el consumo se revisa cada semana (§13).

## 3. Stack obligatorio

| Capa | Tecnología |
|---|---|
| API | .NET 10 / ASP.NET Core 10 con **Controllers**, REST + OpenAPI 3.1 versionado `/api/v1`, FluentValidation, Serilog, ProblemDetails |
| Datos | MongoDB en Azure DocumentDB (plan B: Atlas) con el controlador oficial de .NET; una base lógica por servicio (ADR-002) |
| Eventos y plazos | Outbox transaccional (transacciones de MongoDB) + mensajes programados (MassTransit 8 o Wolverine, se fija en TT-API-04) · mecanismo de eventos entre servicios y su costo se fijan en TT-ARQ-03 |
| Tiempo real | SignalR con Redis como backplane (reparte los mensajes entre réplicas); push con FCM |
| Datos compartidos | Redis 7 (contenedor propio en Container Apps, §12): tokens revocados, caché, contadores del límite de peticiones y backplane de SignalR. Es la segunda base NoSQL (clave-valor) |
| Acceso | Firebase Phone Auth → JWT propio 15 min + refresh rotativo 30 días (refresh guardado como huella en MongoDB); el logout revoca el `jti` en Redis |
| IA y ML | Azure OpenAI con filtro de datos personales (si no hay cuota, el proveedor alterno lo fija un ADR) · ML.NET |
| App móvil | Flutter estable, Riverpod, go_router, cliente Dart generado, ARB es/en |
| Panel interno | Angular + Angular Material, cliente TypeScript generado, TypeScript strict |
| Nube | Azure (Azure for Students, 5 regiones permitidas): Container Apps por servicio, Azure DocumentDB, Blob, Key Vault, Application Insights; **Terraform**. Redis como contenedor propio; Front Door con WAF y Redis administrado no se despliegan salvo decisión de costos; la CDN de imágenes públicas la fija un ADR (§13) |
| Landing | Railway (espacio ya contratado por el líder del proyecto; HTTPS), Google Analytics solo con consentimiento de cookies |
| Calidad | **Activo hoy:** xUnit · Testcontainers · NetArchTest · Stryker.NET (informativa) · flutter_test · Playwright (panel). **Se activa después:** mutation_test, StrykerJS, k6 (TT-QA-02), RESTler y un job nocturno |
| Seguridad | **Activo hoy:** Semgrep (con SARIF) · gitleaks · OSV-Scanner · dependency-review · Trivy · Dockle · Checkov · oasdiff · ZAP baseline. **Se activa después:** CodeQL, SBOM y firma de imágenes; Checkov pasa de Bicep a Terraform con TT-AZ-02 |

No se agregan dependencias sin evaluar necesidad, licencia, mantenimiento y vulnerabilidades (registrar en `Memoria.md`).

## 4. Arquitectura

### knock-backend
- `src/Knock.<Servicio>.Api/Modules/<Modulo>/{Controllers,Dtos,Validators,Services,Domain,Data,Events}` + `Common/`, `Hosting/`, `Middleware/`; `src/Knock.Gateway/` y `src/Knock.Contracts/` (solo contratos, sin lógica). Mientras TT-ARQ-01 no termine, el código sigue en `src/Knock.Api/`.
- Servicios y módulos: Identidad y confianza (Identity, Trust) · Mensajería y notificaciones (Messaging, Notifications) · Mercado (Catalog, Requests, Reputation, Assistant). Una base lógica MongoDB por servicio.
- Orden fijo de la tubería (fuente: `docs/MIDDLEWARES.md`, comprobado por `PipelineOrderTests`): `ForwardedHeaders → CorrelationId → SecurityHeaders → ExceptionHandling → RequestLogging → HSTS (no Dev) → HttpsRedirection → CORS → RateLimiter → RequestBodyLimit → Authentication → Authorization → Endpoints`. Hoy están implementados `ForwardedHeaders`, `CorrelationId`, `ExceptionHandling`, `RequestLogging`, `RateLimiter`, `RequestBodyLimit`, `Authentication` y `Authorization`; `SecurityHeaders`, `HSTS`, `HttpsRedirection` y `CORS` entran con TT-API-06.
- Errores en ProblemDetails (RFC 9457); estados con máquina de estados en `Domain/` (transición inválida → 409).
- Scripts de índices y semillas en `db/`; infraestructura con Terraform en `deploy/`, con cuatro pasos manuales (crear, desplegar, eliminar lo desplegado y destruir). El clúster gratuito de DocumentDB vive en una raíz de Terraform aparte (`deploy/persistent/`) que el destroy principal no toca.
- **Transición de datos:** mientras TT-BD-06 no termine, el código, las pruebas y el CI existentes con PostgreSQL y EF Core se toleran y no se amplían; ningún código nuevo usa PostgreSQL ni EF Core.

### knock-frontend
- App: `app/lib/{core,features/<feature>/{presentation,domain,data}}`; `domain/` Dart puro; cliente generado en `app/packages/knock_api`.
- Panel: `panel/src/app/{core,shared,features/<feature>/{pages,components,services,models},api}`; componentes sin `HttpClient`; `api/` generado.
- Landing: `landing/` (sitio estático) con las páginas de privacidad, términos y eliminar cuenta.
- Contrato vendorizado en `contracts/openapi.v1.json`.

## 5. Código y datos

- Inglés en código; español en specs, documentación y textos de UI (archivos de traducción).
- MongoDB: colecciones en `snake_case` plural, campos en camelCase, `_id` de tipo `ObjectId`, fechas en UTC y dinero como `Decimal128` en MXN. El campo `version` del documento se devuelve como `ETag` y se exige como `If-Match` al escribir (412 si cambió), así una sola pieza cubre la caché de lectura y la concurrencia optimista.
- Idempotencia: el encabezado `Idempotency-Key` (UUID) se guarda en el campo `idempotencyKey` con índice único por usuario y operación durante 24 h; con el mismo cuerpo devuelve la respuesta original, con otro cuerpo devuelve 422.
- Complejidad ≤ 10 por método; warnings como errores; sin `TODO` sin issue; sin código muerto.
- Supresiones de analizadores solo con excepción registrada (`EXC-NNN`, ver §8).

## 6. Seguridad y privacidad

- OWASP Top 10 y OWASP API Security Top 10 como matriz mínima (skill `security-core`).
- Autorización por recurso en el servidor; el técnico solo ve la colonia hasta ser elegido; panel con permisos y doble factor.
- **Biométricos (INE y selfie):** contenedor privado y separado, cifrados con llave propia por verificación, enlaces de 5 min, registro de cada acceso; se borran al decidir o a los 7 días destruyendo la llave. Nunca en la base, logs, fixtures ni datos de prueba reales.
- CURP solo como HMAC; dirección y celular cifrados; ubicación del dispositivo convertida en colonia y descartada.
- Secretos solo en Key Vault o variables; nada en el repositorio. Acciones de GitHub fijadas a SHA; permisos mínimos por job.
- Límite de peticiones: los servicios internos no se exponen a internet (ingreso interno). El gateway limita por IP antes de autenticar y por usuario (`sub`) después, con contadores en Redis; más estricto en inicio de sesión y envío de SMS. Cada servicio limita por la IP original solo si `ForwardedHeaders` confía en el gateway (red del entorno de Container Apps). Valores en configuración.
- JWT: el gateway y cada servicio validan el token (HS256, clave en Key Vault, solo ese algoritmo); un ingreso interno no sustituye a la autenticación. Riesgo aceptado: cualquier servicio que tenga la clave podría emitir tokens; se reevalúa con un algoritmo asimétrico si el proyecto sigue después del 2-dic.
- SMS solo a números +52. Los SMS reales requieren el plan Blaze de Firebase, que se activa antes de abrir la prueba cerrada de Google Play (los testers son más de los 10 números de prueba permitidos), con presupuesto y alertas; hasta entonces, números de prueba.
- Eliminar cuenta con confirmación por SMS y 7 días de gracia; las opiniones se anonimizan y no se borran; los reportes se conservan solo el tiempo que obligue la ley.
- No se usan datos personales reales en desarrollo ni en pruebas (no solo los biométricos).
- El aviso de privacidad declara que los datos se alojan en Estados Unidos (Azure y Google) y pide consentimiento expreso para los datos sensibles (INE y selfie).

## 7. Pruebas y calidad

- Doce tipos posibles: unitarias, widget/componente, integración, aceptación, seguridad negativa, contrato, mutación, arquitectura, E2E, rendimiento, usabilidad, accesibilidad (+ fuzzing, base de datos, smoke, caos, compatibilidad cuando aplique). Cada spec declara cuáles aplican y por qué.
- Cobertura mínima: backend 60 % líneas y 50 % ramas; app 50 % líneas; panel 60 % líneas y 50 % ramas.
- Mutación por módulo crítico: sesión (Identity) ≥ 80 %; reglas de negocio (Requests, Trust, Reputation; `domain/` de la app) ≥ 70 %.
- El umbral de mutación se aplica en un job nocturno (por crear); mientras no exista es una meta y no un control. Hoy Stryker corre informativo en cada PR (`--break-at 0`).
- Accesibilidad en la app: etiquetas semánticas en cada control y áreas táctiles de al menos 48 dp.
- Rendimiento con k6 contra 2 réplicas; cada técnica de caché (principio 10) tiene una prueba que comprueba la invalidación al escribir.
- Los umbrales no se bajan para que el CI pase.

## 8. Vulnerabilidades y excepciones

- Crítica o alta explotable en el contexto de ejecución → bloquea el merge.
- Falso positivo o riesgo aceptado → `security/exceptions.yaml` con id, herramienta, regla, alcance, justificación con evidencia, responsable, fecha, vencimiento (máx. 90 días) y plan. El CI falla con excepciones vencidas o supresiones sin `EXC-NNN`.
- Prohibidas las supresiones globales o silenciosas.

## 9. Flujo de trabajo

- Ramas: `main` (producción) ← `release` (estabilización: sin entorno desplegado; las pruebas pesadas pasan a correr aquí cuando se configure en el CI) ← `develop` (integración) ← `sprint-XX` ← `<tipo>/KNOCK-<N>-<english-description>`; `hotfix/KNOCK-<N>-…` desde main. El CI rechaza nombres y combinaciones fuera del esquema. Hay un solo entorno en Azure y el único despliegue sale de `main`.
- Commits en inglés, Conventional Commits `type(scope): description in english, imperative, lowercase (KNOCK-N)` (la clave es obligatoria y coincide con la de la rama; sin atribución a IA ni `Co-Authored-By` de agentes; el de una persona del equipo sí se permite); PR `KNOCK-N · <ID> <título>` con plantilla, informe de verificación y sección «Uso de IA»; aprueba y fusiona el Tech Lead (cuando él es el autor, aprueba el revisor de pares); todo PR tiene al menos un comentario de revisión de otro integrante; checks requeridos en verde.
- Versiones `X.Y.Z` por componente (cada servicio, gateway, app, panel y landing), con la **matriz de puntos** y primera versión `1.0.0`. Cada PR declara `puntos-<componente>-N` por cada componente que toca; un despliegue con 0 puntos no cambia la versión; el despliegue suma los puntos y aplica la versión. El `versionCode` de Android es `X×1 000 000 + Y×10 000 + Z×100 + n` (n = subida de esa versión, de 0 a 99). Hasta que TT-CD-03 implemente el control en el CI, la etiqueta se revisa a mano. Esquema y matriz en `docs/VERSIONAMIENTO.md`.
- Todo despliegue sale del pipeline; nadie sube a producción a mano.
- Una sola cuenta de GitHub y un solo correo del proyecto para dar de alta los servicios.
- Definición de terminado: criterios cumplidos, pruebas del plan en verde, PR revisado, CI en verde, «Uso de IA» completo, `Memoria.md` al día y la fila correspondiente de `docs/CUMPLIMIENTO_LINEAMIENTOS.md` actualizada.

## 10. Agentes de IA

- Todo código generado o modificado por IA pasa los mismos controles que cualquier otro cambio.
- Los commits y PR no llevan atribución a IA (trailers `Co-Authored-By` de agentes ni líneas «Generated with…»); el uso de IA se declara solo en la sección «Uso de IA» de la plantilla del PR. Un `Co-Authored-By` de una persona del equipo sí se permite.
- Prohibido: omitir pruebas, desactivar reglas/escáneres/workflows, cambiar pruebas para ocultar fallos, agregar dependencias sin evaluar, exponer secretos o datos sensibles, afirmar ejecuciones no hechas, marcar vulnerabilidades como resueltas sin verificar, operaciones destructivas o sobre producción sin autorización explícita.
- Las instrucciones de agentes no sustituyen los controles de CI ni la revisión humana de cambios sensibles.
- MCP se usa donde el servicio lo ofrece (Jira, GitHub y MongoDB de solo lectura en desarrollo).

## 11. Gobernanza SDD

- Estados de spec: Borrador (agente) → Aprobada (persona; habilita el código; requiere KNOCK-N y revisor) → Firmada (tras el merge; no se edita) → Reemplazada (por <ID>; cuando otra spec la sustituye, como TT-BD-01 y TT-BD-02 por la migración a MongoDB).
- `Memoria.md` por repositorio: estado, decisiones con porqué y errores a evitar. Sin datos sensibles.
- Cambios a esta constitución: PR en ambos repositorios con la aprobación de al menos 2 de los 4 integrantes, `version` incrementada y registro en `Memoria.md`.

## 12. Rendimiento, caché y escalabilidad

- **Caché en tres niveles:** base de datos (capa de acceso con Redis, `cache-aside`, invalidación al escribir, índices y proyecciones), backend (`ETag` débil `W/` y respuesta 304; las respuestas por usuario llevan `Cache-Control: private, no-cache`) y app (caché local con revalidación `ETag`). Los valores por defecto están en el principio 10.
- **Paginación por cursor:** 20 por página, máximo 50.
- **Compresión:** respuestas JSON de más de 1 KB, salvo `/auth/*` y las que llevan tokens (riesgo BREACH); serializadores JSON generados en compilación; fotos reducidas en el teléfono antes de subirlas.
- **Réplicas:** en la ventana de evaluación, `minReplicas = maxReplicas = 2`. Fuera de ella el número se cambia a mano por variable de Terraform (1; 0 solo para un servicio que no ejecuta procesos con tiempo). El servicio que ejecuta procesos con tiempo (oleadas, vencimientos, borrados) mantiene al menos 1 réplica; con 2, cada tarea se reserva con una operación atómica para que solo una réplica la ejecute.
- **Redis:** contenedor propio de 1 réplica, ingreso TCP interno, contraseña desde Key Vault y sin persistencia. Riesgo aceptado: si se reinicia, un access token cerrado vale hasta 15 min; la revocación del refresh es permanente en MongoDB. No es gratis: su consumo cuenta contra el crédito.
- **Balanceo:** la entrada de Container Apps; cada servicio expone `/health` y `/health/ready` y lee los encabezados reenviados.
- **Observabilidad:** Application Insights con `X-Correlation-Id` en toda llamada; los registros no llevan datos personales, y una prueba pasa un celular +52 y una CURP por Serilog y comprueba que no salen.

## 13. Nube y costo

- **Costo cero:** niveles gratuitos y crédito de Azure for Students; 2 réplicas fijas solo del 25-nov al 5-dic de 2026; consumo y permisos se revisan cada semana (las alertas de presupuesto avisan, no frenan). Lo que exceda la capa gratuita sale del crédito, y la estimación con tarifas citadas (TT-AZ-04) es requisito antes de abrir la ventana.
- **Servicios sin nivel gratuito** (Front Door con WAF, Redis administrado, registro de contenedores de Azure) no se despliegan sin decisión explícita.
- **Imágenes públicas:** Blob Storage detrás de una CDN de Azure; el producto y su costo se fijan en un ADR antes de la ventana (Front Door Standard o el servicio de CDN que Azure permita crear), encendido solo cuando haga falta. Las fotos de verificación nunca pasan por la CDN.
- **IaC:** todo con Terraform; el pipeline entra a Azure con identidad federada (OIDC), sin contraseñas, con rol mínimo sobre el grupo de recursos de Knock.
- **Moderación:** Content Safety (nivel gratuito, con cuota mensual limitada). Las pruebas de carga usan un moderador falso para no agotarla.
- **IA generativa:** es obligatoria para la entrega. Proveedor: Azure OpenAI; si 3 semanas antes de la evaluación (11-nov) la cuota sigue en cero, un ADR fija el proveedor alterno con el mismo filtro de datos personales (principio 5). Las plantillas y reglas locales solo responden cuando el proveedor falla.
- **Respaldo:** `mongodump` diario a Blob con restauración probada antes de la ventana (TT-BD-04); el DocumentDB gratuito no tiene respaldo.
- **Región:** la base y los servicios van en West US; el grupo de recursos puede estar en otra región (ADR-006).
- **Registro de imágenes:** GitHub Container Registry; imágenes con etiqueta de versión, nunca `latest`.

## 14. App móvil y landing

- **Sesión viva** hasta el logout, con renovación automática; se pierde por logout, token de renovación reutilizado o 30 días sin abrir la app.
- **Permisos** del teléfono pedidos al usarlos, con su motivo y sin romper la app si se niegan; no se pide el permiso de contactos.
- **Aviso de privacidad y eliminar cuenta** dentro de la app, enlazados a la landing (requisito de Google Play).
- **Push (FCM):** solicitud nueva, cotización recibida, cotización elegida y cita confirmada.
- **Modo sin conexión:** caché y borradores locales; SQLite con sincronización en alcance mínimo (catálogos, últimas conversaciones y cola de mensajes por enviar).
- **Maqueta:** ninguna función de la maqueta se elimina; una función se puede posponer como historia en espera.
- **Publicación:** AAB firmado desde GitHub a Google Play con claves en GitHub Secrets; pista interna, prueba cerrada con al menos 12 testers (el plan recluta 20 por si alguno se sale) durante 14 días seguidos, y producción.
- **Landing:** redirección a Google Play, parámetros UTM, páginas de privacidad, términos y eliminar cuenta; Google Analytics solo si la persona acepta las cookies.
- **Entrega del curso (2-dic-2026):** proyectos en `.rar` y el script de la base (`mongosh`) en `db/`.
