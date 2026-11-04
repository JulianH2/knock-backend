terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

# La suscripción llega por ARM_SUBSCRIPTION_ID; nunca se escribe aquí.
provider "azurerm" {
  features {}
}
