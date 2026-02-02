resource "aws_secretsmanager_secret" "api_key" {
  name = "hardcover-api-key"
}

resource "aws_secretsmanager_secret_version" "api_key" {
  secret_id     = aws_secretsmanager_secret.api_key.id
  secret_string = var.hardcover_api_key
}
