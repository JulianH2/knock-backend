# TT-BD-02 — Catálogo base: 3 oficios como datos y colonias de la ciudad piloto (SEPOMEX + polígonos); consulta "colonia de un punto"

> **Jira:** KNOCK-23 · **Área:** Base de datos · **Responsable:** Ángel Alexis Blas Castillo · **Sprint:** S0 · **Puntos:** Por confirmar
> **Repositorio:** knock-backend · **Ficha:** TT-BD-02_CatalogoBase3Oficios_Knock.docx
> **Skills:** Por confirmar
> **Estado:** Borrador

## 1. Objetivo
Crear datos de catálogo para los oficios Plomería, Electricidad y Mecánica como seed y no como enum, y disponer del catálogo de colonias de Tula de Allende, Hidalgo, basado en SEPOMEX con polígonos, para consultar qué colonia contiene un punto `(lat, lng)` mediante PostGIS. Esta especificación se limita a ese catálogo de Tula y, por confirmación del equipo, reemplaza Pachuca solo para este catálogo; no modifica la decisión general del proyecto sobre su ciudad piloto.

## 2. Historia y casos de prueba
**TT-BD-02** — Tarea técnica. Los casos de geometría requieren puntos de prueba y polígonos disponibles y validados; ninguno se inventa en esta spec.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-BD-02-01 | Dado un punto de prueba dentro de una colonia cubierta de Tula de Allende y los polígonos disponibles, cuando se consulta el punto `(lat, lng)`, entonces se devuelve la colonia que lo contiene. | Prueba de base de datos con PostGIS y punto dentro de Tula acordado; suite/método, punto y polígono de prueba por confirmar. |
| TC-TT-BD-02-02 | Dado un punto de prueba fuera de Tula de Allende, cuando se consulta el punto `(lat, lng)`, entonces no se devuelve una colonia de Tula. | Prueba de base de datos con PostGIS y punto fuera de Tula acordado; suite/método y punto de prueba por confirmar. |
| TC-TT-BD-02-03 | Dado que se ejecutó el seed, cuando se consultan los oficios del catálogo, entonces están Plomería, Electricidad y Mecánica como datos, no como enum. | Verificación de los datos persistidos después del seed; suite/método por confirmar. |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Seed de los oficios Plomería, Electricidad y Mecánica como datos y no como enum.
  - Catálogo de colonias de Tula de Allende, Hidalgo, basado en SEPOMEX e incluyendo polígonos, sujeto a disponibilidad de datos adecuados.
  - Consulta PostGIS que recibe un punto `(lat, lng)` y determina la colonia que lo contiene.
  - Casos mínimos: punto dentro de Tula devuelve una colonia; punto fuera de Tula no devuelve una colonia de Tula; los tres oficios aparecen después del seed.
- **Out:**
  - Catálogo de colonias de otros municipios.
  - Otros oficios distintos de los tres indicados.
  - Geometrías inventadas o polígonos presentados como oficiales sin fuente disponible y verificada.
  - Otros endpoints o comportamiento de producto no especificados para esta tarea.
- **Dependencia:** TT-BD-01 (KNOCK-22).

## 4. Contratos API y eventos
No se especifican endpoints ni eventos. La consulta de punto `(lat, lng)` se plantea como consulta PostGIS de esta tarea; si requiere un contrato API, queda por definir antes de aprobar.

## 5. Modelo de datos
- Los tres oficios deben persistirse como datos de catálogo sembrados, no como enum.
- Se requiere un catálogo de colonias de Tula de Allende con polígonos utilizables por PostGIS, derivado de SEPOMEX si los datos adecuados están disponibles.
- Tablas, campos, claves, sistema de referencia espacial, formato de importación y estrategia de migración/seed quedan por confirmar; no se inventan geometrías ni detalles de esquema.

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Geometrías ausentes, no verificables o incorrectas pueden producir una clasificación de colonia equivocada o aparentar cobertura no sustentada. | No inventar polígonos ni declararlos oficiales sin disponer de la fuente SEPOMEX correspondiente; documentar procedencia y validar los datos antes de cargarlos. Disponibilidad, versión y validación de la fuente siguen abiertas. | TC-TT-BD-02-01 · TC-TT-BD-02-02; su ejecución depende de disponer de datos y puntos de prueba acordados. |
| El seed puede no contener los oficios requeridos o representarlos como valores fijos de código en lugar de datos. | Persistir los tres oficios como datos de catálogo y verificar el estado posterior al seed. | TC-TT-BD-02-03 |
| Entradas de coordenadas mal interpretadas podrían cambiar el resultado de la consulta. | Usar parámetros para la consulta PostGIS y mantener explícito el orden `(lat, lng)` indicado en esta spec. No se especifica aquí una API ni validación adicional de entrada. | TC-TT-BD-02-01 · TC-TT-BD-02-02; los puntos y la forma de comprobar el uso parametrizado quedan por acordar. |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Base de datos / integración geoespacial | Sí | Ejecutar los casos dentro y fuera con PostGIS real y verificar la colonia o ausencia de resultado (TC-TT-BD-02-01 y TC-TT-BD-02-02). La suite, fixture y puntos quedan por confirmar; no usar puntos ni polígonos inventados. |
| Seed de catálogo | Sí | Ejecutar el seed y comprobar que se encuentran los tres oficios especificados como datos (TC-TT-BD-02-03). La suite/método queda por confirmar. |
| Unitarias | Por confirmar | No se conoce aún si la tarea añade lógica separable de la consulta PostGIS. |
| Contrato API / eventos | No por ahora | No se define endpoint ni evento en los hechos disponibles. |
| Mutación | Por confirmar | No se ha definido la lógica de implementación ni una meta específica para esta tarea. |
| Rendimiento, UI y accesibilidad | No | No se solicitan en los hechos disponibles. |

**Gates:** ver `AGENTS.md §5` en lo que aplique; no se afirma aquí que se hayan ejecutado.

**Límites y dudas abiertas:**
- Responsable, Sprint, Puntos, ficha de Jira/backlog y Skills oficiales de la tarea.
- Disponibilidad del extracto SEPOMEX pertinente para Tula de Allende; fuente exacta, fecha/versión, licencia y cobertura de polígonos. No se inventarán geometrías oficiales. Si no hay datos adecuados, la carga queda bloqueada y debe reportarse.
- Puntos concretos de prueba dentro de una colonia y fuera de Tula, y cómo se demostrará su relación con una fuente confiable.
- Esquema de datos, columnas, claves, CRS, formato de importación y estrategia de seed/migración.
- Definición de la consulta: comportamiento en límites compartidos entre polígonos y en puntos sin coincidencia, salvo que aplique el caso fuera de Tula.
- Confirmar si la consulta permanece interna a la capa de datos o requiere contrato API.
- Suite, fixtures de prueba, alcance de pruebas unitarias y meta de mutación.
- Tula de Allende reemplaza Pachuca únicamente para este catálogo de KNOCK-23; la documentación de referencia general sigue pendiente de una actualización humana si el equipo decide cambiar también la ciudad piloto del producto.

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
