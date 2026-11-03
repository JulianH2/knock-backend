# TT-AZ-02 — Infraestructura como código (Terraform): Container Apps por servicio, Azure DocumentDB (MongoDB), Redis, Blob Storage y Key Vault

> **Jira:** KNOCK-34 · **Área:** Azure · **Épica:** EP-13 · **Responsable:** José Julián Hernández Hernández · **Sprint:** S2 · **Puntos:** 3 · **Prioridad:** Técnica
> **Repositorio:** knock-backend · **Ficha:** TT-AZ-02_InfraestructuraComoCodigoContainer_Knock.docx (Jira KNOCK-34) · **Rama:** `chore/KNOCK-34-tt-az-02` → PR a `sprint-02`
> **Depende de:** TT-AZ-01 (KNOCK-27, firmada) y TT-AZ-05 (KNOCK-158, estado remoto: sin él esta tarea no puede aplicar). **Habilita:** TT-AZ-06 (KNOCK-180), TT-CD-01 (KNOCK-35), TT-AZ-03 (KNOCK-165) y TT-AZ-07 (KNOCK-184).
> **Fuentes:** ADR-001 (servicios y regla 6) · ADR-002 (DocumentDB y Redis) · ADR-003 (reglas 1 a 5) · ADR-006 (costos, regiones y ventana de evaluación) · constitución 2.2.x (§3 «Nube», §4 «Datos», §6 «Secretos», §12 y §13) · `docs/PLAN_ADAPTACION.md` · `docs/evidencia/KNOCK-27-azure.md`
> **Skills:** story-workflow, security-core, testing-strategy-backend
> **Estado:** Borrador

## 1. Objetivo
Que el entorno de Azure del proyecto se cree desde cero con Terraform y no a mano: un entorno de Container Apps con una aplicación por servicio (Identidad y confianza, Mensajería y notificaciones, Mercado) y el gateway, Redis como contenedor propio, Key Vault, Blob Storage y, en una raíz aparte, el clúster gratuito de DocumentDB que el destroy principal no toca (constitución §Datos).

**Terminada cuando** (ficha): el entorno se crea desde cero con un comando. En este repositorio son dos raíces, así que el criterio se cumple con un comando por raíz (`deploy/persistent/` y `deploy/`), documentados en el README (duda 1).

