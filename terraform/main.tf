locals {
  ecr_repo              = data.terraform_remote_state.bic_infra.outputs.listopia_parser_ecr_name
  batch_role_arn        = data.terraform_remote_state.bic_infra.outputs.batch_service_role_arn
  ecs_instance_role_arn = data.terraform_remote_state.bic_infra.outputs.ecs_instance_role_arn
  batch_sg_id           = data.terraform_remote_state.bic_infra.outputs.batch_sg_id

  rds_connection_str = join("", [
    "Host=${data.terraform_remote_state.bic_infra.outputs.db_endpoint};",
    "Port=${var.rds_host_port};",
    "Database=${var.rds_database_name};",
    "Username=${data.terraform_remote_state.bic_infra.outputs.db_master_username};",
    "Password=${data.terraform_remote_state.bic_infra.outputs.db_master_password}"
  ])
}


data "aws_ssm_parameter" "image_id" {
  name = "/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-arm64"
}

resource "aws_launch_template" "batch_launch_template" {

  block_device_mappings {
    device_name = "/dev/xvda"

    ebs {
      volume_size = 50
    }
  }

  image_id = data.aws_ssm_parameter.image_id.value
}

resource "aws_batch_compute_environment" "spot" {
  name_prefix = "spot-fleet-"

  compute_resources {
    allocation_strategy = "SPOT_CAPACITY_OPTIMIZED"

    instance_role = local.ecs_instance_role_arn
    instance_type = [
      "optimal"
    ]


    max_vcpus     = 64
    min_vcpus     = 0
    desired_vcpus = 0

    security_group_ids = [
      local.batch_sg_id
    ]

    subnets = data.aws_subnets.subnet.ids

    type = "SPOT"

    launch_template {
      launch_template_id = aws_launch_template.batch_launch_template.id
      version            = aws_launch_template.batch_launch_template.latest_version
    }
  }

  service_role = local.batch_role_arn
  type         = "MANAGED"
}

resource "aws_batch_job_queue" "queue" {
  name     = "queue"
  state    = "ENABLED"
  priority = "1"

  compute_environment_order {
    order               = 1
    compute_environment = aws_batch_compute_environment.spot.arn
  }

  lifecycle {
    replace_triggered_by = [
      aws_batch_compute_environment.spot.id
    ]
  }

}

data "aws_ecr_image" "server_image" {
  repository_name = local.ecr_repo
  image_tag       = "latest"
}

resource "aws_batch_job_definition" "job" {
  name = "listopia_parser_batch_job_definition"
  type = "container"
  container_properties = jsonencode({
    image = data.aws_ecr_image.server_image.image_uri

    resourceRequirements = [
      {
        type  = "VCPU"
        value = "1"
      },
      {
        type  = "MEMORY"
        value = "2048"
      }
    ]

    environment = [
      {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      },
      {
        name  = "PGVECTOR_CONN"
        value = local.rds_connection_str
      }
    ]
  })
}
