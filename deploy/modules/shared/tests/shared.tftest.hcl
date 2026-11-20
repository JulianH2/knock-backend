# Spec 005, R-06 y CA-01: la región está limitada a las cinco que permite la suscripción y el módulo compartido
# no crea ningún servicio. Se ejecuta con `terraform test` sin tocar Azure (proveedor simulado).

mock_provider "azurerm" {
  mock_data "azurerm_resource_group" {
    defaults = {
      name     = "rg-prueba"
      location = "northcentralus"
    }
  }
  mock_data "azurerm_client_config" {
    defaults = {
      tenant_id = "00000000-0000-0000-0000-000000000000"
    }
  }
}

variables {
  environment         = "dev"
  name_suffix         = "ab12"
  resource_group_name = "rg-prueba"
}

run "region_permitida_CA_01" {
  command = plan
  variables {
    location = "westus"
  }

  assert {
    condition     = azurerm_container_registry.this.admin_enabled == false
    error_message = "El registro no debe tener usuario administrador: cada servicio usa su identidad."
  }

  assert {
    condition     = azurerm_key_vault.this.rbac_authorization_enabled
    error_message = "Key Vault debe usar RBAC, no políticas de acceso."
  }
}

run "region_fuera_de_la_politica_R_06" {
  command = plan
  variables {
    location = "eastus"
  }
  expect_failures = [var.location]
}

run "sufijo_invalido" {
  command = plan
  variables {
    location    = "westus"
    name_suffix = "NO VALE"
  }
  expect_failures = [var.name_suffix]
}

# Spec 032, CA-23 (R-01, R-02, R-19): dos cuentas, la privada sin lectura anónima, las dos sin llave compartida y con TLS 1.2,
# el contenedor público con acceso por blob y la regla de un día en `imagenes-cuarentena`. Sin ningún recurso de CDN.
run "imagenes_publicas_CA_23" {
  command = plan
  variables {
    location = "westus"
  }

  assert {
    condition     = azurerm_storage_account.files.allow_nested_items_to_be_public == false
    error_message = "La cuenta privada no debe permitir lectura anónima."
  }

  assert {
    condition     = azurerm_storage_account.public_images.allow_nested_items_to_be_public
    error_message = "La cuenta pública necesita la lectura anónima a nivel de cuenta para que el contenedor la limite a blob."
  }

  assert {
    condition     = azurerm_storage_account.files.shared_access_key_enabled == false && azurerm_storage_account.public_images.shared_access_key_enabled == false
    error_message = "Las dos cuentas deben tener la llave compartida deshabilitada."
  }

  assert {
    condition     = azurerm_storage_account.files.min_tls_version == "TLS1_2" && azurerm_storage_account.public_images.min_tls_version == "TLS1_2"
    error_message = "Las dos cuentas deben exigir TLS 1.2."
  }

  assert {
    condition     = azurerm_storage_account.files.https_traffic_only_enabled && azurerm_storage_account.public_images.https_traffic_only_enabled
    error_message = "Las dos cuentas deben aceptar solo HTTPS."
  }

  assert {
    condition     = azurerm_storage_account.files.name != azurerm_storage_account.public_images.name
    error_message = "Debe haber dos cuentas distintas."
  }

  assert {
    condition     = azurerm_storage_container.public_images.name == "imagenes-publicas" && azurerm_storage_container.public_images.container_access_type == "blob"
    error_message = "El contenedor público es `imagenes-publicas` con acceso anónimo de nivel blob (sin listado)."
  }

  assert {
    condition     = azurerm_storage_container.private["imagenes-cuarentena"].container_access_type == "private"
    error_message = "`imagenes-cuarentena` vive en la cuenta privada y es privado."
  }

  assert {
    condition     = one([for r in azurerm_storage_management_policy.quarantine.rule : one([for a in r.actions : one(a.base_blob).delete_after_days_since_creation_greater_than])]) == 1
    error_message = "La cuarentena se borra al día."
  }

  assert {
    condition     = length(azurerm_storage_account.public_images.blob_properties[0].cors_rule) == 0 && length(azurerm_storage_account.files.blob_properties[0].cors_rule) == 0
    error_message = "Ninguna de las dos cuentas lleva reglas de CORS."
  }
}
