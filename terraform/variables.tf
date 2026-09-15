variable "region" {
  description = "Região AWS da function e do API Gateway."
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Ambiente implantado. Compõe o nome de todos os recursos."
  type        = string

  validation {
    condition     = contains(["homologacao", "production"], var.environment)
    error_message = "environment deve ser homologacao ou production."
  }
}

variable "app_hostname" {
  description = "Hostname público da API no EKS para onde o gateway encaminha as rotas da aplicação (ex.: api.bgt3.com.br)."
  type        = string
}

variable "package_path" {
  description = "Caminho, relativo ao módulo, do ZIP publicado da function. Gerado pelo pipeline antes do plan."
  type        = string
  default     = "build/wrench-auth.zip"
}

variable "runtime" {
  description = "Runtime gerenciado da Lambda; acompanha o TargetFramework do projeto."
  type        = string
  default     = "dotnet10"
}

variable "lambda_memory_size" {
  description = "Memória das functions em MB."
  type        = number
  default     = 512
}

variable "lambda_timeout" {
  description = "Timeout das functions em segundos."
  type        = number
  default     = 15
}

variable "log_retention_days" {
  description = "Retenção dos logs das functions e do gateway."
  type        = number
  default     = 30
}

variable "authorizer_cache_ttl" {
  description = "Tempo, em segundos, que o gateway reaproveita a decisão do authorizer para o mesmo token."
  type        = number
  default     = 300
}

variable "throttling_rate_limit" {
  description = "Requisições por segundo sustentadas aceitas pelo gateway."
  type        = number
  default     = 50
}

variable "throttling_burst_limit" {
  description = "Rajada máxima de requisições aceitas pelo gateway."
  type        = number
  default     = 100
}

variable "jwt_signing_key" {
  description = "Chave HS256 compartilhada com a API (secret JWT_SIGNING_KEY). Mínimo de 32 caracteres."
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.jwt_signing_key) >= 32
    error_message = "jwt_signing_key deve ter ao menos 32 caracteres."
  }
}

variable "jwt_issuer" {
  description = "Issuer do JWT; idêntico ao JwtOptions.Issuer da API."
  type        = string
  default     = "Wrench Auto Repair"
}

variable "jwt_audience" {
  description = "Audience do JWT; idêntica ao JwtOptions.Audience da API."
  type        = string
  default     = "Wrench Auto Repair"
}

variable "jwt_expiration_minutes" {
  description = "Tempo de vida do token em minutos."
  type        = number
  default     = 30
}

variable "database_username" {
  description = "Role somente leitura da function, criado pelo stack roles/ do infra-db."
  type        = string
  sensitive   = true
}

variable "database_password" {
  description = "Senha do role somente leitura da function."
  type        = string
  sensitive   = true
}

variable "tfc_organization" {
  description = "Organização do HCP Terraform onde está o state do banco."
  type        = string
  default     = "bgt3"
}

variable "rds_workspace" {
  description = "Workspace do stack rds/ do infra-db, lido por remote state."
  type        = string
  default     = "wrench_auto_repair_rds"
}

variable "tags" {
  description = "Tags adicionais aplicadas a todos os recursos."
  type        = map(string)
  default = {
    Terraform = "true"
    Project   = "wrench-auto-repair"
  }
}
