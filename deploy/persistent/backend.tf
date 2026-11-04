# Estado remoto de la raíz persistente (clúster de DocumentDB, que el destroy principal no toca) (TT-AZ-05, KNOCK-158). Solo nombres: ninguna llave, SAS ni cadena de conexión.
# El nombre de la cuenta de almacenamiento lleva un sufijo que se define al crearla, así que se pasa al iniciar:
#   terraform -chdir=deploy/persistent init -backend-config="storage_account_name=<cuenta>"
# La suscripción y el tenant llegan por ARM_SUBSCRIPTION_ID y ARM_TENANT_ID, nunca en este archivo.
terraform {
  backend "azurerm" {
    resource_group_name = "rg-knock-tfstate"
    container_name      = "tfstate"
    key                 = "dev-persistent.tfstate"
    use_azuread_auth    = true
  }
}
