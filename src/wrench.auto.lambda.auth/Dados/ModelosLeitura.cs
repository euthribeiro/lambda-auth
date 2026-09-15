namespace wrench.auto.lambda.auth.Dados;

/// <summary>Projeção somente leitura de <c>public."Clientes"</c>. Mapeia apenas as colunas do contrato.</summary>
public sealed class ClienteLeitura
{
    public Guid Id { get; init; }
    public string Documento { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

/// <summary>Projeção somente leitura de <c>public."Usuarios"</c>. <see cref="Ativo"/> é o status consultado na autenticação.</summary>
public sealed class UsuarioLeitura
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid PerfilId { get; init; }
    public bool Ativo { get; init; }
}

/// <summary>Projeção somente leitura de <c>public."Perfis"</c>.</summary>
public sealed class PerfilLeitura
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = string.Empty;
}

/// <summary>Dados do cliente necessários para decidir a autenticação e montar as claims do token.</summary>
/// <param name="UsuarioId">Identificador do usuário vinculado ao cliente pelo e-mail.</param>
/// <param name="Email">E-mail do usuário, usado como claim de nome.</param>
/// <param name="Perfil">Nome do perfil, usado como claim de role.</param>
/// <param name="Ativo">Status do usuário.</param>
public sealed record CredencialCliente(Guid UsuarioId, string Email, string Perfil, bool Ativo);
