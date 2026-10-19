# Nota del spike — KNOCK-21 · TT-API-02 Validar el token de Firebase y emitir la sesión propia (JWT)

> Spec: `specs/TT-API-02-spike-token-firebase-sesion-jwt/spec.md` (Aprobada) · Fecha: 2026-10-17
> Es un spike: demuestra que el canje funciona de punta a punta contra el emulador oficial de Firebase Auth. No es la solución completa de HU-001.
> No se registran claves, tokens, códigos de verificación ni números de teléfono.

## Condiciones antes de usar esto (leer primero)
- **El refresh token todavía no funciona.** El canje devuelve un `refreshToken` (256 bits aleatorios), pero no se guarda su hash ni existe un endpoint para canjearlo, rotarlo o revocarlo. La app no debe contar con él hasta HU-001 y HU-003.
- **No desplegar sin rate limit.** `POST /api/v1/auth/firebase` es público y no tiene límite de peticiones (solo el tope de 8 KiB de cuerpo y 4096 caracteres para el ID token). Se acepta únicamente mientras la API corre en local. Hasta que exista el pipeline de seguridad de la constitución §4 (RateLimiter, CORS, HSTS, cabeceras) esta API no se despliega a ningún entorno compartido.
- **Sin cierre de sesión ni lista de revocación.** Un JWT emitido vale 15 minutos y no se puede invalidar antes (falta `blacklist:{jti}` en Redis).

## Qué funcionó
- `POST /api/v1/auth/firebase` valida el ID token con el Firebase Admin SDK, exige `phone_number` y responde `{ accessToken, refreshToken }`; cualquier rechazo (malformado, vencido, sin teléfono, de otro proyecto) da el mismo 401 `application/problem+json`.
- JWT propio HS256 de 15 minutos con `sub`, `jti`, `iss` y `aud`; sin teléfono ni `role`. `GET /api/v1/me` protegido responde `{ id }` con el `sub`.
- El `sub` es un GUID v5 determinista del `uid` de Firebase (namespace fijo e inmutable): el mismo `uid` da el mismo `sub` sin base de datos. El vector de prueba se calculó con `uuid.uuid5` de Python.
- El emulador de Firebase Auth valida el proyecto y el vencimiento como producción (los tokens de otro proyecto y los vencidos se rechazan); no valida la firma real, que es la parte del caso 15.
- Los controllers de Identity son `internal` y se descubren con un `ControllerFeatureProvider` en `Hosting/`; las pruebas de arquitectura de TT-API-01 siguen en verde sin cambios.
- La clave del JWT sale de user-secrets o variables de entorno (Base64, al menos 32 bytes); en Development, si falta, se genera una aleatoria por arranque; fuera de Development la API no arranca sin ella. La API tampoco arranca fuera de Development si está definida `FIREBASE_AUTH_EMULATOR_HOST`.
- El emulador corre con `npx firebase-tools@15.34.0 emulators:start --only auth --project demo-knock`, escuchando solo en 127.0.0.1, sin credenciales ni Docker y sin Java (verificado por el revisor); requiere Node 20, 22 o 24.

## Qué falta para HU-001
- Persistir usuarios (cifrado de campo y HMAC para buscar por teléfono), `esNuevo` y «perfil incompleto»; hoy el teléfono no se guarda.
- Refresh token real: guardar solo su SHA-256, rotación en cada uso, detección de reuso y revocación de la familia; cierre de sesión y `blacklist:{jti}` en Redis.
- Rate limit del canje y del resto del pipeline de seguridad (otra tarea con su spec).
- `role` y permisos, consentimiento, nombre, zona y foto.
- Contrato vivo: `docs/openapi.v1.json`, `docs/endpoints.md` y su publicación a knock-frontend (pospuesto por la spec).
- Cuenta de servicio de Firebase: la verificación del ID token usa llaves públicas de Google y no se configuró ninguna; la necesitarán funciones como revocar sesiones de Firebase. Que la verificación de producción funcione con ese esquema se comprueba en el caso 15 (no verificado todavía).
- Mutación del módulo Identity (meta de 80 %) cuando exista su lógica real.

## Riesgos
- **Canje público sin límite** (ver condiciones): fuerza bruta o ráfagas de tokens contra el endpoint.
- **Emulador sin firma:** sus tokens no llevan firma; un despliegue con `FIREBASE_AUTH_EMULATOR_HOST` los aceptaría. Mitigado con el guardia de arranque (la API no inicia) y su prueba.
- **Firma real no probada en CI:** solo el caso 15, manual y contra el proyecto `knock-0` con un número de prueba, comprueba la firma de Google.
- **`firebase-tools` 15.34.0:** riesgo aceptado como herramienta de pruebas. El `npm audit` del revisor reporta 3 vulnerabilidades altas y 4 moderadas en dependencias del propio CLI (FTP, proxy, braces, chokidar). Condiciones: el emulador escucha solo en 127.0.0.1, usa un proyecto `demo-` sin credenciales, corre solo en CI y local, y la versión se revisa cada mes.
- **`sub` derivable:** quien conozca un `uid` y el namespace puede calcular el `sub`; no autoriza nada por sí solo (hace falta el JWT firmado). Riesgo bajo aceptado.
- **SHA-1 propio:** el GUID v5 exige SHA-1 y los analizadores del repositorio prohíben la API estándar sin excepción registrada; se implementó a mano con prueba contra la referencia de Python. Si se prefiere la API estándar, hay que registrar una excepción.
- **Datos de prueba:** solo números ficticios y el emulador; los códigos de los números de prueba de `knock-0` nunca van al repositorio.

## Pendiente de una persona
- Caso 15: canje real contra `knock-0` con un número de prueba de TT-FB-01 y `/me` en 200; evidencia en el PR sin códigos, llaves ni tokens.
- Paso del emulador en el CI (`.github/` es protegido): `npx firebase-tools@15.34.0 emulators:start --only auth --project demo-knock`, `FIREBASE_AUTH_EMULATOR_HOST=127.0.0.1:9099`, Node 20, 22 o 24.
