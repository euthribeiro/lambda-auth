using Microsoft.EntityFrameworkCore;

namespace wrench.auto.lambda.auth.Dados;

/// <summary>
/// Contexto somente leitura sobre as tabelas criadas pelas migrations da API. Não possui migrations e
/// não deve ganhar colunas fora do contrato: o role do banco usado pela function só tem
/// <c>SELECT</c> nas colunas mapeadas aqui, e qualquer coluna extra faz a consulta falhar por
/// permissão. O contrato é verificado do lado da API por teste sobre <c>information_schema</c>.
/// </summary>
public sealed class AutenticacaoDbContext(DbContextOptions<AutenticacaoDbContext> options) : DbContext(options)
{
    public const string Schema = "public";

    public DbSet<ClienteLeitura> Clientes => Set<ClienteLeitura>();
    public DbSet<UsuarioLeitura> Usuarios => Set<UsuarioLeitura>();
    public DbSet<PerfilLeitura> Perfis => Set<PerfilLeitura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<ClienteLeitura>(cliente =>
        {
            cliente.ToTable("Clientes");
            cliente.HasKey(c => c.Id);
            cliente.Property(c => c.Id).HasColumnName("Id");
            cliente.Property(c => c.Documento).HasColumnName("Documento");
            cliente.Property(c => c.Email).HasColumnName("Email");
        });

        modelBuilder.Entity<UsuarioLeitura>(usuario =>
        {
            usuario.ToTable("Usuarios");
            usuario.HasKey(u => u.Id);
            usuario.Property(u => u.Id).HasColumnName("Id");
            usuario.Property(u => u.Email).HasColumnName("Email");
            usuario.Property(u => u.PerfilId).HasColumnName("PerfilId");
            usuario.Property(u => u.Ativo).HasColumnName("Ativo");
        });

        modelBuilder.Entity<PerfilLeitura>(perfil =>
        {
            perfil.ToTable("Perfis");
            perfil.HasKey(p => p.Id);
            perfil.Property(p => p.Id).HasColumnName("Id");
            perfil.Property(p => p.Nome).HasColumnName("Nome");
        });
    }
}
