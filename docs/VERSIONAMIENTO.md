# Matriz de versionamiento

> **Tarea:** TT-GES-03 (lineamiento 11: matriz de numeración de versiones antes del desarrollo, aplicada en cada despliegue).
> **Estado:** vigente desde 2026-10-20. Se aplica con release-please (TT-CD-03) y con los pipelines de despliegue (TT-CD-01 y TT-CD-02).
> **Este esquema reemplaza** las etiquetas `api-vX.Y.Z`, `app-vX.Y.Z` y `panel-vX.Y.Z` de la constitución §9 y la etiqueta `vX.Y.Z` de la ficha de TT-CD-03; ambas se actualizan al esquema nuevo (la constitución, en el PR descrito en `docs/decisiones/README.md`).

## 1. Esquema
**SemVer 2.0: `MAYOR.MENOR.PARCHE`**, con commits convencionales (`type(scope): description (KNOCK-N)`):

| Commit | Sube | Ejemplo |
|---|---|---|
| `fix`, `perf` | PARCHE | 1.2.3 → 1.2.4 |
| `feat` | MENOR | 1.2.3 → 1.3.0 |
| Cualquier tipo con `!` o `BREAKING CHANGE:` | MAYOR | 1.2.3 → 2.0.0 |
| `docs`, `chore`, `ci`, `test`, `refactor` | no sube (salvo que se cambie el mapeo en `release-please-config.json`) | — |

Mientras un componente esté en `0.x`, un cambio incompatible sube el MENOR. **La primera versión publicada de cada componente es `0.1.0`**; `1.0.0` es la que se publica en producción de Google Play.

## 2. Componentes versionados por separado
| Componente | Dónde vive | Etiqueta de git | Dónde se refleja la versión |
|---|---|---|---|
| Gateway | `knock-backend/src/Knock.Gateway` | `gateway-vX.Y.Z` | Etiqueta de la imagen |
| Servicio Identidad y confianza | `src/Knock.Identity.Api` | `identity-vX.Y.Z` | Etiqueta de la imagen |
| Servicio Mensajería y notificaciones | `src/Knock.Messaging.Api` | `messaging-vX.Y.Z` | Etiqueta de la imagen |
| Servicio Mercado | `src/Knock.Market.Api` | `market-vX.Y.Z` | Etiqueta de la imagen |
| Servicio Asistente | `src/Knock.Assistant.Api` | `assistant-vX.Y.Z` | Etiqueta de la imagen |
| Contratos | `src/Knock.Contracts` | `contracts-vX.Y.Z` | Paquete interno |
| Infraestructura | `deploy/` | `infra-vX.Y.Z` | Variable `infra_version` de Terraform |
| App móvil | `knock-frontend/app` | `app-vX.Y.Z` | `versionName` = `X.Y.Z`; `versionCode` entero creciente |
| Panel | `knock-frontend/panel` | `panel-vX.Y.Z` | `package.json` |
| Landing | `knock-frontend/landing` (cuando exista) | `landing-vX.Y.Z` | Pie de página |
| API pública | URL del gateway | — | Prefijo `/api/v1`; solo sube a `/api/v2` con un cambio incompatible |

## 3. Reglas de despliegue
1. **Imagen:** `<servicio>:<X.Y.Z>` y `<servicio>:<X.Y.Z>-<sha corto>`; nunca `latest`.
2. **Staging:** versiones candidatas `X.Y.Z-rc.N`, que produce el workflow de despliegue a Staging (TT-CD-01) a partir de la versión que calcula release-please; release-please solo crea el PR de versión y la etiqueta al fusionar a `main` (TT-CD-03). **Producción:** solo versiones sin sufijo.
3. **App:** `versionCode` = número de ejecución del pipeline de publicación (siempre creciente). Pista **interna** y **cerrada** de Google Play con `-rc`; **producción** con la versión estable.
4. Cada despliegue **anota una fila** en [`docs/VERSIONES_DESPLEGADAS.md`](VERSIONES_DESPLEGADAS.md) (fecha, componente, versión, entorno, responsable).
5. Un servicio no despliega con un `contracts-vX.Y.Z` mayor que el que sus clientes soportan.

## 4. Compatibilidad
| App | Gateway / API | Contratos | Comentario |
|---|---|---|---|
| 0.x | `/api/v1` | 0.x | Fase de desarrollo; cualquier combinación debe probarse junta |
| 1.x | `/api/v1` | 1.x | Producción; la app indica la **versión mínima** de API que necesita |

El gateway puede rechazar apps por debajo de una versión mínima cuando un cambio incompatible lo exija (se decide en la spec del cambio).

## 5. Registro
- Un `CHANGELOG.md` por componente, generado por release-please.
- Etiquetas de git con el formato de la tabla; cada una enlaza al PR de la versión.

## 6. Configuración de release-please y pendientes (se confirman al implementar TT-CD-03)
- **Versión inicial:** `0.1.0` por componente (`initial-version`/`release-as`) y `bump-minor-pre-major: true`, para que en `0.x` un cambio incompatible suba el MENOR y un `feat` también; por defecto la primera versión sería `1.0.0`.
- **Un componente por carpeta** (modo manifiesto), con su propio `CHANGELOG.md` y su etiqueta `<componente>-vX.Y.Z`.
- **Actualizadores por archivo** (`extra-files`) para el `versionCode` de la app y la variable `infra_version` de Terraform.
- **Pendientes de definir:** dónde se publica `Knock.Contracts` (no hay feed definido), la rama `hotfix/`, y cómo se comprueba la regla 5 de compatibilidad («sus clientes soportan») en el pipeline.
- Las rutas del frontend (`app`, `panel`, `landing`) están en `knock-frontend/`; las del backend, en `knock-backend/`.
