terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

# La suscripción llega por ARM_SUBSCRIPTION_ID; nunca se escribe aquí.
provider "azurerm" {
  features {
    key_vault {
      # Con protección contra purga el destroy deja la bóveda borrada de forma temporal; el siguiente apply usa otro sufijo.
      purge_soft_delete_on_destroy    = false
      recover_soft_deleted_key_vaults = false
    }
  }
}
