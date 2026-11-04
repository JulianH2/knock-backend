variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "environment_id" {
  type = string
}

variable "key_vault_id" {
  type = string
}

variable "key_vault_uri" {
  description = "URI de la bóveda; solo hace falta si secret_env no está vacío."
  type        = string
  default     = ""
}

variable "image" {
  type = string
}

variable "target_port" {
  type = number
}

variable "external_ingress" {
  description = "true solo para la API pública y el gateway."
  type        = bool
}

variable "replicas" {
  description = "Réplicas fijas (mínimo = máximo)."
  type        = number

  validation {
    condition     = var.replicas >= 1
    error_message = "replicas debe ser al menos 1: ninguna aplicación se escala a 0."
  }
}

variable "cpu" {
  type    = number
  default = 0.25
}

variable "memory" {
  type    = string
  default = "0.5Gi"
}

variable "probe_path" {
  description = "Ruta de las sondas de arranque y de vida (HTTP). null: sin sondas."
  type        = string
  default     = null
}

variable "env" {
  description = "Variables de entorno sin secretos (nombre => valor)."
  type        = map(string)
  default     = {}
}

variable "secret_env" {
  description = "Variables de entorno cuyo valor es un secreto de Key Vault (variable de entorno => nombre del secreto). Terraform solo referencia el secreto; no conoce su valor."
  type        = map(string)
  default     = {}
}

variable "tags" {
  type = map(string)
}
