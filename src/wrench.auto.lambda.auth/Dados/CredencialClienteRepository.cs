using Microsoft.EntityFrameworkCore;

namespace wrench.auto.lambda.auth.Dados;

/// <summary>Consulta a credencial do cliente a partir do documento.</summary>
public interface ICredencialClienteRepository
{
    /// <summary>Obtém usuário, perfil e status do cliente com o documento informado (somente dígitos).</summary>
    /// <returns><c>null</c> quando não existe cliente com o documento ou usuário vinculado a ele.</returns>
    Task<CredencialCliente?> ObterPorDocumentoAsync(string documento, CancellationToken cancellationToken);
}

/// <summary>
/// Implementação com EF Core. Cria um contexto por consulta porque o ambiente de execução da Lambda
/// reaproveita a instância entre invocações; as opções do contexto são compartilhadas.
/// </summary>
public sealed class CredencialClienteRepository(DbContextOptions<AutenticacaoDbContext> opcoes) : ICredencialClienteRepository
{
    public async Task<CredencialCliente?> ObterPorDocumentoAsync(string documento, CancellationToken cancellationToken)
    {
        await using var contexto = new AutenticacaoDbContext(opcoes);

        return await (
                from cliente in contexto.Clientes
                join usuario in contexto.Usuarios on cliente.Email equals usuario.Email
                join perfil in contexto.Perfis on usuario.PerfilId equals perfil.Id
                where cliente.Documento == documento
                select new CredencialCliente(usuario.Id, usuario.Email, perfil.Nome, usuario.Ativo))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
