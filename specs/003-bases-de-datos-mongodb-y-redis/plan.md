# Plan 003 — Bases de datos: MongoDB y Redis

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Hacer la prueba técnica de DocumentDB (consulta geoespacial, transacción, índice de texto y conexiones de los tres servicios).
2. Crear la clase de configuración y el cliente único de MongoDB por servicio.
3. Definir la interfaz base de repositorio y su implementación.
4. Crear `db/` con el esquema de numeración y las colecciones técnicas.
5. Preparar los fixtures de Testcontainers para MongoDB y Redis.

## Archivos que se crean o cambian
- `src/<Servicio>/Modules/<Modulo>/Data/`
- `db/001_indices.js` y siguientes
- `tests/IntegrationTest/Fixtures/`
- `deploy/` (respaldo)

## Dependencias
- Controlador oficial de MongoDB para .NET
- StackExchange.Redis
- Testcontainers

## Decisiones de la spec que este plan sigue
- **MongoDB:** es un requerimiento usar una base NoSQL; el nivel gratuito de DocumentDB existe en Azure; un documento guarda el objeto completo y se lee con una sola consulta; el índice `2dsphere` resuelve búsquedas por ubicación; y permite transacciones para el patrón de bandeja de salida.
- **Una base lógica por servicio:** cada servicio es dueño de sus datos.
- **Plan B, MongoDB Atlas:** externo y gratuito, con límites estrechos; solo si DocumentDB no cumple tras la prueba técnica.
- **Redis como segunda base NoSQL (clave-valor):** es muy rápida y compartirla entre copias evita guardar estado en memoria. Riesgo aceptado: si se reinicia, pierde la lista de tokens cancelados.
- **Sin base de grafos:** las relaciones son listas simples por persona, no recorridos de varios saltos; una tercera base costaría sin aportar.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
