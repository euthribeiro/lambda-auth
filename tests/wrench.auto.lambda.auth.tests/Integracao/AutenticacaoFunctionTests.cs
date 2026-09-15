using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using wrench.auto.lambda.auth.Configuracao;
using wrench.auto.lambda.auth.Funcoes;
using wrench.auto.lambda.auth.Seguranca;
using wrench.auto.lambda.auth.tests.Suporte;

namespace wrench.auto.lambda.auth.tests.Integracao;

public class AutenticacaoFunctionTests(BancoAutenticacaoFixture banco) : IClassFixture<BancoAutenticacaoFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private AutenticacaoFunction CriarFunction()
    {
        var ambiente = new Dictionary<string, string?>
        {
            [ConfiguracaoAmbiente.ChaveJwt] = Fixtures.Jwt.ChaveAssinatura,
            [ConfiguracaoAmbiente.ConnectionString] = banco.ConnectionStringLambda
        };

        return new AutenticacaoFunction(Composicao.CriarAutenticador(nome => ambiente.GetValueOrDefault(nome)));
    }

    private Task<APIGatewayHttpApiV2ProxyResponse> Autenticar(string? corpo, bool base64 = false) =>
        CriarFunction().HandleAsync(
            new APIGatewayHttpApiV2ProxyRequest
            {
                RouteKey = "POST /auth/cpf",
                RawPath = "/auth/cpf",
                Body = corpo is not null && base64 ? Convert.ToBase64String(Encoding.UTF8.GetBytes(corpo)) : corpo,
                IsBase64Encoded = base64,
                Headers = new Dictionary<string, string> { ["x-correlation-id"] = "teste-integracao" }
            },
            new TestLambdaContext());

    private static string Corpo(string documento) => JsonSerializer.Serialize(new { documento });

    [Fact]
    public async Task ClienteAtivo_Devolve200_ComTokenAceitoPeloAuthorizer()
    {
        var resposta = await Autenticar(Corpo("529.982.247-25"));

        Assert.Equal((int)HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("teste-integracao", resposta.Headers["x-correlation-id"]);

        var token = JsonSerializer.Deserialize<TokenEmitido>(resposta.Body, Json)!;
        Assert.Equal("ativo@cliente.com", token.Username);
        Assert.Equal("Cliente", token.Role);

        var principal = await new ValidadorToken(Fixtures.Jwt).ValidarAsync($"Bearer {token.Token}");
        Assert.NotNull(principal);
        Assert.True(principal.IsInRole("Cliente"));
    }

    [Fact]
    public async Task CorpoEmBase64_EhAceito()
    {
        var resposta = await Autenticar(Corpo(BancoAutenticacaoFixture.CpfAtivo), base64: true);

        Assert.Equal((int)HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("52998224724")]
    [InlineData("123")]
    [InlineData("")]
    public async Task DocumentoInvalido_Devolve400(string documento)
    {
        var resposta = await Autenticar(Corpo(documento));

        Assert.Equal((int)HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("nao-e-json")]
    public async Task CorpoInvalido_Devolve400(string? corpo)
    {
        var resposta = await Autenticar(corpo);

        Assert.Equal((int)HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task ClienteInativo_Devolve403()
    {
        var resposta = await Autenticar(Corpo(BancoAutenticacaoFixture.CpfInativo));

        Assert.Equal((int)HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.DoesNotContain("token", resposta.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsuarioNaoCliente_Devolve403()
    {
        var resposta = await Autenticar(Corpo(BancoAutenticacaoFixture.CpfFuncionario));

        Assert.Equal((int)HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Theory]
    [InlineData(BancoAutenticacaoFixture.CpfNaoCadastrado)]
    [InlineData(BancoAutenticacaoFixture.CpfSemUsuario)]
    public async Task ClienteNaoEncontrado_Devolve404(string documento)
    {
        var resposta = await Autenticar(Corpo(documento));

        Assert.Equal((int)HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task BancoIndisponivel_Devolve500_SemVazarExcecao()
    {
        var ambiente = new Dictionary<string, string?>
        {
            [ConfiguracaoAmbiente.ChaveJwt] = Fixtures.Jwt.ChaveAssinatura,
            [ConfiguracaoAmbiente.ConnectionString] = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=2"
        };
        var function = new AutenticacaoFunction(Composicao.CriarAutenticador(nome => ambiente.GetValueOrDefault(nome)));

        var resposta = await function.HandleAsync(
            new APIGatewayHttpApiV2ProxyRequest { Body = Corpo(BancoAutenticacaoFixture.CpfAtivo) },
            new TestLambdaContext());

        Assert.Equal((int)HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.DoesNotContain("Npgsql", resposta.Body);
        Assert.DoesNotContain("Exception", resposta.Body);
    }
}
