using wrench.auto.lambda.auth.Dados;
using wrench.auto.lambda.auth.Dominio;
using wrench.auto.lambda.auth.Seguranca;

namespace wrench.auto.lambda.auth.Aplicacao;

/// <summary>Desfecho da tentativa de autenticação por documento.</summary>
public enum StatusAutenticacao
{
    Autenticado,
    DocumentoInvalido,
    NaoEncontrado,
    Inativo,
    PerfilNaoPermitido
}

/// <summary>Resultado da autenticação; <see cref="Token"/> só existe quando o status é <see cref="StatusAutenticacao.Autenticado"/>.</summary>
public sealed record ResultadoAutenticacao(StatusAutenticacao Status, TokenEmitido? Token = null);

/// <summary>
/// Caso de uso da autenticação por CPF/CNPJ: valida o documento, consulta existência e status do
/// cliente e emite o JWT. Só o perfil <see cref="PerfilCliente"/> autentica por documento: um
/// funcionário que também seja cliente com o mesmo e-mail receberia token de funcionário apenas com
/// o CPF, sem senha.
/// </summary>
public sealed class AutenticadorCliente(ICredencialClienteRepository repositorio, GeradorToken geradorToken)
{
    public const string PerfilCliente = "Cliente";

    public async Task<ResultadoAutenticacao> AutenticarAsync(string? documento, CancellationToken cancellationToken)
    {
        if (!DocumentoCliente.TentarNormalizar(documento, out var numeracao))
            return new ResultadoAutenticacao(StatusAutenticacao.DocumentoInvalido);

        var credencial = await repositorio.ObterPorDocumentoAsync(numeracao, cancellationToken);

        if (credencial is null)
            return new ResultadoAutenticacao(StatusAutenticacao.NaoEncontrado);

        if (!string.Equals(credencial.Perfil, PerfilCliente, StringComparison.Ordinal))
            return new ResultadoAutenticacao(StatusAutenticacao.PerfilNaoPermitido);

        if (!credencial.Ativo)
            return new ResultadoAutenticacao(StatusAutenticacao.Inativo);

        return new ResultadoAutenticacao(StatusAutenticacao.Autenticado, geradorToken.Gerar(credencial));
    }
}
