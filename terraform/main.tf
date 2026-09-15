locals {
  pacote = "${path.module}/${var.package_path}"

  nome_funcao_autenticacao = "wrench-auth-cpf-${var.environment}"
  nome_funcao_authorizer   = "wrench-auth-authorizer-${var.environment}"
  nome_gateway             = "wrench-api-gateway-${var.environment}"

  handler_autenticacao = "wrench.auto.lambda.auth::wrench.auto.lambda.auth.Funcoes.AutenticacaoFunction::HandleAsync"
  handler_authorizer   = "wrench.auto.lambda.auth::wrench.auto.lambda.auth.Funcoes.AuthorizerFunction::HandleAsync"

  connection_string = join(";", [
    "Host=${data.terraform_remote_state.rds.outputs.database_hostname}",
    "Port=5432",
    "Database=${data.terraform_remote_state.rds.outputs.database_name}",
    "Username=${var.database_username}",
    "Password=${var.database_password}",
    "SSL Mode=Require",
    "Trust Server Certificate=true"
  ])

  variaveis_jwt = {
    JWT_SIGNING_KEY        = var.jwt_signing_key
    JWT_ISSUER             = var.jwt_issuer
    JWT_AUDIENCE           = var.jwt_audience
    JWT_EXPIRATION_MINUTES = tostring(var.jwt_expiration_minutes)
  }

  funcoes = {
    autenticacao = {
      nome    = local.nome_funcao_autenticacao
      handler = local.handler_autenticacao
      variaveis = merge(local.variaveis_jwt, {
        DATABASE_CONNECTION_STRING = local.connection_string
      })
    }
    authorizer = {
      nome      = local.nome_funcao_authorizer
      handler   = local.handler_authorizer
      variaveis = local.variaveis_jwt
    }
  }

  rotas_publicas_aplicacao = {
    "POST /api/v1/autenticacao"           = "/api/v1/autenticacao"
    "PUT /api/v1/usuario/primeiro-acesso" = "/api/v1/usuario/primeiro-acesso"
    "GET /api/v1/ordem-servico/{id}"      = "/api/v1/ordem-servico/{id}"
    "GET /health"                         = "/health"
    "GET /health/ready"                   = "/health/ready"
    "GET /docs-ui"                        = "/docs-ui"
    "GET /docs-ui/{proxy+}"               = "/docs-ui/{proxy}"
    "GET /openapi/{proxy+}"               = "/openapi/{proxy}"
  }

  rotas_protegidas_aplicacao = {
    "ANY /api/{proxy+}"                       = "/api/{proxy}"
    "GET /api/v1/ordem-servico/cliente"       = "/api/v1/ordem-servico/cliente"
    "GET /api/v1/ordem-servico/monitoramento" = "/api/v1/ordem-servico/monitoramento"
  }

  rotas_aplicacao = merge(
    { for rota, caminho in local.rotas_publicas_aplicacao : rota => { caminho = caminho, protegida = false } },
    { for rota, caminho in local.rotas_protegidas_aplicacao : rota => { caminho = caminho, protegida = true } }
  )
}
