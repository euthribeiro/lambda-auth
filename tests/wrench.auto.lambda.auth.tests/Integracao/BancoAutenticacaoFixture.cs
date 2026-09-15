using Npgsql;
using Testcontainers.PostgreSql;

namespace wrench.auto.lambda.auth.tests.Integracao;

/// <summary>
/// PostgreSQL em container com as três tabelas do contrato e um role com <c>SELECT</c> apenas nas
/// colunas do contrato, igual ao provisionado pelo <c>infra-db</c>. A function conecta com esse role:
/// se a consulta do EF tocar em qualquer coluna fora do contrato, o teste falha por permissão.
/// </summary>
public sealed class BancoAutenticacaoFixture : IAsyncLifetime
{
    public const string RoleLambda = "wrench_lambda_auth";
    private const string SenhaRoleLambda = "senha-da-lambda-de-teste";

    public static readonly Guid PerfilCliente = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid PerfilAdmin = Guid.Parse("10000000-0000-0000-0000-000000000002");

    public const string CpfAtivo = "52998224725";
    public const string CpfInativo = "39053344705";
    public const string CpfSemUsuario = "11144477735";
    public const string CpfNaoCadastrado = "16899535009";
    public const string CpfFuncionario = "74682489070";

    public static readonly Guid UsuarioAtivo = Guid.Parse("20000000-0000-0000-0000-000000000001");

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public string ConnectionStringLambda { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var conexao = new NpgsqlConnection(_container.GetConnectionString());
        await conexao.OpenAsync();

        var script = $"""
            CREATE TABLE "Perfis" (
                "Id" uuid PRIMARY KEY,
                "Nome" text NOT NULL,
                "Descricao" text NOT NULL,
                "Ativo" boolean NOT NULL,
                "DataCriacao" timestamp with time zone NOT NULL
            );

            CREATE TABLE "Usuarios" (
                "Id" uuid PRIMARY KEY,
                "Email" text NOT NULL UNIQUE,
                "Senha" text NULL,
                "PerfilId" uuid NOT NULL REFERENCES "Perfis" ("Id"),
                "Ativo" boolean NOT NULL,
                "DateCadastro" timestamp with time zone NOT NULL
            );

            CREATE TABLE "Clientes" (
                "Id" uuid PRIMARY KEY,
                "Documento" text NOT NULL UNIQUE,
                "Nome" text NOT NULL,
                "Email" text NOT NULL UNIQUE,
                "DataCadastro" timestamp with time zone NOT NULL
            );

            INSERT INTO "Perfis" VALUES
                ('{PerfilCliente}', 'Cliente', 'Cliente da oficina', true, now()),
                ('{PerfilAdmin}', 'Admin', 'Administrador', true, now());

            INSERT INTO "Usuarios" VALUES
                ('{UsuarioAtivo}', 'ativo@cliente.com', 'hash', '{PerfilCliente}', true, now()),
                ('20000000-0000-0000-0000-000000000002', 'inativo@cliente.com', 'hash', '{PerfilCliente}', false, now()),
                ('20000000-0000-0000-0000-000000000003', 'funcionario@wrench.com', 'hash', '{PerfilAdmin}', true, now());

            INSERT INTO "Clientes" VALUES
                ('30000000-0000-0000-0000-000000000001', '{CpfAtivo}', 'Cliente Ativo', 'ativo@cliente.com', now()),
                ('30000000-0000-0000-0000-000000000002', '{CpfInativo}', 'Cliente Inativo', 'inativo@cliente.com', now()),
                ('30000000-0000-0000-0000-000000000003', '{CpfSemUsuario}', 'Cliente Sem Usuario', 'sem-usuario@cliente.com', now()),
                ('30000000-0000-0000-0000-000000000004', '{CpfFuncionario}', 'Funcionario Cliente', 'funcionario@wrench.com', now());

            CREATE ROLE {RoleLambda} LOGIN PASSWORD '{SenhaRoleLambda}';
            GRANT CONNECT ON DATABASE "{new NpgsqlConnectionStringBuilder(_container.GetConnectionString()).Database}" TO {RoleLambda};
            GRANT USAGE ON SCHEMA public TO {RoleLambda};
            GRANT SELECT ("Id", "Documento", "Email") ON "Clientes" TO {RoleLambda};
            GRANT SELECT ("Id", "Email", "PerfilId", "Ativo") ON "Usuarios" TO {RoleLambda};
            GRANT SELECT ("Id", "Nome") ON "Perfis" TO {RoleLambda};
            """;

        await using (var comando = new NpgsqlCommand(script, conexao))
            await comando.ExecuteNonQueryAsync();

        ConnectionStringLambda = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Username = RoleLambda,
            Password = SenhaRoleLambda
        }.ConnectionString;
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
