resource "aws_apigatewayv2_api" "gateway" {
  name          = local.nome_gateway
  protocol_type = "HTTP"
  description   = "Ponto de entrada do Wrench Auto Repair (${var.environment}): autenticação por CPF e rotas da API protegidas por Lambda authorizer."
}

resource "aws_cloudwatch_log_group" "gateway" {
  name              = "/aws/apigateway/${local.nome_gateway}"
  retention_in_days = var.log_retention_days
}

resource "aws_apigatewayv2_stage" "default" {
  api_id      = aws_apigatewayv2_api.gateway.id
  name        = "$default"
  auto_deploy = true

  default_route_settings {
    throttling_rate_limit  = var.throttling_rate_limit
    throttling_burst_limit = var.throttling_burst_limit
  }

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.gateway.arn
    format = jsonencode({
      requestId          = "$context.requestId"
      ip                 = "$context.identity.sourceIp"
      routeKey           = "$context.routeKey"
      status             = "$context.status"
      latencyMs          = "$context.responseLatency"
      integrationLatency = "$context.integration.latency"
      integrationStatus  = "$context.integrationStatus"
      integrationError   = "$context.integrationErrorMessage"
      authorizerError    = "$context.authorizer.error"
      authorizerStatus   = "$context.authorizer.status"
      usuarioId          = "$context.authorizer.usuarioId"
      perfil             = "$context.authorizer.perfil"
    })
  }
}

resource "aws_apigatewayv2_authorizer" "jwt" {
  api_id                            = aws_apigatewayv2_api.gateway.id
  name                              = "wrench-jwt-${var.environment}"
  authorizer_type                   = "REQUEST"
  authorizer_uri                    = aws_lambda_function.function["authorizer"].invoke_arn
  authorizer_payload_format_version = "2.0"
  enable_simple_responses           = true
  identity_sources                  = ["$request.header.Authorization"]
  authorizer_result_ttl_in_seconds  = var.authorizer_cache_ttl
}

resource "aws_apigatewayv2_integration" "autenticacao" {
  api_id                 = aws_apigatewayv2_api.gateway.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.function["autenticacao"].invoke_arn
  integration_method     = "POST"
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "autenticacao" {
  api_id    = aws_apigatewayv2_api.gateway.id
  route_key = "POST /auth/cpf"
  target    = "integrations/${aws_apigatewayv2_integration.autenticacao.id}"
}

resource "aws_apigatewayv2_integration" "aplicacao" {
  for_each = local.rotas_aplicacao

  api_id               = aws_apigatewayv2_api.gateway.id
  integration_type     = "HTTP_PROXY"
  integration_method   = split(" ", each.key)[0]
  integration_uri      = "https://${var.app_hostname}${each.value.caminho}"
  timeout_milliseconds = 29000
}

resource "aws_apigatewayv2_route" "aplicacao" {
  for_each = local.rotas_aplicacao

  api_id             = aws_apigatewayv2_api.gateway.id
  route_key          = each.key
  target             = "integrations/${aws_apigatewayv2_integration.aplicacao[each.key].id}"
  authorization_type = each.value.protegida ? "CUSTOM" : "NONE"
  authorizer_id      = each.value.protegida ? aws_apigatewayv2_authorizer.jwt.id : null
}

resource "aws_lambda_permission" "autenticacao" {
  statement_id  = "AllowApiGatewayInvokeAutenticacao"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.function["autenticacao"].function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.gateway.execution_arn}/*/*/auth/cpf"
}

resource "aws_lambda_permission" "authorizer" {
  statement_id  = "AllowApiGatewayInvokeAuthorizer"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.function["authorizer"].function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.gateway.execution_arn}/authorizers/${aws_apigatewayv2_authorizer.jwt.id}"
}
