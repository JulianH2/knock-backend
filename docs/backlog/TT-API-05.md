---
id: "TT-API-05"
tipo: "tarea técnica"
jira: "KNOCK-42"
titulo: "Archivos: Blob privado, enlaces de 5 minutos, compresión y quitar metadatos de ubicación (EXIF)"
epica: "EP-13"
sprint: "Sprint 1 (19/10 – 23/10/2026)"
responsable: "Ángel Alexis Blas Castillo"
puntos: "3"
fuente: "TT-API-05_ArchivosBlobPrivadoEnlaces_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-API-05_ArchivosBlobPrivadoEnlaces_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-42); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-API-05 — Archivos: Blob privado, enlaces de 5 minutos, compresión y quitar metadatos de ubicación (EXIF)

## 1. Datos generales

- **ID de la tarea:** TT-API-05
- **Puntos:** 3
- **Título:** Archivos: Blob privado, enlaces de 5 minutos, compresión y quitar metadatos de ubicación (EXIF)
- **Área:** API · Backend base
- **Épica:** EP-13
- **Responsable:** Ángel Alexis Blas Castillo
- **Sprint:** Sprint 1 (19/10 – 23/10/2026)
- **Día sugerido:** Lunes 19 – martes 20
- **Clave de Jira:** KNOCK-42
- **Rama:** chore/KNOCK-42-archivos-blob-sas  →  PR a sprint-01
- **Depende de:** TT-AZ-02
- **Terminada cuando:** Un enlace vencido falla; las fotos no tienen EXIF.

## 2. Objetivo

Tener un servicio único para subir y ver fotos de forma privada, que usan el perfil (HU-002), las solicitudes y los trabajos.

## 3. Alcance

- Subida directa a Blob con enlace temporal de escritura.
- Lectura con enlaces de 5 minutos.
- Validación de tipo y tamaño, y eliminación de EXIF.

## 4. Fuera de alcance

- Fotos de verificación (tienen su contenedor y cifrado propios en HU-021).

## 5. Pasos

1. Endpoint POST /api/v1/archivos/subidas que valida tipo (JPG/PNG) y tamaño (≤ 8 MB) y devuelve un enlace SAS de escritura de 5 minutos.
1. Confirmación POST /api/v1/archivos/{id}/confirmar que verifica el archivo, quita EXIF y genera la versión reducida.
1. Enlaces de lectura de 5 minutos generados solo para quien tiene permiso sobre el recurso.
1. Azurite en docker-compose para desarrollo y pruebas.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Azure.Storage.Blobs | Blob y SAS |
| SixLabors.ImageSharp (revisar licencia) o SkiaSharp | Reducir y quitar EXIF |
| Azurite | Blob local |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Un enlace vencido falla | Prueba de integración |
| V2 | Las fotos guardadas no tienen EXIF | Prueba con una foto con GPS |
| V3 | Archivo de 9 MB o PDF → 422 | Prueba negativa |

## 8. Seguridad y reglas

- Contenedores privados; nunca enlaces públicos.
- Se valida el contenido real, no solo la extensión.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida de pruebas.

## 11. Referencias

- SEGURIDAD_Y_PRIVACIDAD.md §2 y §5.
