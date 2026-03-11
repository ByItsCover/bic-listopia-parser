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

variable "batch_vcpu" {
  type        = number
  description = "VCPU count for batch job"
  default     = 1
}

variable "batch_memory" {
  type        = number
  description = "Memory size for batch job"
  default     = 512
}
