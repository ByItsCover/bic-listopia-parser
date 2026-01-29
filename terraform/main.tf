locals {
  ecr_repo = data.terraform_remote_state.bic_infra.outputs.listopia_parser_ecr_name
  batch_role_arn = data.terraform_remote_state.bic_infra.outputs.batch_service_role_arn
  ecs_role_arn = data.terraform_remote_state.bic_infra.outputs.ecs_instance_role_arn
}


data "aws_ecr_image" "server_image" {
  repository_name = local.ecr_repo
  image_tag       = "latest"
}
