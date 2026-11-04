variable "location" {
  description = "Región de Container Apps, Key Vault, Blob y Log Analytics. Solo las cinco que permite la política de la suscripción."
  type        = string
  default     = "westus"

  validation {
    condition     = contains(["chilecentral", "norwayeast", "westus", "francecentral", "northcentralus"], var.location)
    error_message = "location debe ser una de las regiones permitidas por la política de la suscripción: chilecentral, norwayeast, westus, francecentral o northcentralus."
  }
}

variable "resource_group_name" {
  description = "Grupo de recursos que ya existe (TT-AZ-01). Terraform lo lee, no lo crea ni lo borra."
  type        = string
  default     = "rg-knock-dev"
}

variable "tags" {
  description = "Etiquetas de todos los recursos, iguales a las del grupo de recursos."
  type        = map(string)
  default = {
    proyecto    = "knock"
    entorno     = "dev"
    responsable = "julian"
  }
}

variable "evaluation_window" {
  description = "true durante la evaluación (3 y 4 de nov, 25-nov a 5-dic de 2026 y cualquier remedial): la API (y los demás servicios, si están activos) corren con 2 réplicas fijas. false: 1 réplica. Redis siempre 1."
  type        = bool
  default     = false
}

variable "image_repository" {
  description = "Imagen del monolito en GHCR (src/Knock.Api/Dockerfile, puerto 8080). El paquete debe ser público."
  type        = string
  default     = "ghcr.io/julianh2/knock-api"
}

variable "image_tag" {
  description = "Etiqueta de la imagen de la API publicada en GHCR (por ejemplo el SHA corto del commit). Sin valor por defecto: se elige a propósito."
  type        = string
}

variable "enable_phone_protection" {
  description = "Carga también las llaves del celular (Identity__PhoneProtection__*). La API de sprint-02 aún no las lee (HU-001 está pendiente); activar solo cuando se fusione. Exige cargar sus secretos antes."
  type        = bool
  default     = false
}

variable "enable_other_services" {
  description = "Crea el gateway y los servicios de Identidad, Mensajería y Mercado (hoy con la imagen de ejemplo). false: solo la API del monolito, para no gastar crédito."
  type        = bool
  default     = false
}

variable "enable_redis" {
  description = "Crea Redis. Exige que una persona haya cargado el secreto redis-password en Key Vault. false: Redis no se despliega todavía."
  type        = bool
  default     = false
}

variable "secrets_admin_object_id" {
  description = "Object ID de la persona que carga los secretos en Key Vault (rol Key Vault Secrets Officer, solo en la bóveda). Se pasa con TF_VAR_secrets_admin_object_id; no se escribe en archivos."
  type        = string
  default     = null
}

variable "marker_image" {
  description = "Imagen pública de ejemplo para el gateway y los servicios que aún no tienen imagen propia (TT-CD-01)."
  type        = string
  default     = "mcr.microsoft.com/k8se/quickstart@sha256:3a4d93c34c6753f24765ab17a36f1754aee9b02082b7d9553d17697b9e8252c4"
}

variable "redis_image" {
  description = "Imagen de Redis 7 fijada por digest."
  type        = string
  default     = "docker.io/library/redis@sha256:8da19c7399dca900c6822a1cf30f9d2285585e2c4b457ff4095b0f4500054d3f"
}
