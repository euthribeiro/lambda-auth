using DocumentValidator;

namespace wrench.auto.lambda.auth.Dominio;

/// <summary>
/// Validação do documento informado pelo cliente. Usa a mesma biblioteca de dígitos verificadores do
/// value object <c>CpfCnpj</c> da API e normaliza para somente dígitos, que é o formato gravado na
/// coluna <c>Clientes.Documento</c>. Aceita CPF (11 dígitos) e CNPJ (14 dígitos), porque a base
/// cadastra pessoas físicas e jurídicas.
/// </summary>
public static class DocumentoCliente
{
    /// <summary>Valida o documento e devolve a numeração sem pontuação.</summary>
    /// <returns><c>true</c> quando os dígitos verificadores conferem.</returns>
    public static bool TentarNormalizar(string? documento, out string numeracao)
    {
        numeracao = string.Empty;

        if (string.IsNullOrWhiteSpace(documento))
            return false;

        var digitos = new string(documento.Where(char.IsAsciiDigit).ToArray());

        var valido = digitos.Length switch
        {
            11 => CpfValidation.Validate(digitos),
            14 => CnpjValidation.Validate(digitos),
            _ => false
        };

        if (!valido)
            return false;

        numeracao = digitos;
        return true;
    }
}
