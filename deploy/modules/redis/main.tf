# Redis como contenedor propio: 1 réplica, ingreso TCP interno y sin persistencia. La contraseña la carga una persona en
# Key Vault (secreto "redis-password") y Terraform solo la referencia por su dirección: el valor nunca pasa por el estado.
locals {
  password_secret_name = "redis-password"
}

resource "azurerm_user_assigned_identity" "this" {
  name                = "id-ca-knock-redis"
  location            = var.location
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

resource "azurerm_role_assignment" "secrets_user" {
  scope                = var.key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.this.principal_id
}

resource "azurerm_container_app" "this" {
  name                         = "ca-knock-redis"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.environment_id
  revision_mode                = "Single"
  tags                         = var.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.this.id]
  }

  secret {
    name                = local.password_secret_name
    identity            = azurerm_user_assigned_identity.this.id
    key_vault_secret_id = "${var.key_vault_uri}secrets/${local.password_secret_name}"
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name    = "redis"
      image   = var.image
      cpu     = 0.25
      memory  = "0.5Gi"
      command = ["/bin/sh", "-c"]
      args    = ["exec redis-server --requirepass \"$REDIS_PASSWORD\" --save '' --appendonly no"]

      env {
        name        = "REDIS_PASSWORD"
        secret_name = local.password_secret_name
      }
    }
  }

  ingress {
    external_enabled = false
    target_port      = 6379
    exposed_port     = 6379
    transport        = "tcp"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  depends_on = [azurerm_role_assignment.secrets_user]
}
