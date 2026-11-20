# Módulo compartido (spec 005, R-02): red, entorno de Container Apps, Key Vault, Blob Storage y registros.
# No crea servicios: eso lo hace `deploy/apps` (CA-01).

data "azurerm_resource_group" "this" {
  name = var.resource_group_name
}

data "azurerm_client_config" "current" {}

locals {
  prefix = "knock-${var.environment}"
  # Los nombres de Key Vault, Storage y ACR son únicos en todo Azure: se les agrega el sufijo del equipo.
  compact = replace("knock${var.environment}${var.name_suffix}", "-", "")
  tags = merge(var.tags, {
    proyecto = "knock"
    entorno  = var.environment
  })
}

# ---- Registros y monitorización ----

resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-${local.prefix}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.this.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  daily_quota_gb      = var.log_daily_quota_gb
  tags                = local.tags
}

resource "azurerm_application_insights" "this" {
  name                = "appi-${local.prefix}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.this.name
  workspace_id        = azurerm_log_analytics_workspace.this.id
  application_type    = "web"
  tags                = local.tags
}

# Grupo de acciones que recibe las alertas (spec 011, R-05). El correo viene de una variable sin valor por
# defecto real: si llega vacío, el grupo se crea sin receptores (la alerta se ve en el portal, pero nadie recibe
# correos). La dirección no vive en el repositorio; quien la pasa en el -var la deja solo en el estado remoto.
resource "azurerm_monitor_action_group" "this" {
  name                = "ag-${local.prefix}"
  resource_group_name = data.azurerm_resource_group.this.name
  short_name          = "knock-alert"

  dynamic "email_receiver" {
    for_each = var.alert_email_receiver == "" ? [] : [var.alert_email_receiver]
    content {
      name                    = "errores"
      email_address           = email_receiver.value
      use_common_alert_schema = true
    }
  }

  tags = local.tags
}

# Alerta de pico de errores (spec 011, R-05 y CA-03): peticiones fallidas del Application Insights
# (requests/failed) por encima de var.alert_failed_requests_threshold en una ventana de 5 minutos, evaluada
# cada minuto, severidad 2 (aviso), enlazada al grupo de acciones. La agregación Count cuenta las peticiones
# fallidas de la ventana. Costo aproximado: 0.10 USD al mes por regla de alerta de métricas; los correos que
# envía el grupo de acciones no se cobran.
resource "azurerm_monitor_metric_alert" "failed_requests" {
  name                = "alert-failed-requests-${local.prefix}"
  resource_group_name = data.azurerm_resource_group.this.name
  scopes              = [azurerm_application_insights.this.id]
  description         = "Pico de peticiones fallidas en 5 minutos (spec 011, CA-03)."
  severity            = 2
  frequency           = "PT1M"
  window_size         = "PT5M"

  criteria {
    metric_namespace = "Microsoft.Insights/components"
    metric_name      = "requests/failed"
    aggregation      = "Count"
    operator         = "GreaterThan"
    threshold        = var.alert_failed_requests_threshold
  }

  action {
    action_group_id = azurerm_monitor_action_group.this.id
  }

  tags = local.tags
}

# ---- Red ----

resource "azurerm_virtual_network" "this" {
  name                = "vnet-${local.prefix}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.this.name
  address_space       = [var.vnet_cidr]
  tags                = local.tags
}

resource "azurerm_subnet" "container_apps" {
  #checkov:skip=CKV2_AZURE_31: EXC-014 Container Apps administra su propio tráfico; un NSG a mano puede romper el entorno
  name                 = "snet-container-apps"
  resource_group_name  = data.azurerm_resource_group.this.name
  virtual_network_name = azurerm_virtual_network.this.name
  # Un entorno de Container Apps solo de consumo necesita al menos un /23.
  address_prefixes = [var.container_apps_subnet_cidr]

  delegation {
    name = "container-apps"
    service_delegation {
      name    = "Microsoft.App/environments"
      actions = ["Microsoft.Network/virtualNetworks/subnets/join/action"]
    }
  }
}

resource "azurerm_container_app_environment" "this" {
  name                       = "cae-${local.prefix}"
  location                   = var.location
  resource_group_name        = data.azurerm_resource_group.this.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id
  infrastructure_subnet_id   = azurerm_subnet.container_apps.id
  # Solo el gateway tiene ingreso externo; los demás servicios usan ingreso interno (spec 001, R-02).
  internal_load_balancer_enabled = false
  tags                           = local.tags

  # Azure devuelve este perfil en los entornos existentes; se declara para que el plan no intente quitarlo (KNOCK-34, deploy/README.md).
  workload_profile {
    name                  = "Consumption"
    workload_profile_type = "Consumption"
    minimum_count         = 0
    maximum_count         = 0
  }
}

# ---- Imágenes ----

