data "azurerm_resource_group" "this" {
  name = var.resource_group_name
}

# El clúster de DocumentDB NO está aquí: vive en deploy/persistent/ para que el destroy de esta raíz no lo toque (TC-TT-AZ-02-02).

locals {
  replicas = var.evaluation_window ? 2 : 1

  # Variable de entorno => nombre del secreto en Key Vault. Los valores NO están en los archivos: una persona los carga
  # con el comando de deploy/README.md y Container Apps los lee con la identidad de la aplicación.
  api_secret_env = merge(
    {
      ConnectionStrings__KnockMongo = "knock-mongo"
      ConnectionStrings__KnockBlob  = "knock-blob"
      Jwt__SigningKey               = "jwt-signing-key"
      Firebase__ProjectId           = "firebase-project-id"
    },
    var.enable_phone_protection ? {
      Identity__PhoneProtection__EncryptionKeys__1 = "phone-encryption-key-1"
      Identity__PhoneProtection__HmacKeys__1       = "phone-hmac-key-1"
    } : {}
  )

  api_env = var.enable_phone_protection ? {
    Identity__PhoneProtection__EncryptionKeyVersion = "1"
    Identity__PhoneProtection__HmacKeyVersion       = "1"
  } : {}
}

module "shared" {
  source = "./modules/shared"

  resource_group_name     = data.azurerm_resource_group.this.name
  location                = var.location
  tags                    = var.tags
  secrets_admin_object_id = var.secrets_admin_object_id
}

# La API del monolito: ingreso EXTERNO por HTTPS (lo termina el ingreso de Container Apps), /health como sonda.
module "api" {
  source = "./modules/service"

  name                = "ca-knock-api"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = var.location
  environment_id      = module.shared.environment_id
  key_vault_id        = module.shared.key_vault_id
  key_vault_uri       = module.shared.key_vault_uri
  image               = "${var.image_repository}:${var.image_tag}"
  target_port         = 8080
  external_ingress    = true
  replicas            = local.replicas
  cpu                 = 0.5
  memory              = "1Gi"
  probe_path          = "/health"
  env                 = local.api_env
  secret_env          = local.api_secret_env
  tags                = var.tags
}

# Gateway y servicios separados (ADR-001): detrás de enable_other_services para no gastar crédito mientras no tengan imagen.
# Solo el gateway tiene ingreso externo; los demás son internos.
module "gateway" {
  count  = var.enable_other_services ? 1 : 0
  source = "./modules/service"

  name                = "ca-knock-gateway"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = var.location
  environment_id      = module.shared.environment_id
  key_vault_id        = module.shared.key_vault_id
  image               = var.marker_image
  target_port         = 80
  external_ingress    = true
  replicas            = local.replicas
  tags                = var.tags
}

module "identity" {
  count  = var.enable_other_services ? 1 : 0
  source = "./modules/service"

  name                = "ca-knock-identity"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = var.location
  environment_id      = module.shared.environment_id
  key_vault_id        = module.shared.key_vault_id
  image               = var.marker_image
  target_port         = 80
  external_ingress    = false
  replicas            = local.replicas
  tags                = var.tags
}

module "messaging" {
  count  = var.enable_other_services ? 1 : 0
  source = "./modules/service"

  name                = "ca-knock-messaging"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = var.location
  environment_id      = module.shared.environment_id
  key_vault_id        = module.shared.key_vault_id
  image               = var.marker_image
  target_port         = 80
  external_ingress    = false
  replicas            = local.replicas
  tags                = var.tags
}

module "market" {
  count  = var.enable_other_services ? 1 : 0
  source = "./modules/service"

  name                = "ca-knock-market"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = var.location
  environment_id      = module.shared.environment_id
  key_vault_id        = module.shared.key_vault_id
  image               = var.marker_image
  target_port         = 80
  external_ingress    = false
  replicas            = local.replicas
  tags                = var.tags
}

# Redis siempre con 1 réplica: dos réplicas sin persistencia serían dos bases distintas con la lista de revocados partida.
module "redis" {
  count  = var.enable_redis ? 1 : 0
  source = "./modules/redis"

  resource_group_name = data.azurerm_resource_group.this.name
  location            = var.location
  environment_id      = module.shared.environment_id
  key_vault_id        = module.shared.key_vault_id
  key_vault_uri       = module.shared.key_vault_uri
  image               = var.redis_image
  tags                = var.tags
}
