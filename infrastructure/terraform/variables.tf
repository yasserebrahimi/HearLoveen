variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "hearloveen-prod-rg"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "westeurope"
}

variable "project_name" {
  description = "Project name"
  type        = string
  default     = "hearloveen"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "production"
}

variable "db_admin_username" {
  description = "Database admin username"
  type        = string
  sensitive   = true
}

variable "db_admin_password" {
  description = "Database admin password"
  type        = string
  sensitive   = true
}
