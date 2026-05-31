<!-- BEGIN_TF_DOCS -->
## Requirements

| Name | Version |
|------|---------|
| <a name="requirement_terraform"></a> [terraform](#requirement\_terraform) | >= 1.2 |
| <a name="requirement_aws"></a> [aws](#requirement\_aws) | ~> 6.0 |

## Providers

| Name | Version |
|------|---------|
| <a name="provider_aws"></a> [aws](#provider\_aws) | 6.47.0 |
| <a name="provider_terraform"></a> [terraform](#provider\_terraform) | n/a |

## Modules

No modules.

## Resources

| Name | Type |
|------|------|
| [aws_batch_compute_environment.spot](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/batch_compute_environment) | resource |
| [aws_batch_job_definition.job](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/batch_job_definition) | resource |
| [aws_batch_job_queue.queue](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/batch_job_queue) | resource |
| [aws_ecs_cluster.spot_cluster](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/ecs_cluster) | resource |
| [aws_launch_template.batch_launch_template](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/launch_template) | resource |
| [aws_ecr_image.server_image](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/data-sources/ecr_image) | data source |
| [aws_ssm_parameter.image_id](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/data-sources/ssm_parameter) | data source |
| [aws_subnets.subnet](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/data-sources/subnets) | data source |
| [aws_vpc.default](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/data-sources/vpc) | data source |
| [terraform_remote_state.bic_infra](https://registry.terraform.io/providers/hashicorp/terraform/latest/docs/data-sources/remote_state) | data source |

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| <a name="input_aws_region"></a> [aws\_region](#input\_aws\_region) | AWS Region | `string` | n/a | yes |
| <a name="input_batch_memory"></a> [batch\_memory](#input\_batch\_memory) | Memory size for batch job | `number` | `512` | no |
| <a name="input_batch_vcpu"></a> [batch\_vcpu](#input\_batch\_vcpu) | VCPU count for batch job | `number` | `1` | no |
| <a name="input_bic_infra_workspace"></a> [bic\_infra\_workspace](#input\_bic\_infra\_workspace) | Terraform Cloud Workspace BIC-Infra name | `string` | n/a | yes |
| <a name="input_dotnet_env"></a> [dotnet\_env](#input\_dotnet\_env) | The ASPNETCORE\_ENVIRONMENT for the AWS batch container | `string` | n/a | yes |
| <a name="input_max_duration"></a> [max\_duration](#input\_max\_duration) | Maximum duration for batch task, after which will be terminated | `number` | `3600` | no |
| <a name="input_tfe_org_name"></a> [tfe\_org\_name](#input\_tfe\_org\_name) | Terraform Cloud organization name | `string` | `"ByItsCover"` | no |

## Outputs

No outputs.
<!-- END_TF_DOCS -->