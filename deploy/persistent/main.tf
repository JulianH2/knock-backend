data "azurerm_resource_group" "this" {
  name = var.resource_group_name
}

# Hallazgo (2026-11-03): el clúster gratuito YA EXISTE (creado a mano, nivel Free, West US, 32 GB, versión 8.0) y la suscripción
# solo permite un clúster gratuito. Esta raíz NO lo crea: lo adopta con un bloque import y lo declara con los mismos valores para
# que el primer plan no proponga recrearlo ni destruirlo. Difiere de la spec TT-AZ-02 (decisión 4), que lo daba por creado aquí.
import {
  to = azurerm_mongo_cluster.this
  id = "/subscriptions/${var.subscription_id}/resourceGroups/${var.resource_group_name}/providers/Microsoft.DocumentDB/mongoClusters/${var.cluster_name}"
}

resource "azurerm_mongo_cluster" "this" {
  name                   = var.cluster_name
  resource_group_name    = data.azurerm_resource_group.this.name
  location               = var.location
  administrator_username = var.administrator_username
  administrator_password = var.administrator_password
  shard_count            = 1
  compute_tier           = "Free"
  high_availability_mode = "Disabled"
  storage_size_in_gb     = 32
  version                = "8.0"
  tags                   = var.tags

  lifecycle {
    # Los datos no se pierden por un destroy ni por un cambio que fuerce el reemplazo.
    prevent_destroy = true
    # No se pueden leer de Azure: Terraform no debe intentar cambiarlos.
    ignore_changes = [administrator_username, administrator_password]
  }
}