## 2. Historia y casos de prueba
**TT-AZ-02** — Tarea técnica (no es historia de usuario). Los casos 01 a 03 y 09 a 11 se verifican con comandos locales y el CI; los 04 a 08 y 12 con el `plan` y el `apply` de Terraform, que ejecuta una persona (los agentes no operan Azure).

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-AZ-02-01 | Dadas las raíces `deploy/` y `deploy/persistent/`, cuando se ejecuta `terraform fmt -check -recursive` y `terraform init -backend=false && terraform validate` en cada una, entonces terminan sin errores y sin credenciales de Azure. | Salida de los comandos (CI y local) |
| TC-TT-AZ-02-02 | Dada la separación de raíces, entonces el clúster de DocumentDB está solo en `deploy/persistent/` y ningún archivo de `deploy/` lo declara, de modo que `terraform destroy` en `deploy/` no lo toca. | `terraform plan -destroy` en `deploy/` sin el clúster y revisión de los archivos; la ejecuta una persona |
| TC-TT-AZ-02-03 | Dada la variable `location`, cuando se pide una región distinta de las cinco permitidas por la política de la suscripción (`chilecentral`, `norwayeast`, `westus`, `francecentral`, `northcentralus`), entonces `terraform plan` falla con un mensaje de validación y no propone ningún recurso. | `terraform plan -var location=eastus` falla (comando) |
| TC-TT-AZ-02-04 | Dado el `plan` de `deploy/`, entonces crea un entorno de Container Apps y una aplicación por cada servicio de negocio y el gateway; solo el gateway tiene ingreso externo, los demás ingreso interno, y todos con `allowInsecure = false`. | Salida del `plan` y `az containerapp list` (lectura); la ejecuta una persona |
| TC-TT-AZ-02-05 | Dadas las variables de réplicas por aplicación, cuando se aplica fuera de la ventana de evaluación, entonces cada aplicación corre con 1 réplica (0 solo en un servicio que no ejecuta procesos con tiempo) y, con la variable de la ventana activada, con `minReplicas = maxReplicas = 2` y sin reglas de escalado. | `terraform plan` con y sin la variable de la ventana |
| TC-TT-AZ-02-06 | Dado Redis, entonces es una aplicación de Container Apps de 1 réplica con ingreso TCP interno, sin persistencia, con la imagen fijada por digest y la contraseña tomada de un secreto de Key Vault, nunca escrita en los archivos. | Salida del `plan` y revisión de los archivos |
| TC-TT-AZ-02-07 | Dado Key Vault, entonces usa autorización por roles, cada aplicación tiene su identidad administrada con solo el rol de lectura de secretos, y ningún valor de secreto está en los archivos de Terraform ni en `*.tfvars` versionados. | `plan`, `az role assignment list` (lectura) y `gitleaks` limpio |
| TC-TT-AZ-02-08 | Dado el Blob Storage, entonces exige TLS 1.2, no permite acceso público a blobs ni llaves compartidas, y los contenedores son privados (las fotos de verificación nunca pasan por la CDN). | `az storage account show` (lectura) |
| TC-TT-AZ-02-09 | Dados todos los recursos, entonces llevan las etiquetas `proyecto`, `entorno` y `responsable`, igual que el grupo de recursos de TT-AZ-01. | Salida del `plan` |
| TC-TT-AZ-02-10 | Dado el repositorio, entonces no hay `*.tfstate`, `*.tfvars` con valores ni credenciales versionados, las salidas de Terraform no exponen secretos y `gitleaks` queda limpio. | `git ls-files` filtrado y `gitleaks` en el CI |
| TC-TT-AZ-02-11 | Dados los archivos `.tf`, cuando corre Checkov en el CI, entonces termina sin hallazgos sin registrar: cada supresión cita un `EXC-NNN` vigente de `security/exceptions.yaml`. | `ci-pr.yml` (cambio protegido, lo hace el Tech Lead) y `check_exceptions.py` |
| TC-TT-AZ-02-12 | Dado un grupo de recursos vacío, cuando el Tech Lead ejecuta `terraform apply` en `deploy/persistent/` y luego en `deploy/`, entonces el entorno queda creado, las salidas listan solo nombres y direcciones, y un segundo `plan` no propone cambios. | Capturas del `apply` y de un segundo `plan` sin cambios, sin identificadores sensibles; la ejecuta una persona |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `deploy/` (raíz principal): entorno de Container Apps con su espacio de trabajo de Log Analytics, una aplicación por servicio y el gateway, Redis, Key Vault, Blob Storage, identidades administradas y roles mínimos. Un módulo por servicio y uno compartido (ADR-003).
  - `deploy/persistent/` (raíz aparte): el clúster gratuito de Azure DocumentDB.
  - Variables: región, etiquetas, réplicas por aplicación y la bandera de la ventana de evaluación (constitución §12). Sin valores reales ni secretos en el repositorio.
  - `deploy/README.md` con los comandos (`init`, `plan`, `apply`, `destroy`) por raíz y cómo verificar cada caso.
  - Evidencia en `docs/evidencia/KNOCK-34-terraform.md`, con el ID de suscripción, el tenant y los correos ocultos.
  - Cambio de Checkov de Bicep a Terraform en `ci-pr.yml` (archivo protegido: lo hace o autoriza el Tech Lead).
