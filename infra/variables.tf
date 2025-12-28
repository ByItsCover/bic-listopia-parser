variable "rds_cluster_id" {
  type        = string
  description = "AWS RDS cluster identifier"
}

variable "rds_database_name" {
  type        = string
  description = "AWS RDS database name"
}

variable "rds_master_username" {
  type        = string
  description = "AWS RDS master user username"
}

variable "rds_master_password" {
  type        = string
  description = "AWS RDS master user password"
}

variable "rds_scaling_config" {
  type        = map(any)
  description = "AWS RDS scaling configuration"
  default = {
    max_capacity             = 1.0
    min_capacity             = 0.0
    seconds_until_auto_pause = 300
  }
}

variable "aws_region" {
  type        = string
  description = "AWS Region"
}

variable "aws_profile" {
  type        = string
  description = "AWS profile name"
}

variable "tfe_organization" {
  type        = string
  description = "Terraform organization name"
}

variable "tfe_workspace_name" {
  type        = string
  description = "Terraform workspace name"
}

variable "tfe_workspace_project" {
  type        = string
  description = "Terraform workspace project name"
}
