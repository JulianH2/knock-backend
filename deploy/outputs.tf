# Solo nombres y direcciones: ninguna salida lleva secretos.
output "api_url" {
  description = "URL pública de la API (HTTPS lo termina el ingreso de Container Apps)."
  value       = "https://${module.api.fqdn}"
}

output "gateway_fqdn" {
  description = "Dirección pública del gateway (null si enable_other_services es false)."
  value       = try(module.gateway[0].fqdn, null)
}

output "internal_fqdns" {
  description = "Direcciones internas de los servicios separados (vacías si enable_other_services es false)."
  value = {
    identity  = try(module.identity[0].fqdn, null)
    messaging = try(module.messaging[0].fqdn, null)
    market    = try(module.market[0].fqdn, null)
  }
}

output "redis_fqdn" {
  description = "Dirección interna de Redis (null si enable_redis es false)."
  value       = try(module.redis[0].fqdn, null)
}

output "container_app_environment_name" {
  value = module.shared.environment_name
}

output "key_vault_name" {
  description = "Nombre de la bóveda (lleva un sufijo aleatorio que cambia al recrearla)."
  value       = module.shared.key_vault_name
}

output "key_vault_uri" {
  value = module.shared.key_vault_uri
}

output "storage_account_name" {
  description = "Cuenta de Blob de la aplicación."
  value       = module.shared.storage_account_name
}

output "replicas_per_app" {
  description = "Réplicas fijas de la API y de los demás servicios activos (Redis siempre 1)."
  value       = local.replicas
}
