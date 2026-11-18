# Plan 015 — Registro e inicio de sesión

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Definir las colecciones `usuarios` y `sesiones` con sus índices (único en `sub`, único en `celularHash`, único en `huellaRenovacion`, TTL en `venceEn`) en un script de `db/`.
2. Crear el módulo `Sesiones` del servicio de Identidad como rebanada vertical: validador, servicio, repositorios y controller.
3. Implementar el verificador de tokens de Firebase detrás de una interfaz.
4. Derivar el `sub`, cifrar el celular y calcular su hash; emitir el JWT de acceso y el token de renovación con su huella.
5. Marcar la ruta como pública en el gateway con su política de límite estricta.
6. Escribir las pruebas desde los criterios de aceptación.

## Archivos que se crean o cambian
- `db/002-identidad-usuarios-sesiones.js`
- `src/Knock.Identity.Api/Modules/Sesiones/Controllers/AuthController.cs`
- `src/Knock.Identity.Api/Modules/Sesiones/Dtos/` (solicitud y respuesta)
- `src/Knock.Identity.Api/Modules/Sesiones/Validators/FirebaseLoginValidator.cs`
- `src/Knock.Identity.Api/Modules/Sesiones/Services/` (inicio de sesión, emisión de tokens)
- `src/Knock.Identity.Api/Modules/Sesiones/Domain/` (`Sesion`, estados de la cuenta, derivación del `sub`)
- `src/Knock.Identity.Api/Modules/Sesiones/Data/` (repositorios de `usuarios` y `sesiones`)
- `src/Knock.Identity.Api/Common/Firebase/IFirebaseTokenVerifier.cs` y su implementación
- `src/Knock.Gateway/` (ruta pública y política de límite de autenticación)
- `tests/` (unitarias, integración y seguridad negativa del módulo)

## Dependencias
- Specs 002, 003, 009 y 010 (estructura, datos, JWT y respuestas sin compresión).
- Proyecto de Firebase con autenticación por teléfono.
- Una librería para verificar el token de Firebase (por evaluar y registrar en `Memoria.md`).
- Clave de firma en Key Vault.

## Decisiones de la spec que este plan sigue
- **Registro y login en un solo endpoint:** 201 distingue la cuenta nueva.
- **El nombre se pide después:** `perfilCompleto = false` hasta `PUT /me`.
- **Se hereda HU-001 y TT-API-12:** `sub` derivado, celular cifrado más hash y 409 por conflicto.
- **Token de renovación opaco y con huella.**
- **Sin SMS propio:** lo envía Firebase.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
