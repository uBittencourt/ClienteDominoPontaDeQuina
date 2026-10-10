namespace ClienteDominoPontaDeQuina.Models;

/// <summary>
/// Modelo base de requisição para ações genéricas que alteram o estado da partida (ex: comprar ou passar).
/// </summary>
/// <param name="VersaoEsperada">Versão atual esperada do estado para controle de concorrência.</param>
/// <param name="ChaveIdempotencia">Identificador UUID único da requisição para evitar duplicidade em falhas de rede.</param>
/// <param name="JogadorId">ID do jogador que está executando a ação.</param>
public record AcaoRequest(
    long VersaoEsperada,
    Guid ChaveIdempotencia,
    Guid JogadorId
);

/// <summary>
/// Objeto de requisição específico para efetuar uma jogada de uma pedra na mesa.
/// </summary>
/// <param name="VersaoEsperada">Versão atual esperada do estado da partida.</param>
/// <param name="ChaveIdempotencia">Chave UUID de idempotência da ação.</param>
/// <param name="JogadorId">ID do jogador executando a jogada.</param>
/// <param name="ValorA">Valor da ponta A da pedra a ser jogada.</param>
/// <param name="ValorB">Valor da ponta B da pedra a ser jogada.</param>
/// <param name="Lado">Lado da mesa onde a pedra será encaixada (0 = esquerda, 1 = direita).</param>
public record JogarRequest(
    long VersaoEsperada,
    Guid ChaveIdempotencia,
    Guid JogadorId,
    int ValorA,
    int ValorB,
    int Lado 
);

/// <summary>
/// Retorno de confirmação emitido após a execução bem-sucedida de uma ação na partida.
/// </summary>
/// <param name="ChaveIdempotencia">Chave que originou a requisição.</param>
/// <param name="AcaoIdempotente">Indica se a requisição foi tratada como repetição idempotente de uma ação anterior.</param>
/// <param name="Estado">O estado atualizado da partida após a ação.</param>
public record ConfirmacaoAcaoPartida(
    Guid ChaveIdempotencia,
    bool AcaoIdempotente,
    EstadoPartidaParticipante? Estado
);

/// <summary>
/// Representa o formato de resposta retornado pela API em cenários de erro de validação ou conflito.
/// </summary>
/// <param name="Codigo">Código descritivo do erro ocorrido.</param>
/// <param name="EstadoAutoritativo">Estado real e atualizado da partida fornecido pelo servidor para sincronização do cliente.</param>
public record RespostaErro(
    string? Codigo,
    EstadoPartidaParticipante? EstadoAutoritativo
);