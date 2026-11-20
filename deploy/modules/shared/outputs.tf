output "container_app_environment_id" {
  value = azurerm_container_app_environment.this.id
}

output "container_app_environment_domain" {
  value = azurerm_container_app_environment.this.default_domain
}

output "registry_id" {
  value = azurerm_container_registry.this.id
}

output "registry_login_server" {
  value = azurerm_container_registry.this.login_server
}

output "key_vault_id" {
  value = azurerm_key_vault.this.id
}

output "key_vault_uri" {
  value = azurerm_key_vault.this.vault_uri
}

output "storage_account_name" {
  value = azurerm_storage_account.files.name
}

output "storage_account_id" {
  value = azurerm_storage_account.files.id
}

output "quarantine_container_id" {
  value = azurerm_storage_container.private["imagenes-cuarentena"].id
}

output "public_images_account_name" {
  value = azurerm_storage_account.public_images.name
}

output "public_images_account_id" {
  value = azurerm_storage_account.public_images.id
}

output "public_images_container_id" {
  value = azurerm_storage_container.public_images.id
}

# Dirección base de las imágenes públicas (spec 032, R-03): el módulo de apps la pasa como Imagenes__BaseUrlPublica.
# Es la `primary_blob_endpoint` sin barra final más el contenedor; si algún día hay una CDN delante, solo cambia esta salida.
output "public_images_base_url" {
  value = "${trimsuffix(azurerm_storage_account.public_images.primary_blob_endpoint, "/")}/${azurerm_storage_container.public_images.name}"
}

output "application_insights_connection_string" {
  value     = azurerm_application_insights.this.connection_string
  sensitive = true
}
