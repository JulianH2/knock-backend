# plan.md — TT-BD-05 — Spike: Azure DocumentDB frente a MongoDB Atlas

> Borrador. Se completa cuando la spec esté Aprobada y se resuelvan las preguntas abiertas de spec §8.

## 1. Mapeo de archivos
- **Reutilizar:** `db/seeds/colonias_tula.geojson`, `db/sepomex/tula_colonias.csv`, convenciones de pruebas del repo (xUnit v2, AwesomeAssertions, `Directory.Packages.props`), formato de `docs/evidencia/KNOCK-21-spike-firebase.md`.
- **Crear:** `tests/Spike.DocumentDb/` (arnés con `MongoDB.Driver` y Testcontainers), `docs/evidencia/KNOCK-147-spike-documentdb.md`.
- **Modificar:** `Directory.Packages.props` (driver de MongoDB y módulo de Testcontainers, tras evaluar paquetes).
- **No tocar:** `src/` de los módulos, `.github/`, `scripts/`, constitución; ningún secreto en archivos versionados.

## 2. Riesgos y efectos secundarios
- Filtración de la cadena de conexión (repositorio público): solo variable de entorno o user-secrets.
- Un solo clúster gratuito por suscripción: no crear uno que bloquee TT-BD-06.
- P3 depende de la red de quien mide: anotar origen y separar ping de consulta.
- P6 depende del monitor TTL (~60 s): esperas acotadas.

## 3. Pruebas por caso
| Caso | Prueba planeada | Destino |
|---|---|---|
| TC-TT-BD-05-01 | Creación del clúster gratuito en West US. | Comando (Azure) |
| TC-TT-BD-05-02 | `$geoIntersects`, respaldo `$near` y fuera de cobertura. | Local y Azure |
| TC-TT-BD-05-03 | p95 < 100 ms. | Azure |
| TC-TT-BD-05-04 | Transacción solicitud + outbox, commit y abort. | Local y Azure |
| TC-TT-BD-05-05 | Texto en español sin acentos. | Local y Azure |
| TC-TT-BD-05-06 | TTL borra tokens vencidos. | Local y Azure |
| TC-TT-BD-05-07 | 8 clientes con pool concurrentes. | Azure |
| TC-TT-BD-05-08 | `mongodump`/`mongorestore`. | Comando |
| TC-TT-BD-05-09 | Tabla de diferencias local vs Azure. | Ambos |

## 4. Secuencia
1. Resolver preguntas (a)–(d); evaluar paquetes.
2. Arnés local con `documentdb-local` y `mongo:8.0`; P2, P4–P6, P9 en verde o documentados.
3. Crear el clúster (P1); correr el arnés contra Azure (P2–P7, P9); P8.
4. Escribir la evidencia y la recomendación; proponer el cambio a ADR-002 si aplica.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py --trace TT-BD-05
dotnet test tests/Spike.DocumentDb -c Release                       # local
KNOCK_SPIKE_MONGO=<desde user-secrets o env> dotnet test tests/Spike.DocumentDb -c Release   # Azure; nunca pegar el valor
```