- **Out:**
  - El estado remoto (TT-AZ-05) y la identidad federada de GitHub hacia Azure (TT-CD-01, ADR-003 regla 5).
  - El despliegue de las imágenes y el smoke test (TT-CD-01) y el `terraform destroy` desde el pipeline (TT-AZ-03).
  - Las redes conocidas de `ForwardedHeaders` (TT-AZ-06, KNOCK-180), Redis administrado, Front Door con WAF y la CDN de imágenes (decisión de costos aparte, ADR-006).
  - Application Insights y las alertas (TT-OBS-01 los crea a mano; esta tarea solo el espacio de trabajo de Log Analytics que Container Apps necesita).
  - El código de los servicios y el gateway (TT-ARQ-01 y TT-ARQ-02).

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin colecciones; el clúster se crea vacío)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Servicios internos expuestos a internet | Solo el gateway con ingreso externo; los demás internos; `allowInsecure = false` | TC-TT-AZ-02-04 |
| Secretos en el repositorio público (contraseña de Redis, cadenas de conexión) | Key Vault con identidades administradas; ningún valor en `.tf` ni `*.tfvars`; `gitleaks` | TC-TT-AZ-02-06, 07, 10 |
| Acceso excesivo a los secretos | Rol de lectura de secretos por identidad, sin roles de administración | TC-TT-AZ-02-07 |
| Despliegue en una región no permitida o con costos fuera de la capa gratuita | Validación de `location`; réplicas por variable; clúster gratuito en raíz aparte | TC-TT-AZ-02-03, 05 |
| Pérdida de los datos al destruir el entorno | El clúster está en `deploy/persistent/`, fuera del destroy principal | TC-TT-AZ-02-02 |
| Fotos privadas expuestas por una configuración de Blob | Sin acceso público ni llaves compartidas; contenedores privados | TC-TT-AZ-02-08 |
| Recursos olvidados que gastan crédito | Etiquetas, alertas de TT-AZ-01 y réplicas por variable | TC-TT-AZ-02-05, 09 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es infraestructura sin código de producto. Cada caso se verifica con el comando o la evidencia indicada. Checkov y `gitleaks` corren en el CI.
**Gates:** `AGENTS.md §5` en lo que aplique: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `terraform fmt` y `validate` en OK, `Memoria.md` al día y «Uso de IA» completo en el PR.
**Límites y dudas abiertas** (recomendación entre paréntesis; las decide el Tech Lead):
1. **Dos raíces y «un comando».** (Recomendación: un comando por raíz, documentado en el README; no se agrega un script que los encadene.)
2. **Registro de imágenes.** Azure Container Registry no tiene capa gratuita (ADR-006) y no se despliega sin decisión. (Recomendación: GitHub Container Registry; los repositorios y las imágenes son públicos, sin secretos dentro.)
3. **Imagen inicial.** Antes de TT-CD-01 no hay imágenes de los servicios. (Recomendación: una imagen pública de ejemplo como marcador, reemplazada por TT-CD-01.)
4. **Región.** DocumentDB gratuito solo en `westus`, `francecentral` y `norwayeast` entre las cinco permitidas (ADR-006; candidata: West US); el grupo de recursos está en `northcentralus`. (Recomendación: Container Apps y DocumentDB en `westus`, para no añadir latencia; el grupo de recursos se queda donde está.)
5. **Servicios sin código todavía.** (Recomendación: crear ya las cuatro aplicaciones con el marcador, para que TT-ARQ-01 y TT-CD-01 solo cambien la imagen.)
6. **Key Vault.** (Recomendación: autorización por roles, borrado temporal activo y sin protección contra purga, para poder recrearlo con el mismo nombre tras un destroy en este proyecto escolar.)
7. **Application Insights.** (Recomendación: esta tarea crea solo Log Analytics; Application Insights y las alertas siguen manuales en TT-OBS-01.)
8. **Réplica de 0 en un servicio.** Qué servicio ejecuta los procesos con tiempo lo fija TT-ARQ-01. (Recomendación: todos con mínimo 1 hasta entonces.)
9. **Costo.** Con 4 aplicaciones y Redis, el consumo cuenta contra el crédito de estudiante; se estima con tarifas citadas al ejecutar el `plan` (TT-AZ-04, `docs/evidencia/KNOCK-145-costo-cero.md`).
10. Un agente no opera Azure: el Tech Lead ejecuta `terraform plan` y `apply` y comparte la evidencia sin identificadores.

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
