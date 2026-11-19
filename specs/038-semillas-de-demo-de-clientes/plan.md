# Plan 038 — Semillas de demo de clientes

> **Jira:** KNOCK-40 · **Estado:** Aprobada · **Servicio:** Identidad y confianza. Plan de la spec aprobada.

## Enfoque
1. Resolver la frontera de datos antes de implementar y aplicar las decisiones de `spec.md` §9; aplicar las decisiones de Julián de §8.
2. Reutilizar el formato y patrón de semillas de `db/seeds/catalog/` y la protección y derivación de `src/Knock.Identity.Api/Modules/Sesiones/Services/` (`PhoneProtector`, `UserIdDerivation`); no copiar acceso entre servicios.
3. Preparar diez clientes ficticios activos, con perfil completo y consentimientos vigentes; extender el acceso a datos de Identidad y conectar el cargador al arranque con las guardas de R-07, incluida la lista estricta de hosts locales.
4. Probar idempotencia, recuperación tras fallo parcial, índices únicos y carreras, estado y consentimientos, guardas de hosts analizados y documentos no demo, frontera y humo local; documentar carga, conteo y borrado, y la carga manual separada a Azure con `db/entrega.js`. El cifrado aleatorio se verifica por descifrado y no por igualdad de bytes.

## Archivos previstos
- `db/seeds/demo/identity/usuarios.json` y `db/seeds/README.md`.
- `src/Knock.Identity.Api/Modules/Sesiones/Data/` y punto de arranque de `Knock.Identity.Api`: ubicación concreta y coordinación del acceso sujetas a §9.
- `tests/DatabaseTest/Demo/` y `tests/UnitTest/Architecture/`: pruebas propuestas de CA-01 a CA-08 y CA-11 a CA-13.
- No se cambian semillas del catálogo ni se agregan dependencias; el script de entrega y respaldos están fuera de alcance.

## Dependencias y riesgos
- Spec 015 y su implementación de `usuarios`, índices únicos, derivación y protección; formato del catálogo de TT-BD-06; `documentdb-local` y fixture existente.
- CA-11 exige cero escrituras ante un conflicto detectado previamente; los índices únicos rechazan carreras y un fallo intermedio puede dejar escrituras recuperables con upserts por `_id`. R-07 exige analizar todos los hosts con `MongoUrl.Servers`, rechazar SRV y abortar ante documentos no demo; un túnel a una base remota vacía o solo demo sigue siendo un riesgo.
- Revisión humana de nombres inventados; evidencia sin secretos ni cadenas.

## Verificación
Ejecutar las pruebas de CA-01 a CA-08 y CA-11 a CA-13, usando `documentdb-local` para la integración y los casos de guardas de CA-12; registrar en el PR los dos conteos de CA-09 y ejecutar build y formato de CA-10. Completar los gates aplicables del repositorio y actualizar `Memoria.md` al implementar. Nada de esto se ejecuta durante esta migración documental.
