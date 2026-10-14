# Evidencia — KNOCK-28 · TT-FB-01 Proyecto de Firebase, autenticación por teléfono, FCM y apps

> Spec: `specs/TT-FB-01-proyecto-de-firebase-autenticacion-por-t/spec.md` (Aprobada) · Fecha: 2026-10-14
> La configuración la hizo el Tech Lead en la consola de Firebase; ningún agente operó Firebase. Esta nota resume lo que él mostró en capturas y lo que reportó. Las capturas no están en el repositorio.
> No se registran claves, tokens, códigos de verificación ni archivos de configuración de las apps.

## Resumen por caso
| Caso | Resultado | Evidencia |
|---|---|---|
| TC-TT-FB-01-01 Un número de prueba recibe código sin SMS real | **Parcial.** El número de prueba está configurado; falta probar el inicio de sesión de punta a punta (app o spike TT-API-02). | Sección 2 |
| TC-TT-FB-01-02 FCM habilitado y credenciales fuera del repositorio | Hecho: la API de FCM (V1) está habilitada y no se descargó ninguna llave de cuenta de servicio. | Sección 3 |
| TC-TT-FB-01-03 Apps Android e iOS registradas, sin versionar sus archivos | **Pendiente.** Identificador decidido (`app.knock.mobile`); falta registrar las apps en la consola. Los archivos de configuración ya están en `.gitignore`. | Sección 4 |
| TC-TT-FB-01-04 Costo y límites de SMS confirmados | **Parcial.** El plan es Spark sin costo; con números de prueba no se envían SMS. La cuota de SMS reales no se verificó. | Sección 5 |

## 1. Proyecto
- Nombre `knock`, ID `knock-0`, plan **Spark** (sin costo, 0 USD).
- **Google Analytics desactivado** (no se necesita y reduce los datos tratados).
- Acceso del equipo configurado en «Usuarios y permisos» (propietario: Tech Lead; el resto con rol de editor, según lo reportado; no se verificó con captura).

## 2. Autenticación por teléfono (TC-01)
- Proveedor **Teléfono habilitado** en Authentication.
- **Cuatro números de prueba ficticios** con código fijo: `+1 650 555 0101`, `0102`, `0103` y `0104` (los 555-01xx son números de ficción). Los códigos no se documentan aquí; se guardan donde el equipo guarda contraseñas.
- No se usan teléfonos reales de personas: un número de prueba entra sin SMS con un código fijo, y si fuera real cualquiera que conozca el código podría iniciar sesión como esa persona.
- Pendiente para cerrar el caso: iniciar sesión con un número de prueba contra el emulador o el proyecto, desde la app (TT-MOB-*) o con el spike TT-API-02 que valida el token en el backend.

## 3. Cloud Messaging (TC-02)
- FCM API (V1) **habilitada** en Configuración del proyecto → Cloud Messaging, según lo reportado por el Tech Lead.
- No se descargó ninguna llave de cuenta de servicio. Cuando el backend envíe notificaciones, esa credencial irá en Key Vault (KNOCK-36) y nunca en el repositorio.

## 4. Apps Android e iOS (TC-03)
- Identificador **decidido por el Tech Lead**: `app.knock.mobile` para el paquete Android y el bundle de iOS. Pendiente: registrar ambas apps en la consola (aún no se ha hecho).
- `google-services.json` y `GoogleService-Info.plist` se guardan fuera del repositorio; `.gitignore` ya los ignora.
- Más adelante, `flutterfire configure --project=knock-0` se ejecuta dentro del proyecto Flutter (`app/`, TT-MOB-01) y reutiliza las apps registradas. El archivo `firebase_options.dart` que genera contiene identificadores del proyecto que no son secretos, pero un escáner como gitleaks puede marcarlos: se decidirá entonces si se versiona.
- Las huellas SHA-1 y SHA-256 de Android hacen falta para el inicio por teléfono en una app real; se añaden cuando exista la app.

## 5. Costo y límites (TC-04)
- Plan Spark: «Sin costo». Con números de prueba el costo es 0 porque no se envía ningún SMS.
- No se verificó la cuota de SMS reales en el plan gratuito ni el precio de Phone Auth si se pasa a un plan de pago. Se confirma antes de la demo (riesgo 1 de `docs/DECISION_TECNICA.md`).

## 6. Decisiones y riesgos
- **Este proyecto es de desarrollo.** Los números de prueba no deben existir en producción: para un lanzamiento real se crea otro proyecto (por ejemplo `knock-prod`) sin ellos.
- Los códigos de los números de prueba son credenciales de desarrollo: no van al repositorio ni a tickets.
- Riesgo: costo y límites de SMS reales sin verificar.
- Riesgo: el inicio por teléfono de Android puede requerir Play Integrity y las huellas SHA, que dependen de la app.

## 7. Pendiente
- Registrar las apps Android e iOS con `app.knock.mobile` (TC-03).
- Probar el inicio de sesión de punta a punta con un número de prueba (TC-01).
- Confirmar la cuota y el costo de SMS reales (TC-04).
