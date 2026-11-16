# Modelo de amenazas (STRIDE)

> TT-SEC-03 (KNOCK-78). Fecha: 2026-11-06. Cubre los componentes que existen hoy en el backend y su entorno de Azure (`rg-knock-dev`). Cada amenaza tiene su mitigación existente (archivo, spec o prueba reales) o queda marcada como **Brecha**. Los hallazgos del escaneo ZAP y las brechas se concentran en la sección 3.

> **Alcance del escaneo ZAP, leer antes de citar este documento.** El escaneo de la sección 3.1 fue un **baseline pasivo** de menos de diez peticiones. El rastreo solo alcanzó la raíz, `robots.txt` y `sitemap.xml`, porque todo lo demás responde 401. **No cubrió ninguna ruta protegida por autenticación** y **no constituye una validación completa de la seguridad del API**. Para esa validación falta un escaneo de API o activo, con el OpenAPI y un token de prueba, en infraestructura temporal creada y destruida con Terraform sin costo (ver sección 4). La parte de STRIDE de este documento es un análisis de diseño y de código; no depende de ese escaneo.

## 1. Alcance y método

- STRIDE por componente: **S**uplantación, **M**anipulación (Tampering), **R**epudio, **D**ivulgación de información, **N**egación de servicio y **E**levación de privilegios.
- Fuentes: `docs/arquitectura.md`, `docs/security-headers.md`, `docs/rate-limit-matrix.md`, las specs 001, 003, 005, 007, 009 y 015 a 018, `deploy/`, `.github/workflows/ci-pr.yml` y la configuración real del entorno desplegado (consultada en solo lectura).
- Las pruebas citadas son las de `tests/SecurityTest` (`SEC_<spec>_<NN>`) y de `tests/IntegrationTest/Gateway` (`CA_009_<NN>`).
- La app móvil vive en el repositorio `knock-frontend`: aquí solo se modela lo que el backend hace frente a ella. Sus controles propios se modelan en ese repositorio.

### Flujo y fronteras de confianza

```
app móvil / panel ─HTTPS─▶ Gateway (único ingreso externo)
                              ├─▶ Identidad  ─▶ MongoDB (knock-docdb) · Redis · Firebase (verificación del ID token)
                              ├─▶ Mercado    ─▶ MongoDB · Redis
                              └─▶ Mensajería ─▶ MongoDB
Secretos: Key Vault, leídos por la identidad administrada de cada servicio.  Despliegue: GitHub Actions + Terraform.
```

Fronteras: Internet → Gateway; Gateway → servicios (red interna del entorno de Container Apps); servicios → MongoDB, Redis y Key Vault; repositorio → canalización de CI → Azure.

## 2. Amenazas por componente

Estado: **Cubierta** (mitigación vigente y probada), **Parcial** (mitigada con una limitación aceptada o sin prueba) o **Brecha** (sin mitigación; ver sección 3).

### 2.1 Gateway (`src/Knock.Gateway`)

