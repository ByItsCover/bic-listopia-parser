locals {
  ecs_execution_role_arn = data.terraform_remote_state.bic_infra.outputs.ecs_execution_role_arn
  hardcover_secret_arn   = data.terraform_remote_state.bic_infra.outputs.hardcover_secret_arn
  cover_dump_name        = data.terraform_remote_state.bic_infra.outputs.s3_cover_dump_name
}


resource "aws_batch_job_definition" "job" {
  name = "listopia_parser_batch_job_definition"
  type = "container"
  container_properties = jsonencode({
    image = data.aws_ecr_image.server_image.image_uri

    executionRoleArn = local.ecs_execution_role_arn

    resourceRequirements = [
      {
        type  = "VCPU"
        value = tostring(var.batch_vcpu)
      },
      {
        type  = "MEMORY"
        value = tostring(var.batch_memory)
      }
    ]

    environment = [
      {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.dotnet_env
      },
      {
        name  = "AWS_REGION"
        value = var.aws_region
      },
      {
        name  = "ListopiaOptions__BucketName"
        value = local.cover_dump_name
      }
    ]

    secrets = [
      {
        name      = "HardcoverOptions__Token"
        valueFrom = local.hardcover_secret_arn
      }
    ]
  })

  timeout {
    attempt_duration_seconds = var.max_duration
  }
}
