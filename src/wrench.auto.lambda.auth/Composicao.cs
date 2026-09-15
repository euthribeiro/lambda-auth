using Microsoft.EntityFrameworkCore;
using wrench.auto.lambda.auth.Aplicacao;
using wrench.auto.lambda.auth.Configuracao;
using wrench.auto.lambda.auth.Dados;
using wrench.auto.lambda.auth.Seguranca;

namespace wrench.auto.lambda.auth;

/// <summary>
/// Monta as dependências de cada function a partir das variáveis de ambiente. O authorizer não recebe
/// connection string: ele valida só a assinatura do token e não acessa o banco.
/// </summary>
public static class Composicao
{
    public static AutenticadorCliente CriarAutenticador(Func<string, string?> lerVariavel)
    {
        var jwt = ConfiguracaoAmbiente.LerJwt(lerVariavel);
        var connectionString = ConfiguracaoAmbiente.LerConnectionString(lerVariavel);

        var opcoes = new DbContextOptionsBuilder<AutenticacaoDbContext>()
            .UseNpgsql(connectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new AutenticadorCliente(new CredencialClienteRepository(opcoes), new GeradorToken(jwt, TimeProvider.System));
    }

    public static ValidadorToken CriarValidador(Func<string, string?> lerVariavel) =>
        new(ConfiguracaoAmbiente.LerJwt(lerVariavel));
}
