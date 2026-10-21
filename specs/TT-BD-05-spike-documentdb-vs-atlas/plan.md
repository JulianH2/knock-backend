# plan.md — TT-BD-05 — Spike: Azure DocumentDB frente a MongoDB Atlas

> Borrador. Preguntas (a)–(d) de spec §8 resueltas; falta la aprobación de la spec.

## 1. Mapeo de archivos
- **Reutilizar:** `db/seeds/colonias_tula.geojson`, `db/sepomex/tula_colonias.csv`, convenciones de pruebas del repo (xUnit v2, AwesomeAssertions, `Directory.Packages.props`), formato de `docs/evidencia/KNOCK-21-spike-firebase.md`.
- **Crear:** `tests/Spike.DocumentDb/` (arnés con `MongoDB.Driver` y Testcontainers, scripts de índices y semillas y fixtures sintéticos de centros; fuera de `Knock.slnx` y del CI hasta TT-BD-07), `docs/evidencia/KNOCK-147-spike-documentdb.md`.
- **Modificar:** `Directory.Packages.props` (driver de MongoDB y módulo de Testcontainers, tras evaluar paquetes).
- **No tocar:** `src/` de los módulos, `Knock.slnx`, `.github/`, `scripts/`, constitución, `db/` (la ruta `db/mongo/` es de TT-BD-09), la configuración del clúster `knock-docdb`; ningún secreto en archivos versionados.

## 2. Riesgos y efectos secundarios
- Filtración de la cadena de conexión (repositorio público): llega por gestor de contraseñas; solo variable de entorno o user-secrets.
- `knock-docdb` es el único clúster gratuito: no borrarlo ni reconfigurarlo; se conserva para TT-BD-06 y la importación con Terraform.
- P3 depende de la red de quien mide: anotar origen y separar ping de consulta; un fallo solo de red lleva a cambiar la región o acercar los servicios, no a descartar DocumentDB.
- P7: Microsoft no publica el límite de conexiones del nivel gratuito; se mide y se compara con Atlas M0 (500).
- P6 depende del monitor TTL (~60 s): esperas acotadas.

## 3. Pruebas por caso
| Caso | Prueba planeada | Destino |
|---|---|---|
| TC-TT-BD-05-01 | Verificar que `knock-docdb` cumple (West US, nivel gratuito, 32 GB, servidor 8.0). | Comando (Azure) |
| TC-TT-BD-05-02 | `$geoIntersects`, respaldo `$near` y fuera de cobertura. | Local y Azure |
| TC-TT-BD-05-03 | p95 < 100 ms; red y consulta por separado. | Azure |
| TC-TT-BD-05-04 | Transacción solicitud + outbox, commit y abort. | Local y Azure |
| TC-TT-BD-05-05 | Texto en español sin acentos. | Local y Azure |
| TC-TT-BD-05-06 | TTL borra tokens vencidos. | Local y Azure |
| TC-TT-BD-05-07 | 8 clientes con pool concurrentes; límite de conexiones medido frente a Atlas M0. | Azure |
| TC-TT-BD-05-08 | `mongodump`/`mongorestore`. | Comando |
| TC-TT-BD-05-09 | Tabla de diferencias local vs Azure. | Ambos |

## 4. Secuencia
1. Aprobar la spec; evaluar paquetes; pedir al líder la cadena (por gestor de contraseñas) y la regla de firewall para la IP.
2. Arnés local con `documentdb-local` y `mongo:8.0`; P2, P4–P6, P9 en verde o documentados.
3. Verificar el clúster existente (P1); correr el arnés contra Azure (P2–P7, P9); P8; pedir que se quite la regla de la IP.
4. Escribir la evidencia y la recomendación; proponer el cambio a ADR-002 si aplica.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py --trace TT-BD-05
dotnet test tests/Spike.DocumentDb -c Release                       # local
KNOCK_SPIKE_MONGO=<desde user-secrets o env> dotnet test tests/Spike.DocumentDb -c Release   # Azure; nunca pegar el valor
```
