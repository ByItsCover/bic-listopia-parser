aws_region = "us-east-2"

bic_infra_workspace = "bic-infra-prod"

# Batch

dotnet_env                = "Production"
max_duration              = 1800
batch_vcpu                = 2
batch_memory              = 2048
hot_cover_parse_frequency = "cron(15 12 * * ? *)" # Every day at 8:15 AM EST
