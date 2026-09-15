using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using wrench.auto.lambda.auth.Aplicacao;

namespace wrench.auto.lambda.auth.Funcoes;

/// <summary>
/// Handler de <c>POST /auth/cpf</c> no API Gateway HTTP (payload 2.0).
/// Handler Lambda: <c>wrench.auto.lambda.auth::wrench.auto.lambda.auth.Funcoes.AutenticacaoFunction::HandleAsync</c>.
/// O documento nunca é registrado em log. Erros inesperados devolvem mensagem genérica; o detalhe
/// fica só no log da function.
/// </summary>
public sealed class AutenticacaoFunction
{
    private const string CabecalhoCorrelacao = "x-correlation-id";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AutenticadorCliente _autenticador;

    public AutenticacaoFunction() : this(Composicao.CriarAutenticador(Environment.GetEnvironmentVariable))
    {
    }

    public AutenticacaoFunction(AutenticadorCliente autenticador)
    {
        _autenticador = autenticador;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> HandleAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var correlacao = ObterCorrelacao(request, context);

        try
        {
            var corpo = LerCorpo(request);

            if (corpo is null)
                return Responder(HttpStatusCode.BadRequest, new ErroResposta("Corpo da requisição inválido."), correlacao);

            var resultado = await _autenticador.AutenticarAsync(corpo.Documento, CancellationToken.None);

            context.Logger.LogInformation($"Autenticação por documento concluída. Status={resultado.Status} CorrelationId={correlacao}");

            return resultado.Status switch
            {
                StatusAutenticacao.Autenticado => Responder(HttpStatusCode.OK, resultado.Token!, correlacao),
                StatusAutenticacao.DocumentoInvalido => Responder(HttpStatusCode.BadRequest, new ErroResposta("Documento inválido."), correlacao),
                StatusAutenticacao.NaoEncontrado => Responder(HttpStatusCode.NotFound, new ErroResposta("Cliente não encontrado."), correlacao),
                StatusAutenticacao.Inativo => Responder(HttpStatusCode.Forbidden, new ErroResposta("Cliente inativo."), correlacao),
                StatusAutenticacao.PerfilNaoPermitido => Responder(HttpStatusCode.Forbidden, new ErroResposta("Autenticação por documento disponível apenas para clientes."), correlacao),
                _ => Responder(HttpStatusCode.InternalServerError, new ErroResposta("Não foi possível autenticar."), correlacao)
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Falha inesperada na autenticação por documento. CorrelationId={correlacao} Erro={ex}");
            return Responder(HttpStatusCode.InternalServerError, new ErroResposta("Não foi possível autenticar."), correlacao);
        }
    }

    private static RequisicaoAutenticacao? LerCorpo(APIGatewayHttpApiV2ProxyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return null;

        var conteudo = request.IsBase64Encoded
            ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
            : request.Body;

        try
        {
            return JsonSerializer.Deserialize<RequisicaoAutenticacao>(conteudo, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ObterCorrelacao(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        if (request.Headers is not null)
        {
            foreach (var (nome, valor) in request.Headers)
            {
                if (string.Equals(nome, CabecalhoCorrelacao, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(valor))
                    return valor;
            }
        }

        return context.AwsRequestId;
    }

    private static APIGatewayHttpApiV2ProxyResponse Responder<T>(HttpStatusCode status, T corpo, string correlacao) => new()
    {
        StatusCode = (int)status,
        Body = JsonSerializer.Serialize(corpo, Json),
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            [CabecalhoCorrelacao] = correlacao
        }
    };
}

/// <summary>Corpo de <c>POST /auth/cpf</c>.</summary>
/// <param name="Documento">CPF ou CNPJ, com ou sem pontuação.</param>
public sealed record RequisicaoAutenticacao(string? Documento);

/// <summary>Corpo das respostas de erro.</summary>
public sealed record ErroResposta(string Mensagem);
