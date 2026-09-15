output "api_gateway_url" {
  description = "URL base do API Gateway neste ambiente."
  value       = aws_apigatewayv2_api.gateway.api_endpoint
}

output "api_gateway_id" {
  description = "ID do API Gateway HTTP."
  value       = aws_apigatewayv2_api.gateway.id
}

output "autenticacao_url" {
  description = "Endpoint de autenticação por CPF."
  value       = "${aws_apigatewayv2_api.gateway.api_endpoint}/auth/cpf"
}

output "autenticacao_function_name" {
  description = "Nome da Lambda de autenticação por CPF."
  value       = aws_lambda_function.function["autenticacao"].function_name
}

output "authorizer_function_name" {
  description = "Nome da Lambda authorizer."
  value       = aws_lambda_function.function["authorizer"].function_name
}

output "app_hostname" {
  description = "Hostname da API para onde as rotas da aplicação são encaminhadas."
  value       = var.app_hostname
}

output "database_name" {
  description = "Database lido pela Lambda de autenticação neste ambiente."
  value       = local.database_name
}
