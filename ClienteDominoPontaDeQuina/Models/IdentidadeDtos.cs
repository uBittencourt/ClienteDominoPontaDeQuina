namespace ClienteDominoPontaDeQuina.Models;

/// <summary>
/// Objeto de requisição para solicitar a geração de um token de acesso via e-mail.
/// </summary>
/// <param name="Email">E-mail acadêmico cadastrado.</param>
public record SolicitarTokenRequest(string? Email);

/// <summary>
/// Objeto de resposta contendo os dados da sessão autenticada do usuário.
/// </summary>
/// <param name="UsuarioId">Identificador único do usuário no sistema da API.</param>
public record SessaoResponse(string? UsuarioId);