data "terraform_remote_state" "rds" {
  backend = "remote"

  config = {
    organization = var.tfc_organization
    workspaces = {
      name = var.rds_workspace
    }
  }
}

data "aws_iam_policy_document" "lambda_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

data "aws_iam_policy_document" "lambda_logs" {
  statement {
    effect    = "Allow"
    actions   = ["logs:CreateLogStream", "logs:PutLogEvents"]
    resources = [for grupo in aws_cloudwatch_log_group.function : "${grupo.arn}:*"]
  }
}
