---
name: auth-firebase-jwt
description: Inicio de sesión de Knock con Firebase Phone Auth en la app y JWT propios en el backend (refresh rotativo, detección de reuso, blacklist en Redis). Úsala en el módulo Identity y al proteger endpoints.
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/auth-firebase-jwt/SKILL.md. No editar a mano. -->
# auth-firebase-jwt

## Flujo
1. App: `FirebaseAuth.verifyPhoneNumber` → código SMS → `signInWithCredential` → `getIdToken()`.
2. App → `POST /api/v1/auth/firebase { firebaseIdToken }` (la ruta de la ficha, HU-001 y TT-PRU-04; decisión del revisor en KNOCK-21).
3. Backend: `FirebaseAuth.DefaultInstance.VerifyIdTokenAsync` (Firebase Admin SDK) → toma `phone_number` y `uid` **del token**.
4. Crea o recupera el usuario y emite JWT propio + refresh token.

## JWT
- HS256, 15 min, `ClockSkew = Zero`, `ValidAlgorithms = [HS256]`, issuer/audience, claims `sub`, `jti`, `role`.
- Clave ≥ 32 bytes solo desde variable de entorno o Key Vault.
- `OnTokenValidated`: rechaza si existe `blacklist:{jti}` en Redis.

## Refresh
- 256 bits aleatorios; se guarda solo SHA-256; 30 días.
- Rotación en cada uso: el anterior se revoca y apunta al nuevo (`replaced_by`).
- **Reuso detectado** (token ya revocado) → revocar toda la familia y responder 401.

## Logout
- Revoca el refresh y agrega `blacklist:{jti}` con TTL = vida restante del access token.

## Desarrollo y pruebas
- Números de prueba de Firebase en la app de demo.
- Integración con el **emulador oficial de Firebase Auth** (`FIREBASE_AUTH_EMULATOR_HOST`).

## Verificación
- Algoritmo `none` o RS256 rechazado; token vencido 401; reuso de refresh revoca familia; logout invalida al instante.
