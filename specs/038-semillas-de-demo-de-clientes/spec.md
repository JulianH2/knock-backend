# Spec 038 — Semillas de demo de clientes

> **Jira:** KNOCK-40 · **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Servicio:** Identidad y confianza (`Knock.Identity.Api`) · **Origen:** TT-BD-03 · **Capa:** BD

## 1. Objetivo
Disponer de diez clientes inventados para demostraciones locales, cargados desde JSON versionado en la base `identity`, sin duplicados ni datos personales reales. Esta spec migra los once casos de TT-BD-03; su aprobación histórica no aprueba esta versión.

## 2. Alcance
**Dentro:** JSON de clientes, cargador de Identidad, protección de celulares, guardas, pruebas, instrucciones de carga, conteo y borrado por `demo: true`. La evidencia de carga se obtiene en `documentdb-local`.

**Fuera:** técnicos y solicitudes (KNOCK-246, después de la spec 020), y mapa de técnicos; Staging; datos reales o de producción; cuentas Firebase para estos usuarios (no inician sesión); Bogus y otras dependencias nuevas; fotos, direcciones, INE, selfie y verificación; cambios a semillas del catálogo, script de entrega y respaldos. El cargador no implementa caché propia.

## 3. Requisitos
- **R-01:** `db/seeds/demo/identity/usuarios.json` contiene exactamente diez clientes inventados, nombres escritos a mano y ningún técnico ni solicitud. Es un arreglo con `_id` fijo `{"$oid":"<24 hex>"}`, campos camelCase, `version: 1`, `demo: true` y fechas UTC ISO 8601; sus identificadores son únicos entre sí y frente a las semillas del catálogo.
- **R-02:** Cada entrada tiene un `uid` único `demo-cliente-NN` y un celular único del rango ficticio `+5200000NNNNN`; no incluye INE, selfie, CURP, dirección, correo, foto ni ubicación, ni teléfonos, correos o URL en los nombres.
- **R-03:** El cargador de `Knock.Identity.Api` deriva `sub` del `uid` con `UserIdDerivation.Derive`, como la spec 015; el `uid` solo se usa para esa derivación y no se guarda en MongoDB.
- **R-04:** El celular en claro solo está en el JSON de demostración; en `usuarios` se guarda únicamente como `celularCifrado` y `celularHash`, con sus versiones de clave, usando la protección y las llaves del entorno de la spec 015. No se registra el celular ni el `uid`.
- **R-05:** La carga hace upsert por `_id` fijo a través del acceso a datos de Identidad y solo usa la base `identity`. Valida el archivo y los choques de `sub` y `celularHash` con otro `_id` antes de escribir; un conflicto detectado en esa validación informa el índice sin exponer celular ni hash y no escribe nada. Los índices únicos existentes de `sub` y `celularHash` rechazan también los choques por una carrera. Si la base falla durante la carga, lo ya escrito permanece; una carga posterior completa el resto mediante upserts idempotentes por `_id`, sin garantía de atomicidad de toda la carga.
- **R-06:** Una segunda carga conserva conteo, `_id`, `version` y `celularHash`. `version` sube solo si cambia el contenido, sin comparar `celularCifrado`, cuyo nonce es aleatorio; el cifrado debe seguir descifrándose al celular de origen.
- **R-07:** La guarda de host es incondicional y no depende de ninguna bandera, ambiente ni configuración: antes de cualquier escritura, el cargador obtiene los hosts de la cadena analizada con `MongoUrl` (`MongoUrl.Servers`), nunca del texto; todos deben ser `localhost`, `127.0.0.1` o `documentdb-local`. Las credenciales `usuario:clave@host` no forman parte del host. Rechaza siempre `mongodb+srv://`, que puede resolver a servidores remotos. Como segunda barrera, si `usuarios` contiene algún documento sin `demo: true`, aborta sin escribir, incluso si el host es local. La lista de hosts permitidos está fija en el código; ninguna clave de configuración, variable de entorno ni marca la amplía o la desactiva. `Seed:Demo = true` y estar fuera de `Production` solo deciden si el cargador se intenta ejecutar al arrancar; nunca relajan las guardas. Invocado sin la marca o en `Production`, falla indicando la causa sin valores de configuración y no escribe.
- **R-08:** Documentar en `db/seeds/README.md` el contenido, la carga local, el conteo y el borrado de la demo por `demo: true`; la carga a Azure se realiza aparte y a mano con `db/entrega.js`. No incluir secretos ni cadenas de conexión en la evidencia.
- **R-09:** No se agrega un endpoint ni evento. Si se definen códigos de error, se usa inglés y `snake_case`, conforme a la convención de la spec 002; no se fija aquí un código nuevo.
- **R-10:** Las cuentas demo quedan activas, con perfil completo y consentimientos aceptados con la versión vigente, conforme al modelo de la spec 015; sus nombres son claramente ficticios.

