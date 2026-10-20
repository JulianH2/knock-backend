# ADR-004 — Firebase (Auth y FCM) con el backend en Azure

> **Estado:** decidida por el Tech Lead el 2026-10-20; pendiente de revisión en el PR que la incorpora.
> **Complementa:** las secciones 7, 8 y 10 de `docs/DECISION_TECNICA.md`.
> **Tarea:** TT-GES-06 · Impacto de implementación: TT-FB-02, TT-CI-03.

## Contexto
Los lineamientos piden desplegar el backend en una sola nube (punto 15). Knock usa **Firebase Authentication** (inicio de sesión por teléfono) y **Firebase Cloud Messaging** (notificaciones push), que son servicios de Google, mientras que el backend, la base y la infraestructura están en Azure.

## Decisión
Se mantiene Firebase para dos cosas y nada más:
- **Auth por teléfono:** la app obtiene un token de Firebase; el servicio de Identidad lo valida con las claves públicas de Google y emite la sesión propia (JWT, TT-API-02). **El backend no depende de dónde corre Firebase.**
- **FCM:** es la única vía de push en Android, sea cual sea la nube. El servicio de Notificaciones envía por la API HTTP v1 con una cuenta de servicio.

No se usa Firestore, Cloud Functions ni otros servicios de Firebase.

## Por qué
- FCM es inevitable para push en Android.
- Auth se consume como cliente de identidad, no aloja ninguna función de Knock; la API completa sigue alojada en Azure.
- Reemplazar Auth por un SMS propio en Azure cuesta más tiempo y más dinero que mantener Firebase.

## Plan Blaze y controles de gasto
El inicio de sesión por teléfono con **SMS reales** requiere el plan **Blaze** (con tarjeta vinculada); el plan gratuito no lo incluye **(confirmar en la consola de Firebase)**. Las cuotas gratuitas siguen siendo las mismas y solo se paga al excederlas. Controles obligatorios (TT-FB-02):
1. **Política de regiones de SMS:** solo México (+52) ([documentación de Google](https://docs.cloud.google.com/identity-platform/docs/admin/sms-regions)).
2. **Presupuesto y alertas** en la facturación de Google Cloud (avisan, **no frenan**).
3. **App Check** para reducir el abuso desde clientes falsos **(confirmar su efecto sobre el inicio por teléfono)**.
4. **Números de prueba** de Firebase solo para el CI y las pruebas; los códigos no se escriben en el repositorio, en Jira ni en PR.
5. Revisar el uso de SMS cada semana durante la prueba con testers.

## Riesgo aceptado
El docente podría objetar el uso de servicios de Google con el backend en Azure. La justificación queda escrita aquí; el cambio de proveedor de Auth es posible (la validación del token está aislada en el servicio de Identidad).
