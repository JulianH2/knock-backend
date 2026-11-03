# Solo nombres: la cadena de conexión no se expone (va a Key Vault, TT-SEC-01).
output "cluster_name" {
  value = azurerm_mongo_cluster.this.name
}

output "cluster_location" {
  value = azurerm_mongo_cluster.this.location
}
