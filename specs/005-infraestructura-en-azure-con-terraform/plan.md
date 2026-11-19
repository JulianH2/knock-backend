# Plan 005 — Infraestructura en Azure con Terraform

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Crear la estructura `deploy/` con el módulo compartido y un módulo por servicio.
2. Configurar el almacenamiento del estado.
3. Configurar la identidad federada del pipeline y las identidades administradas.
4. Escribir los cuatro procesos manuales de GitHub.
5. Importar el clúster gratuito de DocumentDB.

## Archivos que se crean o cambian
- `deploy/modules/shared/` y `deploy/modules/<servicio>/`
- `deploy/main.tf`, `variables.tf` y `backend.tf`
- `.github/workflows/infra-*.yml`
- `deploy/README.md`

## Dependencias
- Terraform
- Proveedor de Azure para Terraform
- Suscripción de Azure for Students y su política de regiones

## Decisiones de la spec que este plan sigue
- **Azure:** el backend es .NET; la suscripción de estudiante da crédito sin tarjeta; Container Apps incluye uso mensual gratuito; y en la misma nube están la IA, los secretos, el almacenamiento y la monitorización.
- **Terraform:** es repetible, se revisa como código y permite levantar y tumbar todo en minutos, como se establece en los requerimientos.
- **Pasos manuales:** nada se crea ni se borra por accidente.
- **Una sola región para el backend:** replicarlo multiplicaría el costo; la CDN reparte las imágenes públicas.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
