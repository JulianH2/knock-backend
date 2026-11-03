# Una aplicación de Container Apps con su identidad administrada y solo el rol de lectura de secretos en la bóveda.
resource "azurerm_user_assigned_identity" "this" {
  name                = "id-${var.name}"
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
  name                         = var.name
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.environment_id
  revision_mode                = "Single"
  tags                         = var.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.this.id]
  }

  # Cada secreto se lee de Key Vault con la identidad de la aplicación; el valor nunca pasa por Terraform.
  dynamic "secret" {
    for_each = toset(values(var.secret_env))
    content {
      name                = secret.value
      identity            = azurerm_user_assigned_identity.this.id
      key_vault_secret_id = "${var.key_vault_uri}secrets/${secret.value}"
    }
  }

  # Réplicas fijas (min = max) y sin reglas de escalado.
  template {
    min_replicas = var.replicas
    max_replicas = var.replicas

    container {
      name   = "app"
      image  = var.image
      cpu    = var.cpu
      memory = var.memory

      dynamic "env" {
        for_each = var.env
        content {
          name  = env.key
          value = env.value
        }
      }

      dynamic "env" {
        for_each = var.secret_env
        content {
          name        = env.key
          secret_name = env.value
        }
      }

      dynamic "startup_probe" {
        for_each = var.probe_path == null ? [] : [var.probe_path]
        content {
          transport               = "HTTP"
          path                    = startup_probe.value
          port                    = var.target_port
          interval_seconds        = 5
          timeout                 = 3
          failure_count_threshold = 24
        }
      }

      dynamic "liveness_probe" {
        for_each = var.probe_path == null ? [] : [var.probe_path]
        content {
          transport               = "HTTP"
          path                    = liveness_probe.value
          port                    = var.target_port
          interval_seconds        = 30
          timeout                 = 3
          failure_count_threshold = 3
        }
      }
    }
  }

  ingress {
    external_enabled           = var.external_ingress
    target_port                = var.target_port
    allow_insecure_connections = false
    transport                  = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  # El rol de lectura debe existir antes de que la aplicación intente leer los secretos.
  depends_on = [azurerm_role_assignment.secrets_user]
}
