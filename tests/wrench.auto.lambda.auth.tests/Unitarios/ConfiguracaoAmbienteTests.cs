using wrench.auto.lambda.auth.Configuracao;

namespace wrench.auto.lambda.auth.tests.Unitarios;

public class ConfiguracaoAmbienteTests
{
    private static Func<string, string?> Ambiente(Dictionary<string, string?> valores) =>
        nome => valores.TryGetValue(nome, out var valor) ? valor : null;

    [Fact]
    public void LerJwt_SemChave_FalhaNaInicializacao()
    {
        var erro = Assert.Throws<InvalidOperationException>(() => ConfiguracaoAmbiente.LerJwt(Ambiente([])));

        Assert.Contains(ConfiguracaoAmbiente.ChaveJwt, erro.Message);
    }

    [Fact]
    public void LerJwt_ChaveCurta_FalhaNaInicializacao()
    {
        var ambiente = Ambiente(new() { [ConfiguracaoAmbiente.ChaveJwt] = "curta" });

        Assert.Throws<InvalidOperationException>(() => ConfiguracaoAmbiente.LerJwt(ambiente));
    }

    [Fact]
    public void LerJwt_ExpiracaoInvalida_FalhaNaInicializacao()
    {
        var ambiente = Ambiente(new()
        {
            [ConfiguracaoAmbiente.ChaveJwt] = new string('k', 40),
            [ConfiguracaoAmbiente.ExpiracaoJwt] = "0"
        });

        Assert.Throws<InvalidOperationException>(() => ConfiguracaoAmbiente.LerJwt(ambiente));
    }

    [Fact]
    public void LerJwt_SomenteChave_UsaIssuerAudienceEExpiracaoDaApi()
    {
        var ambiente = Ambiente(new() { [ConfiguracaoAmbiente.ChaveJwt] = new string('k', 40) });

        var jwt = ConfiguracaoAmbiente.LerJwt(ambiente);

        Assert.Equal("Wrench Auto Repair", jwt.Issuer);
        Assert.Equal("Wrench Auto Repair", jwt.Audience);
        Assert.Equal(30, jwt.ExpiracaoMinutos);
    }

    [Fact]
    public void LerConnectionString_Ausente_FalhaNaInicializacao()
    {
        Assert.Throws<InvalidOperationException>(() => ConfiguracaoAmbiente.LerConnectionString(Ambiente([])));
    }
}