## 4. Criterios de aceptación y verificación
Los métodos son pruebas propuestas, aún no ejecutadas; CA-01 a CA-11 conservan el número del caso heredado `TC-TT-BD-03-NN`, y CA-12 y CA-13 cubren las decisiones nuevas.

- **CA-01:** Dado el JSON, al leerlo es un arreglo con formato del catálogo, `_id` válidos y únicos también frente al catálogo, camelCase, `version: 1` y `demo: true`. Verificación: `DatabaseTest/Demo/DemoSeedFileTests.FormatAndIds_CA_038_01`.
- **CA-02:** Dado el JSON, al contarlo hay diez clientes, cada uno con nombre no vacío y `uid` ficticio único `^demo-cliente-\d{2}$`, sin técnicos ni solicitudes. Verificación: `DatabaseTest/Demo/DemoSeedFileTests.TenClients_CA_038_02`.
- **CA-03:** Al revisar el JSON, todos los celulares únicos cumplen `^\+5200000\d{5}$`, no hay campos sensibles indicados en R-02 ni teléfonos, correo o URL en nombres. Verificación: `DatabaseTest/Demo/DemoSeedFileTests.FictitiousData_CA_038_03`.
- **CA-04:** Tras cargar, cada `sub` coincide con `UserIdDerivation.Derive(uid)` y el documento no contiene el `uid`. Verificación: `DatabaseTest/Demo/DemoSeedLoaderTests.DerivedSub_CA_038_04`.
- **CA-05:** En `identity` vacía, dos cargas dejan exactamente diez documentos demo y los `_id` originales; conteo, `_id`, `version` y `celularHash` no cambian y el cifrado se descifra al original, sin comparar sus bytes. Verificación: `DatabaseTest/Demo/DemoSeedLoaderTests.RepeatedLoad_CA_038_05`.
- **CA-06:** Tras cargar, ningún campo contiene celulares en claro; cada usuario tiene cifrado y hash de la spec 015, y el cifrado se descifra con la llave de prueba y dato asociado `usuarios|celular|<_id>`. Verificación: `DatabaseTest/Demo/DemoSeedLoaderTests.ProtectedPhones_CA_038_06`.
- **CA-07:** En `Production` o sin `Seed:Demo = true`, invocar el cargador falla con causa sin valores de configuración y deja cero documentos. Verificación: `DatabaseTest/Demo/DemoSeedLoaderTests.EnvironmentGuard_CA_038_07`.
- **CA-08:** La prueba de arquitectura comprueba que el cargador de Identidad escribe mediante su acceso a datos, sin `IMongoCollection` propio, solo en `identity` y sin leer colecciones ajenas. Verificación: `UnitTest/Architecture/DemoSeedBoundaryTests.IdentityBoundary_CA_038_08`.
- **CA-09:** En `documentdb-local` con `identity` vacía, dos arranques no productivos de la API con la marca dejan `countDocuments({ demo: true }) = 10` tras cada uno. Verificación: (comando) conteos en el PR sin credenciales ni cadenas.
- **CA-10:** Al compilar y verificar formato, `dotnet build Knock.slnx -c Release --no-restore` termina con cero errores y advertencias y `dotnet format Knock.slnx --verify-no-changes` sin cambios. Verificación: (comando) salida en el PR.
- **CA-11:** Si un usuario existente tiene el mismo `sub` o `celularHash` con otro `_id`, la carga informa el índice sin celular ni hash, no crea documentos demo y no altera al existente. Se verifica por separado que una carrera sea rechazada por cada índice único y que, ante un fallo de base a mitad de carga, los documentos escritos permanezcan y una siguiente carga complete los diez sin duplicados. Verificación: `DatabaseTest/Demo/DemoSeedLoaderTests.UniqueConflict_CA_038_11`.
- **CA-12:** Con un host de conexión distinto de `localhost`, `127.0.0.1` y `documentdb-local`, el cargador aborta antes de escribir aunque `Seed:Demo = true`, fuera de `Production` y con cualquier otra configuración; no existe forma de omitir la guarda. Se prueban varios hosts locales permitidos y una lista mixta rechazada; credenciales `usuario:clave@host` con host local y remoto; rechazo de `mongodb+srv://` incluso con nombre local; y rechazo sin escritura cuando `usuarios` contiene un documento sin `demo: true` aunque el host sea local. La extracción usa `MongoUrl.Servers`, no texto. Verificación: `DatabaseTest/Demo/DemoSeedLoaderTests.NonLocalHost_CA_038_12`.
- **CA-13:** Tras cargar, los diez usuarios están activos, tienen perfil completo y consentimientos aceptados con la versión vigente según la spec 015; sus nombres son claramente ficticios. Verificación: `DatabaseTest/Demo/DemoSeedLoaderTests.DemoAccountState_CA_038_13` y revisión humana de nombres.

