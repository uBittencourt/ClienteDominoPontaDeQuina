namespace ClienteDominoPontaDeQuina.Models;

/// <summary>
/// Representa o perfil detalhado de um jogador registrado na conta.
/// </summary>
/// <param name="JogadorId">Identificador único do jogador.</param>
/// <param name="Nome">Nome escolhido para exibição no jogo.</param>
/// <param name="Ativo">Indica se o jogador está atualmente ativado neste cliente.</param>
/// <param name="AtivadoEmUtc">Data e hora UTC em que o jogador foi ativado, se aplicável.</param>
public record PerfilJogador(
    Guid JogadorId,
    string? Nome,
    bool Ativo,
    DateTime? AtivadoEmUtc
);

/// <summary>
/// Objeto de requisição para registrar um novo perfil de jogador.
/// </summary>
/// <param name="Nome">Nome do jogador (deve conter entre 1 e 150 caracteres).</param>
public record CriarJogadorRequest(string? Nome);

/// <summary>
/// Objeto de requisição para realizar a ativação exclusiva do jogador no cliente.
/// </summary>
/// <param name="ChaveAtivacao">Chave secreta de 64 caracteres hexadecimais gerada pelo cliente.</param>
public record AtivarJogadorRequest(string? ChaveAtivacao);