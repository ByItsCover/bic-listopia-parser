resource "aws_rds_cluster" "covercluster" {
  cluster_identifier  = var.rds_cluster_id
  engine              = "aurora-postgresql"
  engine_mode         = "provisioned"
  engine_version      = "17.4"
  database_name       = var.rds_database_name
  master_username     = var.rds_master_username
  master_password     = var.rds_master_password
  storage_encrypted   = true
  skip_final_snapshot = true
  apply_immediately   = true

  serverlessv2_scaling_configuration {
    max_capacity             = var.rds_scaling_config.max_capacity
    min_capacity             = var.rds_scaling_config.min_capacity
    seconds_until_auto_pause = var.rds_scaling_config.seconds_until_auto_pause
  }
}

resource "aws_rds_cluster_instance" "covercluster_instance" {
  cluster_identifier = aws_rds_cluster.covercluster.id
  instance_class     = "db.serverless"
  engine             = aws_rds_cluster.covercluster.engine
  engine_version     = aws_rds_cluster.covercluster.engine_version
}
