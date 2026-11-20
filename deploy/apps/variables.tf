variable "subscription_id" {
  type = string
}

variable "use_oidc" {
  type    = bool
  default = false
}

variable "environment" {
  type    = string
  default = "dev"
}

variable "location" {
  type    = string
  default = "westus"
}

variable "resource_group_name" {
  type = string
}

variable "image_tag" {
  description = "Etiqueta de las imágenes en el ACR (la publica el pipeline, spec 007). Usa el SHA del commit, no `latest`."
  type        = string
}

variable "panel_origin" {
  description = "Origen exacto del panel interno para CORS (spec 033, R-27): `https://host[:puerto]` sin ruta ni barra final. Vacío = ningún origen permitido. No es un secreto."
  type        = string
  default     = ""
}

variable "replicas" {
  description = <<-EOT
    Réplicas por servicio (ADR-006, spec 006 R-01 y R-06). Por defecto una réplica por servicio, sin autoescalado, para cuidar
    el costo (R-06). Para la ventana de evaluación (25-nov al 5-dic de 2026) se suben a min = max = 2 con el tfvars de ese
    entorno o con `-var 'replicas={gateway={min=2,max=2},identity={min=2,max=2},messaging={min=2,max=2},market={min=2,max=2}}'`.
    Redis siempre 1 (se fija en main.tf).
  EOT
  type = map(object({
    min = number
    max = number
  }))
  default = {
    gateway   = { min = 1, max = 1 }
    identity  = { min = 1, max = 1 }
    messaging = { min = 1, max = 1 }
    market    = { min = 1, max = 1 }
  }

  validation {
    condition     = alltrue([for k, v in var.replicas : v.min >= 0 && v.max >= v.min && v.max <= 10])
    error_message = "Cada servicio necesita 0 <= min <= max <= 10."
  }
}

variable "redis_image" {
  description = "Imagen de Redis (spec 003, R-07): una réplica, sin persistencia. 7.2 es la última con licencia BSD-3; no subir a 7.4 o posterior (RSAL/SSPL)."
  type        = string
  default     = "docker.io/library/redis:7.2-alpine@sha256:2c933526722b4126a61d5c170f766dc159adf97682f0b1cbbc27b1666ba96d84"
}

variable "container_apps_subnet_cidr" {
  description = <<-EOT
    Subred del entorno de Container Apps (la misma que `container_apps_subnet_cidr` del módulo compartido). Los servicios
    la declaran como red de confianza para leer `X-Forwarded-For`: sin ella, todos los clientes llegarían con la IP del
    ingreso y compartirían un solo cubo del límite de peticiones por IP. Nunca pongas 0.0.0.0/0: permitiría suplantar la IP.
  EOT
  type        = string
  default     = "10.20.0.0/23"
}
