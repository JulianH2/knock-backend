data "azurerm_client_config" "current" {}

# Sufijo aleatorio corto: la bóveda tiene protección contra purga y su nombre queda bloqueado tras un destroy,
# así que cada creación usa un nombre nuevo. Tampoco es un secreto.
resource "random_string" "suffix" {
  length  = 4
  lower   = true
  upper   = false
  numeric = true
  special = false
}

resource "azurerm_log_analytics_workspace" "this" {
  name                = "law-knock-dev"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_container_app_environment" "this" {
  name                       = "cae-knock-dev"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id
  tags                       = var.tags
}

# Key Vault por roles (sin directivas de acceso), con borrado temporal y protección contra purga.
resource "azurerm_key_vault" "this" {
  name                       = "kv-knock-dev-${random_string.suffix.result}"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  purge_protection_enabled   = true
  soft_delete_retention_days = 7
  tags                       = var.tags
}

# La persona que carga los secretos tiene escritura solo en la bóveda, no en la suscripción.
resource "azurerm_role_assignment" "secrets_admin" {
  count                = var.secrets_admin_object_id == null ? 0 : 1
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = var.secrets_admin_object_id
}

# Cuenta de Blob de la aplicación. Conserva las llaves compartidas mientras Media firme los SAS con la clave compartida
# (spec TT-AZ-02, duda 11); se desactivan cuando exista la tarea de código con Azure.Identity.
resource "azurerm_storage_account" "app" {
  name                            = "stknockapp${random_string.suffix.result}"
  location                        = var.location
  resource_group_name             = var.resource_group_name
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  account_kind                    = "StorageV2"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = true
  tags                            = var.tags
}

# Contenedores privados. Las fotos de verificación nunca pasan por la CDN.
resource "azurerm_storage_container" "fotos_publicas" {
  name                  = "fotos-publicas"
  storage_account_id    = azurerm_storage_account.app.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "verificacion" {
  name                  = "verificacion"
  storage_account_id    = azurerm_storage_account.app.id
  container_access_type = "private"
}
