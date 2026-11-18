# Plan 031 — Moderación de contenido con Azure

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Interfaz y tipos en `Knock.Contracts/Moderation/` (R-01), primero, porque las specs 020, 030 y 032 la necesitan para avanzar con un doble.
2. Funciones puras: normalizador, lista local y comparación con umbrales; plantillas y traductor a HTTP (R-03, R-05, R-13, R-14, R-18).
3. `ContentSafetyClient` con `Azure.Identity`, tiempo máximo, reintento único y cortacircuito; servidor simulado para las pruebas con respuestas grabadas (R-05, R-07, R-08, R-09, R-16).
4. Caché de decisiones y topes en Redis; registro de uso en MongoDB (R-10, R-11, R-12, R-17).
5. `ContentSafetyModerator` y `AddKnockModeration()` con opciones validadas y modos (R-02, R-04, R-06, R-15, R-19).
6. Copia en Mensajería con la excepción de duplicación y prueba de listas idénticas; pruebas de arquitectura.
7. Conectar los llamadores (020, 030, 032) con el traductor y correr su regresión.

## Archivos que se crean o cambian
- `src/Knock.Contracts/Moderation/` (interfaz y tipos).
- `src/Knock.Market.Api/Common/Moderation/` y `src/Knock.Messaging.Api/Common/Moderation/` (implementación, `lista-local.es-MX.txt`, plantillas).
- `db/0NN_uso_moderacion.js` (en `market` y `messaging`); `db/entrega.js` lo carga.
- `security/exceptions.yaml` necesita una entrada nueva de duplicación: es zona protegida y la agrega el líder.
- `tests/` (`Spec031*` en UnitTest, IntegrationTest, SecurityTest, ArchitectureTest, DatabaseTest).
- `deploy/` (recurso de Content Safety y rol de la identidad administrada), coordinado con la spec 005.
- `Memoria.md`.

## Dependencias
- Spec 024 (patrones del filtro de datos personales) y 032 (`Azure.Identity` y los bytes procesados).
- Recurso de Content Safety con plan gratuito y su cuota confirmada (decisión de gasto del líder).

## Decisiones de la spec que este plan sigue
- Content Safety por REST; lista local que solo rechaza; caché versionada; falla cerrada; copia por servicio con excepción.

## Riesgos del plan
- Sin recurso en Azure, solo se valida con el simulador; la prueba manual se hace al crearlo.
- La excepción de duplicación requiere autorización (zona protegida).

## Verificación
- Cada criterio con su prueba de la tabla 4.1; `python scripts/sdd_lint.py --trace 031`; `python scripts/ci/check_exceptions.py`; comandos de `AGENTS.md`; `Memoria.md` al día.
