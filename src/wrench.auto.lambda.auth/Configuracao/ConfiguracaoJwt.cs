namespace wrench.auto.lambda.auth.Configuracao;

/// <summary>
/// Parâmetros de emissão e validação do JWT. Issuer, audience e chave de assinatura formam o contrato
/// com a API do <c>app-k8s</c>: os três precisam ser idênticos aos de <c>JwtOptions</c>, senão a API
/// responde 401 para todo token emitido aqui.
/// </summary>
/// <param name="ChaveAssinatura">Chave HMAC-SHA256, convertida para bytes em ASCII como na API. Mínimo de 32 caracteres.</param>
/// <param name="Issuer">Emissor esperado pela API.</param>
/// <param name="Audience">Audiência esperada pela API.</param>
/// <param name="ExpiracaoMinutos">Tempo de vida do token.</param>
public sealed record ConfiguracaoJwt(string ChaveAssinatura, string Issuer, string Audience, int ExpiracaoMinutos)
{
    /// <summary>Tamanho mínimo da chave: HS256 exige 256 bits.</summary>
    public const int TamanhoMinimoChave = 32;
}
