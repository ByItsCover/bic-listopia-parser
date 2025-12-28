aws_region  = "us-east-2"
aws_profile = "dev"

tfe_organization      = "ByItsCover"
tfe_workspace_name    = "By Its Cover Dev"
tfe_workspace_project = "by-its-cover-dev"

rds_cluster_id      = "covercluster"
rds_database_name   = "coverdb"
rds_master_username = "tester"
rds_master_password = "testpass"
rds_scaling_config = {
  max_capacity             = 1.0
  min_capacity             = 0.0
  seconds_until_auto_pause = 300
}
