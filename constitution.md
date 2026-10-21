---
version: 2.1.0
ratified: pendiente
last_amended: 2026-10-20
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
- **knock-backend:** API ASP.NET Core en **microservicios** (Identidad y confianza, Mensajería y notificaciones, Mercado, Asistente) con un gateway, bases **MongoDB** (Azure DocumentDB), scripts de datos, infraestructura con **Terraform** y pruebas (ADR-001 a ADR-006 en `docs/decisiones/`).
- **knock-frontend:** app móvil Flutter (clientes y técnicos) y panel interno Angular (moderación, verificación, métricas).
- Cada historia (HU-NNN), tarea técnica (TT-ÁREA-NN) o defecto (BUG-NNN) tiene una spec en `specs/<ID>-<slug>/` y un issue de Jira `KNOCK-<N>`. **Sin spec Aprobada no hay código ni cambio de contrato.**
- Fuera del MVP: pagos en la app, servicios remotos, verificación automática KYC, multi-región.

## 2. Principios no negociables

1. **Microservicios con gateway:** cuatro servicios desplegables por separado y una puerta de entrada; cada servicio es dueño de su base lógica y solo se comunica por su contrato público (REST o gRPC) o por eventos. Dentro de cada servicio los módulos conservan sus fronteras (ADR-001).
2. **Stateless:** nada de estado de negocio en memoria del proceso; lo compartido vive en MongoDB o Redis.
3. **Seguridad y privacidad primero:** validar en la frontera, denegar por defecto, mínimo privilegio, datos mínimos, sin secretos en código, logs ni errores.
4. **Contrato primero:** el OpenAPI de knock-backend es la fuente; knock-frontend lo vendoriza y genera sus clientes; nadie escribe llamadas HTTP a mano.
5. **La IA propone, la persona decide:** ningún cambio de estado, sanción o aprobación lo ejecuta un modelo; la IA nunca recibe INE, CURP, dirección ni teléfono.
6. **Reutilizar antes de crear:** se busca en el mapa de código antes de escribir algo nuevo; el CI rechaza duplicados y violaciones de arquitectura.
7. **Pruebas que prueban:** cada criterio de aceptación tiene un caso `TC-<ID>-KK` y una prueba que falla si la lógica está mal.
8. **Honestidad:** no se declara que algo pasó si no se ejecutó; las limitaciones se informan.
9. **Real-first:** servicios reales en integración (MongoDB, Redis, emulador de Firebase); fakes solo en unitarias.

## 3. Stack obligatorio

| Capa | Tecnología |
|---|---|
| API | .NET 10 / ASP.NET Core 10 con **Controllers**, REST + OpenAPI 3.1 versionado `/api/v1`, FluentValidation, Serilog, ProblemDetails |
| Datos | MongoDB en Azure DocumentDB (plan B: Atlas) con el controlador oficial de .NET; una base lógica por servicio (ADR-002) |
| Eventos y plazos | Outbox transaccional (transacciones de MongoDB) + mensajes programados (MassTransit 8 o Wolverine, se fija en TT-API-04) · mecanismo de eventos entre servicios y su costo se fijan en TT-ARQ-03 |
| Tiempo real | SignalR (Redis como backplane solo si ADR-006 lo aprueba); push con FCM |
| Acceso | Firebase Phone Auth → JWT propio 15 min + refresh rotativo 30 días |
| IA y ML | Azure OpenAI con filtro de datos personales · ML.NET |
| App móvil | Flutter estable, Riverpod, go_router, cliente Dart generado, ARB es/en |
| Panel interno | Angular + Angular Material, cliente TypeScript generado, TypeScript strict |
| Nube | Azure (Azure for Students, 5 regiones permitidas): Container Apps por servicio, Azure DocumentDB, Blob, Key Vault, Application Insights; **Terraform**. Redis y Front Door solo si la decisión de costos lo permite (ADR-003, ADR-006) |
| Calidad | xUnit · Testcontainers · NetArchTest · Stryker.NET · flutter_test · mutation_test · Playwright · StrykerJS · k6 · RESTler |
| Seguridad | CodeQL · Semgrep · gitleaks · OSV-Scanner · dependency-review · Trivy · Dockle · ZAP · SBOM + firma de imágenes |

No se agregan dependencias sin evaluar necesidad, licencia, mantenimiento y vulnerabilidades (registrar en `Memoria.md`).

## 4. Arquitectura

### knock-backend
- `src/Knock.<Servicio>.Api/Modules/<Modulo>/{Controllers,Dtos,Validators,Services,Domain,Data,Events}` + `Common/`, `Hosting/`, `Middleware/`; `src/Knock.Gateway/` y `src/Knock.Contracts/` (solo contratos, sin lógica). Mientras TT-ARQ-01 no termine, el código sigue en `src/Knock.Api/`.
- Servicios y módulos: Identidad y confianza (Identity, Trust) · Mensajería y notificaciones (Messaging, Notifications) · Mercado (Catalog, Requests, Reputation) · Asistente (Assistant). Una base lógica MongoDB por servicio.
- Pipeline fijo: `SecurityHeaders → ExceptionHandling → ForwardedHeaders → HSTS (no Dev) → HttpsRedirection → CORS → RateLimiter → Authentication → Authorization → Controllers / Hubs`.
- Errores en ProblemDetails (RFC 9457); estados con máquina de estados en `Domain/` (transición inválida → 409).
- Scripts de índices y semillas en `db/`; infraestructura con Terraform en `deploy/`.