| ID | STRIDE | Amenaza | Mitigación existente | Estado |
|---|---|---|---|---|
| GW-1 | S | JWT falsificado, con otro algoritmo, otra llave, otro destinatario o vencido | Un solo algoritmo (HMAC-SHA256), firmante, destinatario y vencimiento sin tolerancia (`Hosting/AuthenticationExtensions.cs`); `CA_009_04`, `SEC_009_02`, `SEC_009_04`, `SEC_009_05` | Cubierta |
| GW-2 | S | Sesión cerrada que sigue usando su JWT | El `sid` cancelado en Redis rechaza el JWT aunque no haya vencido (`RevokedSessions.cs`); `CA_009_06` | Cubierta |
| GW-3 | M | Suplantar la IP con `X-Forwarded-For` para evadir el límite | Solo se confía en la red del ingreso (`ForwardedHeaders__KnownNetworks__0` en `deploy/apps/main.tf`); `SEC_015_13` | Cubierta |
| GW-4 | M | Ruta no declarada llega a un servicio | Lo que no declara política cae en «exigir sesión»; una ruta fuera de la tabla responde 404 (`appsettings.json`, `docs/arquitectura.md`); `UnknownRoute_IsNotForwardedToAnyService_R_02` en `GatewayRoutingTests` | Cubierta |
| GW-5 | R | No poder rastrear una petición | `X-Correlation-Id` validado y reenviado, Serilog estructurado (specs 001 y 011) | Cubierta |
| GW-6 | D | Respuestas de error que revelan el motivo o datos internos | Cuerpo `ProblemDetails` idéntico en todo rechazo (`SEC_009_03`, `CA_009_01`); encabezados de seguridad y `no-store` en toda respuesta (`SecurityHeadersMiddleware.cs`, `SEC_009_09`, `CA_009_02`); registros con redacción (`TelemetryRedactor.cs`) | Cubierta |
| GW-7 | D | Se anuncia el servidor o falta HSTS en el despliegue real | Se pretende retirar `Server` y emitir HSTS (`SecurityHeadersMiddleware.cs`, `Program.cs`), pero el gateway desplegado respondía `server: Kestrel` (corregido en KNOCK-240 y verificado en vivo el 2026-11-13) y no envía `Strict-Transport-Security` | **Cubierta** (H-01 y H-02 cerradas el 2026-11-13 con `GET` al gateway desplegado; tabla en «Evidencia de cierre») |
| GW-8 | N | Fuerza bruta o inundación | Límite por IP (100/min), estricto en `/auth` (10/min) y por usuario (120/min) con contadores en Redis compartidos entre réplicas (`docs/rate-limit-matrix.md`); `CA_009_07` | Cubierta |
| GW-9 | N | Redis caído deja al gateway sin límites o sin revocación | Responde 503 con `Retry-After` en vez de saltarse el control (`CA_009_06`, `CA_009_07`). Cuesta disponibilidad: sin Redis no hay servicio | Parcial |
| GW-10 | E | Acceder a recursos de otra persona por el gateway | El gateway solo autentica; la autorización por recurso se decide en cada servicio (ver 2.2 y 2.3). Denegar por defecto | Cubierta |
| GW-11 | M | CORS abierto a cualquier origen | Solo los orígenes de `Security:Cors:AllowedOrigins`; sin ninguno no se permite ninguno (`CorsExtensions.cs`); `CA_009_03` | Cubierta |

### 2.2 Identidad (`src/Knock.Identity.Api`)

