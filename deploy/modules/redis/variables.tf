variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "environment_id" {
  type = string
}

variable "key_vault_id" {
  type = string
}

variable "key_vault_uri" {
  type = string
}

variable "image" {
  type = string
}

variable "tags" {
  type = map(string)
}