### knock-frontend
- App: `app/lib/{core,features/<feature>/{presentation,domain,data}}`; `domain/` Dart puro; cliente generado en `app/packages/knock_api`.
- Panel: `panel/src/app/{core,shared,features/<feature>/{pages,components,services,models},api}`; componentes sin `HttpClient`; `api/` generado.
- Contrato vendorizado en `contracts/openapi.v1.json`.

## 5. Código y datos

- Inglés en código; español en specs, documentación y textos de UI (archivos de traducción).
- MongoDB: colecciones y campos en `snake_case`, identificadores `uuid` v7, fechas en UTC, dinero como decimal en MXN y campo de versión del documento para la concurrencia optimista (se confirma en TT-BD-06).
- Complejidad ≤ 10 por método; warnings como errores; sin `TODO` sin issue; sin código muerto.
- Supresiones de analizadores solo con excepción registrada (`EXC-NNN`, ver §8).

## 6. Seguridad y privacidad

- OWASP Top 10 y OWASP API Security Top 10 como matriz mínima (skill `security-core`).
- Autorización por recurso en el servidor; el técnico solo ve la colonia hasta ser elegido; panel con permisos y doble factor.
- **Biométricos (INE y selfie):** contenedor privado y separado, cifrados con llave propia por verificación, enlaces de 5 min, registro de cada acceso; se borran al decidir o a los 7 días destruyendo la llave. Nunca en la base, logs, fixtures ni datos de prueba reales.
- CURP solo como HMAC; dirección y celular cifrados; ubicación del dispositivo convertida en colonia y descartada.
- Secretos solo en Key Vault o variables; nada en el repositorio. Acciones de GitHub fijadas a SHA; permisos mínimos por job.

## 7. Pruebas y calidad

- Doce tipos posibles: unitarias, widget/componente, integración, aceptación, seguridad negativa, contrato, mutación, arquitectura, E2E, rendimiento, usabilidad, accesibilidad (+ fuzzing, base de datos, smoke, caos, compatibilidad cuando aplique). Cada spec declara cuáles aplican y por qué.
- Cobertura mínima: backend 60 % líneas y 50 % ramas; app 50 % líneas; panel 60 % líneas y 50 % ramas.
- Mutación por módulo crítico: sesión (Identity) ≥ 80 %; reglas de negocio (Requests, Trust, Reputation; `domain/` de la app) ≥ 70 %.
- Los umbrales no se bajan para que el CI pase.

## 8. Vulnerabilidades y excepciones

- Crítica o alta explotable en el contexto de ejecución → bloquea el merge.
- Falso positivo o riesgo aceptado → `security/exceptions.yaml` con id, herramienta, regla, alcance, justificación con evidencia, responsable, fecha, vencimiento (máx. 90 días) y plan. El CI falla con excepciones vencidas o supresiones sin `EXC-NNN`.
- Prohibidas las supresiones globales o silenciosas.

## 9. Flujo de trabajo

- Ramas: `main` (producción) ← `release` (Staging) ← `develop` (integración) ← `sprint-XX` ← `<tipo>/KNOCK-<N>-<english-description>`; `hotfix/KNOCK-<N>-…` desde main. El CI rechaza nombres y combinaciones fuera del esquema.
- Commits en inglés, Conventional Commits `type(scope): description in english, imperative, lowercase (KNOCK-N)` (la clave es obligatoria y coincide con la de la rama; sin atribución a IA ni `Co-Authored-By` de agentes; el de una persona del equipo sí se permite); PR `KNOCK-N · <ID> <título>` con plantilla, informe de verificación y sección «Uso de IA»; 1 aprobación de otro integrante; checks requeridos en verde.
- Versiones con release-please por componente, con etiquetas `<componente>-vX.Y.Z` (esquema y matriz en `docs/VERSIONAMIENTO.md`).
- Definición de terminado: criterios cumplidos, pruebas del plan en verde, PR revisado, CI en verde, «Uso de IA» completo, `Memoria.md` al día.

## 10. Agentes de IA

- Todo código generado o modificado por IA pasa los mismos controles que cualquier otro cambio.
- Los commits y PR no llevan atribución a IA (trailers `Co-Authored-By` de agentes ni líneas «Generated with…»); el uso de IA se declara solo en la sección «Uso de IA» de la plantilla del PR. Un `Co-Authored-By` de una persona del equipo sí se permite.
- Prohibido: omitir pruebas, desactivar reglas/escáneres/workflows, cambiar pruebas para ocultar fallos, agregar dependencias sin evaluar, exponer secretos o datos sensibles, afirmar ejecuciones no hechas, marcar vulnerabilidades como resueltas sin verificar, operaciones destructivas o sobre producción sin autorización explícita.
- Las instrucciones de agentes no sustituyen los controles de CI ni la revisión humana de cambios sensibles.

## 11. Gobernanza SDD

- Estados de spec: Borrador (agente) → Aprobada (persona; habilita el código; requiere KNOCK-N y revisor) → Firmada (tras el merge; no se edita).
- `Memoria.md` por repositorio: estado, decisiones con porqué y errores a evitar. Sin datos sensibles.
- Cambios a esta constitución: PR en ambos repositorios aprobado por el equipo, `version` incrementada y registro en `Memoria.md`.
