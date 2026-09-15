using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using wrench.auto.lambda.auth.Configuracao;
using wrench.auto.lambda.auth.Dados;

namespace wrench.auto.lambda.auth.Seguranca;

/// <summary>Token emitido para o cliente.</summary>
/// <param name="Token">JWT assinado.</param>
/// <param name="Username">E-mail do usuário.</param>
/// <param name="Role">Perfil do usuário.</param>
public sealed record TokenEmitido(string Token, string Username, string Role);

/// <summary>
/// Emite o JWT no mesmo formato do <c>JwtTokenGenerator</c> da API: HS256 com a chave em ASCII e as
/// claims <see cref="ClaimTypes.NameIdentifier"/>, <see cref="ClaimTypes.Name"/> e
/// <see cref="ClaimTypes.Role"/>. As autorizações por role dos controllers dependem dessas claims.
/// </summary>
public sealed class GeradorToken(ConfiguracaoJwt configuracao, TimeProvider relogio)
{
    public TokenEmitido Gerar(CredencialCliente credencial)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        var chave = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuracao.ChaveAssinatura));

        var descritor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, credencial.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, credencial.Email),
                new Claim(ClaimTypes.Role, credencial.Perfil)
            ]),
            IssuedAt = agora,
            NotBefore = agora,
            Expires = agora.AddMinutes(configuracao.ExpiracaoMinutos),
            Issuer = configuracao.Issuer,
            Audience = configuracao.Audience,
            SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(handler.CreateToken(descritor));

        return new TokenEmitido(token, credencial.Email, credencial.Perfil);
    }
}
