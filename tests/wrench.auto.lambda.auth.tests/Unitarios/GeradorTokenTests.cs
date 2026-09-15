using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using wrench.auto.lambda.auth.Seguranca;
using wrench.auto.lambda.auth.tests.Suporte;

namespace wrench.auto.lambda.auth.tests.Unitarios;

public class GeradorTokenTests
{
    /// <summary>Espelho de <c>AuthenticationConfiguration.ConfigureJwtBearer</c> da API.</summary>
    private static TokenValidationParameters ParametrosDaApi() => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Fixtures.Jwt.ChaveAssinatura)),
        ValidIssuer = Fixtures.Jwt.Issuer,
        ValidAudience = Fixtures.Jwt.Audience,
        RequireExpirationTime = true,
        ValidateIssuer = true,
        ValidateAudience = true
    };

    [Fact]
    public async Task Gerar_TokenAceitoPeloJwtBearerDaApi_ComRoleCliente()
    {
        var emitido = new GeradorToken(Fixtures.Jwt, TimeProvider.System).Gerar(Fixtures.ClienteAtivo);

        var handlerDoJwtBearer = new JsonWebTokenHandler { MapInboundClaims = true };
        var resultado = await handlerDoJwtBearer.ValidateTokenAsync(emitido.Token, ParametrosDaApi());

        Assert.True(resultado.IsValid, resultado.Exception?.Message);
        var principal = new ClaimsPrincipal(resultado.ClaimsIdentity);
        Assert.True(principal.IsInRole("Cliente"));
        Assert.Equal(Fixtures.ClienteAtivo.UsuarioId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(Fixtures.ClienteAtivo.Email, principal.FindFirst(ClaimTypes.Name)?.Value);
    }

    [Fact]
    public void Gerar_RespeitaExpiracaoEAlgoritmo()
    {
        var agora = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

        var emitido = new GeradorToken(Fixtures.Jwt, new RelogioFixo(agora)).Gerar(Fixtures.ClienteAtivo);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(emitido.Token);

        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        Assert.Equal(agora.UtcDateTime.AddMinutes(30), token.ValidTo);
        Assert.Equal("Cliente", emitido.Role);
        Assert.Equal(Fixtures.ClienteAtivo.Email, emitido.Username);
    }
}
