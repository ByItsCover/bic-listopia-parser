aws_region = "us-east-2"

bic_infra_workspace = "bic-infra-prod"

# Batch

dotnet_env                = "Production"
max_duration              = 1800
batch_vcpu                = 1
batch_memory              = 1024
hot_cover_parse_frequency = "cron(30 12 * * ? *)" # Every day at 8:30 AM EST
