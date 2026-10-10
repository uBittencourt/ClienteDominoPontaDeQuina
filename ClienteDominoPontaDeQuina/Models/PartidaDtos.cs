namespace ClienteDominoPontaDeQuina.Models;

/// <summary>
/// Representa uma pedra individual de dominó utilizada na partida.
/// </summary>
/// <param name="ValorA">Primeiro lado numérico da pedra (0 a 6).</param>
/// <param name="ValorB">Segundo lado numérico da pedra (0 a 6).</param>
public record PecaPartida(int ValorA, int ValorB);

/// <summary>
/// Contém dados resumidos de uma partida disponível para o ingresso de participantes.
/// </summary>
/// <param name="PartidaId">Identificador único da partida.</param>
/// <param name="PontuacaoAlvo">Pontuação limite para declarar o vencedor da partida.</param>
/// <param name="VersaoEstado">Versão atual de controle de concorrência do estado.</param>
public record PartidaDisponivel(
    Guid PartidaId,
    int PontuacaoAlvo,
    long VersaoEstado
);

/// <summary>
/// Objeto de requisição para a criação de uma nova sala de partida.
/// </summary>
/// <param name="PontuacaoAlvo">Pontuação limite da partida.</param>
/// <param name="JogadorId">ID do jogador criador/participante inicial.</param>
/// <param name="Webhook">URL opcional para recebimento de notificações assíncronas de eventos da partida.</param>
public record CriarPartidaRequest(
    int PontuacaoAlvo,
    Guid JogadorId,
    string? Webhook = null
);

/// <summary>
/// Objeto de requisição para ingressar em uma partida existente.
/// </summary>
/// <param name="VersaoEsperada">Versão do estado esperada para validação otimista de concorrência.</param>
/// <param name="ChaveIdempotencia">Identificador único (Guid) que garante segurança contra reenvios acidentais.</param>
/// <param name="JogadorId">ID do jogador que está ingressando.</param>
/// <param name="Webhook">URL opcional de webhook.</param>
public record IngressarRequest(
    long VersaoEsperada,
    Guid ChaveIdempotencia,
    Guid JogadorId,
    string? Webhook = null
);

/// <summary>
/// Contém o resumo de um participante dentro de uma partida em andamento.
/// </summary>
/// <param name="JogadorId">ID do jogador participante.</param>
/// <param name="Nome">Nome do participante.</param>
/// <param name="Pontuacao">Pontuação atual acumulada na partida.</param>
/// <param name="QuantidadePedrasNaMao">Número de pedras que o jogador ainda possui na mão (ocultando quais são).</param>
/// <param name="EhVez">Indica se o turno de jogada atual pertence a este participante.</param>
public record ResumoParticipantePartida(
    Guid JogadorId,
    string? Nome,
    int Pontuacao,
    int QuantidadePedrasNaMao,
    bool EhVez
);

/// <summary>
/// Objeto autoritativo detalhado que representa o estado completo de uma partida na perspectiva do participante.
/// </summary>
public record EstadoPartidaParticipante(
    Guid PartidaId,
    long VersaoEstado,
    int Situacao,
    int NumeroRodada,
    Guid? JogadorDoTurnoId,
    List<PecaPartida>? MinhaMao,
    List<PecaPartida>? Tabuleiro,
    List<ResumoParticipantePartida>? Participantes,
    Guid? VencedorId,
    int ProximaAcao,
    PecaPartida? PedraAberturaObrigatoria,
    Guid ParticipacaoId
);