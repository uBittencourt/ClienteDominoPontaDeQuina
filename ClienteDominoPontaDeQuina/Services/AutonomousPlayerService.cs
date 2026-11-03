using ClienteDominoPontaDeQuina.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClienteDominoPontaDeQuina.Services
{
    /// <summary>
    /// Serviço responsável por gerenciar o ciclo de vida e a tomada de decisão 
    /// de um jogador autônomo (bot) integrado à API de Dominó.
    /// </summary>
    public class AutonomousPlayerService
    {
        private readonly DominoApiService _apiService;
        private CancellationTokenSource? _cts;
        private bool _isExecuting;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="AutonomousPlayerService"/>.
        /// </summary>
        /// <param name="apiService">Serviço de comunicação HTTP com a API de Dominó.</param>
        public AutonomousPlayerService(DominoApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        /// <summary>
        /// Indica se o jogador autônomo está ativo e executando o loop de partidas.
        /// </summary>
        public bool IsExecuting => _isExecuting;

        /// <summary>
        /// Inicia a execução em background do jogador autônomo para uma partida específica.
        /// </summary>
        /// <param name="token">Token Bearer de autenticação da sessão.</param>
        /// <param name="chaveAtivacao">Chave hexadecimal de ativação do cliente.</param>
        /// <param name="partidaId">Identificador único da partida.</param>
        /// <param name="jogadorId">Identificador único do jogador controlado pelo bot.</param>
        /// <exception cref="ArgumentException">Lançado se os parâmetros obrigatórios forem inválidos.</exception>
        public void IniciarBot(string token, string chaveAtivacao, Guid partidaId, Guid jogadorId)
        {
            if (_isExecuting)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("O token de autenticação não pode ser nulo ou vazio.", nameof(token));
            }

            _cts = new CancellationTokenSource();
            _isExecuting = true;
            _ = ExecutarLoopAsync(token, chaveAtivacao, partidaId, jogadorId, _cts.Token);
        }

        /// <summary>
        /// Para a execução do jogador autônomo.
        /// </summary>
        public void PararBot()
        {
            if (!_isExecuting) return;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _isExecuting = false;
        }

        /// <summary>
        /// Loop principal de polling que verifica o estado da mesa e executa jogadas automaticamente.
        /// </summary>
        private async Task ExecutarLoopAsync(string token, string chaveAtivacao, Guid partidaId, Guid jogadorId, CancellationToken cancellationToken)
        {
            long versaoAtual = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var estadoPartida = await _apiService.ConsultarEstadoPartidaAsync(
                        token,
                        chaveAtivacao,
                        partidaId,
                        jogadorId,
                        versaoAtual
                    );

                    if (estadoPartida != null)
                    {
                        versaoAtual = estadoPartida.VersaoEstado;

                        if (estadoPartida.Situacao == 2)
                        {
                            PararBot();
                            break;
                        }

                        if (EhTurnoDoBot(estadoPartida, jogadorId))
                        {
                            await RealizarJogadaAutomaticaAsync(token, chaveAtivacao, partidaId, jogadorId, estadoPartida);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Operação de polling cancelada via token.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO CRÍTICO no Polling]: {ex.Message} | StackTrace: {ex.StackTrace}");
                }

                await Task.Delay(3000, cancellationToken);
            }

            _isExecuting = false;
        }

        /// <summary>
        /// Verifica se o turno atual da partida pertence ao jogador autônomo.
        /// </summary>
        private bool EhTurnoDoBot(EstadoPartidaParticipante estadoPartida, Guid jogadorId)
        {
            return estadoPartida.JogadorDoTurnoId == jogadorId;
        }

        /// <summary>
        /// Analisa as peças disponíveis na mão, avalia a melhor jogada estratégica (múltiplos de 5 no Ponta de Quina) e executa na API.
        /// </summary>
        private async Task RealizarJogadaAutomaticaAsync(string token, string chaveAtivacao, Guid partidaId, Guid jogadorId, EstadoPartidaParticipante estadoPartida)
        {
            try
            {
                // Ação 1: Jogar Abertura / Ação 2: Jogar Normal
                if (estadoPartida.ProximaAcao == 1 || estadoPartida.ProximaAcao == 2)
                {
                    if (estadoPartida.MinhaMao != null && estadoPartida.MinhaMao.Any())
                    {
                        var melhorJogada = SelecionarMelhorJogada(estadoPartida);

                        if (melhorJogada != null)
                        {
                            await _apiService.JogarPecaAsync(
                                token,
                                chaveAtivacao,
                                partidaId,
                                estadoPartida.VersaoEstado,
                                jogadorId,
                                melhorJogada.Value.Peca.ValorA,
                                melhorJogada.Value.Peca.ValorB,
                                melhorJogada.Value.Lado
                            );
                        }
                    }
                }
                else if (estadoPartida.ProximaAcao == 3) // Comprar
                {
                    await _apiService.ComprarPecaAsync(token, chaveAtivacao, partidaId, estadoPartida.VersaoEstado, jogadorId);
                }
                else if (estadoPartida.ProximaAcao == 4) // Passar Turno
                {
                    await _apiService.PassarTurnoAsync(token, chaveAtivacao, partidaId, estadoPartida.VersaoEstado, jogadorId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO NA JOGADA AUTOMÁTICA]: {ex.Message} | Detalhes: {ex.InnerException?.Message}");
            }
        }

        /// <summary>
        /// Avalia todas as jogadas possíveis na mão contra o tabuleiro e retorna a melhor opção.
        /// </summary>
        private (PecaPartida Peca, int Lado)? SelecionarMelhorJogada(EstadoPartidaParticipante estadoPartida)
        {
            var mao = estadoPartida.MinhaMao;
            var tabuleiro = estadoPartida.Tabuleiro;

            // Se o tabuleiro estiver vazio (abertura), joga a maior pedra (priorizando duplas)
            if (tabuleiro == null || !tabuleiro.Any())
            {
                var pecaAbertura = mao
                    .OrderByDescending(p => p.ValorA == p.ValorB)
                    .ThenByDescending(p => p.ValorA + p.ValorB)
                    .First();

                return (pecaAbertura, 0);
            }

            int pontaEsquerda = tabuleiro.First().ValorA;
            int pontaDireita = tabuleiro.Last().ValorB;

            var jogadasValidas = new List<OpcaoJogada>();

            foreach (var peca in mao)
            {
                // Testar encaixe na Ponta Esquerda (Lado 0)
                if (peca.ValorA == pontaEsquerda || peca.ValorB == pontaEsquerda)
                {
                    int novaPontaEsq = (peca.ValorA == pontaEsquerda) ? peca.ValorB : peca.ValorA;
                    int somaResultado = novaPontaEsq + pontaDireita;
                    int pontos = (somaResultado % 5 == 0) ? somaResultado : 0;

                    jogadasValidas.Add(new OpcaoJogada(peca, 0, pontos, peca.ValorA + peca.ValorB));
                }

                // Testar encaixe na Ponta Direita (Lado 1)
                if (peca.ValorA == pontaDireita || peca.ValorB == pontaDireita)
                {
                    int novaPontaDir = (peca.ValorA == pontaDireita) ? peca.ValorB : peca.ValorA;
                    int somaResultado = pontaEsquerda + novaPontaDir;
                    int pontos = (somaResultado % 5 == 0) ? somaResultado : 0;

                    jogadasValidas.Add(new OpcaoJogada(peca, 1, pontos, peca.ValorA + peca.ValorB));
                }
            }

            if (!jogadasValidas.Any())
            {
                return null;
            }

            var melhorOpcao = jogadasValidas
                .OrderByDescending(j => j.PontosObtidos)
                .ThenByDescending(j => j.PesoPeca)
                .First();

            return (melhorOpcao.Peca, melhorOpcao.Lado);
        }

        /// <summary>
        /// Estrutura auxiliar interna para comparar pontuações de jogadas candidatas.
        /// </summary>
        private record OpcaoJogada(PecaPartida Peca, int Lado, int PontosObtidos, int PesoPeca);
    }
}