## 5. Contrato y datos
- Sin endpoints, eventos ni cambios en OpenAPI. `Seed:Demo` habilita la carga según R-07; las llaves de cifrado y HMAC son secretos del entorno y nunca se versionan.
- El JSON de entrada incluye `_id`, `uid`, `celular`, `nombre`, `estado`, `perfilCompleto`, `creadoEn`, `version` y `demo`. El estado, perfil y consentimientos cargados cumplen R-10 y el modelo de la spec 015.
- En `usuarios`, `sub` sustituye al `uid` y `celularCifrado` y `celularHash` al celular de entrada. Se usan los índices únicos existentes de `sub` y `celularHash`; no se agregan índices.

## 6. Amenazas y controles
| Amenaza | Control y verificación |
|---|---|
| Datos reales o sensibles en la semilla | Datos inventados, rango ficticio y revisión humana de nombres; CA-02 y CA-03. |
| Fuga de celular, uid, llaves o conexión | Protección de la spec 015, no registrar valores y evidencia sin secretos; CA-04, CA-06 y CA-09. |
| Carga fuera del entorno o datos mezclados | Guardas de entorno, hosts analizados y colección sin documentos no demo; CA-01, CA-07 y CA-12. Un túnel a una base remota vacía o exclusivamente demo sigue siendo un riesgo. |
| Duplicados o conflictos con usuarios existentes | Upsert por `_id`, validación previa e índices únicos; una falla durante la carga puede dejar escrituras parciales recuperables; CA-05 y CA-11. |
| Acceso a otra base | Frontera del servicio y acceso a datos propio; CA-08. |
| Suplantación | No crear cuentas Firebase para la demo; `uid` y teléfonos ficticios y `sub` derivado; CA-02 a CA-04. |
| Envío de celulares a IA | El celular permanece en Identidad; la semilla no incluye textos libres salvo el nombre; CA-03 y CA-06. |

## 7. Plan de pruebas
| Tipo | ¿Aplica? | Motivo |
|---|---|---|
| Base de datos e integración | Sí | JSON y cargador con `documentdb-local` y Testcontainers: CA-01 a CA-07 y CA-11 a CA-13. |
| Arquitectura | Sí | Frontera de Identidad: CA-08. |
| Seguridad negativa | Sí | Datos sensibles, guardas y conflicto: CA-03, CA-06, CA-07, CA-11 y CA-12. |
| Humo local | Sí | Dos arranques y conteos: CA-09. |
| Build y formato | Sí | CA-10. |
| Contrato HTTP, eventos, UI, rendimiento y mutación | No | No hay endpoint, evento ni lógica de negocio; sin meta de mutación específica heredada. |

La heredada indica cobertura mínima de 60 % de líneas y 50 % de ramas, sin bajar umbrales. Ninguna prueba de esta migración se declara ejecutada.

## 8. Decisiones
Diez clientes, sin técnicos ni solicitudes; nombres manuales y números ficticios; carga dentro de la API, al arrancar con marca y fuera de producción; sin cuentas Firebase; upsert por `_id`, sin escritura ante conflicto detectado previamente y recuperable tras fallo parcial; comparar hash y no cifrado aleatorio. La aprobación anterior corresponde a TT-BD-03, no a esta spec.

**Julián, 2026-11-10:** cuentas demo activas, con perfil completo, consentimientos aceptados con la versión vigente según la spec 015 y nombres claramente ficticios; el cargador solo escribe con host `localhost`, `127.0.0.1` o `documentdb-local`, sin excepción ni opción de omitir la guarda; Azure se carga aparte y a mano con `db/entrega.js`. Técnicos y solicitudes corresponden a KNOCK-246, después de la spec 020.

## 9. Riesgos y dudas abiertas
- **Frontera de datos:** la heredada usa `IdentityStore` y `Modules/Identity`; el servicio actual tiene `Modules/Sesiones`. ¿Qué acceso a datos concreto debe extenderse y cómo se mantiene la regla de arquitectura al adaptarlo?
- **Atomicidad:** **Decidido (Julián):** basta un upsert por `_id`; los índices únicos rechazan carreras y no se garantiza atomicidad de toda la carga. Un fallo intermedio deja lo escrito y la siguiente carga completa el resto, conforme a R-05.
- **Caché:** la constitución exige invalidación al escribir. **Decidido (Julián, 2026-11-10, sí a la propuesta):** no hay caché de usuarios que invalidar; confirmar frente a la caché de perfil existente.
- **Errores:** ¿se necesitan códigos para fallos del cargador no HTTP? **Decidido (Julián, 2026-11-10, sí a la propuesta):** no hacen falta, porque no expone HTTP; si falla, aborta el arranque con un log sin datos sensibles. Si se definen, aplicar R-09.
- **Nombre:** ¿dónde se guarda? **Decidido (Julián, 2026-11-10, sí a la propuesta):** en `usuarios`, como en la spec 017.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge).
