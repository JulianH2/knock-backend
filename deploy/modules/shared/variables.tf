variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "tags" {
  type = map(string)
}

variable "secrets_admin_object_id" {
  type    = string
  default = null
}
