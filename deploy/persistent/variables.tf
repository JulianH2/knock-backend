variable "location" {
  description = "Región del clúster. El nivel gratuito de DocumentDB solo existe en estas tres entre las cinco que permite la política de la suscripción."
  type        = string
  default     = "westus"

  validation {
    condition     = contains(["westus", "francecentral", "norwayeast"], var.location)
    error_message = "location debe ser westus, francecentral o norwayeast: es donde existe el clúster gratuito de DocumentDB. Con otra región el plan pasaría y el apply fallaría tarde."
  }
}

variable "resource_group_name" {
  description = "Grupo de recursos que ya existe (TT-AZ-01). Terraform lo lee, no lo crea ni lo borra."
  type        = string
  default     = "rg-knock-dev"
}

variable "cluster_name" {
  description = "Nombre del clúster que ya existe y que esta raíz adopta."
  type        = string
  default     = "knock-docdb"
}

variable "subscription_id" {
  description = "ID de la suscripción, solo para armar el id del bloque import. Se pasa con TF_VAR_subscription_id; no se escribe en archivos."
  type        = string
}

variable "administrator_username" {
  description = "Usuario administrador del clúster existente. Terraform lo ignora (ignore_changes): solo hace falta para que la configuración sea válida. Se pasa con TF_VAR_administrator_username."
  type        = string
}

variable "administrator_password" {
  description = "Contraseña del administrador. Terraform la ignora (ignore_changes) y no puede leerla de Azure: cualquier valor sirve para el plan. Se pasa con TF_VAR_administrator_password; nunca en archivos."
  type        = string
  sensitive   = true
}

variable "tags" {
  type = map(string)
  default = {
    proyecto    = "knock"
    entorno     = "dev"
    responsable = "julian"
  }
}
