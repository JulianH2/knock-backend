# TT-BD-03 — tareas

> Servicio afectado: Identidad y confianza. Ninguna tarea empieza hasta que la spec esté Aprobada y esté fusionado #75 (HU-001, cifrado del celular; #73 ya está fusionado). Si #75 no llega antes del 30-oct-2026, pasa al siguiente sprint (lo decide el equipo). Técnicos y solicitudes: fuera de alcance hasta que existan las specs de HU-017 y HU-024 (spec §8, D1).

## T1 — JSON de clientes de demostración
- **Estado:** pendiente. 10 clientes en `db/seeds/demo/identity/usuarios.json` con el formato de `db/seeds/catalog/*.json`, `demo: true`, `uid` ficticio `demo-cliente-NN`, nombres escritos a mano y celulares `+5200000NNNNN`.
- **Verificar:** TC-TT-BD-03-01 · TC-TT-BD-03-02 · TC-TT-BD-03-03

## T2 — Cargador repetible de Identity
- **Estado:** pendiente. En el módulo Identity (carpeta según P10, spec §8), cifrando con `Services/PhoneProtector`: upsert por `_id`, validación previa, `sub` con `UserIdDerivation.Derive(uid)`, cifrado del celular con el código de HU-001, guarda de entorno (`Production` y `Seed:Demo`).
- **Verificar:** TC-TT-BD-03-04 · TC-TT-BD-03-05 · TC-TT-BD-03-06 · TC-TT-BD-03-07 · TC-TT-BD-03-08

## T3 — Documentación
- **Estado:** pendiente. Sección de demostración en `db/seeds/README.md` (carga y conteo en `documentdb-local`, borrado por `demo: true`).
- **Verificar:** revisión del PR

## T4 — Carga local y gates
- **Estado:** pendiente. Dos cargas en `documentdb-local` (compose de TT-BD-07 o contenedor a mano con la imagen de `DocumentDbFixture`, spec §7) y conteos en el PR, sin cadenas ni credenciales.
- **Verificar:** TC-TT-BD-03-09 · TC-TT-BD-03-10
