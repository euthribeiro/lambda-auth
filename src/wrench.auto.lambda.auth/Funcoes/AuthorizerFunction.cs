using System.Security.Claims;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using wrench.auto.lambda.auth.Seguranca;

namespace wrench.auto.lambda.auth.Funcoes;

/// <summary>
/// Lambda authorizer do tipo REQUEST com resposta simples (payload 2.0), associado às rotas
/// protegidas do API Gateway.
/// Handler Lambda: <c>wrench.auto.lambda.auth::wrench.auto.lambda.auth.Funcoes.AuthorizerFunction::HandleAsync</c>.
/// A decisão é cacheada pelo gateway por token (identity source <c>Authorization</c>), então um token
/// pode passar pela borda até o fim do TTL do cache mesmo depois de expirar; a API revalida a
/// expiração em toda requisição.
/// </summary>
public sealed class AuthorizerFunction
{
    private readonly ValidadorToken _validador;

    public AuthorizerFunction() : this(Composicao.CriarValidador(Environment.GetEnvironmentVariable))
    {
    }

    public AuthorizerFunction(ValidadorToken validador)
    {
        _validador = validador;
    }

    public async Task<APIGatewayCustomAuthorizerV2SimpleResponse> HandleAsync(APIGatewayCustomAuthorizerV2Request request, ILambdaContext context)
    {
        var principal = await _validador.ValidarAsync(ObterCabecalho(request));

        if (principal is null)
        {
            context.Logger.LogWarning($"Acesso negado pelo authorizer. Rota={request.RouteKey}");
            return new APIGatewayCustomAuthorizerV2SimpleResponse { IsAuthorized = false };
        }

        return new APIGatewayCustomAuthorizerV2SimpleResponse
        {
            IsAuthorized = true,
            Context = new Dictionary<string, object>
            {
                ["usuarioId"] = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                ["email"] = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                ["perfil"] = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty
            }
        };
    }

    private static string? ObterCabecalho(APIGatewayCustomAuthorizerV2Request request)
    {
        if (request.Headers is not null)
        {
            foreach (var (nome, valor) in request.Headers)
            {
                if (string.Equals(nome, "authorization", StringComparison.OrdinalIgnoreCase))
                    return valor;
            }
        }

        return request.IdentitySource?.FirstOrDefault();
    }
}
