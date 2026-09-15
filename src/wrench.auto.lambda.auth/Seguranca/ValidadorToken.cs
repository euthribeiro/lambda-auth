using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using wrench.auto.lambda.auth.Configuracao;

namespace wrench.auto.lambda.auth.Seguranca;

/// <summary>
/// Valida o header <c>Authorization</c> com os mesmos parâmetros de
/// <c>AuthenticationConfiguration</c> da API: assinatura, issuer, audience e expiração obrigatória.
/// Divergir desses parâmetros cria tokens aceitos na borda e recusados na API, ou o contrário.
/// </summary>
public sealed class ValidadorToken
{
    private const string PrefixoBearer = "Bearer ";

    private readonly TokenValidationParameters _parametros;
    private readonly JwtSecurityTokenHandler _handler = new();

    public ValidadorToken(ConfiguracaoJwt configuracao)
    {
        _parametros = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuracao.ChaveAssinatura)),
            ValidIssuer = configuracao.Issuer,
            ValidAudience = configuracao.Audience,
            RequireExpirationTime = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };
    }

    /// <summary>Valida um header no formato <c>Bearer &lt;token&gt;</c>.</summary>
    /// <returns>As claims do token, ou <c>null</c> quando o header está ausente, malformado ou o token é inválido.</returns>
    public async Task<ClaimsPrincipal?> ValidarAsync(string? cabecalhoAuthorization)
    {
        if (string.IsNullOrWhiteSpace(cabecalhoAuthorization)
            || !cabecalhoAuthorization.StartsWith(PrefixoBearer, StringComparison.OrdinalIgnoreCase))
            return null;

        var token = cabecalhoAuthorization[PrefixoBearer.Length..].Trim();

        if (token.Length == 0)
            return null;

        var resultado = await _handler.ValidateTokenAsync(token, _parametros);

        return resultado.IsValid ? new ClaimsPrincipal(resultado.ClaimsIdentity) : null;
    }
}
