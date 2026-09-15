using wrench.auto.lambda.auth.Dominio;
using wrench.auto.lambda.auth.tests.Suporte;

namespace wrench.auto.lambda.auth.tests.Unitarios;

public class DocumentoClienteTests
{
    [Theory]
    [InlineData(Fixtures.CpfValido, Fixtures.CpfValido)]
    [InlineData(Fixtures.CpfValidoFormatado, Fixtures.CpfValido)]
    [InlineData(Fixtures.CnpjValido, Fixtures.CnpjValido)]
    [InlineData("11.222.333/0001-81", Fixtures.CnpjValido)]
    public void TentarNormalizar_DocumentoValido_DevolveSomenteDigitos(string documento, string esperado)
    {
        var valido = DocumentoCliente.TentarNormalizar(documento, out var numeracao);

        Assert.True(valido);
        Assert.Equal(esperado, numeracao);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("52998224724")]
    [InlineData("11111111111")]
    [InlineData("1234567890")]
    [InlineData("11222333000180")]
    public void TentarNormalizar_DocumentoInvalido_Recusa(string? documento)
    {
        var valido = DocumentoCliente.TentarNormalizar(documento, out var numeracao);

        Assert.False(valido);
        Assert.Empty(numeracao);
    }
}
