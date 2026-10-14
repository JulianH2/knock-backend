# plan.md — TT-BD-02 — Catálogo base de oficios y colonias

## 1. Mapeo de archivos
- **Reutilizar:** No se prevén cambios de código en esta spec; los detalles de implementación siguen abiertos.
- **Crear:** Pendiente de determinar según la fuente SEPOMEX disponible y el esquema que se acuerde.
- **Modificar:** Pendiente de confirmar.
- **No tocar:** No inventar polígonos ni presentarlos como oficiales sin fuente disponible y verificada. No ampliar el catálogo a otros municipios u oficios.

## 2. Riesgos y efectos secundarios
- Datos geográficos no disponibles o no verificables pueden dar resultados de colonia incorrectos. Si no se dispone de polígonos adecuados, detener la carga y reportar el bloqueo.
- Una consulta con coordenadas mal interpretadas puede clasificar incorrectamente un punto; los casos de dentro y fuera requieren puntos acordados sin inventarlos.
- El seed podría omitir oficios requeridos o no persistirlos como datos; comprobar los tres valores posteriores al seed.
- Dependencia de TT-BD-01 (KNOCK-22).

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-BD-02-01 | Consultar con PostGIS un punto acordado dentro de Tula y verificar la colonia devuelta. Punto, polígono y método por confirmar. | Base de datos/integración; suite pendiente de identificar. |
| TC-TT-BD-02-02 | Consultar con PostGIS un punto acordado fuera de Tula y verificar que no se devuelve colonia de Tula. Punto y método por confirmar. | Base de datos/integración; suite pendiente de identificar. |
| TC-TT-BD-02-03 | Ejecutar el seed y comprobar que Plomería, Electricidad y Mecánica están persistidos como datos, no como enum. | Base de datos/seed; suite pendiente de identificar. |

## 4. Secuencia
1. Confirmar disponibilidad, procedencia y condiciones de uso de los polígonos SEPOMEX pertinentes para Tula; si no hay datos adecuados, detener y reportar.
2. Acordar puntos de prueba confiables para los casos dentro y fuera, además del comportamiento de la consulta que sigue abierto en §8.
3. Precisar esquema, importación y estrategia de seed/migración tras la aprobación humana de la spec.
4. Implementar el seed de los tres oficios y la consulta con PostGIS dentro del alcance aprobado.
5. Verificar TC-TT-BD-02-01, TC-TT-BD-02-02 y TC-TT-BD-02-03 con PostGIS real y registrar los resultados.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py
```
Los comandos de seed y de pruebas de base de datos dependen de la suite y el procedimiento que se acuerden; no se inventan en este borrador.
