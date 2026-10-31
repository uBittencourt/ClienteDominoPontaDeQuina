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
    /// Inicializa uma nova instância da classe <see cref="DominoApiService"/> configurando o cliente HTTP e as dependências de configuração.
    /// </summary>
    /// <param name="httpClient">Instância do cliente HTTP injetada para comunicação com a API.</param>
    /// <param name="configuration">Instância de configuração da aplicação utilizada para obter parâmetros como URL base e tokens.</param>
    /// <exception cref="InvalidOperationException">Lançada quando a chave <c>URL_API</c> não está devidamente configurada no arquivo de configuração.</exception>
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
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
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
    /// Lista de forma assíncrona todos os perfis de jogadores cadastrados no sistema.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <returns>Uma lista contendo os perfis dos jogadores encontrados, ou uma lista vazia em caso de falha na requisição ou ausência de registros.</returns>
    public async Task<List<PerfilJogador>> ListarJogadoresAsync(string tokenBearer)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/jogadores");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return new();

        return await response.Content.ReadFromJsonAsync<List<PerfilJogador>>(_jsonOptions) ?? new();
    }

    /// <summary>
    /// Cria um novo jogador/cliente de forma assíncrona, enviando o nome informado no corpo da requisição.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="nome">Nome do jogador que será cadastrado.</param>
    /// <returns>O objeto contendo o perfil do jogador recém-criado, ou nulo se a requisição falhar.</returns>
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
    /// Ativa o perfil de um cliente/jogador de forma assíncrona, enviando a chave de ativação correspondente no corpo da requisição.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="jogadorId">Identificador único (UUID) do jogador que será ativado.</param>
    /// <param name="chaveAtivacao">Chave de ativação necessária para validar a ativação do perfil.</param>
    /// <returns>O objeto contendo o perfil atualizado do jogador após a ativação, ou nulo se a requisição falhar.</returns>
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
    /// Desativa o perfil de um cliente/jogador de forma assíncrona, enviando o identificador e a chave de ativação correspondente.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="jogadorId">Identificador único (UUID) do jogador que será desativado.</param>
    /// <param name="chaveAtivacao">Chave de ativação do jogador utilizada no cabeçalho X-Jogador-Ativacao.</param>
    /// <returns>O objeto contendo o perfil atualizado do jogador após a desativação, ou nulo se não houver conteúdo.</returns>
    /// <exception cref="InvalidOperationException">Lançada quando a API retorna um código de status de erro ao tentar desativar o cliente.</exception>
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
    /// Lista de forma assíncrona todas as partidas disponíveis no servidor que estão abertas para visualização ou ingresso.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <returns>Uma lista contendo as partidas disponíveis encontradas, ou uma lista vazia em caso de falha ou ausência de registros.</returns>
    public async Task<List<PartidaDisponivel>> ListarPartidasDisponiveisAsync(string tokenBearer)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/partidas");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return new();

        return await response.Content.ReadFromJsonAsync<List<PartidaDisponivel>>(_jsonOptions) ?? new();
    }

    /// <summary>
    /// Cria uma nova partida de dominó de forma assíncrona, enviando a pontuação alvo e o identificador do jogador criador.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="chaveAtivacao">Chave de ativação do jogador utilizada no cabeçalho X-Jogador-Ativacao.</param>
    /// <param name="pontuacaoAlvo">Pontuação limite definida para o término da partida.</param>
    /// <param name="jogadorId">Identificador único (UUID) do jogador criador da sala.</param>
    /// <returns>O objeto contendo o estado inicial da partida e a participação do jogador criado.</returns>
    /// <exception cref="InvalidOperationException">Lançada quando a API retorna um código de status de erro ao tentar criar a partida.</exception>
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
    /// Realiza o ingresso de um jogador em uma partida existente de forma assíncrona, enviando o payload de solicitação e os cabeçalhos de autenticação e ativação.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="chaveAtivacao">Chave de ativação do jogador utilizada no cabeçalho X-Jogador-Ativacao.</param>
    /// <param name="partidaId">Identificador único (UUID) da partida na qual o jogador deseja ingressar.</param>
    /// <param name="versaoEsperada">Versão atual esperada do estado da partida para controle de concorrência.</param>
    /// <param name="chaveIdempotencia">Chave única (UUID) para garantir a idempotência da requisição.</param>
    /// <param name="jogadorId">Identificador único (UUID) do jogador que está ingressando na partida.</param>
    /// <returns>O objeto atualizado contendo o novo estado da partida e a participação do jogador após o ingresso.</returns>
    /// <exception cref="InvalidOperationException">Lançada quando a API retorna um código de status de erro (falha ao ingressar na partida).</exception>
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
    /// Consulta de forma assíncrona o estado atual de uma partida específica para um determinado jogador, permitindo opcionalmente filtrar por versão.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="chaveAtivacao">Chave de ativação do jogador utilizada no cabeçalho X-Jogador-Ativacao.</param>
    /// <param name="partidaId">Identificador único (UUID) da partida que está sendo consultada.</param>
    /// <param name="jogadorId">Identificador único (UUID) do jogador que está realizando a consulta.</param>
    /// <param name="versaoEstado">Versão opcional do estado para controle de atualização (padrão é 0).</param>
    /// <returns>O objeto contendo o estado atualizado da partida e participação.</returns>
    /// <exception cref="InvalidOperationException">Lançada quando a API retorna um código de status de erro ao tentar consultar a partida.</exception>
    public async Task<EstadoPartidaParticipante?> ConsultarEstadoPartidaAsync(string tokenBearer, string chaveAtivacao, Guid partidaId, Guid jogadorId, long versaoEstado = 0)
    {
        var url = $"/api/partidas/{partidaId}?jogadorId={jogadorId}";
        if (versaoEstado > 0)
        {
            url += $"&versaoEstado={versaoEstado}";
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenBearer);
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
    /// Realiza a jogada de uma peça específica no tabuleiro da partida informada, utilizando os parâmetros de validação e o lado desejado.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="chaveAtivacao">Chave de ativação do jogador utilizada no cabeçalho X-Jogador-Ativacao.</param>
    /// <param name="partidaId">Identificador único (UUID) da partida em andamento.</param>
    /// <param name="versaoEsperada">Versão atual esperada do estado da partida para controle de concorrência.</param>
    /// <param name="jogadorId">Identificador único (UUID) do jogador que está realizando a jogada.</param>
    /// <param name="valorA">Primeiro valor numérico da peça de dominó.</param>
    /// <param name="valorB">Segundo valor numérico da peça de dominó.</param>
    /// <param name="lado">Lado do tabuleiro onde a peça será encaixada (ex: 0 para esquerda, 1 para direita).</param>
    /// <returns>O objeto atualizado contendo o novo estado da partida e a participação do jogador.</returns>
    /// <exception cref="InvalidOperationException">Lançada quando a API retorna um código de status de erro (falha na jogada).</exception>
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
    /// Executa a ação de comprar uma peça do monte para o jogador na partida informada.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="chaveAtivacao">Chave de ativação do jogador utilizada no cabeçalho X-Jogador-Ativacao.</param>
    /// <param name="partidaId">Identificador único (UUID) da partida em andamento.</param>
    /// <param name="versaoEsperada">Versão atual esperada do estado da partida para controle de concorrência.</param>
    /// <param name="jogadorId">Identificador único (UUID) do jogador que está realizando a compra da peça.</param>
    /// <returns>O objeto atualizado contendo o novo estado da partida após a compra.</returns>
    /// <exception cref="InvalidOperationException">Lançada quando a API retorna um código de status de erro (falha na compra).</exception>
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
    /// Executa a ação de passar o turno do jogador atual na partida informada, caso não possua jogadas ou compras disponíveis.
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="chaveAtivacao">Chave de ativação do jogador utilizada no cabeçalho X-Jogador-Ativacao.</param>
    /// <param name="partidaId">Identificador único (UUID) da partida em andamento.</param>
    /// <param name="versaoEsperada">Versão atual esperada do estado da partida para controle de concorrência.</param>
    /// <param name="jogadorId">Identificador único (UUID) do jogador que está passando o turno.</param>
    /// <returns>O objeto atualizado contendo o novo estado da partida após a passagem do turno.</returns>
    /// <exception cref="InvalidOperationException">Lançada quando a API retorna um código de status de erro (falha ao passar turno).</exception>
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
    /// Inicia uma partida de dominó de forma assíncrona, aplicando tentativas de reenvio em caso de conflito de versão (concorrência otimista).
    /// </summary>
    /// <param name="tokenBearer">Token de autenticação Bearer da sessão.</param>
    /// <param name="chaveAtivacao">Chave de ativação do jogador utilizada no cabeçalho X-Jogador-Ativacao.</param>
    /// <param name="partidaId">Identificador único (UUID) da partida que será iniciada.</param>
    /// <param name="versaoEsperada">Versão atual esperada do estado da partida para controle de concorrência.</param>
    /// <param name="chaveIdempotencia">Chave única (UUID) para garantir a idempotência da requisição contra duplicações.</param>
    /// <param name="jogadorId">Identificador único (UUID) do jogador responsável por iniciar a partida.</param>
    /// <returns>O objeto atualizado contendo o novo estado da partida após o início, ou nulo se esgotarem as tentativas.</returns>
    /// <exception cref="InvalidOperationException">Lançada quando ocorrem falhas persistentes na API ao tentar iniciar a partida.</exception>
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

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict && tentativa < maxTentativas)
            {
                var estadoAtual = await ConsultarEstadoPartidaAsync(tokenBearer, chaveAtivacao, partidaId, jogadorId, 0);
                if (estadoAtual != null)
                {
                    versaoEsperada = estadoAtual.VersaoEstado;
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