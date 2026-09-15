using wrench.auto.lambda.auth.Configuracao;
using wrench.auto.lambda.auth.Dados;

namespace wrench.auto.lambda.auth.tests.Suporte;

/// <summary>Relógio com instante fixo, para emitir tokens já expirados.</summary>
internal sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}

internal static class Fixtures
{
    public const string CpfValido = "52998224725";
    public const string CpfValidoFormatado = "529.982.247-25";
    public const string CnpjValido = "11222333000181";

    public static readonly ConfiguracaoJwt Jwt = new(
        "chave-de-teste-com-mais-de-32-caracteres-0123456789",
        "Wrench Auto Repair",
        "Wrench Auto Repair",
        30);

    public static readonly CredencialCliente ClienteAtivo = new(
        Guid.Parse("7d4b8f0e-3c1a-4e8b-9a52-1f6d2c3b4a51"),
        "cliente@wrench.com.br",
        "Cliente",
        true);
}