resource "azurerm_container_registry" "this" {
  #checkov:skip=CKV_AZURE_233: EXC-011 requieren ACR Premium; el crédito de estudiante no lo cubre (ADR-006). El escaneo de imágenes lo hace Trivy en el CI
  #checkov:skip=CKV_AZURE_237: EXC-011 requieren ACR Premium; el crédito de estudiante no lo cubre (ADR-006). El escaneo de imágenes lo hace Trivy en el CI
  #checkov:skip=CKV_AZURE_167: EXC-011 requieren ACR Premium; el crédito de estudiante no lo cubre (ADR-006). El escaneo de imágenes lo hace Trivy en el CI
  #checkov:skip=CKV_AZURE_166: EXC-011 requieren ACR Premium; el crédito de estudiante no lo cubre (ADR-006). El escaneo de imágenes lo hace Trivy en el CI
  #checkov:skip=CKV_AZURE_165: EXC-011 requieren ACR Premium; el crédito de estudiante no lo cubre (ADR-006). El escaneo de imágenes lo hace Trivy en el CI
  #checkov:skip=CKV_AZURE_164: EXC-011 requieren ACR Premium; el crédito de estudiante no lo cubre (ADR-006). El escaneo de imágenes lo hace Trivy en el CI
  #checkov:skip=CKV_AZURE_163: EXC-011 requieren ACR Premium; el crédito de estudiante no lo cubre (ADR-006). El escaneo de imágenes lo hace Trivy en el CI
  #checkov:skip=CKV_AZURE_139: EXC-011 requieren ACR Premium; el crédito de estudiante no lo cubre (ADR-006). El escaneo de imágenes lo hace Trivy en el CI
  name                = "acr${local.compact}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.this.name
  sku                 = "Basic"
  admin_enabled       = false # cada servicio hace pull con su identidad administrada
  tags                = local.tags
}

# ---- Secretos ----

resource "azurerm_key_vault" "this" {
  #checkov:skip=CKV_AZURE_109: EXC-012 Container Apps no es un servicio de confianza del firewall y los endpoints privados cuestan; sin purge protection para poder destruir y recrear (ADR-006, punto 6)
  #checkov:skip=CKV_AZURE_189: EXC-012 Container Apps no es un servicio de confianza del firewall y los endpoints privados cuestan; sin purge protection para poder destruir y recrear (ADR-006, punto 6)
  #checkov:skip=CKV_AZURE_110: EXC-012 Container Apps no es un servicio de confianza del firewall y los endpoints privados cuestan; sin purge protection para poder destruir y recrear (ADR-006, punto 6)
  #checkov:skip=CKV_AZURE_42: EXC-012 Container Apps no es un servicio de confianza del firewall y los endpoints privados cuestan; sin purge protection para poder destruir y recrear (ADR-006, punto 6)
  #checkov:skip=CKV2_AZURE_32: EXC-012 Container Apps no es un servicio de confianza del firewall y los endpoints privados cuestan; sin purge protection para poder destruir y recrear (ADR-006, punto 6)
  name                       = "kv-${local.compact}"
  location                   = var.location
  resource_group_name        = data.azurerm_resource_group.this.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  soft_delete_retention_days = 7
  # Se puede destruir y volver a crear en el mismo ciclo de evaluación (ADR-006, punto 6). Ver EXC-012.
  purge_protection_enabled = false
  tags                     = local.tags
}

# Quién puede escribir los valores de los secretos (los valores nunca pasan por Terraform: CA-04).
resource "azurerm_role_assignment" "secret_admins" {
  for_each             = toset(var.secret_admin_object_ids)
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = each.value
}

# ---- Archivos (spec 013) y respaldos (spec 003, R-08) ----

resource "azurerm_storage_account" "files" {
  #checkov:skip=CKV_AZURE_206: EXC-013 replicación geográfica, endpoints privados y llaves administradas cuestan; sin cola y sin acceso público a blobs
  #checkov:skip=CKV_AZURE_33: EXC-013 replicación geográfica, endpoints privados y llaves administradas cuestan; sin cola y sin acceso público a blobs
  #checkov:skip=CKV2_AZURE_21: EXC-013 replicación geográfica, endpoints privados y llaves administradas cuestan; sin cola y sin acceso público a blobs
  #checkov:skip=CKV_AZURE_59: EXC-013 replicación geográfica, endpoints privados y llaves administradas cuestan; sin cola y sin acceso público a blobs
  #checkov:skip=CKV2_AZURE_33: EXC-013 replicación geográfica, endpoints privados y llaves administradas cuestan; sin cola y sin acceso público a blobs
  #checkov:skip=CKV2_AZURE_1: EXC-013 replicación geográfica, endpoints privados y llaves administradas cuestan; sin cola y sin acceso público a blobs
  name                            = "st${local.compact}"
  location                        = var.location
  resource_group_name             = data.azurerm_resource_group.this.name
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  account_kind                    = "StorageV2"
  min_tls_version                 = "TLS1_2"
  https_traffic_only_enabled      = true
  allow_nested_items_to_be_public = false
  # Sin llaves compartidas: todo acceso es con Entra ID y permisos mínimos (CKV2_AZURE_40).
  shared_access_key_enabled       = false
  default_to_oauth_authentication = true
  tags                            = local.tags

  blob_properties {
    delete_retention_policy {
      days = 7
    }
    container_delete_retention_policy {
      days = 7
    }
  }

  sas_policy {
    expiration_period = "01.00:00:00"
  }
}

