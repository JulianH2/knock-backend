# Seeds de catálogo

## Semillas de MongoDB (`catalog/`)

Desde TT-BD-06 el catálogo vive en MongoDB (base `market`, colecciones `oficios` y `colonias`). El cargador del módulo (`Modules/Catalog/Data/CatalogSeeder`) aplica:

| Archivo | Contenido |
|---|---|
| `catalog/oficios.json` | Oficios con `_id` fijos (`ObjectId`), `clave`, `nombre`, `nombreBusqueda`, `modalidad` (`domicilio` o `taller`) y `activo`. |
| `catalog/colonias.json` | Las 13 colonias del extracto SEPOMEX con `_id` fijos. Las 9 con polígono llevan `geom` (MultiPolygon de `colonias_tula.geojson`) y `centro` (exportado una vez de PostGIS con `ST_PointOnSurface`); las 4 sin polígono no tienen esos campos. |

Los `_id` son iguales en todos los entornos (los referencian TT-BD-03 y la zona del perfil): **no se cambian**. El cargador es repetible (upsert por clave natural), recalcula `nombreBusqueda`, valida todo antes de escribir y solo sube `version` si cambia el contenido.

Los dos JSON los generó una sola vez `db/scripts/generate_catalog_seeds.py` desde el GeoJSON, el CSV y la antigua `oficios.sql`, con un PostGIS desechable para el centro. Al volver a correrlo reutiliza los `_id` existentes. `oficios.sql` se retiró en TT-BD-06 (autorización del líder en #53), así que hoy el generador solo sirve como registro de cómo se obtuvieron los centros.

## Demo de clientes

`demo/identity/usuarios.json` contiene diez clientes inventados, sin cuentas Firebase, técnicos ni solicitudes. El cargador de Identity protege los celulares antes de guardarlos y solo admite destinos locales y una colección sin usuarios que no sean demo. No usarlo para datos reales ni en Production.

Para cargar en local, desde la raíz del repositorio:

1. Configura de forma privada las credenciales requeridas por `deploy/docker-compose.local.yml` (`KNOCK_MONGO_PASSWORD`, `KNOCK_REDIS_PASSWORD` y `POSTGRES_PASSWORD`; Compose interpola las variables del archivo completo). Levanta los servicios necesarios: `docker compose -f deploy/docker-compose.local.yml up -d documentdb redis`.
2. Configura de forma privada las opciones de Identity, incluidas `ConnectionStrings__KnockMongo` (con `<cadena-local>` hacia `localhost`, `127.0.0.1` o `documentdb-local`), `ConnectionStrings__KnockRedis`, `Jwt__SigningKey` y las llaves y versiones `Identity__PhoneProtection__*`. Consulta la configuración del proyecto; no incluyas valores de secretos en el repositorio ni en evidencia.
3. En un entorno que **no** sea Production, ejecuta `ASPNETCORE_ENVIRONMENT=Development Seed__Demo=true dotnet run --project src/Knock.Identity.Api`. La carga ocurre al iniciar. Se pueden repetir los arranques; no crea cuentas Firebase.

Para contar o borrar la demo, abre `mongosh --nodb` contra el DocumentDB **local**, pasando `<cadena-local>` por la variable de entorno `KNOCK_DB_URI` en lugar de poner credenciales en la línea de comandos. Dentro de `mongosh`:

```javascript
const identity = new Mongo(process.env.KNOCK_DB_URI).getDB('identity');
identity.usuarios.countDocuments({ demo: true });
// Solo si se desea borrar la demo local:
identity.usuarios.deleteMany({ demo: true });
```

Verifica el destino y el filtro antes de borrar; esta operación elimina esos documentos. El cliente local de documentdb-local usa TLS con certificado autofirmado: configura su confianza solo para el entorno local.

**Azure no se carga con este cargador de demo.** La entrega de Azure se hace aparte y manualmente con `db/entrega.js`; ese script aplica índices y semillas del catálogo, **no carga los diez clientes demo**. No se ha implementado aquí un cargador de esos clientes para Azure.

## GeoJSON piloto de Tula

`colonias_tula.geojson` contiene nueve geometrías piloto aproximadas, no oficiales. Cada feature documenta su autor y fecha de elaboración; las geometrías no se deben presentar como límites oficiales. Antes de integrar o modificar el archivo, ejecutar:

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
