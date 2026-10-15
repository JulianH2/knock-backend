---
id: "TT-BD-01"
tipo: "tarea técnica"
jira: "KNOCK-22"
titulo: "PostgreSQL + PostGIS en Docker Compose y conexión con EF Core (Npgsql + NetTopologySuite) y migraciones"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "Ángel Alexis Blas Castillo"
puntos: "2"
fuente: "TT-BD-01_PostgresqlPostgisDockerCompose_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-BD-01_PostgresqlPostgisDockerCompose_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-22); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-BD-01 — PostgreSQL + PostGIS en Docker Compose y conexión con EF Core (Npgsql + NetTopologySuite) y migraciones

## 1. Datos generales

- **ID de la tarea:** TT-BD-01
- **Puntos:** 2
- **Título:** PostgreSQL + PostGIS en Docker Compose y conexión con EF Core (Npgsql + NetTopologySuite) y migraciones
- **Área:** BD · Base de datos
- **Épica:** EP-13
- **Responsable:** Ángel Alexis Blas Castillo
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Martes 13
- **Clave de Jira:** KNOCK-22
- **Rama:** chore/KNOCK-22-postgres-postgis-efcore  →  PR a sprint-00
- **Depende de:** TT-API-01
- **Terminada cuando:** La API migra la base local con un comando.

## 2. Objetivo

Que todos tengan la misma base de datos local con soporte geográfico y que el esquema se cree solo con migraciones.

## 3. Alcance

- PostgreSQL 16 + PostGIS en Docker Compose (y Redis para más adelante).
- Conexión con EF Core y NetTopologySuite.
- Un esquema por módulo.
- Primera migración.

## 4. Fuera de alcance

- Tablas del catálogo (TT-BD-02).
- Base en Azure (TT-AZ-02).

## 5. Pasos

1. Crear deploy/docker-compose.yml con postgis/postgis (versión 16) en el puerto 5432 y redis:7; datos en un volumen; contraseña desde un archivo .env que no se sube.
1. En el módulo Catalog agregar Npgsql.EntityFrameworkCore.PostgreSQL y su paquete NetTopologySuite; configurar UseNpgsql(cadena, o => o.UseNetTopologySuite()).
1. Un DbContext por módulo con su esquema (HasDefaultSchema("catalog"), "identity", …) y nombres en snake_case (EFCore.NamingConventions).
1. Activar la extensión: modelBuilder.HasPostgresExtension("postgis").
1. Crear la migración inicial y un comando documentado para aplicarla.
1. Conectar /health/ready a la base de datos (AspNetCore.HealthChecks.NpgSql).
1. Agregar en tests un fixture de Testcontainers con la imagen de PostGIS para las pruebas de integración.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| deploy/docker-compose.yml | Base de datos y Redis locales |
| .env.example | Variables de ejemplo sin valores reales |
| dotnet ef migrations add Inicial | Primera migración |
| Testcontainers.PostgreSql | Base real en las pruebas |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | docker compose up levanta la base y SELECT postgis_version() responde | Comando psql |
| V2 | dotnet ef database update crea los esquemas | Comando local |
| V3 | /health/ready responde 200 con la base arriba y 503 sin ella | Prueba manual |
| V4 | Una prueba de integración corre contra Testcontainers | dotnet test |

## 8. Seguridad y reglas

- La contraseña local vive en .env (ignorado). En Azure irá en Key Vault.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida de las migraciones y de la prueba con Testcontainers.

## 11. Referencias

- DECISION_TECNICA.md §5.
