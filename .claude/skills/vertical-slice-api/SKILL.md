---
name: vertical-slice-api
description: Cómo construir una rebanada de backend en un módulo de Knock (colecciones e índices, dominio, servicio, controller, DTOs, validadores, eventos y pruebas) respetando las fronteras del monolito modular. Úsala en cada historia o tarea con capa [Back].
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/vertical-slice-api/SKILL.md. No editar a mano. -->
# vertical-slice-api

## Estructura del módulo
```
src/Knock.Api/Modules/<Module>/
  Controllers/V1/<Resource>Controller.cs   # delgado, sin try/catch, sin acceso a la base
  Dtos/                                     # records inmutables, camelCase en JSON
  Validators/                               # AbstractValidator<TDto>, uno por DTO de entrada
  Services/                                 # lógica de aplicación; usa el Store del módulo
  Domain/                                   # entidades, value objects, máquina de estados
  Data/<Module>Store.cs                     # único punto con IMongoCollection; base lógica Mongo:Databases:<Module>
  Events/                                   # records de eventos (nombre en pasado)
  <Module>Api.cs                            # interfaz pública I<Module>Api para otros módulos
```
Copia la forma de `src/Knock.Api/Modules/_Template`; no la modifiques.

## Orden de trabajo
1. Contrato: actualiza OpenAPI esperado y `docs/endpoints.md`.
2. Colecciones e índices: define las llaves en la spec, créalas con `EnsureIndexesAsync` del Store y, si hay catálogo, la semilla en `db/seeds/` (ver `database-scripts`).
3. Dominio + servicio + pruebas unitarias.
4. Controller + pruebas de integración, seguridad y contrato.
5. Eventos vía outbox y registro en `docs/events.md`.

## Reglas
- Un módulo **nunca** referencia `Domain` ni `Data` de otro: usa `I<Otro>Api` o consume un evento.
- Transiciones de estado solo con métodos del dominio (`request.AcceptQuote(...)`); transición inválida → `DomainException` → 409.
- Concurrencia optimista con un campo `version` y actualización condicional por la versión esperada → 409 en conflicto.
- Publica eventos dentro de la misma transacción de MongoDB que el cambio de estado (outbox); nunca después de confirmarla.
- Autorización por recurso en el servicio (el técnico solo ve la colonia hasta ser elegido).
- `[ApiVersion("1.0")]`, `[Route("api/v{version:apiVersion}/<recurso>")]`, `[ProducesResponseType]` para cada código.
- Listas públicas con `CursorPage<T>`; panel con `PagedResult<T>`.

## Verificación
- Pruebas de arquitectura (NetArchTest) siguen verdes.
- Cada caso `TC-<ID>-KK` de este repositorio tiene prueba con `_TC_<ID>_KK` en el nombre.