resource "azurerm_storage_container" "private" {
  #checkov:skip=CKV2_AZURE_21: EXC-013 el registro de lecturas necesita un diagnóstico con costo; los contenedores son privados
  # `imagenes-cuarentena` (spec 032, R-01): bytes tal como los subió la app; nunca en la cuenta pública.
  for_each              = toset(["imagenes", "respaldos", "imagenes-cuarentena"])
  name                  = each.value
  storage_account_id    = azurerm_storage_account.files.id
  container_access_type = "private"
}

# Una regla de ciclo de vida borra todo blob de `imagenes-cuarentena` con más de un día (spec 032, R-19 y CA-23); la regla corre una vez al día.
resource "azurerm_storage_management_policy" "quarantine" {
  storage_account_id = azurerm_storage_account.files.id

  rule {
    name    = "borrar-cuarentena-de-imagenes"
    enabled = true

    filters {
      blob_types   = ["blockBlob"]
      prefix_match = ["imagenes-cuarentena/"]
    }

    actions {
      base_blob {
        delete_after_days_since_creation_greater_than = 1
      }
    }
  }
}

# ---- Imágenes públicas (spec 032, ADR-007 opción D: sin recurso de CDN) ----
# Cuenta aparte de la privada: solo recibe imágenes ya procesadas y moderadas, con lectura anónima por blob (sin listado).
# No hay ningún recurso de CDN de Azure ni de Front Door (CA-23); la dirección base es configurable para poner una CDN después sin cambiar código.

resource "azurerm_storage_account" "public_images" {
  #checkov:skip=CKV2_AZURE_47: EXC-029: la lectura anónima por blob es el diseño de la spec 032 (ADR-007 opción D); solo imágenes ya procesadas y moderadas, sin listado
  #checkov:skip=CKV_AZURE_190: EXC-029: la lectura anónima por blob es el diseño de la spec 032 (ADR-007)
  #checkov:skip=CKV_AZURE_206: EXC-029 replicación geográfica, endpoints privados y llaves administradas cuestan; solo contiene imágenes públicas ya procesadas
  #checkov:skip=CKV_AZURE_33: EXC-029 replicación geográfica, endpoints privados y llaves administradas cuestan; solo contiene imágenes públicas ya procesadas
  #checkov:skip=CKV2_AZURE_21: EXC-029 replicación geográfica, endpoints privados y llaves administradas cuestan; solo contiene imágenes públicas ya procesadas
  #checkov:skip=CKV_AZURE_59: EXC-029 replicación geográfica, endpoints privados y llaves administradas cuestan; solo contiene imágenes públicas ya procesadas
  #checkov:skip=CKV2_AZURE_33: EXC-029 replicación geográfica, endpoints privados y llaves administradas cuestan; solo contiene imágenes públicas ya procesadas
  #checkov:skip=CKV2_AZURE_1: EXC-029 replicación geográfica, endpoints privados y llaves administradas cuestan; solo contiene imágenes públicas ya procesadas
  name                            = "stpub${local.compact}"
  location                        = var.location
  resource_group_name             = data.azurerm_resource_group.this.name
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  account_kind                    = "StorageV2"
  min_tls_version                 = "TLS1_2"
  https_traffic_only_enabled      = true
  allow_nested_items_to_be_public = true # lectura anónima a nivel de cuenta; el contenedor la limita a «blob»
  # Sin llaves compartidas: las SAS son de delegación de usuario con la identidad administrada (spec 032, R-02).
  shared_access_key_enabled       = false
  default_to_oauth_authentication = true
  tags                            = local.tags

  # Sin bloque cors_rule: ninguna regla de CORS (R-02).
  blob_properties {
    delete_retention_policy {
      days = 7
    }
    container_delete_retention_policy {
      days = 7
    }
  }

  sas_policy {
    expiration_period = "01.00:00:00"
  }
}

resource "azurerm_storage_container" "public_images" {
  #checkov:skip=CKV2_AZURE_21: EXC-029 el registro de lecturas necesita un diagnóstico con costo
  #checkov:skip=CKV_AZURE_34: EXC-029: el acceso anónimo de nivel blob es el diseño de la spec 032 (ADR-007); se lee un archivo si se conoce su nombre, no se lista
  name                  = "imagenes-publicas"
  storage_account_id    = azurerm_storage_account.public_images.id
  container_access_type = "blob"
}
