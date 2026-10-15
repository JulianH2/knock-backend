---
id: "TT-BD-02"
tipo: "tarea técnica"
jira: "KNOCK-23"
titulo: "Catálogo base: 3 oficios como datos y colonias de la ciudad piloto (SEPOMEX + polígonos); consulta \"colonia de un punto\""
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "Ángel Alexis Blas Castillo"
puntos: "3"
fuente: "TT-BD-02_CatalogoBase3Oficios_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-BD-02_CatalogoBase3Oficios_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-23); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-BD-02 — Catálogo base: 3 oficios como datos y colonias de la ciudad piloto (SEPOMEX + polígonos); consulta "colonia de un punto"

## 1. Datos generales

- **ID de la tarea:** TT-BD-02
- **Puntos:** 3
- **Título:** Catálogo base: 3 oficios como datos y colonias de la ciudad piloto (SEPOMEX + polígonos); consulta "colonia de un punto"
- **Área:** BD · Base de datos
- **Épica:** EP-13
- **Responsable:** Ángel Alexis Blas Castillo
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Miércoles 14 – jueves 15
- **Clave de Jira:** KNOCK-23
- **Rama:** chore/KNOCK-23-catalogo-oficios-colonias  →  PR a sprint-00
- **Depende de:** TT-BD-01
- **Terminada cuando:** Prueba con PostGIS que devuelve la colonia correcta.

## 2. Objetivo

Tener los datos base que usa casi todo Knock: los 3 oficios como datos (no en el código) y las colonias de la ciudad piloto, con la consulta que convierte una ubicación en colonia sin pagar geocodificación.

## 3. Alcance

- Tabla de oficios con 3 registros y su modalidad (taller o domicilio).
- Tabla de colonias de la ciudad piloto con código postal y geometría.
- Consulta "colonia de un punto" y "colonia por código postal".

## 4. Fuera de alcance

- Administrar oficios y zonas desde el panel (HU-072, Media).
- Lista de espera (HU-009, Sprint 1).

## 5. Pasos

1. Crear catalog.oficios (id, clave, nombre, modalidad, activo) con mecánica (taller), plomería (domicilio) y electricidad (domicilio).
1. Descargar el catálogo de códigos postales de Correos de México (SEPOMEX) y filtrar los municipios de la ciudad piloto [por definir; la maqueta usa Pachuca como ejemplo].
1. SEPOMEX trae nombres y códigos postales, no polígonos. Para las 8 a 12 colonias del piloto dibujar polígonos aproximados en geojson.io; para el resto guardar un punto central.
1. Crear catalog.colonias (id, nombre, cp, municipio, piloto, geom Polygon 4326 nulo, centro Point 4326) con índice GiST.
1. Cargar los datos con una migración de semilla o un script versionado en deploy/seeds/.
1. Consulta 1: dado un punto, la colonia cuyo polígono lo contiene (ST_Contains); si no hay, la de centro más cercano dentro de 2 km (ST_DWithin + orden por distancia); si no, "fuera de cobertura".
1. Consulta 2: dado un código postal, sus colonias.
1. Exponer ambas como métodos del módulo Catalog (interfaz pública) con pruebas contra Testcontainers.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Catálogo de códigos postales de Correos de México | Nombres de colonias y CP |
| geojson.io | Dibujar polígonos aproximados del piloto |
| deploy/seeds/colonias_pachuca.geojson | Datos versionados |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Hay 3 oficios activos | Consulta SQL |
| V2 | Un punto dentro de una colonia piloto devuelve esa colonia | Prueba de integración |
| V3 | Un punto en Tula responde fuera de cobertura | Prueba negativa |
| V4 | El CP 42000 devuelve sus colonias | Prueba de integración |

## 8. Seguridad y reglas

- No se guardan coordenadas de usuarios: la consulta recibe el punto, devuelve la colonia y el punto se descarta.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura del mapa de polígonos y salida de las pruebas.

## 11. Referencias

- DECISIONES.md #1, #12 y #22.
- SEGURIDAD_Y_PRIVACIDAD.md §2 (ubicación no se guarda).
