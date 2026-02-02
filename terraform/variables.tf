# General AWS

variable "aws_region" {
  type        = string
  description = "AWS Region"
}

# Terraform Cloud

variable "tfe_org_name" {
  type        = string
  description = "Terraform Cloud organization name"
  default     = "ByItsCover"
}

variable "bic_infra_workspace" {
  type        = string
  description = "Terraform Cloud Workspace BIC-Infra name"
}

# Batch

variable "dotnet_env" {
  type        = string
  description = "The ASPNETCORE_ENVIRONMENT for the AWS batch container"
}

variable "hardcover_api_key" {
  type        = string
  description = "The Hardcover API Key"
  sensitive   = true
}

variable "max_duration" {
  type        = number
  description = "Maximum duration for batch task, after which will be terminated"
  default     = 3600
}

# RDS

variable "rds_host_port" {
  type        = number
  description = "Port of the RDS database host"
  default     = 5432
}

variable "rds_database_name" {
  type        = string
  description = "RDS database name"
}

variable "rds_timeout" {
  type        = number
  description = "Timeout for RDS database connection establishment"
  default     = 30
}
