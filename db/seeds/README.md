# Seeds de catálogo

## GeoJSON piloto de Tula

`colonias_tula.geojson` contiene ocho geometrías piloto aproximadas, no oficiales. Cada feature documenta su autor y fecha de elaboración; las geometrías no se deben presentar como límites oficiales. Antes de integrar o modificar el archivo, ejecutar:

```bash
python db/scripts/validate_colonias_tula.py
```

El validador usa solo la biblioteca estándar de Python y no modifica el archivo. Si una geometría es `Polygon`, la normaliza en memoria a `MultiPolygon` para validar la estructura; la entrada original permanece sin cambios. Se aceptan `Polygon` y `MultiPolygon` con coordenadas GeoJSON.

### Contrato requerido por Feature

`properties` debe contener:

| Propiedad | Tipo / restricción |
|---|---|
| `nombre` | Texto no vacío, nombre SEPOMEX de la colonia. |
| `cp` | Texto de cinco dígitos; no número, para preservar ceros iniciales si los hubiera. |
| `tipo_asentamiento` | Texto no vacío; para este extracto, `Colonia`. |
| `municipio` | `Tula de Allende`. |
| `estado` | `Hidalgo`. |
| `piloto` | Booleano `true`. |
| `aproximado` | Booleano `true`; las geometrías no son límites oficiales. |
| `autor` | Texto no vacío con la persona responsable de elaborar la geometría. |
| `fecha_elaboracion` | Fecha ISO `YYYY-MM-DD` de elaboración de esa geometría. |

### Coordenadas

- GeoJSON usa posiciones `[longitud, latitud]` (X=longitud, Y=latitud) en WGS 84, orden EPSG:4326.
- Cada posición debe incluir al menos dos números finitos; longitud debe estar en `[-180, 180]` y latitud en `[-90, 90]`.
- Para el piloto de Tula, la longitud debe estar entre `-99.40` y `-99.28`, y la latitud entre `20.00` y `20.12` (límites inclusivos). Estos límites acotados ayudan a detectar geometrías desplazadas; no demuestran que el polígono corresponda al límite oficial de la colonia.
- Para el piloto de Tula, la longitud debe ser oeste (negativa) y la latitud norte (positiva); esto ayuda a detectar ejes invertidos.
- Los anillos deben tener al menos cuatro posiciones y estar cerrados.
- El validador verifica forma, rangos, hemisferios y el área acotada del piloto, pero no puede probar la ubicación correcta ni la exactitud de un polígono solo a partir de esos valores. El responsable debe revisar visualmente la geometría y confirmar que el orden declarado es realmente longitud/latitud.

No incluir coordenadas de usuarios ni afirmar que las geometrías aproximadas son oficiales.
