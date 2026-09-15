resource "aws_cloudwatch_log_group" "function" {
  for_each = local.funcoes

  name              = "/aws/lambda/${each.value.nome}"
  retention_in_days = var.log_retention_days
}

resource "aws_iam_role" "lambda" {
  name               = "wrench-auth-lambda-${var.environment}"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume_role.json
}

resource "aws_iam_role_policy" "lambda_logs" {
  name   = "logs"
  role   = aws_iam_role.lambda.id
  policy = data.aws_iam_policy_document.lambda_logs.json
}

resource "aws_lambda_function" "function" {
  for_each = local.funcoes

  function_name    = each.value.nome
  role             = aws_iam_role.lambda.arn
  handler          = each.value.handler
  runtime          = var.runtime
  architectures    = ["x86_64"]
  filename         = local.pacote
  source_code_hash = filebase64sha256(local.pacote)
  memory_size      = var.lambda_memory_size
  timeout          = var.lambda_timeout

  environment {
    variables = each.value.variaveis
  }

  logging_config {
    log_format            = "JSON"
    application_log_level = "INFO"
    system_log_level      = "WARN"
    log_group             = aws_cloudwatch_log_group.function[each.key].name
  }

  depends_on = [aws_iam_role_policy.lambda_logs]
}
