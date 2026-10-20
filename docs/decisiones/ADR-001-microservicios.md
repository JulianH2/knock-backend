# ADR-001 — Arquitectura: microservicios con una puerta de entrada

> **Estado:** decidida por el Tech Lead el 2026-10-20.
> **Reemplaza:** la sección 2 («Arquitectura: monolito modular») y la fila «Arquitectura» de la sección 1 de `docs/DECISION_TECNICA.md`.
> **Tarea:** TT-GES-06 · Impacto de implementación: TT-ARQ-01† a TT-ARQ-05††.

## Contexto
Los lineamientos del curso (punto 15) dan libertad entre API o microservicios, y el docente espera microservicios. El equipo decide **microservicios**, cada uno con su propia base de datos. El código ya está dividido en módulos con fronteras estrictas (`Identity`, `Trust`, `Messaging`, `Notifications`, `Catalog`, `Requests`, `Reputation`, `Assistant`), que se hablan por interfaces C#; la decisión anterior ya los dejó «listos para separarse».

## Decisión
Cuatro servicios y una puerta de entrada, desplegados por separado desde el repositorio `knock-backend`:

| Servicio | Módulos que agrupa | Base lógica propia | Responsable técnico |
|---|---|---|---|
| **Identidad y confianza** (`Knock.Identity.Api`) | Identity, Trust | `identity` | Guillermo |
| **Mensajería y notificaciones** (`Knock.Messaging.Api`) | Messaging, Notifications | `messaging` | Guillermo |
| **Mercado** (`Knock.Market.Api`) | Catalog, Requests, Reputation | `market` | Alexis |
| **Asistente** (`Knock.Assistant.Api`) | Assistant (IA generativa y ML) | `assistant` | Alexis, con apoyo de José Julián en IA |
| **Gateway** (`Knock.Gateway`, YARP) | — | ninguna | José Julián y Guillermo |

## Reglas
1. **Ningún servicio lee la base de otro.** Lo que necesite de otro servicio lo pide por su contrato.
2. **Los contratos viven en una biblioteca compartida (`Knock.Contracts`) que contiene solo tipos y contratos, sin lógica de negocio**, para no acoplar los servicios.
3. **Entrada única:** la app y el panel llaman solo al gateway, que termina HTTPS, valida el JWT, aplica límite de tasa y enruta. Los servicios internos no se exponen a internet.
4. **Comunicación:** síncrona por REST con OpenAPI (o gRPC, si TT-ARQ-01† lo justifica) y asíncrona por eventos con outbox transaccional (TT-API-04). El mecanismo de mensajería y su costo se fijan en TT-ARQ-03† **(confirmar)**.
5. **Trazabilidad:** un identificador de correlación viaja por todas las llamadas; los logs y trazas van a Application Insights (TT-OBS-01).
6. **Cada servicio se construye, prueba y despliega solo** (CI por servicio, imagen propia, módulo de Terraform propio).
7. **Los módulos internos se conservan** con sus fronteras: sigue habiendo un orden por capas dentro de cada servicio.

## Alternativas evaluadas
| Opción | Por qué no |
|---|---|
| Monolito modular (decisión anterior) | Cumple el lineamiento de forma técnica, pero el docente espera microservicios |
| Ocho servicios (uno por módulo) | Multiplica contenedores, costo, pipelines y esfuerzo de coordinación para un equipo de cuatro personas y cinco semanas |
| Tres servicios (juntando Mensajería con Asistente) | Es la salida de emergencia si el tiempo se acaba; los módulos permiten separarlos después |

## Consecuencias
- Cambia la estructura del repositorio, el CI, los Dockerfiles, la infraestructura y el despliegue (ver [`docs/PLAN_ADAPTACION.md`](../PLAN_ADAPTACION.md)).
- **Costo:** cinco contenedores con 2 réplicas son diez réplicas; ver ADR-006.
- **Orden de trabajo:** no se separa nada hasta que (a) el spike de la base de datos responda (ADR-002) y (b) se fusionen los PR abiertos de Sprint 1 (#26, #31, #40, frontend #16).
- Se actualizan, por separado y a cargo del Tech Lead, los archivos protegidos (`AGENTS.md`, `constitution.md`) que mencionan el monolito.

## Pendiente de confirmar
- Mecanismo de eventos y su costo (TT-ARQ-03†).
- Si el Gateway justifica un contenedor propio con 2 réplicas o comparte plan con otro servicio.
