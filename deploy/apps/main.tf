# Pasos 2 y 3 «desplegar» y «eliminar lo desplegado» (spec 005): los servicios viven en su propio estado para
# poder borrarlos sin tocar la infraestructura compartida ni la base de datos.
#
# Los secretos (nombres fijados aquí, spec 005 §5) deben existir en Key Vault antes de aplicar; sus valores los
# escribe una persona con `az keyvault secret set` y nunca pasan por Terraform ni por el repositorio (CA-04).

data "terraform_remote_state" "infra" {
  backend = "azurerm"
  config = {
    resource_group_name  = "rg-knock-tfstate"
    storage_account_name = "stknocktfstate"
    container_name       = "tfstate"
    key                  = "infra.tfstate"
    use_azuread_auth     = true
    subscription_id      = var.subscription_id
    use_oidc             = var.use_oidc
  }
}

locals {
  infra = data.terraform_remote_state.infra.outputs
  tags  = { proyecto = "knock", entorno = var.environment }

  image = { for s in ["gateway", "identity", "messaging", "market"] :
    s => "${local.infra.registry_login_server}/knock-${s}:${var.image_tag}"
  }

  # Dirección interna de cada servicio dentro del entorno: la Container App se resuelve por su nombre.
  cluster_address = { for s in ["identity", "messaging", "market"] : s => "http://knock-${s}/" }

  # `redis` es la contraseña del servidor Redis; `redis_cs` es la cadena que leen los servicios
  # (ConnectionStrings:KnockRedis, p. ej. «knock-redis:6379,password=...»; spec 003, R-02).
  kv           = { redis = "redis-password", redis_cs = "redis-connection-string" }
  redis_secret = { ConnectionStrings__KnockRedis = local.kv.redis_cs }

  # Red de confianza para X-Forwarded-For (gateway, Identity, Market y Messaging; spec 009 R-04 y spec 006 R-03).
  forwarded = { ForwardedHeaders__KnownNetworks__0 = var.container_apps_subnet_cidr }

  services = {
    gateway = {
      external = true
      env = merge(local.forwarded, {
        ReverseProxy__Clusters__identidad__Destinations__principal__Address  = local.cluster_address.identity
        ReverseProxy__Clusters__mercado__Destinations__principal__Address    = local.cluster_address.market
        ReverseProxy__Clusters__mensajeria__Destinations__principal__Address = local.cluster_address.messaging
      })
      secret_env = merge(local.redis_secret, { Jwt__SigningKey = "jwt-signing-key" })
    }
    identity = {
      external = false
      # Identity lee la sección Identity:PhoneProtection (PhoneProtectionOptions): llaves numeradas y su versión vigente.
      # Firebase:ProjectId ya trae su valor en appsettings.json (knock-0).
      env = merge(local.forwarded, {
        Identity__PhoneProtection__EncryptionKeyVersion = "1"
        Identity__PhoneProtection__HmacKeyVersion       = "1"
        # Dirección interna de Mercado: Identity valida la colonia del perfil con GET /api/v1/colonias/existe (spec 017, R-06). Sin esto, PUT /me con colonia da 503.
        Services__Market__BaseUrl = local.cluster_address.market
      })
      secret_env = merge(local.redis_secret, {
        ConnectionStrings__KnockMongo                = "mongo-identity"
        Jwt__SigningKey                              = "jwt-signing-key"
        Identity__PhoneProtection__EncryptionKeys__1 = "pii-encryption-key"
        Identity__PhoneProtection__HmacKeys__1       = "pii-hmac-key"
      })
    }
    # BUG-004 (KNOCK-241): Mensajería valida el JWT de Knock y lee la lista de sesiones canceladas en Redis; sin estos dos secretos no arranca.
    messaging = {
      external = false
      env      = local.forwarded
      secret_env = merge(local.redis_secret, {
        ConnectionStrings__KnockMongo = "mongo-messaging"
        Jwt__SigningKey               = "jwt-signing-key"
      })
    }
    market = {
      external = false
      env      = local.forwarded
      # Market usa Redis para la caché del catálogo (spec 010, R-01/R-02): el módulo Catalog registra el cliente al arrancar.
      secret_env = merge(local.redis_secret, {
        ConnectionStrings__KnockMongo = "mongo-market"
        Jwt__SigningKey               = "jwt-signing-key"
      })
    }
  }
}

module "redis" {
  source = "../modules/service"

  name                = "knock-redis"
  location            = var.location
  resource_group_name = var.resource_group_name
  environment_id      = local.infra.container_app_environment_id
  key_vault_id        = local.infra.key_vault_id
  key_vault_uri       = local.infra.key_vault_uri
  image               = var.redis_image
  secret_env          = { REDIS_PASSWORD = local.kv.redis }
  ingress_transport   = "tcp"
  target_port         = 6379
  health_path         = null
  ready_path          = null
  min_replicas        = 1
  max_replicas        = 1
  command             = ["sh", "-c"]
  # Sin persistencia (spec 003, R-07): si Redis se reinicia, se pierde la lista de sesiones canceladas por `sid`
  # y un token cerrado vale hasta 15 minutos. Riesgo aceptado (constitución §12).
  args = ["exec redis-server --requirepass \"$REDIS_PASSWORD\" --save '' --appendonly no"]
  tags = local.tags
}

module "service" {
  source   = "../modules/service"
  for_each = local.services

  name                           = "knock-${each.key}"
  location                       = var.location
  resource_group_name            = var.resource_group_name
  environment_id                 = local.infra.container_app_environment_id
  registry_id                    = local.infra.registry_id
  registry_login_server          = local.infra.registry_login_server
  key_vault_id                   = local.infra.key_vault_id
  key_vault_uri                  = local.infra.key_vault_uri
  image                          = local.image[each.key]
  ingress_external               = each.value.external
  env                            = each.value.env
  secret_env                     = each.value.secret_env
  min_replicas                   = var.replicas[each.key].min
  max_replicas                   = var.replicas[each.key].max
  enable_app_insights            = true
  app_insights_connection_string = local.infra.application_insights_connection_string
  tags                           = local.tags

  # Con la sonda de disponibilidad, un servicio que usa Redis no pasa a «listo» hasta que Redis responde: Redis primero.
  depends_on = [module.redis]
}
