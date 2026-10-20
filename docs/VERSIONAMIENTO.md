# Matriz de versionamiento

> **Tarea:** TT-GES-03 (lineamiento 11: matriz de numeración de versiones antes del desarrollo, aplicada en cada despliegue).
> **Estado:** vigente desde 2026-10-20. Se aplica con release-please (TT-CD-03) y con los pipelines de despliegue (TT-CD-01 y TT-CD-02).

## 1. Esquema
**SemVer 2.0: `MAYOR.MENOR.PARCHE`**, con commits convencionales (`type(scope): description (KNOCK-N)`):

| Commit | Sube | Ejemplo |
|---|---|---|
| `fix`, `perf`, `docs` que cambian comportamiento visible | PARCHE | 1.2.3 → 1.2.4 |
| `feat` | MENOR | 1.2.3 → 1.3.0 |
| Cualquier tipo con `!` o `BREAKING CHANGE:` | MAYOR | 1.2.3 → 2.0.0 |
| `chore`, `ci`, `test`, `refactor`, `docs` sin efecto visible | no sube | — |

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
2. **Staging:** versiones candidatas `X.Y.Z-rc.N`. **Producción:** solo versiones sin sufijo.
3. **App:** `versionCode` = número de ejecución del pipeline de publicación (siempre creciente). Pista **interna** y **cerrada** de Google Play con `-rc`; **producción** con la versión estable.
4. Cada despliegue **anota una fila** en `docs/VERSIONES_DESPLEGADAS.md` (fecha, componente, versión, entorno, responsable).
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
