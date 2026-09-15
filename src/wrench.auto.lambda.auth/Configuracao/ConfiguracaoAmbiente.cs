using System.Globalization;

namespace wrench.auto.lambda.auth.Configuracao;

/// <summary>
/// Lê a configuração das variáveis de ambiente da function. Não existe valor padrão para segredo:
/// a ausência de chave JWT ou de connection string interrompe a inicialização, para que a function
/// nunca suba emitindo token assinado com chave vazia ou sem acesso ao banco.
/// </summary>
public static class ConfiguracaoAmbiente
{
    public const string ChaveJwt = "JWT_SIGNING_KEY";
    public const string IssuerJwt = "JWT_ISSUER";
    public const string AudienceJwt = "JWT_AUDIENCE";
    public const string ExpiracaoJwt = "JWT_EXPIRATION_MINUTES";
    public const string ConnectionString = "DATABASE_CONNECTION_STRING";

    private const string IssuerPadrao = "Wrench Auto Repair";
    private const string AudiencePadrao = "Wrench Auto Repair";
    private const int ExpiracaoPadraoMinutos = 30;

    /// <summary>Lê e valida os parâmetros do JWT.</summary>
    /// <exception cref="InvalidOperationException">Chave ausente, curta demais ou expiração inválida.</exception>
    public static ConfiguracaoJwt LerJwt(Func<string, string?> lerVariavel)
    {
        var chave = lerVariavel(ChaveJwt);

        if (string.IsNullOrWhiteSpace(chave))
            throw new InvalidOperationException($"Variável {ChaveJwt} não configurada.");

        if (chave.Length < ConfiguracaoJwt.TamanhoMinimoChave)
            throw new InvalidOperationException($"Variável {ChaveJwt} deve ter ao menos {ConfiguracaoJwt.TamanhoMinimoChave} caracteres.");

        var expiracaoTexto = lerVariavel(ExpiracaoJwt);
        var expiracao = ExpiracaoPadraoMinutos;

        if (!string.IsNullOrWhiteSpace(expiracaoTexto)
            && (!int.TryParse(expiracaoTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out expiracao) || expiracao <= 0))
            throw new InvalidOperationException($"Variável {ExpiracaoJwt} deve ser um inteiro positivo.");

        return new ConfiguracaoJwt(
            chave,
            ValorOuPadrao(lerVariavel(IssuerJwt), IssuerPadrao),
            ValorOuPadrao(lerVariavel(AudienceJwt), AudiencePadrao),
            expiracao);
    }

    /// <summary>Lê a connection string do usuário somente leitura da function.</summary>
    /// <exception cref="InvalidOperationException">Connection string ausente.</exception>
    public static string LerConnectionString(Func<string, string?> lerVariavel)
    {
        var connectionString = lerVariavel(ConnectionString);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Variável {ConnectionString} não configurada.");

        return connectionString;
    }

    private static string ValorOuPadrao(string? valor, string padrao) =>
        string.IsNullOrWhiteSpace(valor) ? padrao : valor;
}
