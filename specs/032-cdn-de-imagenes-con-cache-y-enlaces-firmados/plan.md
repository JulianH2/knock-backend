# Plan 032 — CDN de imágenes: entrega con caché y enlaces firmados

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Infraestructura primero: cuenta pública, contenedor de cuarentena con su regla de ciclo de vida, roles de la identidad de Mercado y salidas de Terraform, sin ningún recurso de CDN.
2. Pieza pura de procesamiento (`ProcesadorImagen`) con sus pruebas unitarias, que no depende de nada más y puede avanzar en paralelo con la infraestructura.
3. Acceso a Blob (`AlmacenBlob`) contra Azurite: SAS de delegación de usuario de escritura y de lectura, propiedades, descarga, publicación y borrado.
4. Rebanada vertical del módulo `Media` en Mercado: validadores, servicio, almacenes de MongoDB, límites en Redis y controller.
5. `IEnlacesPrivados` con su registro de acceso.
6. Gateway, contrato OpenAPI, `docs/endpoints.md` y pruebas de seguridad negativa.
7. Humo en Azure y cierre de la documentación.

## Archivos que se crean o cambian
- `deploy/modules/shared/` (cuenta pública, contenedor `imagenes-cuarentena`, regla de ciclo de vida, roles, salidas) y `deploy/apps/main.tf` (variables de entorno `Imagenes__BaseUrlPublica`, `Perfil__HostsFoto__0` y la de la spec 020).
- `db/NNN-market-subidas-imagen.js` (siguiente número libre): colecciones `subidas_imagen` y `accesos_privados` con sus índices y TTL.
- `src/Knock.Market.Api/Modules/Media/` (`Controllers`, `Dtos`, `Validators`, `Services`, `Domain`, `Data`, `Options`).
- `src/Knock.Market.Api/Program.cs` o su extensión de módulos (`AddMediaModule()`).
- `src/Knock.Gateway/appsettings.json` (ruta `/api/v1/imagenes/**` hacia Mercado).
- `tests/UnitTest`, `tests/IntegrationTest`, `tests/SecurityTest`, `tests/ArchitectureTest` y `tests/ContractTest` (clases `Spec032*`).
- `docs/endpoints.md`, `docs/openapi.v1.json` (regenerado con el script, nunca a mano), `Memoria.md`.

## Dependencias
- Specs 002, 003, 005, 009, 010, 011 y 013; ADR-007 aprobado.
- Interfaz de moderación de la spec 013 (doble en pruebas) y su implementación de la spec 031 para el humo en Azure.
- Paquetes nuevos por evaluar: `Azure.Storage.Blobs`, `Azure.Identity`, `SkiaSharp` y `SkiaSharp.NativeAssets.Linux.NoDependencies`.
- Una persona registra en `security/exceptions.yaml` la excepción del contenedor público (archivo protegido).

## Riesgos del plan
- Azurite no implementa todas las restricciones de SAS de delegación de usuario igual que Azure: CA-03 y CA-04 se repiten en el humo en Azure.
- Las bibliotecas nativas de SkiaSharp pueden exigir ajustes en el `Dockerfile` de Mercado; se valida con la imagen del CI antes de integrar.

## Decisiones de la spec que este plan sigue
- Sin CDN (ADR-007); cuarentena en la cuenta privada; nombre por huella; recodificar a JPEG; SAS de delegación de usuario; moderar después de procesar.

## Verificación
- `dotnet build Knock.slnx -c Release`, `dotnet test Knock.slnx -c Release`, `dotnet format Knock.slnx --verify-no-changes`.
- `python scripts/sdd_lint.py --trace 032` sin faltantes.
- `terraform validate`, `terraform plan` y Checkov del módulo compartido.
- `python scripts/export_openapi.py` y `git diff` del contrato limpio después de regenerar.
