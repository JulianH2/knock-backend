# Tareas 032 — CDN de imágenes: entrega con caché y enlaces firmados

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> **Paralelo:** T-01, T-02 y T-03 no dependen entre sí y pueden tomarlas tres personas a la vez. T-04 bloquea a T-06 y T-07. T-08 a T-10 cierran.

| Tarea | Qué se hace | Cubre | Depende de | ¿En paralelo? | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Terraform: cuenta pública con `imagenes-publicas` (acceso por blob), `imagenes-cuarentena` en la privada con regla de 1 día, sin llave compartida, sin CORS, TLS 1.2, roles de la identidad de Mercado y salidas | R-01, R-02, R-03, R-19 · CA-07, CA-08, CA-23 | — | Sí (con T-02 y T-03) | José Julián | hecha (PR parte 1; `fmt` y `validate` en verde, `plan` y Checkov quedan para el CI y T-10) |
| T-02 | `ProcesadorImagen`: cabecera, límites de píxeles y tiempo, orientación, borrado de metadatos, reducción, recodificación a JPEG y pruebas unitarias con imágenes generadas | R-07, R-08, R-09 · CA-05, CA-09, CA-10 | — | Sí (con T-01 y T-03) | Guillermo | pendiente |
| T-03 | Script de `db/` con `subidas_imagen` y `accesos_privados`, índices y TTL; `ImagenesOptions` validado al arrancar y `ImagenPublicaUrl` con sus pruebas | R-03, R-13, R-19 · CA-19, CA-24 | — | Sí (con T-01 y T-02) | Alexis | hecha (PR parte 1: opciones, dirección pública y script `db/044`; sin conectar al arranque, ver `Memoria.md`) |
| T-04 | `AlmacenBlob` sobre Azurite: llave de delegación de usuario, SAS `cw` de un blob, propiedades, descarga, publicación con encabezados y borrado | R-02, R-05, R-11 · CA-01, CA-03, CA-04, CA-06 | T-03 | No (bloquea T-06 y T-07) | José Julián | pendiente |
| T-05 | Límites en Redis (`img:subidas:{sub}`, `img:confirmaciones:{sub}`) y conteo de pendientes vigentes | R-14 · CA-18 | T-03 | Sí (con T-04) | Erick | pendiente |
| T-06 | `SubidaImagenService` y `ImagenesController`: pedir enlace, confirmar, moderación con falla cerrada, idempotencia, códigos de error, `no-store` sin compresión y 503 de almacenamiento | R-04 a R-12, R-15, R-17, R-18 · CA-02, CA-11 a CA-17, CA-21, CA-22 | T-02, T-04, T-05 | No | Guillermo | pendiente |
| T-07 | `IEnlacesPrivados` con motivos cerrados, SAS de lectura de 5 minutos y registro en `accesos_privados` | R-16 · CA-20 | T-04 | Sí (con T-06) | Alexis | pendiente |
| T-08 | Ruta del gateway, OpenAPI regenerado, `docs/endpoints.md`, pruebas de contrato y de arquitectura | R-04, R-06 · CA-01, CA-02 | T-06 | Sí (con T-09) | Erick | pendiente |
| T-09 | Pruebas de seguridad negativa e integración de punta a punta (dos hosts, Azurite, moderador falso) y medición de rendimiento acotada | CA-03, CA-04, CA-08 a CA-11, CA-15, CA-18, CA-22 | T-06, T-07 | Sí (con T-08) | Guillermo | pendiente |
| T-10 | Humo en Azure (subida real, lectura anónima pública, privada rechazada), excepción del contenedor público registrada por una persona y `Memoria.md` | R-01 a R-03 · CA-06 a CA-08, CA-23 | T-01, T-08, T-09 | No (cierre) | José Julián | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas o comandos en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