| ID | STRIDE | Amenaza | Mitigación existente | Estado |
|---|---|---|---|---|
| ID-1 | S | Token de Firebase falso o roto | Se verifica con el Firebase Admin SDK (firma, vencimiento, emisor y proyecto; `FirebaseIdTokenVerifier.cs`); `SEC_015_01` | Cubierta |
| ID-2 | S | Robo del refresh token | Rotación en cada uso; reutilizar un token robado revoca solo las sesiones de su dueño; todos los rechazos tienen el mismo cuerpo `SEC_016_03`, `SEC_016_04` | Cubierta |
| ID-3 | S | Usar un access token como refresh o al revés | `SEC_016_02` | Cubierta |
| ID-4 | M | Cuenta creada o modificada con campos que el cliente inyecta | La cuenta sale del token verificado, los campos extra se ignoran (`SEC_015_07`, `SEC_017_03`); validación hostil de nombre y campos opcionales (`SEC_017_04`, `SEC_017_05`) | Cubierta |
| ID-5 | M | Texto con CR/LF incrustado en `fotoUrl` | Hallazgo H-3 de KNOCK-203: `ProfileRules` lo acepta y lo guarda. La prueba `SEC_017_06` está omitida (`Skip`) como evidencia; la corrección está en el PR #25, abierto | **Brecha** conocida (H-08) |
| ID-6 | R | Una persona niega haber iniciado o cerrado sesión | Sesiones por dispositivo (spec 018) y registros con correlación, sin datos personales (`SEC_015_12`, `SEC_016_12`) | Parcial: no hay bitácora de auditoría dedicada |
| ID-7 | D | Fuga del celular o de tokens en registros o respuestas | Celular con AES-256-GCM y búsqueda por HMAC-SHA256 (`Services/PhoneProtector.cs`); respuestas sin caché ni compresión (`SEC_015_08`, `SEC_016_11`, `SEC_017_12`); registros sin datos personales (`SEC_015_12`, `SEC_017_14`) | Cubierta |
| ID-8 | D | Enumerar cuentas por el mensaje de error | `SEC_015_09`, `SEC_016_03`, `SEC_017_13`. Excepción: el 400 automático por tipo JSON incorrecto nombra tipos internos de .NET (hallazgo H-2 de KNOCK-203, `SEC_017_16` omitida; corrección en el PR #25) | Parcial (H-08) |
| ID-9 | N | Cuerpos enormes o abuso de `/auth` | Límites de cuerpo (`SEC_015_04`, `SEC_016_07`, `SEC_017_08`); límite estricto por IP (`SEC_016_13`) | Cubierta |
| ID-10 | E | Leer o cambiar el perfil de otra persona; cerrar la sesión de otra | `SEC_017_01`, `SEC_017_02`, `SEC_016_01`, `SEC_016_05` | Cubierta |
| ID-11 | E | Cancelar en Redis el `sid` de otra persona al cerrar sesión | Hallazgo H-1 de KNOCK-203: `LogoutService` escribe `sesion-cancelada:{sid}` sin comprobar que la sesión es del `sub` del token. `SEC_016_06` está omitida (`Skip`); la corrección está en el PR #25, abierto. `SEC_016_05` sí prueba que la sesión de la otra persona no se cierra | **Brecha** conocida (H-08) |

### 2.3 Mercado (`src/Knock.Market.Api`)

| ID | STRIDE | Amenaza | Mitigación existente | Estado |
|---|---|---|---|---|
| MK-1 | S | Llamar a rutas protegidas sin sesión válida | El servicio valida el JWT por su cuenta, además del gateway (`Hosting/AuthenticationExtensions.cs`); `SEC_009_01` y `SEC_009_02` recorren las rutas protegidas de Identidad y Mercado (`Support/ProtectedRoutes.cs`) | Cubierta |
| MK-2 | M | Coordenadas o filtros hostiles | Validación previa y 422 sin eco del valor (`SEC_TT_API_07_01`); filtros del catálogo sin concatenar texto (spec 003) | Cubierta |
| MK-3 | D | Fuga de coordenadas en registros o errores | `SEC_TT_API_07_02`, `SEC_TT_API_07_04` | Cubierta |
| MK-4 | D | El catálogo público devuelve más campos de los debidos | Solo oficios activos y tres campos (`SEC_TT_PRU_01_01`, `SEC_TT_PRU_01_03`) | Cubierta |
| MK-5 | N | Abuso de los endpoints públicos | Límite global por IP en memoria de cada réplica (`SEC_TT_PRU_01_05`, `SEC_TT_API_07_05`); con 2 réplicas el límite efectivo se duplica, y el gateway aplica el límite compartido | Parcial |
| MK-6 | E | Escalar de «lectura» a escritura | Métodos no permitidos responden 405 (`SEC_TT_PRU_01_02`, `SEC_TT_API_07_03`) | Cubierta |
| MK-7 | R | Solicitudes y cotizaciones sin rastro | Las solicitudes (HU de Mercado) todavía no existen en el código; la trazabilidad se diseña con esas specs | Brecha futura, sin código hoy |

### 2.4 Mensajería (`src/Knock.Messaging.Api`)

Hoy no expone ningún controlador de negocio: solo `/health`, `/health/ready` y OpenAPI en Development (`Program.cs`).

| ID | STRIDE | Amenaza | Mitigación existente | Estado |
|---|---|---|---|---|
| MS-1 | S | Llamar al servicio sin pasar por el gateway | Ingreso interno en Container Apps (`external = false` en `deploy/apps/main.tf`). Desde KNOCK-241 (BUG-004) valida el JWT de Knock (HS256, mismo emisor y audiencia, sin `alg=none`), la sesión cancelada en Redis (503 si Redis no responde), pone encabezados de seguridad y limita por IP (`tests/SecurityTest/Messaging`, TC-BUG-004-01 a 05 y 08) | **Cubierta** (H-03 cerrada el 2026-11-13) |
| MS-2 | E | Servicio con rutas de chat sin autorización por recurso | El JWT y la política por defecto «exigir sesión» ya están (BUG-004); la autorización por recurso (quién ve qué conversación) entra con la spec 026, antes de exponer rutas | Parcial |
| MS-3 | D | Mensajes entre personas ajenas visibles | Sin código todavía; la base propia por servicio impide leer datos de otro (`tests/ArchitectureTest`, spec 003) | Parcial |

### 2.5 MongoDB (Azure DocumentDB `knock-docdb`)

| ID | STRIDE | Amenaza | Mitigación existente | Estado |
|---|---|---|---|---|
| DB-1 | S | Conexión con credenciales robadas | Cadena de conexión por servicio en Key Vault (`mongo-identity`, `mongo-market`, `mongo-messaging`), nunca en el repositorio; cada servicio solo lee la suya (`deploy/modules/service/main.tf`) | Cubierta |
| DB-2 | M | Inyección en consultas | Controlador tipado y validación previa, sin concatenar (spec 003; `SEC_015_02`, `SEC_TT_API_07_01`) | Cubierta |
| DB-3 | M | Borrar o recrear el clúster por accidente | `prevent_destroy` y estado separado en `deploy/database/`; el script `deploy/up.sh` (PR #30) se detiene si un plan destruye algo | Cubierta |
| DB-4 | D | Fuga de datos personales en reposo | Celular cifrado (AES-256-GCM) y con HMAC para buscar (`PhoneProtector.cs`) | Parcial: solo el celular tiene cifrado por campo |
| DB-5 | D | Acceso de red desde fuera | El clúster tiene acceso de red público y una regla `azure-services` (0.0.0.0) además de una IP de administración individual; la protección depende solo de usuario y contraseña con TLS | **Brecha** aceptada por costo cero (H-04, excepción EXC-017 en el PR de riesgo aceptado de KNOCK-78) |
| DB-6 | N | Pérdida de datos (nivel gratuito sin respaldo) | `deploy/backup/mongo-backup.sh` (`mongodump` a Blob) con restauración probada en `tests/DatabaseTest/Scripts/Spec003BackupRestoreTests.cs` | Cubierta |
| DB-7 | R | Cambios sin rastro | Registros de actividad de la plataforma de Azure; sin auditoría a nivel de documento | Parcial |

### 2.6 Redis (`knock-redis`, contenedor propio)

| ID | STRIDE | Amenaza | Mitigación existente | Estado |
|---|---|---|---|---|
| RD-1 | S | Acceso sin credenciales | `--requirepass` con la contraseña de Key Vault (`redis-password`) y ingreso TCP interno (`external_enabled = false`) | Cubierta |
| RD-2 | M | Alterar contadores o revocaciones desde un contenedor ajeno | Solo se protege con la contraseña; todos los servicios con acceso comparten la misma | Parcial |
| RD-3 | D | Sniffing del tráfico dentro del entorno | El tráfico a Redis es TCP sin TLS dentro de la red del entorno de Container Apps | **Brecha** aceptada (H-05) |
| RD-4 | N | Reinicio de Redis pierde las revocaciones | Sin persistencia por diseño (spec 003, R-07): un token cerrado vale hasta 15 minutos. Riesgo aceptado en la spec 003 (sección de decisiones) | Parcial |
| RD-5 | N | Redis es punto único de falla | 1 réplica; el gateway responde 503 en vez de abrir el paso (`CA_009_06`) | Parcial |
| RD-6 | E | Instalar una versión con licencia no permitida o con CVE | Imagen fijada por digest (`redis:7.2-alpine@sha256:…` en `deploy/apps/variables.tf`) | Cubierta |

### 2.7 Key Vault (`<key-vault>`)

| ID | STRIDE | Amenaza | Mitigación existente | Estado |
|---|---|---|---|---|
| KV-1 | E | Un servicio lee secretos de otro | Rol «Key Vault Secrets User» por secreto, no por bóveda, con una identidad administrada por servicio (`deploy/modules/service/main.tf`); autorización RBAC activada | Cubierta |
| KV-2 | D | Valores de secretos en Terraform o en el estado | Los valores no pasan por Terraform: los escribe una persona (spec 005, CA-04; `deploy/README.md`) | Cubierta |
| KV-3 | D | Acceso desde Internet | Acceso de red público habilitado: Container Apps no es servicio de confianza del firewall y los endpoints privados cuestan (EXC-012). Se limita con RBAC | Parcial |
| KV-4 | N | Borrado o corrupción de un secreto | Eliminación suave activa, sin protección de purga para poder recrear el entorno (EXC-012) | Parcial |
| KV-5 | M | Llaves débiles | El código exige JWT de al menos 32 bytes, cifrado de exactamente 32 y HMAC distinta (`deploy/README.md`, «Formato que exige el código») | Cubierta |

### 2.8 App móvil (vista del backend)

| ID | STRIDE | Amenaza | Mitigación existente | Estado |
|---|---|---|---|---|
| AM-1 | S | App o cliente falso que usa el API | El backend no confía en la app: exige el ID token de Firebase y emite sus propios JWT (`ID-1`, `GW-1`) | Cubierta |
| AM-2 | S | Abuso del inicio de sesión por teléfono (SMS) | Límite estricto de `/auth` por IP en el gateway (`GW-8`) y en el servicio (`SEC_015_13`); el límite por número es de HU-001/HU-003 | Parcial |
| AM-3 | D | Robo de tokens del dispositivo | Access token de 15 minutos y refresh con rotación y detección de reuso (`ID-2`) | Cubierta |
| AM-4 | E | Manipular la app para ver datos ajenos | Toda autorización por recurso se decide en el servidor (`ID-10`, spec 009) | Cubierta |
| AM-5 | M | Controles propios de la app (almacenamiento seguro, certificados, ofuscación) | Viven en `knock-frontend` | Fuera de este repositorio |

### 2.9 Canalización de CI y despliegue

| ID | STRIDE | Amenaza | Mitigación existente | Estado |
|---|---|---|---|---|
| CI-1 | S | Credenciales del pipeline filtradas | Identidad federada (OIDC) en vez de contraseñas, rol mínimo sobre el grupo de recursos (spec 005; `deploy/bootstrap/bootstrap.sh`) | Cubierta |
| CI-2 | M | Acción de terceros alterada | Acciones fijadas por SHA, `pull_request` (no `pull_request_target`), permisos `contents: read` y secretos fuera de los PR (`.github/workflows/ci-pr.yml`) | Cubierta |
| CI-3 | M | Cambios a CI, scripts o seguridad sin revisión | `CODEOWNERS` pone `/.github/`, `/scripts/` y `/security/` bajo el Tech Lead; reglas de rama (`.github/rulesets/`, `docs/BRANCH_PROTECTION.md`) | Cubierta |
| CI-4 | D | Secretos o dependencias vulnerables llegan a `main` | Gitleaks, Semgrep, OSV-Scanner, Checkov, Trivy y Dockle en el job `gate` (`ci-pr.yml`) | Cubierta |
| CI-5 | M | Imagen con CVE publicada | Trivy bloquea críticas y altas con corrección; imagen base y apt actualizados (KNOCK-232) | Cubierta |
| CI-6 | D | Vulnerabilidades dinámicas sin detectar en PR | El job `dast` (ZAP baseline) existe pero **no corre**: su condición exige `deploy/docker-compose.ci.yml`, `tests/smoke/smoke.sh` y `security/zap-rules.tsv`, que no están en el repositorio | **Brecha** (H-06) |
| CI-7 | R | Cambios en infraestructura sin rastro | Todo por Terraform en el repositorio y estado remoto; el historial de Git es la bitácora | Cubierta |

## 3. Hallazgos y brechas

### 3.1 Escaneo ZAP baseline (pasivo)

- Objetivo: `https://gateway.ejemplo.invalid` (gateway desplegado), 2026-11-06, ZAP 2.17.0 (`ghcr.io/zaproxy/zaproxy:stable`), `zap-baseline.py -m 1 -T 5`, sin escaneo activo, sin fuzzing, sin carga, sin credenciales.
- Alcance real: el rastreo solo pudo pedir la raíz, `robots.txt` y `sitemap.xml` (todas responden 401 porque el gateway exige sesión), en menos de diez peticiones. Ninguna ruta del API se probó con sesión ni se creó ningún dato: el escaneo no genera SMS ni acciones con efectos reales. La cobertura de los endpoints del API requiere el escaneo de API en infraestructura temporal que pide la ficha de la tarea.
- Qué falta para validar el API: un escaneo de API o activo con el OpenAPI y un token de prueba, solo en infraestructura temporal (creada y destruida con Terraform, sin costo) y con autorización del Tech Lead. Hasta entonces, los resultados de este baseline no dicen nada sobre las rutas protegidas.
- Resultado: 66 reglas pasivas sin alerta, 0 altas, 0 medias, 1 baja, 1 informativa.
- Informes: `docs/evidencia/KNOCK-78-zap-baseline.html`, `.json` y `.md`.

| ID | Hallazgo | Gravedad | Qué se hace |
|---|---|---|---|
| Z-1 | `Strict-Transport-Security Header Not Set` (regla 10035) en `/sitemap.xml` | Baja | Es real y no es solo de esa ruta: ninguna respuesta del gateway desplegado trae HSTS (comprobado con `curl -D` en `/health`, `/sitemap.xml` y `/api/v1/me/x`). Pasa a H-01 |
| Z-2 | `Non-Storable Content` (regla 10049) en 3 URL | Informativa | Es el comportamiento buscado: el gateway responde `Cache-Control: no-store` (`docs/security-headers.md`). Sin acción; la regla se documenta como aceptada |
| Z-3 | Aviso de la automatización: el rastreo recibió 401 en la raíz | Informativa | Esperado: toda ruta fuera de las públicas exige sesión |

### 3.2 Brechas del modelo

| ID | Gravedad | Brecha | Qué se hace |
|---|---|---|---|
| H-01 | Baja | El gateway desplegado no emite `Strict-Transport-Security` aunque `Program.cs` llama a `UseHsts()` y `docs/security-headers.md` lo describe. El ingreso ya redirige HTTP a HTTPS con 301 y el certificado es válido, por lo que el riesgo es un primer acceso por HTTP sin protección contra downgrade | Corregido en código (KNOCK-239, BUG-002): `UseHsts` solo emite con `Request.IsHttps` y no a `localhost`, y detrás del ingreso la petición llega como HTTP; el gateway ahora fija `Strict-Transport-Security` en `SecurityHeadersMiddleware` fuera de Development, sin depender del esquema (`SecurityTest/Gateway/HstsTests`, TC-BUG-002-01 a 04). **La causa exacta en Azure no se comprobó** (sin acceso al ingreso): el arreglo no depende de ella. **Cerrada el 2026-11-13** con `GET` al gateway desplegado (TC-BUG-002-05; ver «Evidencia de cierre») |
| H-02 | Baja | El gateway desplegado responde `server: Kestrel`; `docs/security-headers.md` dice que ese encabezado está ausente. Identidad y Mercado fijan `AddServerHeader = false`; el gateway y Mensajería no | Corregido en código (KNOCK-240, BUG-003): `AddServerHeader = false` en el gateway y en Mensajería, con `SecurityTest/Gateway` y `SecurityTest/Messaging` `ServerHeaderTests` (TC-BUG-003-01 a 03). **Cerrada el 2026-11-13** con `GET` al gateway desplegado (TC-BUG-003-04; ver «Evidencia de cierre») |
| H-03 | Media | Mensajería no valida JWT, no tiene encabezados de seguridad ni límite de peticiones; hoy solo la protege el ingreso interno y no tiene rutas de negocio | Corregido en código (KNOCK-241, BUG-004): JWT, sesión cancelada con 503 si Redis falla, encabezados de seguridad y límite por IP, con `tests/SecurityTest/Messaging` y `tests/UnitTest/Messaging`. La autorización por recurso queda para la spec 026 (KNOCK-62). `ForwardedHeaders` ya está (#61, KNOCK-201). **Cerrada el 2026-11-13**: Mensajería corre desplegada con la llave del JWT y la conexión a Redis (ver «Evidencia de cierre») |
| H-04 | Media | MongoDB con acceso de red público y regla `azure-services` (0.0.0.0) | Aceptada por costo cero (el nivel gratuito no admite red privada). Controles compensatorios, revisión (2026-12-05), condición de salida y procedimiento para retirar las IP individuales: excepción EXC-017 y `docs/evidencia/KNOCK-78-riesgo-mongodb-acceso-publico.md` (PR de riesgo aceptado de KNOCK-78) |
| H-05 | Baja | Tráfico a Redis sin TLS dentro del entorno | Aceptada: red interna del entorno y Redis con contraseña; reevaluar si cambia el proveedor. Registrar en `security/exceptions.yaml` |
| H-06 | Media | El job `dast` del CI nunca corre (su condición exige `deploy/docker-compose.ci.yml` y `tests/smoke/smoke.sh`, que no existen; el job usaría además `security/zap-rules.tsv`, que tampoco existe) y el escaneo baseline semanal de la ficha no está programado | Registrado como KNOCK-238 (BUG-001, prioridad alta) para agregar `deploy/docker-compose.ci.yml`, `tests/smoke/smoke.sh` y `security/zap-rules.tsv`, o ajustar el trabajo; programar el baseline semanal fuera de la ventana de evaluación (25-nov a 5-dic) con la autorización del Tech Lead |
| H-07 | Baja | Sin bitácora de auditoría dedicada de sesiones ni de cambios de datos (ID-6, DB-7) | Evaluar al definir observabilidad de seguridad; sin acción inmediata |
| H-08 | Media | Tres defectos de Identidad ya conocidos (H-1 sesión ajena en Redis, H-2 tipos de .NET en el 400, H-3 CR/LF en `fotoUrl`), con sus pruebas `SEC_016_06`, `SEC_017_16` y `SEC_017_06` omitidas hasta que se fusione el PR #25 | Seguimiento en el PR #25 (KNOCK-203); al fusionarlo, quitar los `Skip` y pasar ID-5, ID-8 e ID-11 a «Cubierta» |

### Evidencia de cierre de H-01, H-02 y H-03 (2026-11-13)

Verificación del líder sobre el gateway desplegado, con `GET` a tres rutas (TC-BUG-002-05 y TC-BUG-003-04):

| Ruta | Respuesta | Strict-Transport-Security | Server / X-Powered-By |
|---|---|---|---|
| `/health` | 200 | `max-age=31536000; includeSubDomains` | ausentes |
| `/sitemap.xml` | 401 | igual | ausentes |
| `/api/v1/me/x` | 401 | igual | ausentes |

Mensajería (H-03): `knock-messaging` corre en la revisión `0000004` (Healthy, creada el 2026-11-13) con `Jwt__SigningKey` y la conexión a Redis, e ingreso interno. Como no se consulta desde fuera, su «401 sin token» no se puede mostrar con `curl`: lo cubren las pruebas del CI `SecurityTest/Messaging/AuthenticationTests` (TC-BUG-004-01) y las demás de `tests/SecurityTest/Messaging`.

**Hallazgos altos abiertos: ninguno** (ni en el escaneo ni en las brechas del modelo). Las brechas H-01, H-02 y H-03 están cerradas (2026-11-13); las medias H-04, H-06 y H-08 y las bajas H-05 y H-07 se registran para su triage en Jira.

## 4. Pendiente de la tarea KNOCK-78 fuera de este documento

- Escaneo de API activo con el OpenAPI y un token de prueba, solo en infraestructura temporal creada y destruida con Terraform, sin costo. Sin él, las rutas protegidas no tienen validación dinámica.
- Programar el baseline semanal, con tasa limitada, sin flujos que generen SMS ni acciones con efectos reales y fuera del 25-nov al 5-dic de 2026.
- Brechas ya registradas en Jira: H-06 → KNOCK-238 (BUG-001, prioridad alta), H-01 → KNOCK-239 (BUG-002), H-02 → KNOCK-240 (BUG-003) y H-03 → KNOCK-241 (BUG-004).
