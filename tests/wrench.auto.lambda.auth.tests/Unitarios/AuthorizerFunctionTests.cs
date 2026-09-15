using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using wrench.auto.lambda.auth.Configuracao;
using wrench.auto.lambda.auth.Funcoes;
using wrench.auto.lambda.auth.Seguranca;
using wrench.auto.lambda.auth.tests.Suporte;

namespace wrench.auto.lambda.auth.tests.Unitarios;

public class AuthorizerFunctionTests
{
    private readonly AuthorizerFunction _function = new(new ValidadorToken(Fixtures.Jwt));

    private static string TokenValido(ConfiguracaoJwt? configuracao = null, TimeProvider? relogio = null) =>
        new GeradorToken(configuracao ?? Fixtures.Jwt, relogio ?? TimeProvider.System).Gerar(Fixtures.ClienteAtivo).Token;

    private static APIGatewayCustomAuthorizerV2Request Requisicao(string? authorization) => new()
    {
        Type = "REQUEST",
        RouteKey = "ANY /api/{proxy+}",
        Headers = authorization is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["authorization"] = authorization }
    };

    private Task<APIGatewayCustomAuthorizerV2SimpleResponse> Autorizar(string? authorization) =>
        _function.HandleAsync(Requisicao(authorization), new TestLambdaContext());

    [Fact]
    public async Task TokenValido_Autoriza_EPropagaContexto()
    {
        var resposta = await Autorizar($"Bearer {TokenValido()}");

        Assert.True(resposta.IsAuthorized);
        Assert.Equal("Cliente", resposta.Context["perfil"]);
        Assert.Equal(Fixtures.ClienteAtivo.Email, resposta.Context["email"]);
        Assert.Equal(Fixtures.ClienteAtivo.UsuarioId.ToString(), resposta.Context["usuarioId"]);
    }

    [Fact]
    public async Task TokenExpirado_Nega()
    {
        var relogioPassado = new RelogioFixo(DateTimeOffset.UtcNow.AddHours(-2));

        var resposta = await Autorizar($"Bearer {TokenValido(relogio: relogioPassado)}");

        Assert.False(resposta.IsAuthorized);
    }

    [Fact]
    public async Task ChaveErrada_Nega()
    {
        var outraChave = Fixtures.Jwt with { ChaveAssinatura = "outra-chave-tambem-com-mais-de-32-caracteres" };

        var resposta = await Autorizar($"Bearer {TokenValido(outraChave)}");

        Assert.False(resposta.IsAuthorized);
    }

    [Fact]
    public async Task IssuerErrado_Nega()
    {
        var resposta = await Autorizar($"Bearer {TokenValido(Fixtures.Jwt with { Issuer = "Outro Emissor" })}");

        Assert.False(resposta.IsAuthorized);
    }

    [Fact]
    public async Task AudienceErrada_Nega()
    {
        var resposta = await Autorizar($"Bearer {TokenValido(Fixtures.Jwt with { Audience = "Outra Audiencia" })}");

        Assert.False(resposta.IsAuthorized);
    }

    [Fact]
    public async Task SemCabecalho_Nega()
    {
        var resposta = await Autorizar(null);

        Assert.False(resposta.IsAuthorized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bearer ")]
    [InlineData("Basic dXN1YXJpbzpzZW5oYQ==")]
    [InlineData("token-sem-prefixo")]
    public async Task CabecalhoSemBearer_Nega(string authorization)
    {
        var resposta = await Autorizar(authorization);

        Assert.False(resposta.IsAuthorized);
    }

    [Fact]
    public async Task TokenMalformado_Nega()
    {
        var resposta = await Autorizar("Bearer nao.e.um.jwt");

        Assert.False(resposta.IsAuthorized);
    }
}
