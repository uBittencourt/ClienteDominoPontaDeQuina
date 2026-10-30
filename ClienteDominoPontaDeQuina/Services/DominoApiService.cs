using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ClienteDominoPontaDeQuina.Models;

namespace ClienteDominoPontaDeQuina.Services;

/// <summary>
/// Serviço responsável por gerenciar a comunicação HTTP com a API oficial do Dominó Ponta de Quina.
/// Concentra operações de identidade, listagem, criação e controle de ativação de jogadores.
/// </summary>
public class DominoApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Inicializa uma nova instância do serviço da API de dominó.
    /// </summary>
    /// <param name="httpClient">Instância do HttpClient injetada pelo container.</param>
    /// <param name="configuration">Configurações da aplicação para leitura de parâmetros.</param>
    public DominoApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        var baseUrl = configuration["URL_API"]
                  ?? throw new InvalidOperationException("A URL_API não foi configurada no appsettings.");

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }

    /// <summary>
    /// Obtém o token Bearer configurado no ambiente de desenvolvimento de forma segura.
    /// </summary>
    /// <returns>O token de acesso configurado, ou nulo se ausente.</returns>
    public string? ObterTokenConfigurado()
    {
        return _configuration["TOKEN_BEARER"];
    }

    /// <summary>
    /// Consulta os dados da sessão atual vinculada ao token Bearer fornecido.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <returns>Objeto contendo os dados da sessão do usuário autenticado, ou nulo em caso de falha.</returns>
    public async Task<SessaoResponse?> ConsultarSessaoAsync(string tokenBearer)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/identidade/sessao");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<SessaoResponse>(_jsonOptions);
    }

    /// <summary>
    /// Lista todos os perfis de jogadores associados à conta autenticada.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <returns>Uma lista contendo os perfis dos jogadores cadastrados.</returns>
    public async Task<List<PerfilJogador>> ListarJogadoresAsync(string tokenBearer)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/jogadores");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return new();

        return await response.Content.ReadFromJsonAsync<List<PerfilJogador>>(_jsonOptions) ?? new();
    }

    /// <summary>
    /// Cria um novo perfil de jogador na conta autenticada.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <param name="nome">Nome do jogador (entre 1 e 150 caracteres).</param>
    /// <returns>O perfil do jogador recém-criado, contendo seu identificador exclusivo.</returns>
    public async Task<PerfilJogador?> CriarJogadorAsync(string tokenBearer, string nome)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/jogadores")
        {
            Content = JsonContent.Create(new CriarJogadorRequest(nome))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<PerfilJogador>(_jsonOptions);
    }

    /// <summary>
    /// Realiza a ativação do cliente para um jogador específico utilizando uma chave de 64 caracteres hexadecimais.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <param name="jogadorId">Identificador único do jogador.</param>
    /// <param name="chaveAtivacao">Chave gerada localmente enviada no corpo da requisição.</param>
    /// <returns>O perfil atualizado do jogador refletindo o estado ativo.</returns>
    public async Task<PerfilJogador?> AtivarClienteAsync(string tokenBearer, Guid jogadorId, string chaveAtivacao)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/jogadores/{jogadorId}/ativar-cliente")
        {
            Content = JsonContent.Create(new AtivarJogadorRequest(chaveAtivacao))
        };
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<PerfilJogador>(_jsonOptions);
    }

    /// <summary>
    /// Desativa o cliente de um jogador informando a chave correspondente no cabeçalho de segurança.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <param name="jogadorId">Identificador único do jogador.</param>
    /// <param name="chaveAtivacao">Chave secreta original enviada no header X-Jogador-Ativacao.</param>
    /// <returns>O perfil atualizado do jogador com o status inativado.</returns>
    public async Task<PerfilJogador?> DesativarClienteAsync(string tokenBearer, Guid jogadorId, string chaveAtivacao)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/jogadores/{jogadorId}/desativar-cliente");

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);
        requestMessage.Headers.Add("X-Jogador-Ativacao", chaveAtivacao);

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var erroDetalhe = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Erro da API ({(int)response.StatusCode}): {erroDetalhe}");
        }

        return await response.Content.ReadFromJsonAsync<PerfilJogador>(_jsonOptions);
    }

    /// <summary>
    /// Lista as partidas disponíveis no servidor para visualização ou ingresso de participantes.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <returns>Coleção contendo as partidas disponíveis no lobby.</returns>
    public async Task<List<PartidaDisponivel>> ListarPartidasDisponiveisAsync(string tokenBearer)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/partidas");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return new();

        return await response.Content.ReadFromJsonAsync<List<PartidaDisponivel>>(_jsonOptions) ?? new();
    }

    /// <summary>
    /// Cria uma nova partida definindo a pontuação alvo e registrando o solicitante como primeiro participante.
    /// Requer autenticação via Bearer token e validação criptográfica via header X-Jogador-Ativacao.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta de usuário.</param>
    /// <param name="chaveAtivacao">Chave secreta de ativação do jogador criador (X-Jogador-Ativacao).</param>
    /// <param name="pontuacaoAlvo">Pontuação limite necessária para vencer a partida.</param>
    /// <param name="jogadorId">Identificador único do jogador criador.</param>
    /// <returns>O estado inicial detalhado da partida criada, ou lança exceção descritiva.</returns>
    public async Task<EstadoPartidaParticipante?> CriarPartidaAsync(string tokenBearer, string chaveAtivacao, int pontuacaoAlvo, Guid jogadorId)
    {
        var payload = new CriarPartidaRequest(pontuacaoAlvo, jogadorId);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/partidas")
        {
            Content = JsonContent.Create(payload)
        };

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);
        requestMessage.Headers.Add("X-Jogador-Ativacao", chaveAtivacao.Trim());

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var erroConteudo = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Erro HTTP {(int)response.StatusCode}: {erroConteudo}");
        }

        return await response.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(_jsonOptions);
    }

    /// <summary>
    /// Ingressa em uma partida existente como o segundo participante, transicionando a mesa para o estado ativo.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta.</param>
    /// <param name="chaveAtivacao">Chave secreta de ativação do jogador que está ingressando (X-Jogador-Ativacao).</param>
    /// <param name="partidaId">Identificador único da partida alvo.</param>
    /// <param name="versaoEsperada">Versão atual do estado para controle otimista de concorrência.</param>
    /// <param name="chaveIdempotencia">Guid gerado para assegurar idempotência na requisição.</param>
    /// <param name="jogadorId">Identificador único do jogador que está ingressando.</param>
    /// <returns>O estado atualizado da partida após o ingresso, ou nulo em caso de falha.</returns>
    public async Task<EstadoPartidaParticipante?> IngressarPartidaAsync(string tokenBearer, string chaveAtivacao, Guid partidaId, long versaoEsperada, Guid chaveIdempotencia, Guid jogadorId)
    {
        var payload = new IngressarRequest(versaoEsperada, chaveIdempotencia, jogadorId, null);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/partidas/{partidaId}/entrar")
        {
            Content = JsonContent.Create(payload)
        };

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);
        requestMessage.Headers.Add("X-Jogador-Ativacao", chaveAtivacao.Trim());

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var erroDetalhe = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Erro ao ingressar [HTTP {(int)response.StatusCode}]: {erroDetalhe}");
        }

        return await response.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(_jsonOptions);
    }

    /// <summary>
    /// Consulta o estado atualizado e privado de uma partida em andamento para a visão do participante.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação da conta de usuário.</param>
    /// <param name="chaveAtivacao">Chave secreta de ativação do jogador solicitante (X-Jogador-Ativacao).</param>
    /// <param name="partidaId">Identificador único (Guid) da partida.</param>
    /// <param name="jogadorId">Identificador único (Guid) do jogador consultando a visão privada.</param>
    /// <param name="versaoConhecida">Versão atual conhecida localmente (usada para otimização com retorno 204).</param>
    /// <returns>O estado completo e detalhado da partida (`EstadoPartidaParticipante`), ou nulo se não houver alterações (204).</returns>
    /// <exception cref="InvalidOperationException">Lançado caso ocorra falha ou erro HTTP na consulta.</exception>
    public async Task<EstadoPartidaParticipante?> ConsultarEstadoPartidaAsync(string tokenBearer, string chaveAtivacao, Guid partidaId, Guid jogadorId, long versaoEstado = 0)
    {
        // Monta a URL idêntica ao que funcionou no Swagger
        var url = $"/api/partidas/{partidaId}?jogadorId={jogadorId}";
        if (versaoEstado > 0)
        {
            url += $"&versaoEstado={versaoEstado}";
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

        // 1. Autorização da Conta (Bearer)
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        // 2. Chave de Ativação do Jogador (Header exigido pelo requisito do professor)
        requestMessage.Headers.Add("X-Jogador-Ativacao", chaveAtivacao.Trim());

        var response = await _httpClient.SendAsync(requestMessage);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(_jsonOptions);
        }

        var erroDetalhe = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Erro ao consultar partida [HTTP {(int)response.StatusCode}]: {erroDetalhe}");
    }

    /// <summary>
    /// Executa uma jogada de encaixe de uma pedra em um dos lados do tabuleiro (0 = Esquerda, 1 = Direita).
    /// </summary>
    public async Task<EstadoPartidaParticipante?> JogarPecaAsync(string tokenBearer, string chaveAtivacao, Guid partidaId, long versaoEsperada, Guid jogadorId, int valorA, int valorB, int lado)
    {
        var payload = new JogarRequest(versaoEsperada, Guid.NewGuid(), jogadorId, valorA, valorB, lado);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/partidas/{partidaId}/jogar")
        {
            Content = JsonContent.Create(payload)
        };

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);
        requestMessage.Headers.Add("X-Jogador-Ativacao", chaveAtivacao.Trim());

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var erroDetalhe = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Jogada rejeitada [HTTP {(int)response.StatusCode}]: {erroDetalhe}");
        }

        return await response.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(_jsonOptions);
    }

    /// <summary>
    /// Solicita a compra de uma peça na cava quando não há opções de encaixe na mão.
    /// </summary>
    public async Task<EstadoPartidaParticipante?> ComprarPecaAsync(string tokenBearer, string chaveAtivacao, Guid partidaId, long versaoEsperada, Guid jogadorId)
    {
        var payload = new AcaoRequest(versaoEsperada, Guid.NewGuid(), jogadorId);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/partidas/{partidaId}/comprar")
        {
            Content = JsonContent.Create(payload)
        };

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);
        requestMessage.Headers.Add("X-Jogador-Ativacao", chaveAtivacao.Trim());

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var erroDetalhe = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Compra rejeitada [HTTP {(int)response.StatusCode}]: {erroDetalhe}");
        }

        return await response.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(_jsonOptions);
    }

    /// <summary>
    /// Passa a vez quando não há pedras na cava e nenhum encaixe válido disponível.
    /// </summary>
    public async Task<EstadoPartidaParticipante?> PassarTurnoAsync(string tokenBearer, string chaveAtivacao, Guid partidaId, long versaoEsperada, Guid jogadorId)
    {
        var payload = new AcaoRequest(versaoEsperada, Guid.NewGuid(), jogadorId);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/partidas/{partidaId}/passar")
        {
            Content = JsonContent.Create(payload)
        };

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);
        requestMessage.Headers.Add("X-Jogador-Ativacao", chaveAtivacao.Trim());

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var erroDetalhe = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Passagem rejeitada [HTTP {(int)response.StatusCode}]: {erroDetalhe}");
        }

        return await response.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(_jsonOptions);
    }

    /// <summary>
    /// Inicia a partida de forma resiliente, lidando automaticamente com conflitos de versão (HTTP 409).
    /// </summary>
    public async Task<EstadoPartidaParticipante?> IniciarPartidaAsync(string tokenBearer, string chaveAtivacao, Guid partidaId, long versaoEsperada, Guid chaveIdempotencia, Guid jogadorId)
    {
        const int maxTentativas = 3;
        int tentativa = 0;

        while (tentativa < maxTentativas)
        {
            tentativa++;
            var payload = new AcaoRequest(versaoEsperada, chaveIdempotencia, jogadorId);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/partidas/{partidaId}/iniciar")
            {
                Content = JsonContent.Create(payload)
            };

            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);
            requestMessage.Headers.Add("X-Jogador-Ativacao", chaveAtivacao.Trim());

            var response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EstadoPartidaParticipante>(_jsonOptions);
            }

            // Se der conflito de estado (409), tentamos buscar a versão mais recente e repetir
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict && tentativa < maxTentativas)
            {
                var estadoAtual = await ConsultarEstadoPartidaAsync(tokenBearer, chaveAtivacao, partidaId, jogadorId, 0);
                if (estadoAtual != null)
                {
                    versaoEsperada = estadoAtual.VersaoEstado;
                    // Gera uma nova chave de idempotência para o retry
                    chaveIdempotencia = Guid.NewGuid();
                    continue;
                }
            }

            var erroDetalhe = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Erro ao iniciar partida [HTTP {(int)response.StatusCode}]: {erroDetalhe}");
        }

        return null;
    }
}